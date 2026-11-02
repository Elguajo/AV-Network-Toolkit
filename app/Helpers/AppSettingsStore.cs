using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Helpers;

/// <summary>Existing flat string dictionary; failed/corrupt reads never become an empty saved replacement.</summary>
public static class AppSettingsStore
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, PreservedObjectFile> Files = new(StringComparer.OrdinalIgnoreCase);
    private static PreservedObjectFile Store()
    {
        var path = Path.Combine(AppPaths.DataDirectory, "app_settings.json");
        if (!Files.TryGetValue(path, out var store)) Files.Add(path, store = new(path));
        return store;
    }
    private static JObject Load(PreservedObjectFile store)
    {
        var values = store.Load();
        foreach (var property in values.Properties())
            if (property.Value.Type is not (JTokenType.String or JTokenType.Null)) throw new InvalidDataException("Storage.Damaged");
        return values;
    }
    public static string? Get(string key)
    {
        lock (Gate)
        {
            try { return (string?)Load(Store())[key]; }
            catch { StorageFeedback.Report(); return null; }
        }
    }
    public static void Set(string key, string value) => TrySet(key, value);
    internal static bool TrySet(string key, string value)
    {
        lock (Gate)
        {
            try
            {
                var store = Store(); var values = Load(store); values[key] = value; store.Save(values); return true;
            }
            catch { StorageFeedback.Report(); return false; }
        }
    }
}
