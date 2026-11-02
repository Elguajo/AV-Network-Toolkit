using System;
using System.Collections.Generic;
using System.IO;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Services;

// Additive private UI preference. Old versions ignore it; no existing settings/profile file is rewritten.
internal sealed class WindowLayoutStore
{
    private readonly string _path;
    private readonly LegacyJsonFile _file = new();
    private WindowLayoutState? _last;
    private bool _loaded, _blocked;
    internal bool HasReadError { get; private set; }
    internal WindowLayoutStore() : this(Path.Combine(AppPaths.DataDirectory, "window_layout.json")) { }
    internal WindowLayoutStore(string path) => _path = path;

    internal WindowLayoutState? Load()
    {
        _loaded = true;
        try
        {
            if (File.Exists(_path) && new FileInfo(_path).Length > 65536) throw new InvalidDataException();
            var records = _file.Read<WindowLayoutState>(_path);
            if (records.Count == 0 && !File.Exists(_path)) return null;
            if (records.Count != 1 || !records[0].IsValid) throw new InvalidDataException();
            return _last = records[0];
        }
        catch { HasReadError = _blocked = true; return null; }
    }

    internal bool TrySave(WindowLayoutState state)
    {
        if (!_loaded) Load();
        if (_blocked || !state.IsValid) return false;
        if (state == _last) return true;
        try
        {
            Helpers.AppPaths.ValidateStoragePath(_path);
            _file.Save(_path, new List<WindowLayoutState> { state });
            _last = state;
            return true;
        }
        catch { return false; } // Atomic store retains original/backup on IO error or revision conflict.
    }
}
