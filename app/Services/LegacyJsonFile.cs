using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace NetworkProfileManager.Services;

// Preserves the existing JSON arrays and filenames. Reads never delete or reset user data.
internal sealed class LegacyJsonFile
{
    private readonly Dictionary<string, string?> _revisions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action? _beforeReplace;
    internal LegacyJsonFile(Action? beforeReplace = null) => _beforeReplace = beforeReplace;
    internal List<T> Read<T>(string path)
    {
        var (items, revision) = ReadCore<T>(path);
        _revisions[path] = revision;
        return items;
    }
    private static (List<T>, string?) ReadCore<T>(string path)
    {
        Helpers.AppPaths.ValidateStoragePath(path);
        StorageSafety.CheckPath(System.IO.Path.GetDirectoryName(path)!);
        if (!File.Exists(path)) return (new(), null);
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length > 8 * 1024 * 1024) throw new InvalidDataException("Stored file is too large; preserve it and restore a backup.");
        using var buffer = new MemoryStream(); input.CopyTo(buffer);
        var bytes = buffer.ToArray();
        try
        {
            var json = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\ufeff');
            var items = JsonConvert.DeserializeObject<List<T>>(json) ?? throw new JsonSerializationException();
            if (items.Any(x => x == null)) throw new JsonSerializationException();
            return (items, Convert.ToHexString(SHA256.HashData(bytes)));
        }
        catch (Exception e) when (e is JsonException or DecoderFallbackException)
        { throw new InvalidDataException("Stored file is damaged. Its original and existing backups have been preserved.", e); }
    }
    internal void Save<T>(string path, List<T> items)
    {
        using var lease = Lease(path);
        var (_, actual) = ReadCore<T>(path); // Never overwrite an unreadable/corrupt original.
        _revisions.TryGetValue(path, out var expected);
        if (actual != expected) throw new IOException("Stored data changed. Refresh before saving again.");
        Write(path, items, actual);
    }
    internal void Update<T>(string path, Action<List<T>> change)
    {
        using var lease = Lease(path);
        var (items, revision) = ReadCore<T>(path);
        change(items);
        Write(path, items, revision);
    }
    private static FileStream Lease(string path)
    {
        Helpers.AppPaths.ValidateStoragePath(path);
        StorageSafety.CheckPath(System.IO.Path.GetDirectoryName(path)!);
        return new(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
    }
    private void Write<T>(string path, List<T> items, string? revision)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(items, Formatting.Indented));
        if (bytes.Length > 8 * 1024 * 1024) throw new InvalidDataException("Stored file would exceed the readable size limit.");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { output.Write(bytes); output.Flush(true); }
            _beforeReplace?.Invoke();
            // Atomic replace retains the exact replaced bytes, including a non-cooperating writer's version.
            if (revision != null) File.Replace(temporary, path, path + "." + Guid.NewGuid().ToString("N") + ".bak");
            else File.Move(temporary, path, overwrite: false);
            _revisions[path] = Convert.ToHexString(SHA256.HashData(bytes));
        }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }
}
