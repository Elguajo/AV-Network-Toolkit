using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace NetworkProfileManager.Helpers
{
    /// <summary>
    /// Owns the application's current language. Loads a saved preference,
    /// uses English as the supported UI language, and swaps the merged string
    /// ResourceDictionary (Strings/En.xaml).
    /// </summary>
    public static class LanguageManager
    {
        public const string SystemDefault = "System";
        public const string English       = "en";

        private const string LanguageKey = "Language";

        /// <summary>The user's selected preference (may be "System").</summary>
        public static string CurrentPreference { get; private set; } = SystemDefault;

        /// <summary>The actual supported UI culture: English.</summary>
        public static string EffectiveLanguage { get; private set; } = English;

        /// <summary>Fired after a successful language swap.</summary>
        public static event Action? LanguageChanged;

        /// <summary>All preferences, including legacy tr and unsupported OS cultures,
        /// resolve to English. No OS language lookup or settings write is needed.</summary>
        public static string ResolveEffective(string preference) => English;

        internal static string NormalizePreference(string preference) => preference switch
        {
            English or SystemDefault => preference,
            "tr" => English, // Compatibility with the retired UI language; read-only at startup.
            _ => SystemDefault
        };

        /// <summary>Call once at startup, before any window opens.</summary>
        public static void Initialize()
        {
            CurrentPreference = LoadSavedPreference() ?? SystemDefault;
            ApplyPreference(CurrentPreference, persist: false);
        }

        /// <summary>Switch language at runtime. Persists the choice.</summary>
        public static void ApplyPreference(string preference, bool persist = true)
        {
            preference = NormalizePreference(preference);

            CurrentPreference = preference;
            var effective = ResolveEffective(preference);
            EffectiveLanguage = effective;

            // Set thread culture so DateTime formatting, etc. respect user choice
            var ci = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;

            SwapStringsDictionary(effective);

            if (persist) SavePreference(preference);

            LanguageChanged?.Invoke();
        }

        private static void SwapStringsDictionary(string effective)
        {
            if (Application.Current is null) return;

            var newUri = new Uri(
                "pack://application:,,,/NetworkProfileManager;component/Strings/En.xaml",
                UriKind.Absolute);
            var newDict = new ResourceDictionary { Source = newUri };

            var dicts = Application.Current.Resources.MergedDictionaries;

            // Remove any prior Strings/*.xaml dict
            for (int i = dicts.Count - 1; i >= 0; i--)
            {
                if (dicts[i].Source is not null
                    && dicts[i].Source!.OriginalString.IndexOf("/Strings/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dicts.RemoveAt(i);
                }
            }

            dicts.Add(newDict);
        }

        // ── Persistence ────────────────────────────────────────────

        private static string? LoadSavedPreference() => AppSettingsStore.Get(LanguageKey);

        private static void SavePreference(string preference)
            => AppSettingsStore.Set(LanguageKey, preference);
    }
}
