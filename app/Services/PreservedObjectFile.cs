using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Services;

// Existing object-shaped preferences: preserve shape/unknown keys, damaged originals and stale revisions.
internal sealed class PreservedObjectFile(string path)
{
    private string? _revision;
    private bool _loaded, _blocked;
    internal JObject Load()
    {
        AppPaths.ValidateStoragePath(path);
        _loaded = true;
        try
        {
            if (StorageSafety.Attributes(path) == null) { _revision = null; return new(); }
            var bytes = StorageSafety.ReadBounded(path, 8 * 1024 * 1024);
            var value = StorageSafety.Object(bytes);
            _revision = StorageSafety.Hash(bytes); return value;
        }
        catch { _blocked = true; throw; }
    }
    internal void Save(JObject value)
    {
        AppPaths.ValidateStoragePath(path);
        if (_blocked) throw new IOException("Storage.Damaged");
        if (!_loaded) Load();
        StorageSafety.CheckPath(Path.GetDirectoryName(path)!);
        using var lease = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        var actual = StorageSafety.Attributes(path) == null ? null : StorageSafety.HashFile(path);
        if (actual != _revision) throw new IOException("Storage.SourceChanged");
        if (actual != null) _ = StorageSafety.Object(StorageSafety.ReadBounded(path, 8 * 1024 * 1024));
        var bytes = Encoding.UTF8.GetBytes(value.ToString(Formatting.Indented));
        if (bytes.Length > 8 * 1024 * 1024) throw new InvalidDataException("Storage.Damaged");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            StorageSafety.WriteNew(temporary, bytes);
            if (actual != null) File.Replace(temporary, path, path + "." + Guid.NewGuid().ToString("N") + ".bak");
            else File.Move(temporary, path, overwrite: false);
            _revision = StorageSafety.Hash(bytes);
        }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }
    internal static JObject ParseScanner(byte[] bytes)
    {
        var obj = StorageSafety.Object(bytes);
        foreach (var key in new[] { "StartIp", "EndIp" })
            if (obj.TryGetValue(key, out var v) && v.Type != JTokenType.String) throw new InvalidDataException("Storage.Damaged");
        foreach (var key in new[] { "TimeoutMs", "MaxParallel", "MonitorIntervalMinutes" })
            if (obj.TryGetValue(key, out var v) && (v.Type != JTokenType.Integer || !int.TryParse(v.ToString(), out _))) throw new InvalidDataException("Storage.Damaged");
        foreach (var key in new[] { "UseArp", "UseTcpProbe" })
            if (obj.TryGetValue(key, out var v) && v.Type != JTokenType.Boolean) throw new InvalidDataException("Storage.Damaged");
        return obj;
    }
}
