using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services
{
    public class HistoryRepository
    {
        private const int MaxEntries = 100;
        private readonly LegacyJsonFile _file = new();
        private string DataDir => AppPaths.DataDirectory;

        public List<HistoryEntry> Load(string adapterId)
        {
            return _file.Read<HistoryEntry>(FilePath(adapterId));
        }

        public void Append(string adapterId, HistoryEntry entry)
        {
            _file.Update<HistoryEntry>(FilePath(adapterId), list =>
            {
                list.Insert(0, entry);
                if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
            });
        }

        public void Clear(string adapterId)
        {
            try
            {
                var file = FilePath(adapterId);
                if (File.Exists(file)) File.Delete(file);
            }
            catch { }
        }

        private string FilePath(string adapterId)
            => Path.Combine(DataDir, $"history_{Sanitize(adapterId)}.json");

        private static string Sanitize(string id)
            => Regex.Replace(id, @"[^\w\-]", "_");

    }
}
