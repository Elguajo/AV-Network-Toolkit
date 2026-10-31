using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public sealed record ProjectSnapshot(NetworkProject Data, string Path, string Revision, string? BackupPath = null);
internal enum ProjectWriteStage { Serialize, TempWrite, Flush, Backup, BackupFlush, Replace, AtomicReplace }

/// <summary>Local project IO only. No dependencies on AppPaths, legacy stores or OS network services.</summary>
public sealed class ProjectStore
{
    private readonly Action<ProjectWriteStage>? _beforeStage;
    public ProjectStore() { }
    internal ProjectStore(Action<ProjectWriteStage> beforeStage) => _beforeStage = beforeStage;

    public ProjectSnapshot Open(string path)
    {
        path = ValidatePath(path);
        var bytes = Read(path);
        string json;
        try { json = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { throw new ProjectValidationException("Json"); }
        if (json.Length > 0 && json[0] == '\ufeff') json = json.Substring(1);
        return new ProjectSnapshot(ProjectJson.Parse(json), path, Hash(bytes));
    }

    // Used before an explicit overwrite prompt; even a corrupt file may be backed up and replaced.
    public string? RevisionAt(string path)
    {
        path = ValidatePath(path);
        return File.Exists(path) ? Hash(Read(path)) : null;
    }

    /// <param name="expectedRevision">Null means CREATE ONLY. Existing targets require their
    /// exact observed revision plus the UI's explicit overwrite confirmation.</param>
    public ProjectSnapshot Save(NetworkProject data, string path, string? expectedRevision = null)
    {
        path = ValidatePath(path);
        _beforeStage?.Invoke(ProjectWriteStage.Serialize);
        var bytes = Encoding.UTF8.GetBytes(ProjectJson.Serialize(data));
        var directory = System.IO.Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException();
        var temporary = System.IO.Path.Combine(directory, ".avnet-" + Guid.NewGuid().ToString("N") + ".tmp");
        var leasePath = path + ".lock";
        ValidatePath(leasePath);
        // A cooperative cross-process save lease. DeleteOnClose also cleans it after process exit.
        using var lease = new FileStream(leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.None, 1, FileOptions.DeleteOnClose);
        string? backup = null;
        try
        {
            CheckRevision(path, expectedRevision);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                _beforeStage?.Invoke(ProjectWriteStage.TempWrite);
                stream.Write(bytes);
                _beforeStage?.Invoke(ProjectWriteStage.Flush);
                stream.Flush(flushToDisk: true);
            }
            if (expectedRevision != null)
            {
                backup = path + "." + Guid.NewGuid().ToString("N") + ".bak";
                _beforeStage?.Invoke(ProjectWriteStage.Backup);
                // Deny in-place writes/deletion while copying the exact previous bytes.
                using (var original = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (HashStream(original) != expectedRevision) throw new ProjectValidationException("Conflict");
                    original.Position = 0;
                    using var copy = new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    original.CopyTo(copy);
                    _beforeStage?.Invoke(ProjectWriteStage.BackupFlush);
                    copy.Flush(flushToDisk: true);
                }
                _beforeStage?.Invoke(ProjectWriteStage.Replace);
                CheckRevision(path, expectedRevision);
                // Keep a second race-recovery backup in the same atomic replacement operation.
                // A non-cooperating writer can replace the target after the revision check.
                // In that case its exact bytes survive here rather than being lost.
                var replacedBackup = backup + ".replaced.bak";
                _beforeStage?.Invoke(ProjectWriteStage.AtomicReplace);
                File.Replace(temporary, path, replacedBackup, ignoreMetadataErrors: false);
                if (Hash(Read(replacedBackup)) == expectedRevision)
                {
                    try { File.Delete(replacedBackup); } catch { /* redundant backup is safe to retain */ }
                }
                else
                {
                    // New file is saved, but conflicting external content is in replacedBackup.
                    throw new ProjectValidationException("ConcurrentReplace");
                }
            }
            else
            {
                _beforeStage?.Invoke(ProjectWriteStage.Replace);
                File.Move(temporary, path, overwrite: false);
            }
            return new ProjectSnapshot(data, path, Hash(bytes), backup);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
        }
    }

    private static void CheckRevision(string path, string? expected)
    {
        var actual = File.Exists(path) ? Hash(Read(path)) : null;
        if (actual != expected) throw new ProjectValidationException("Conflict");
    }

    private static byte[] Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > ProjectJson.MaxBytes) throw new ProjectValidationException("TooLarge");
        using var result = new MemoryStream();
        stream.CopyTo(result);
        return result.ToArray();
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string HashStream(Stream stream) => Convert.ToHexString(SHA256.HashData(stream));

    public static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.Path.IsPathFullyQualified(path)
            || path.StartsWith(@"\\", StringComparison.Ordinal) || path.Length < 4
            || path[1] != ':' || path.Substring(2).Contains(':') || path.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
            throw new ProjectValidationException("Path");
        // Check raw segments before GetFullPath can normalize aliases such as trailing dots.
        foreach (var part in path.Substring(3).Split(new[] { '\\', '/' }))
        {
            if (part.EndsWith('.') || part.EndsWith(' ') || part.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0
                || Regex.IsMatch(part, @"^(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³]|CONIN\$|CONOUT\$)(\.|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                throw new ProjectValidationException("Path");
        }
        var full = System.IO.Path.GetFullPath(path);
        if (string.IsNullOrWhiteSpace(System.IO.Path.GetFileName(full))) throw new ProjectValidationException("Path");
        var root = System.IO.Path.GetPathRoot(full)!;
        if (new DriveInfo(root).DriveType == DriveType.Network) throw new ProjectValidationException("Path");
        // Explicitly selected local files only; do not follow junction/symlink destinations.
        for (var current = full; !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
        {
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new ProjectValidationException("Path");
        }
        return full;
    }
}
