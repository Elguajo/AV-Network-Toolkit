using System.Globalization;
using System.IO;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public class LanguageManagerTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("tr")]
    [InlineData("System")]
    [InlineData("ru")]
    [InlineData("garbage")]
    [InlineData("")]
    public void UnsupportedAndLegacyChoicesResolveToEnglish(string preference)
        => Assert.Equal("en", LanguageManager.ResolveEffective(preference));

    [Theory]
    [InlineData("en", "en")]
    [InlineData("tr", "en")]
    [InlineData("System", "System")]
    [InlineData("ru", "System")]
    [InlineData("garbage", "System")]
    [InlineData("", "System")]
    public void PreferencesNormalizeWithoutInventingALanguage(string preference, string expected)
        => Assert.Equal(expected, LanguageManager.NormalizePreference(preference));

    [Theory]
    [InlineData("tr")]
    [InlineData("System")]
    public void InitializeIsReadOnlyAndEnglishOnUnsupportedOsCulture(string savedPreference)
    {
        using var directory = new TestDataDirectoryFixture();
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        var previousPreference = LanguageManager.CurrentPreference;
        try
        {
            AppSettingsStore.Set("Language", savedPreference);
            AppSettingsStore.Set("Theme", "Dark");
            var settings = Path.Combine(directory.Root, "app_settings.json");
            var bytes = File.ReadAllBytes(settings);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            LanguageManager.Initialize();
            LanguageManager.Initialize(); // Idempotent interpretation, no settings migration/write.
            Assert.Equal("en", LanguageManager.EffectiveLanguage);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
            Assert.Equal(bytes, File.ReadAllBytes(settings));
            Assert.Equal("Dark", AppSettingsStore.Get("Theme"));
        }
        finally
        {
            LanguageManager.ApplyPreference(previousPreference, persist: false);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = defaultUiCulture;
        }
    }

    [Fact]
    public void ConstantsExposeSupportedPreferences()
    {
        Assert.Equal("en", LanguageManager.English);
        Assert.Equal("System", LanguageManager.SystemDefault);
    }
}

[Collection("StaticState")]
public class LocTests
{
    [Fact]
    public void Get_ReturnsFallback_WhenApplicationNotRunning()
    {
        // Application.Current is null in xUnit; Loc must not blow up.
        var result = Loc.Get("Some.Missing.Key", "fallback-value");
        Assert.Equal("fallback-value", result);
    }

    [Fact]
    public void Get_ReturnsKey_WhenFallbackNull()
    {
        var result = Loc.Get("Some.Missing.Key");
        Assert.Equal("Some.Missing.Key", result);
    }

    [Fact]
    public void Format_AppliesArgumentsToFallback()
    {
        var result = Loc.Format("Some.Missing.Key.{0}", 42);
        // When key is missing the fallback IS the key string; if it has a {0} placeholder
        // string.Format will fill it
        Assert.Contains("42", result);
    }

    [Fact]
    public void Format_DoesNotThrow_OnMalformedFormatString()
    {
        // Fallback "{not-a-number}" has invalid placeholder syntax — Loc.Format swallows
        var ex = Record.Exception(() => Loc.Format("Some.Key.With.{garbage}"));
        Assert.Null(ex);
    }
}
