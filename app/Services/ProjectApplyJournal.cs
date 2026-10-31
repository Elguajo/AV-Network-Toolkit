using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

internal sealed class ProjectApplyJournal : IProjectApplyJournal
{
    private readonly string _directory;
    internal ProjectApplyJournal(string? directory = null) => _directory = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetworkProfileManager", "apply-recovery");
    internal string DirectoryPath => _directory;
    private string PathFor(Guid id, string suffix)
    {
        if (id == Guid.Empty) throw new InvalidDataException("Invalid operation.");
        var path = ProjectStore.ValidatePath(Path.Combine(_directory, id.ToString("N") + suffix));
        Directory.CreateDirectory(_directory);
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User ?? throw new IOException("Identity unavailable.");
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);
        security.SetOwner(sid);
        security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(_directory).SetAccessControl(security);
        return ProjectStore.ValidatePath(path);
    }
    public Task BeginAsync(ProjectApplyRequest request, CancellationToken cancellation)
    {
        ProjectApplyWire.ValidateRequest(request, DateTimeOffset.UtcNow);
        cancellation.ThrowIfCancellationRequested();
        WriteNew(PathFor(request.OperationId, ".request.json"), request);
        return Task.CompletedTask;
    }
    public Task FinishAsync(Guid operationId, ProjectApplyOutcome outcome)
    {
        WriteNew(PathFor(operationId, ".outcome.json"), outcome);
        return Task.CompletedTask;
    }
    private static void WriteNew<T>(string path, T value)
    {
        var bytes = Encoding.UTF8.GetBytes(ProjectApplyWire.Serialize(value));
        if (bytes.Length > ProjectApplyWire.MaxBytes) throw new InvalidDataException("Snapshot too large.");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        stream.Write(bytes);
        stream.Flush(true);
    }
    // Explicit read-only recovery action. Never called by App startup.
    internal IReadOnlyList<ProjectApplyRequest> ReadSnapshots()
    {
        if (!Directory.Exists(_directory)) return Array.Empty<ProjectApplyRequest>();
        _ = ProjectStore.ValidatePath(Path.Combine(_directory, "inspection.json"));
        var requests = new List<ProjectApplyRequest>();
        var files = Directory.EnumerateFiles(_directory, "*.request.json").Take(1001).ToArray();
        if (files.Length > 1000) throw new InvalidDataException("Too many snapshots; preserve and manage the recovery folder.");
        foreach (var file in files)
        {
            _ = ProjectStore.ValidatePath(file);
            if (new FileInfo(file).Length > ProjectApplyWire.MaxBytes) throw new InvalidDataException("Snapshot too large.");
            var request = ProjectApplyWire.Parse<ProjectApplyRequest>(File.ReadAllText(file, new UTF8Encoding(false, true)));
            // An expired journal is evidence to review, never a replayable worker request.
            if (request.Version != 1 || Path.GetFileName(file) != request.OperationId.ToString("N") + ".request.json")
                throw new InvalidDataException("Invalid journal.");
            ProjectApplyPolicy.ValidateTransition(request.Before, request.Desired, request.Recovery);
            requests.Add(request);
        }
        return requests.OrderByDescending(r => r.ExpiresAt).ToList();
    }
}
