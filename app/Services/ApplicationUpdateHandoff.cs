using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace NetworkProfileManager.Services;

// A copied instance of our packaged executable verifies/locks the installer and waits for real
// parent exit. It never starts ordinary App services, mutates networks or trusts a downloaded helper.
internal static class ApplicationUpdateHandoff
{
    internal const string WorkerSwitch = "--install-application-update";
    internal static async Task PrepareAsync(DownloadedApplicationUpdate update, CancellationToken cancellation)
    {
        if (!update.Installed || ApplicationUpdateTrust.PublicKey == null) throw new InvalidOperationException("Installed update unavailable.");
        var manifest = ApplicationUpdateTrust.Verify(update.Offer.ManifestBytes, update.Offer.Signature, ApplicationUpdateTrust.PublicKey);
        using (ApplicationUpdateService.OpenVerified(update.PackagePath, manifest.Installer)) { }
        var workerDirectory = Path.Combine(update.Directory, "worker");
        ApplicationUpdateService.EnsureNoReparse(workerDirectory); Directory.CreateDirectory(workerDirectory);
        var sourceDirectory = AppContext.BaseDirectory;
        using var workerPins = new WorkerPins();
        foreach (var name in ApplicationUpdateTrust.PayloadNames)
        {
            cancellation.ThrowIfCancellationRequested();
            var source = Path.Combine(sourceDirectory, name); ApplicationUpdateService.EnsureNoReparse(source);
            using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            var target = Path.Combine(workerDirectory, name); ApplicationUpdateService.EnsureNoReparse(target);
            if (File.Exists(target))
            {
                var existing = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read);
                workerPins.Add(existing);
                if (!SHA256.HashData(input).AsSpan().SequenceEqual(SHA256.HashData(existing)))
                    throw new IOException("Cached verifier changed. Check for updates and download a fresh copy.");
                continue;
            }
            using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { input.CopyTo(output); output.Flush(true); }
            var pin = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read); workerPins.Add(pin);
            input.Position = 0;
            if (!SHA256.HashData(input).AsSpan().SequenceEqual(SHA256.HashData(pin))) throw new IOException("Staged verifier checksum mismatch.");
        }
        using var identity = WindowsIdentity.GetCurrent();
        var pipeName = "AVNetworkToolkit_Update_" + Guid.NewGuid().ToString("N");
        using var pipe = NamedPipeServerStreamAcl.Create(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.FirstPipeInstance, 4096, 4096,
            ProjectApplyElevation.CreatePipeSecurity(identity.User ?? throw new IOException("User identity unavailable.")));
        using var parent = Process.GetCurrentProcess();
        var start = new ProcessStartInfo(Path.Combine(workerDirectory, "NetworkProfileManager.exe"))
        { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = workerDirectory };
        foreach (var arg in new[] { WorkerSwitch, pipeName, parent.Id.ToString(CultureInfo.InvariantCulture),
                     parent.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture) }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Update handoff could not start.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var connection = pipe.WaitForConnectionAsync(timeout.Token);
        var exit = process.WaitForExitAsync(timeout.Token);
        if (await Task.WhenAny(connection, exit) != connection) throw new IOException("Update verifier exited.");
        await connection;
        if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var client) || client != process.Id)
            throw new IOException("Update handoff peer rejected.");
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
        if (await reader.ReadLineAsync(timeout.Token) != "ready") throw new IOException("Update verification failed.");
        cancellation.ThrowIfCancellationRequested();
        // After commit is sent, finish the acknowledgment/exit sequence rather than accepting a
        // late cancellation that could leave an authorized helper waiting behind a live app.
        using var commitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await writer.WriteLineAsync("install".AsMemory(), commitTimeout.Token);
        if (await reader.ReadLineAsync(commitTimeout.Token) != "committed") throw new IOException("Update handoff was not acknowledged.");
        timeout.Cancel();
    }
    internal static async Task<int> RunAsync(string[] args)
    {
        string? job = null;
        try
        {
            if (args.Length != 4 || args[0] != WorkerSwitch || !args[1].StartsWith("AVNetworkToolkit_Update_", StringComparison.Ordinal)
                || args[1].Length > 100 || !int.TryParse(args[2], out var parentId) || !long.TryParse(args[3], out var ticks)) return 2;
            var worker = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
            job = Path.GetDirectoryName(worker)!;
            if (Path.GetFileName(worker) != "worker" || !Guid.TryParseExact(Path.GetFileName(job), "N", out _)
                || !Path.GetDirectoryName(job)!.Equals(Path.GetFullPath(ApplicationUpdateService.CacheRoot), StringComparison.OrdinalIgnoreCase)) return 2;
            ApplicationUpdateService.EnsureNoReparse(job);
            var leasePath = Path.Combine(job, "handoff.lock"); ApplicationUpdateService.EnsureNoReparse(leasePath);
            using var jobLease = new FileStream(leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var parent = Process.GetProcessById(parentId);
            if (parent.StartTime.ToUniversalTime().Ticks != ticks || parent.HasExited || parent.MainModule?.FileName is not string original
                || !ApplicationUpdateService.IsInstalled(original)) return 2;
            using (var input = new FileStream(original, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var self = new FileStream(Environment.ProcessPath!, FileMode.Open, FileAccess.Read, FileShare.Read))
                if (!SHA256.HashData(input).AsSpan().SequenceEqual(SHA256.HashData(self))) return 2;
            var key = ApplicationUpdateTrust.PublicKey ?? throw new InvalidDataException("Update key unavailable.");
            var manifestPath = Path.Combine(job, "update-manifest.json");
            var signaturePath = Path.Combine(job, "update-manifest.sig");
            if (new FileInfo(manifestPath).Length > 65536 || new FileInfo(signaturePath).Length != 384) return 2;
            var manifest = ApplicationUpdateTrust.Verify(File.ReadAllBytes(manifestPath), File.ReadAllBytes(signaturePath), key);
            if (!ApplicationUpdateTrust.IsNewer(manifest.Version, ApplicationUpdateTrust.CurrentVersion)
                || Environment.OSVersion.Version.Build < manifest.MinimumWindowsBuild || !Environment.Is64BitProcess) return 2;
            using var package = ApplicationUpdateService.OpenVerified(Path.Combine(job, manifest.Installer.Name), manifest.Installer);
            using var pipe = new NamedPipeClientStream(".", args[1], PipeDirection.InOut, PipeOptions.Asynchronous);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await pipe.ConnectAsync(timeout.Token);
            if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var server) || server != parentId) return 2;
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
            await writer.WriteLineAsync("ready".AsMemory(), timeout.Token);
            if (await reader.ReadLineAsync(timeout.Token) != "install") return 2;
            await writer.WriteLineAsync("committed".AsMemory(), timeout.Token);
            using var exitTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await parent.WaitForExitAsync(exitTimeout.Token);
            // Keep the verified file read-locked across UAC/bootstrap/installation. No shell command string.
            var start = new ProcessStartInfo(Path.Combine(job, manifest.Installer.Name)) { UseShellExecute = true };
            start.ArgumentList.Add("/NORESTART");
            start.ArgumentList.Add("/DIR=" + Path.GetDirectoryName(original));
            using var installer = Process.Start(start) ?? throw new IOException("Installer did not start.");
            await installer.WaitForExitAsync();
            File.WriteAllText(Path.Combine(job, "install-result.txt"), installer.ExitCode == 0 ? "Installer finished." : "Installer canceled or failed. Start the existing application to retry.");
            return installer.ExitCode == 0 ? 0 : 1;
        }
        catch
        {
            if (job != null)
                try { File.WriteAllText(Path.Combine(job, "install-result.txt"), "Update not installed. Start the existing application and retry after exiting other copies."); } catch { }
            System.Windows.MessageBox.Show("The update was not installed. Start the existing AV Network Toolkit application and try again. No user data was removed.", "Application updates");
            return 1;
        }
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint processId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint processId);
    private sealed class WorkerPins : IDisposable
    {
        private readonly List<FileStream> _files = new();
        internal void Add(FileStream file) => _files.Add(file);
        public void Dispose() { foreach (var file in _files) file.Dispose(); }
    }
}
