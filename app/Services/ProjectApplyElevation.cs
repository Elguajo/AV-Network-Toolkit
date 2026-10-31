using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

internal sealed record ApplyWorkerMessage(string Kind, string Stage, ApplyExecutionResult? Result = null);

internal static class ProjectApplyElevation
{
    internal const string WorkerSwitch = "--project-apply";

    internal static async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken cancellation)
    {
        ProjectApplyWire.ValidateRequest(request, DateTimeOffset.UtcNow);
        cancellation.ThrowIfCancellationRequested();
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User ?? throw new IOException("Identity unavailable.");
        var security = CreatePipeSecurity(sid);
        var name = "AVNetworkToolkit_" + Guid.NewGuid().ToString("N");
        using var pipe = NamedPipeServerStreamAcl.Create(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.FirstPipeInstance, 4096, 4096, security);
        var psi = CreateStartInfo(Environment.ProcessPath ?? throw new IOException("Executable unavailable."), name, Environment.ProcessId);
        Process process;
        try { process = await Task.Run(() => Process.Start(psi) ?? throw new IOException("Elevation unavailable.")); }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        { return new(ApplyExecutionStatus.ElevationDenied, false, "UacDenied"); }
        using (process)
        using (var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3)))
        {
            try
            {
                var connection = pipe.WaitForConnectionAsync(timeout.Token);
                var exit = process.WaitForExitAsync(timeout.Token);
                if (await Task.WhenAny(connection, exit) != connection) throw new IOException("Worker exited.");
                await connection;
                if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var client) || client != process.Id
                    || process.HasExited || !SameExecutable(process)) throw new IOException("Peer rejected.");
                await ProjectApplyWire.WriteAsync(pipe, request, timeout.Token);
                while (true)
                {
                    var message = await ProjectApplyWire.ReadAsync<ApplyWorkerMessage>(pipe, timeout.Token);
                    if (message.Kind == "result" && message.Result != null)
                    {
                        await process.WaitForExitAsync(timeout.Token);
                        if (process.ExitCode != 0 || !ValidResult(message.Result)) throw new IOException("Worker rejected.");
                        return message.Result;
                    }
                    if (message.Kind != "ready" || message.Stage is not ("Address" or "DnsPrimary" or "DnsSecondary"))
                        throw new IOException("Protocol rejected.");
                    await ProjectApplyWire.WriteAsync(pipe, !cancellation.IsCancellationRequested, timeout.Token);
                }
            }
            catch
            {
                // Disconnect removes permission for the next command. Wait for an
                // in-flight bounded command before the caller reads actual state.
                pipe.Dispose();
                using var grace = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                try { await process.WaitForExitAsync(grace.Token); } catch { }
                if (!process.HasExited)
                {
                    NetworkMutationLease.Inhibit();
                    return new(ApplyExecutionStatus.Unknown, true, "WorkerStillRunning");
                }
                return new(ApplyExecutionStatus.Unknown, true, "WorkerTransport");
            }
        }
    }

    internal static PipeSecurity CreatePipeSecurity(SecurityIdentifier sid)
    {
        var security = new PipeSecurity();
        security.SetAccessRuleProtection(true, false);
        security.SetOwner(sid);
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        security.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.FullControl, AccessControlType.Allow));
        return security;
    }
    internal static ProcessStartInfo CreateStartInfo(string executable, string name, int parentId)
    {
        var psi = new ProcessStartInfo(executable)
        { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
        psi.ArgumentList.Add(WorkerSwitch);
        psi.ArgumentList.Add(name);
        psi.ArgumentList.Add(parentId.ToString(CultureInfo.InvariantCulture));
        return psi;
    }
    internal static bool ValidResult(ApplyExecutionResult result) =>
        result.Status is ApplyExecutionStatus.Finished or ApplyExecutionStatus.Failed or ApplyExecutionStatus.Cancelled
        && result.Stage is "Validation" or "OperationActive" or "PreStateChanged" or "AdapterChanged" or "Address" or "DnsPrimary" or "DnsSecondary" or "CommandsFinished"
        && (result.Status != ApplyExecutionStatus.Finished || result.MayHaveChanged && result.Stage == "CommandsFinished");

    internal static async Task<int> RunWorkerAsync(string name, string parentPid)
    {
        if (!ElevationHelper.IsElevated || !name.StartsWith("AVNetworkToolkit_", StringComparison.Ordinal)
            || !Guid.TryParseExact(name.Substring("AVNetworkToolkit_".Length), "N", out _)
            || !int.TryParse(parentPid, NumberStyles.None, CultureInfo.InvariantCulture, out var parentId) || parentId <= 0) return 1;
        try
        {
            using var parent = Process.GetProcessById(parentId);
            if (parent.HasExited || !SameExecutable(parent)) return 1;
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            using var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous,
                TokenImpersonationLevel.Identification);
            await pipe.ConnectAsync(30_000, timeout.Token);
            if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var server) || server != parentId || parent.HasExited) return 1;
            var request = await ProjectApplyWire.ReadAsync<ProjectApplyRequest>(pipe, timeout.Token);
            var reader = new WindowsProjectApplyObservation();
            var result = await ProjectApplyWorker.ExecuteAsync(request, reader.ReadAsync, RunNetshAsync, async stage =>
            {
                await ProjectApplyWire.WriteAsync(pipe, new ApplyWorkerMessage("ready", stage), timeout.Token);
                return await ProjectApplyWire.ReadAsync<bool>(pipe, timeout.Token);
            });
            await ProjectApplyWire.WriteAsync(pipe, new ApplyWorkerMessage("result", "Finished", result), timeout.Token);
            return 0;
        }
        catch { return 1; }
    }

    private static bool SameExecutable(Process other)
    {
        try { return string.Equals(other.MainModule?.FileName, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }
    private static async Task RunNetshAsync(System.Collections.Generic.IReadOnlyList<string> args, CancellationToken cancellation)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "netsh.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi) ?? throw new IOException("Command unavailable.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(stdout, stderr);
            if (process.ExitCode != 0) throw new IOException("Command failed.");
        }
        catch { try { process.Kill(true); await process.WaitForExitAsync(); } catch { } throw; }
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint processId);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint processId);
}

internal sealed class WindowsProjectApplyBoundary : IProjectApplyBoundary
{
    private readonly WindowsProjectApplyObservation _observation = new();
    public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity identity, CancellationToken cancellation) => _observation.ReadAsync(identity, cancellation);
    public void ValidateSupported(ApplyAdapterSnapshot snapshot, ApplyIntent desired, bool recovery)
    {
        ProjectApplyPolicy.ValidateSnapshot(snapshot, allowTransient: recovery);
        if (desired.Mode == "static" && desired.DnsMode == "leave-unchanged" && snapshot.DnsSource == "automatic")
            throw new InvalidOperationException("Static automatic DNS unsupported.");
    }
    public Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken cancellation) => ProjectApplyElevation.ExecuteAsync(request, cancellation);
}
