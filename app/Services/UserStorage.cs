using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

internal sealed record UserStorageLocations(string Home, string Legacy, string Executable, string Backups)
{
    internal static UserStorageLocations ForUser(string profile, string local, string executable)
    {
        if (string.IsNullOrWhiteSpace(profile) || string.IsNullOrWhiteSpace(local))
            throw new IOException("Storage.ProfileUnavailable");
        return new(Path.Combine(profile, ".av-network-toolkit"), Path.Combine(local, "NetworkProfileManager"),
            executable, Path.Combine(profile, ".av-network-toolkit-backups"));
    }
}

internal sealed record StorageEntry(string Name, bool Directory, long Size, string Hash);
internal sealed record StorageRootRecord(int Version, string Product, Guid Transaction, string Source,
    string? LegacyExecutable, string Snapshot, DateTimeOffset Created, string LegacyDigest);
internal sealed record StorageReview(string Source, string Destination, string Backup, int Files, long Bytes,
    bool Diverged, int UnavailableRecovery);
internal enum StorageStep { Inventory, Snapshot, Copy, Flush, Validate, Metadata, Publish, Readback }

internal sealed class StorageRootContext(string root, Guid transaction)
{
    internal string Root { get; } = root;
    internal string Data => Path.Combine(Root, "data");
    internal string Recovery => Path.Combine(Root, "apply-recovery");
    internal void EnsureAvailable()
    {
        StorageSafety.CheckPath(Root);
        var record = UserStorage.ReadRecord(Root);
        if (record.Transaction != transaction || !StorageSafety.DirectoryExists(Data)
            || !StorageSafety.DirectoryExists(Recovery)) throw new IOException("Storage.RootUnavailable");
    }
}

internal static class StorageSafety
{
    // Do not use Exists to interpret access failures as an empty/missing store.
    internal static FileAttributes? Attributes(string path)
    {
        try { return File.GetAttributes(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }
    internal static bool DirectoryExists(string path)
    {
        var attributes = Attributes(path);
        if (attributes == null) return false;
        if ((attributes & FileAttributes.Directory) == 0) throw new IOException("Storage.PathConflict");
        return true;
    }
    internal static void CheckPath(string path)
    {
        _ = ProjectStore.ValidatePath(Path.Combine(path, "storage-inspection"));
        // ValidatePath's Exists checks are deliberately complemented with throwing attribute reads.
        for (string? current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            if (Attributes(current) is FileAttributes attributes && (attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Storage.UnsafePath");
    }
    internal static void Protect(string path)
    {
        CheckPath(path);
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User ?? throw new IOException("Storage.ProfileUnavailable");
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);
        security.SetOwner(sid);
        security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        var info = new DirectoryInfo(path);
        info.Create(security);
        info.SetAccessControl(security);
    }
    internal static byte[] ReadBounded(string path, int limit)
    {
        CheckPath(Path.GetDirectoryName(path)!);
        var attributes = Attributes(path) ?? throw new FileNotFoundException();
        if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Storage.UnsafePath");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > limit) throw new InvalidDataException("Storage.Damaged");
        using var buffer = new MemoryStream(); stream.CopyTo(buffer);
        if (buffer.Length > limit) throw new InvalidDataException("Storage.Damaged");
        return buffer.ToArray();
    }
    internal static string HashFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static JObject Object(byte[] bytes)
    {
        var text = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\ufeff');
        using var reader = new JsonTextReader(new StringReader(text)) { MaxDepth = 32, DateParseHandling = DateParseHandling.None };
        var result = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        if (reader.Read()) throw new InvalidDataException("Storage.Damaged");
        return result;
    }
    internal static void WriteNew(string path, byte[] bytes)
    {
        CheckPath(Path.GetDirectoryName(path)!);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes); stream.Flush(true);
    }
}

// All paths and dialogs are injectable. No NIC, registry, updater execution or global AppPaths reads here.
internal sealed class UserStorage(UserStorageLocations locations, Action<StorageStep>? before = null,
    Func<bool>? otherInstance = null, Action? beforeMigration = null)
{
    private const string Product = "AVNetworkToolkit";
    private readonly UserStorageLocations _locations = locations;
    private void Step(StorageStep step) => before?.Invoke(step);
    private static byte[] Json(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, Formatting.Indented));
    private static bool IsEvidence(string name) => name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".lock", StringComparison.OrdinalIgnoreCase);
    private static string Digest(List<StorageEntry> entries) => StorageSafety.Hash(Json(entries.Where(e => !e.Name.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))));

    internal StorageRootContext Open(Func<StorageReview, bool> consent)
    {
        ValidateLocations();
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User?.Value ?? throw new IOException("Storage.ProfileUnavailable");
        var key = StorageSafety.Hash(Encoding.UTF8.GetBytes(sid + "|" + _locations.Home.ToUpperInvariant()));
        using var gate = new Mutex(false, @"Local\AVNetworkToolkitStorage_" + key);
        bool acquired;
        try { acquired = gate.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) throw new IOException("Storage.Busy");
        try { return OpenLocked(consent); }
        finally { gate.ReleaseMutex(); }
    }

    private StorageRootContext OpenLocked(Func<StorageReview, bool> consent)
    {
        Step(StorageStep.Inventory);
        if (StorageSafety.Attributes(_locations.Home) != null)
        {
            var record = ReadRecord(_locations.Home);
            if (record.LegacyExecutable != null && (Inside(record.LegacyExecutable, _locations.Home)
                || Inside(_locations.Home, record.LegacyExecutable) || Inside(record.LegacyExecutable, _locations.Backups)
                || Inside(_locations.Backups, record.LegacyExecutable))) throw new IOException("Storage.UnsafePath");
            new StorageRootContext(_locations.Home, record.Transaction).EnsureAvailable();
            var current = Inventory(record.LegacyExecutable);
            if (Digest(current) != record.LegacyDigest)
            {
                beforeMigration?.Invoke();
                if (otherInstance?.Invoke() == true) throw new IOException("Storage.Busy");
                var id = Guid.NewGuid();
                var backup = Path.Combine(_locations.Backups, id.ToString("N"));
                var sources = _locations.Legacy + (record.LegacyExecutable == null ? "" : "\n" + record.LegacyExecutable);
                if (!consent(new(sources, _locations.Home, backup, current.Count(e => !e.Directory),
                    current.Sum(e => e.Size), true, 0))) throw new OperationCanceledException();
                var backupManifest = Snapshot(current, record.LegacyExecutable, backup);
                if (Digest(Inventory(record.LegacyExecutable)) != Digest(current)) throw new IOException("Storage.SourceChanged");
                Step(StorageStep.Metadata);
                VerifySnapshot(current, backup, backupManifest);
                if (Digest(Inventory(record.LegacyExecutable)) != Digest(current)) throw new IOException("Storage.SourceChanged");
                var path = Path.Combine(_locations.Home, "storage-root.json");
                var temporary = path + "." + id.ToString("N") + ".tmp";
                var original = StorageSafety.ReadBounded(path, 65536);
                StorageSafety.WriteNew(temporary, Json(record with { LegacyDigest = Digest(current), Snapshot = id.ToString("N") }));
                if (ReadRecord(_locations.Home) != record || StorageSafety.HashFile(path) != StorageSafety.Hash(original)) throw new IOException("Storage.SourceChanged");
                File.Replace(temporary, path, path + "." + id.ToString("N") + ".bak");
            }
            var existing = new StorageRootContext(_locations.Home, record.Transaction);
            existing.EnsureAvailable(); return existing;
        }

        beforeMigration?.Invoke();
        if (otherInstance?.Invoke() == true) throw new IOException("Storage.Busy");
        var local = Inventory(null);
        var exe = Inventory(_locations.Executable).Where(e => e.Name.StartsWith("exe/", StringComparison.Ordinal)).ToList();
        bool hasLocal = local.Any(e => !e.Directory && !e.Name.Equals("local/data/.migrated_v1", StringComparison.OrdinalIgnoreCase) && !IsEvidence(e.Name));
        bool hasExe = exe.Any(e => !e.Directory && !IsEvidence(e.Name));
        // An unfinished old import cannot silently become a hybrid home dataset.
        if (hasLocal && hasExe && !local.Any(e => e.Name.Equals("local/data/.migrated_v1", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var entry in exe.Where(e => !e.Directory && e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                var destination = entry.Name == "exe/app_settings.json" ? "local/data/app_settings.json" : "local/" + entry.Name[4..];
                if (!local.Any(e => e.Name.Equals(destination, StringComparison.OrdinalIgnoreCase) && e.Hash == entry.Hash)) throw new IOException("Storage.LegacyConflict");
            }
        }
        string source = hasLocal ? "local" : hasExe ? "exe" : "empty";
        string? legacyExe = source == "exe" ? _locations.Executable : null;
        var inventory = Inventory(legacyExe);
        var transaction = Guid.NewGuid();
        var snapshot = Path.Combine(_locations.Backups, transaction.ToString("N"));
        if (source != "empty" && !consent(new(source == "exe" ? _locations.Executable : _locations.Legacy,
            _locations.Home, snapshot, inventory.Count(e => !e.Directory), inventory.Sum(e => e.Size), false,
            RecoveryUnavailable(inventory, legacyExe)))) throw new OperationCanceledException();
        var snapshotManifest = Snapshot(inventory, legacyExe, snapshot);
        var staging = _locations.Home + ".staging-" + transaction.ToString("N");
        StorageSafety.Protect(staging);
        StorageSafety.Protect(Path.Combine(staging, "data"));
        StorageSafety.Protect(Path.Combine(staging, "apply-recovery"));
        var installed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in inventory.Where(e => e.Directory))
        {
            var relative = ActiveName(entry.Name, source);
            if (relative != null) StorageSafety.Protect(Path.Combine(staging, relative));
        }
        foreach (var entry in inventory.Where(e => !e.Directory))
        {
            string? relative = ActiveName(entry.Name, source);
            if (relative == null || IsEvidence(relative)) continue;
            if (installed.TryGetValue(relative, out var previous))
            {
                if (previous != entry.Hash) throw new IOException("Storage.LegacyConflict");
                continue;
            }
            installed.Add(relative, entry.Hash);
            Copy(Path.Combine(snapshot, entry.Name), Path.Combine(staging, relative), entry);
        }
        Step(StorageStep.Validate);
        try
        {
            ValidateData(Path.Combine(staging, "data"));
            StorageSafety.WriteNew(Path.Combine(snapshot, "validation.json"), Json(new { Version = 1, ActiveFormats = "Valid", UnavailableRecovery = RecoveryUnavailable(inventory, legacyExe) }));
        }
        catch
        {
            StorageSafety.WriteNew(Path.Combine(snapshot, "validation.json"), Json(new { Version = 1, ActiveFormats = "Rejected" }));
            throw;
        }
        foreach (var entry in inventory.Where(e => !e.Directory && ActiveName(e.Name, source)?.StartsWith("apply-recovery/", StringComparison.Ordinal) == true))
            if (StorageSafety.HashFile(Path.Combine(staging, ActiveName(entry.Name, source)!)) != entry.Hash)
                throw new IOException("Storage.SourceChanged");
        if (Digest(Inventory(legacyExe)) != Digest(inventory)) throw new IOException("Storage.SourceChanged");
        Step(StorageStep.Metadata);
        var committedRecord = new StorageRootRecord(1, Product, transaction, source, legacyExe,
            transaction.ToString("N"), DateTimeOffset.UtcNow, Digest(inventory));
        StorageSafety.WriteNew(Path.Combine(staging, "storage-root.json"), Json(committedRecord));
        Step(StorageStep.Publish);
        StorageSafety.CheckPath(_locations.Home);
        if (Digest(Inventory(legacyExe)) != Digest(inventory)) throw new IOException("Storage.SourceChanged");
        VerifySnapshot(inventory, snapshot, snapshotManifest);
        if (ReadRecord(staging) != committedRecord) throw new IOException("Storage.SourceChanged");
        foreach (var entry in installed)
            if (StorageSafety.HashFile(Path.Combine(staging, entry.Key)) != entry.Value) throw new IOException("Storage.SourceChanged");
        Directory.Move(staging, _locations.Home); // Same-volume, no overwrite. Never copy/delete on publication failure.
        Step(StorageStep.Readback);
        var context = new StorageRootContext(_locations.Home, transaction); context.EnsureAvailable(); return context;
    }

    private void ValidateLocations()
    {
        var roots = new[] { _locations.Home, _locations.Legacy, _locations.Executable, _locations.Backups };
        foreach (var path in roots) StorageSafety.CheckPath(path);
        var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_locations.Home))!);
        if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase)) throw new IOException("Storage.UnsafePath");
        for (int i = 0; i < roots.Length; i++)
            for (int j = i + 1; j < roots.Length; j++)
                if (Inside(roots[i], roots[j]) || Inside(roots[j], roots[i])) throw new IOException("Storage.UnsafePath");
    }
    private static bool Inside(string path, string parent) => Path.GetFullPath(path).TrimEnd('\\').Equals(Path.GetFullPath(parent).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    internal static StorageRootRecord ReadRecord(string root)
    {
        try
        {
            var obj = StorageSafety.Object(StorageSafety.ReadBounded(Path.Combine(root, "storage-root.json"), 65536));
            var names = new[] { "Version", "Product", "Transaction", "Source", "LegacyExecutable", "Snapshot", "Created", "LegacyDigest" };
            if (obj.Properties().Any(p => !names.Contains(p.Name)) || names.Any(n => !obj.ContainsKey(n))) throw new InvalidDataException();
            if (obj["Version"]!.Type != JTokenType.Integer || names.Where(n => n is not ("Version" or "LegacyExecutable"))
                .Any(n => obj[n]!.Type != JTokenType.String) || obj["LegacyExecutable"]!.Type is not (JTokenType.String or JTokenType.Null))
                throw new InvalidDataException();
            var record = obj.ToObject<StorageRootRecord>() ?? throw new InvalidDataException();
            if (record.Version != 1 || record.Product != Product || record.Transaction == Guid.Empty
                || record.Source is not ("local" or "exe" or "empty") || !Guid.TryParseExact(record.Snapshot, "N", out _)
                || record.LegacyDigest == null || record.LegacyDigest.Length != 64 || !record.LegacyDigest.All(Uri.IsHexDigit)
                || (record.Source == "exe") != (record.LegacyExecutable != null)) throw new InvalidDataException();
            if (record.LegacyExecutable != null) StorageSafety.CheckPath(record.LegacyExecutable);
            return record;
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException)
        { throw new IOException("Storage.RootConflict"); }
    }

    private List<StorageEntry> Inventory(string? executable)
    {
        var result = new List<StorageEntry>();
        Scan(Path.Combine(_locations.Legacy, "data"), "local/data", result);
        Scan(Path.Combine(_locations.Legacy, "apply-recovery"), "local/apply-recovery", result);
        if (executable != null)
        {
            StorageSafety.CheckPath(executable);
            Scan(Path.Combine(executable, "data"), "exe/data", result);
            var top = Path.Combine(executable, "app_settings.json");
            if (StorageSafety.Attributes(top) != null) AddFile(top, "exe/app_settings.json", result);
        }
        if (result.Select(e => e.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Count)
            throw new IOException("Storage.PathConflict");
        return result.OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
    }
    private static void Scan(string path, string name, List<StorageEntry> result)
    {
        StorageSafety.CheckPath(path);
        if (!StorageSafety.DirectoryExists(path)) return;
        result.Add(new(name, true, 0, ""));
        foreach (var child in Directory.EnumerateFileSystemEntries(path))
        {
            var attributes = StorageSafety.Attributes(child) ?? throw new IOException("Storage.SourceChanged");
            if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Storage.UnsafePath");
            var relative = name + "/" + Path.GetFileName(child);
            if ((attributes & FileAttributes.Directory) != 0) Scan(child, relative, result);
            else AddFile(child, relative, result);
        }
    }
    private static void AddFile(string path, string name, List<StorageEntry> result)
    {
        StorageSafety.CheckPath(Path.GetDirectoryName(path)!);
        if (name.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
        { using var probe = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None); }
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        result.Add(new(name, false, stream.Length, Convert.ToHexString(SHA256.HashData(stream))));
    }
    private string SourcePath(string name, string? executable) => name.StartsWith("local/", StringComparison.Ordinal)
        ? Path.Combine(_locations.Legacy, name[6..]) : Path.Combine(executable ?? throw new IOException("Storage.Damaged"), name[4..]);
    private static string? ActiveName(string name, string source)
    {
        if (source == "local" && name.StartsWith("local/", StringComparison.Ordinal)) return name[6..];
        if (source == "exe" && name.StartsWith("exe/data/", StringComparison.Ordinal)) return name[4..];
        if (source == "exe" && name == "exe/app_settings.json") return "data/app_settings.json";
        return null;
    }
    private string Snapshot(List<StorageEntry> inventory, string? executable, string target)
    {
        Step(StorageStep.Snapshot);
        if (StorageSafety.Attributes(target) != null) throw new IOException("Storage.PathConflict");
        long needed = checked(inventory.Sum(e => e.Size) * 2 + 1024 * 1024);
        if (new DriveInfo(Path.GetPathRoot(target)!).AvailableFreeSpace < needed) throw new IOException("Storage.NoSpace");
        var handles = new List<FileStream>();
        try
        {
            // Retain source read leases through snapshot copying. Inventory checks still cover additions/deletions.
            foreach (var entry in inventory.Where(e => !e.Directory))
                handles.Add(new FileStream(SourcePath(entry.Name, executable), FileMode.Open, FileAccess.Read, FileShare.Read));
            StorageSafety.Protect(_locations.Backups); StorageSafety.Protect(target);
            foreach (var entry in inventory)
            {
                var path = Path.Combine(target, entry.Name);
                if (entry.Directory) StorageSafety.Protect(path);
                else Copy(SourcePath(entry.Name, executable), path, entry);
            }
            var absent = new[] { "local/data", "local/apply-recovery", "exe/data", "exe/app_settings.json" }
                .Where(name => !inventory.Any(e => e.Name == name)).ToArray();
            var manifest = Json(new { Version = 1, Inventory = inventory, AbsentStores = absent });
            StorageSafety.WriteNew(Path.Combine(target, "snapshot.json"), manifest);
            return StorageSafety.Hash(manifest);
        }
        finally { foreach (var handle in handles) handle.Dispose(); }
    }
    private static void VerifySnapshot(List<StorageEntry> inventory, string target, string manifestHash)
    {
        foreach (var entry in inventory.Where(e => !e.Directory))
        {
            var path = Path.Combine(target, entry.Name); StorageSafety.CheckPath(path);
            if (new FileInfo(path).Length != entry.Size || StorageSafety.HashFile(path) != entry.Hash)
                throw new IOException("Storage.SourceChanged");
        }
        var manifest = Path.Combine(target, "snapshot.json"); StorageSafety.CheckPath(manifest);
        if (StorageSafety.HashFile(manifest) != manifestHash) throw new IOException("Storage.SourceChanged");
    }
    private void Copy(string source, string destination, StorageEntry entry)
    {
        Step(StorageStep.Copy);
        StorageSafety.Protect(Path.GetDirectoryName(destination)!);
        using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { input.CopyTo(output); Step(StorageStep.Flush); output.Flush(true); }
        if (new FileInfo(destination).Length != entry.Size || StorageSafety.HashFile(destination) != entry.Hash)
            throw new IOException("Storage.SourceChanged");
    }
    private int RecoveryUnavailable(List<StorageEntry> inventory, string? executable)
    {
        var missing = 0;
        foreach (var entry in inventory.Where(e => !e.Directory && e.Name.StartsWith("local/data/history_", StringComparison.OrdinalIgnoreCase) && e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            List<HistoryEntry> history;
            try { history = new LegacyJsonFile().Read<HistoryEntry>(SourcePath(entry.Name, executable)); }
            catch (InvalidDataException) { continue; } // Snapshot malformed bytes before the later active-format rejection.
            foreach (var pointer in history.Where(h => h.RecoveryOperationId.HasValue))
                try { _ = new ProjectApplyJournal(Path.Combine(_locations.Legacy, "apply-recovery")).ReadSnapshot(pointer.RecoveryOperationId!.Value); }
                catch { missing++; }
        }
        return missing;
    }
    internal static void ValidateData(string directory)
    {
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileName(path).ToLowerInvariant();
            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.StartsWith("profiles_", StringComparison.OrdinalIgnoreCase)) _ = new LegacyJsonFile().Read<IpProfile>(path);
            else if (name.StartsWith("history_", StringComparison.OrdinalIgnoreCase)) _ = new LegacyJsonFile().Read<HistoryEntry>(path);
            else if (name == "app_settings.json")
            {
                var settings = StorageSafety.Object(StorageSafety.ReadBounded(path, 8 * 1024 * 1024));
                if (settings.Properties().Any(p => p.Value.Type is not (JTokenType.String or JTokenType.Null))) throw new InvalidDataException("Storage.Damaged");
            }
            else if (name == "scanner_settings.json")
                _ = PreservedObjectFile.ParseScanner(StorageSafety.ReadBounded(path, 8 * 1024 * 1024));
            else if (name == "window_layout.json")
            { if (new WindowLayoutStore(path).Load() == null) throw new InvalidDataException("Storage.Damaged"); }
            else if (name == "application_updates.json")
            {
                var array = JArray.Parse(new UTF8Encoding(false, true).GetString(StorageSafety.ReadBounded(path, 65536)).TrimStart('\ufeff'),
                    new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (array.Count != 1 || array[0] is not JObject preference || (int?)preference["Version"] != 1
                    || preference["AutomaticChecks"]?.Type != JTokenType.Boolean || preference["IncludePreview"]?.Type != JTokenType.Boolean
                    || (preference["LastCheck"]?.Type is not (null or JTokenType.Null)
                        && (!DateTimeOffset.TryParse((string?)preference["LastCheck"], out var lastCheck) || lastCheck > DateTimeOffset.UtcNow.AddDays(1))))
                    throw new InvalidDataException("Storage.Damaged");
            }
        }
    }
}
