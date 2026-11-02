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
    internal ProjectApplyJournal(string? directory = null) => _directory = directory ?? Helpers.AppPaths.RecoveryDirectory;
    internal string DirectoryPath => _directory;
    internal const int PageSize = 1000;
    internal int UnreadableCount { get; private set; }
    internal bool HasMore { get; private set; }
    private string PathFor(Guid id, string suffix)
    {
        if (id == Guid.Empty) throw new InvalidDataException("Invalid operation.");
        Helpers.AppPaths.ValidateStoragePath(_directory);
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
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { stream.Write(bytes); stream.Flush(true); }
            File.Move(temporary, path, overwrite: false);
        }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }
    // Explicit read-only recovery action. Never called by App startup.
    internal IReadOnlyList<ProjectApplyRequest> ReadSnapshots() => ReadSnapshots(0);
    internal IReadOnlyList<ProjectApplyRequest> ReadSnapshots(int offset)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        UnreadableCount = 0; HasMore = false;
        if (!Directory.Exists(_directory)) return Array.Empty<ProjectApplyRequest>();
        _ = ProjectStore.ValidatePath(Path.Combine(_directory, "inspection.json"));
        var requests = new List<ProjectApplyRequest>();
        var files = Directory.EnumerateFiles(_directory, "*.request.json").Skip(offset).Take(PageSize + 1).ToArray();
        HasMore = files.Length > PageSize;
        foreach (var file in files.Take(PageSize))
        {
            try { requests.Add(ReadSnapshotFile(file)); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException or ProjectValidationException)
            { UnreadableCount++; } // Preserve bad evidence; it cannot hide independently valid entries.
        }
        return requests.OrderByDescending(r => r.ExpiresAt).ToList();
    }
    internal ProjectApplyRequest ReadSnapshot(Guid id) => id == Guid.Empty
        ? throw new InvalidDataException("Invalid journal.")
        : ReadSnapshotFile(Path.Combine(_directory, id.ToString("N") + ".request.json"));
    private static ProjectApplyRequest ReadSnapshotFile(string file)
    {
        _ = ProjectStore.ValidatePath(file);
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > ProjectApplyWire.MaxBytes) throw new InvalidDataException("Snapshot too large.");
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false);
        var request = ProjectApplyWire.Parse<ProjectApplyRequest>(reader.ReadToEnd());
        if (request.Version != 1 || Path.GetFileName(file) != request.OperationId.ToString("N") + ".request.json")
            throw new InvalidDataException("Invalid journal.");
        ProjectApplyPolicy.ValidateTransition(request.Before, request.Desired, request.Recovery);
        return request;
    }
}
