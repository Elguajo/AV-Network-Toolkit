using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace NetworkProfileManager.Tests;

/// <summary>
/// A missing string key does not fail the build: XAML shows an empty label and
/// These tests read the source files so missing labels are caught before display.
/// </summary>
public class StringResourceTests
{
    private static readonly string AppDir = Path.Combine(FindRepoRoot(), "app");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AVNetworkToolkit.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("AVNetworkToolkit.sln not found");
    }

    private static HashSet<string> Keys(string language)
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var doc = XDocument.Load(Path.Combine(AppDir, "Strings", language + ".xaml"));
        return doc.Root!.Elements()
                  .Select(e => (string?)e.Attribute(x + "Key"))
                  .Where(k => k != null)
                  .ToHashSet()!;
    }

    [Fact]
    public void EnglishResourcesHaveUniqueKeysAndNoRetiredLanguageOption()
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = XDocument.Load(Path.Combine(AppDir, "Strings", "En.xaml")).Root!.Elements()
            .Select(e => (string?)e.Attribute(x + "Key")).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.DoesNotContain("Settings.LanguageTurkish", keys);
        Assert.False(File.Exists(Path.Combine(AppDir, "Strings", "Tr.xaml")));
    }

    [Fact]
    public void EveryStringKeyUsedInXaml_Exists()
    {
        var keys = Keys("En");

        // App strings are dotted ("Profiles.Header"); WPF-UI theme keys are not.
        var used = Directory.EnumerateFiles(AppDir, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "Strings" + Path.DirectorySeparatorChar))
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"DynamicResource\s+([A-Za-z]+\.[A-Za-z.]+)")
                                  .Select(m => m.Groups[1].Value))
            .Distinct();

        Assert.Empty(used.Where(k => !keys.Contains(k)));
    }

    [Fact]
    public void EveryStringKeyUsedInCode_Exists()
    {
        var keys = Keys("En");

        var used = Directory.EnumerateFiles(AppDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"Loc\.(?:Get|Format)\(\s*""([^""]+)""")
                                  .Select(m => m.Groups[1].Value))
            .Distinct();

        Assert.Empty(used.Where(k => !keys.Contains(k)));
    }
}
