using System;
using System.IO;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Helpers
{
    /// <summary>
    /// One root selected by ordinary bootstrap before any settings or repositories load.
    /// Production never creates or falls back to a root on a getter call.
    /// Tests may override via <see cref="OverrideDataDirectory"/>.
    /// </summary>
    public static class AppPaths
    {
        private const string MigrationMarker = ".migrated_v1";

        private static string? _override;
        private static StorageRootContext? _active;
        internal static void ResetForTests() { _active = null; _override = null; }

        internal static void Initialize(StorageRootContext context)
        {
            context.EnsureAvailable();
            if (_active != null && _active.Root != context.Root) throw new InvalidOperationException("Storage root already selected.");
            _active = context;
        }
        internal static void EnsureAvailable()
        {
            if (_override != null)
            {
                if (!StorageSafety.DirectoryExists(_override)) throw new IOException("Storage.RootUnavailable");
                return;
            }
            (_active ?? throw new InvalidOperationException("Storage is not initialized.")).EnsureAvailable();
        }
        internal static string RecoveryDirectory
        {
            get
            {
                EnsureAvailable();
                return _override != null ? Path.Combine(_override, "apply-recovery") : _active!.Recovery;
            }
        }
        internal static void ValidateStoragePath(string path)
        {
            if (_active != null && _override == null && Path.GetFullPath(path).StartsWith(
                _active.Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) EnsureAvailable();
            else if (_override != null && Path.GetFullPath(path).StartsWith(_override + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)) EnsureAvailable();
        }

        public static string DataDirectory
        {
            get
            {
                EnsureAvailable();
                return _override ?? _active!.Data;
            }
        }

        /// <summary>Test hook — pass null to restore default.</summary>
        public static void OverrideDataDirectory(string? path)
        {
            if (path != null) Directory.CreateDirectory(path);
            _override = path;
        }

        /// <summary>
        /// One-shot migration: copy any profiles/history/app_settings from the legacy
        /// location (exe-relative) to LocalAppData if they aren't already there.
        /// Safe to call on every startup — writes a marker to skip subsequent runs.
        /// Old files are NOT deleted, they remain as a backup.
        /// Returns the number of files copied.
        /// </summary>
        public static int MigrateLegacyDataIfPresent() => MigrateLegacyDataIfPresent(AppDomain.CurrentDomain.BaseDirectory);

        internal static int MigrateLegacyDataIfPresent(string legacyBase)
        {
            try
            {
                var marker = Path.Combine(DataDirectory, MigrationMarker);
                if (File.Exists(marker)) return 0;

                int copied = 0;
                bool complete = true;

                // 1. profiles_*.json / history_*.json sit under <baseDir>/data/
                var legacyDataDir = Path.Combine(legacyBase, "data");
                if (Directory.Exists(legacyDataDir))
                {
                    foreach (var src in Directory.EnumerateFiles(legacyDataDir, "*.json"))
                    {
                        var name = Path.GetFileName(src);
                        var dst = Path.Combine(DataDirectory, name);
                        if (!File.Exists(dst))
                        {
                            try { CopyLegacy(src, dst); copied++; }
                            catch { complete = false; }
                        }
                    }
                }

                // 2. Old theme used to live as <baseDir>\app_settings.json (top-level)
                var legacyAppSettings = Path.Combine(legacyBase, "app_settings.json");
                if (File.Exists(legacyAppSettings))
                {
                    var dst = Path.Combine(DataDirectory, "app_settings.json");
                    if (!File.Exists(dst))
                    {
                        try { CopyLegacy(legacyAppSettings, dst); copied++; }
                        catch { complete = false; }
                    }
                }

                if (complete) File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
                return copied;
            }
            catch
            {
                return 0;
            }
        }

        private static void CopyLegacy(string source, string destination)
        {
            var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { input.CopyTo(output); output.Flush(true); }
                File.Move(temporary, destination, overwrite: false);
            }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
        }
    }
}
