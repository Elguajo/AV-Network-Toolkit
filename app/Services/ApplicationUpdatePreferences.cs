using System;
using System.Collections.Generic;
using System.IO;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Services;

internal sealed record ApplicationUpdatePreference(int Version, bool AutomaticChecks, bool IncludePreview, DateTimeOffset? LastCheck);
internal sealed class ApplicationUpdatePreferences
{
    private readonly string _path;
    private readonly LegacyJsonFile _file = new();
    private bool _blocked;
    internal bool IsBlocked => _blocked;
    internal ApplicationUpdatePreference Current { get; private set; }
    internal ApplicationUpdatePreferences(string? path = null)
    {
        _path = path ?? Path.Combine(AppPaths.DataDirectory, "application_updates.json");
        Current = new(1, false, ApplicationUpdateTrust.BuildChannel == "Preview", null);
        try
        {
            if (File.Exists(_path) && new FileInfo(_path).Length > 65536) throw new InvalidDataException();
            var values = _file.Read<ApplicationUpdatePreference>(_path);
            if (!File.Exists(_path)) return;
            if (values.Count != 1 || values[0].Version != 1 || values[0].LastCheck > DateTimeOffset.UtcNow.AddDays(1))
                throw new InvalidDataException();
            Current = values[0];
        }
        catch { _blocked = true; }
    }
    internal bool Save(ApplicationUpdatePreference value)
    {
        if (_blocked || value.Version != 1) return false;
        try
        {
            AppPaths.ValidateStoragePath(_path);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            _file.Save(_path, new List<ApplicationUpdatePreference> { value }); Current = value; return true;
        }
        catch { _blocked = true; return false; }
    }
    internal bool IsDue(DateTimeOffset now) => !_blocked && Current.AutomaticChecks
        && (!Current.LastCheck.HasValue || now - Current.LastCheck.Value >= TimeSpan.FromHours(24));
}
