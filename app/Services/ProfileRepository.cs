using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services
{
    public class ProfileRepository
    {
        private readonly LegacyJsonFile _file;
        public ProfileRepository() : this(null) { }
        internal ProfileRepository(Action? beforeReplace) => _file = new(beforeReplace);
        private string DataDir => AppPaths.DataDirectory;

        public List<IpProfile> Load(string adapterId)
        {
            return _file.Read<IpProfile>(FilePath(adapterId));
        }

        public void Save(string adapterId, List<IpProfile> profiles)
        {
            if (profiles.Count > 5)
                throw new InvalidOperationException(
                    Loc.Get("Error.MaxProfiles", "Maksimum 5 profil kaydedilebilir."));

            _file.Save(FilePath(adapterId), profiles);
        }

        private string FilePath(string adapterId)
            => Path.Combine(DataDir, $"profiles_{Sanitize(adapterId)}.json");

        private static string Sanitize(string id)
            => Regex.Replace(id, @"[^\w\-]", "_");

    }
}
