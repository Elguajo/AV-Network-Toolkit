using System.IO;
using System.Windows;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;
using Newtonsoft.Json;

namespace NetworkProfileManager.Tests;

public sealed class WindowLayoutTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AvLayout-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_root, "window_layout.json");
    private static WindowLayoutState Sample => new(1, "fixture", 50, 30, 1000, 700, true, 340, false);
    public WindowLayoutTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void MissingStateReadDoesNotWriteAndRoundTripPreservesPreferences()
    {
        var store = new WindowLayoutStore(FilePath);
        Assert.Null(store.Load());
        Assert.Empty(Directory.GetFiles(_root));
        Assert.True(store.TrySave(Sample));
        Assert.Equal(Sample, new WindowLayoutStore(FilePath).Load());
        Assert.True(store.TrySave(Sample));
        Assert.Empty(Directory.GetFiles(_root, "*.bak"));
        var bytes = File.ReadAllBytes(FilePath);
        Assert.True(store.TrySave(Sample with { SidebarExpanded = true }));
        Assert.Equal(bytes, File.ReadAllBytes(Directory.GetFiles(_root, "*.bak").Single()));
    }

    [Theory]
    [InlineData("broken")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    public void CorruptOrIncompletePreferenceIsPreserved(string content)
    {
        File.WriteAllText(FilePath, content);
        var store = new WindowLayoutStore(FilePath);
        Assert.Null(store.Load()); Assert.True(store.HasReadError);
        Assert.False(store.TrySave(Sample));
        Assert.Equal(content, File.ReadAllText(FilePath));
        Assert.Single(Directory.GetFiles(_root));
    }

    [Fact]
    public void NewerVersionAndOversizedFileAreNeverReplaced()
    {
        foreach (var content in new[] { JsonConvert.SerializeObject(new[] { Sample with { Version = 2 } }), new string('x', 65537) })
        {
            File.WriteAllText(FilePath, content);
            var store = new WindowLayoutStore(FilePath);
            Assert.Null(store.Load()); Assert.False(store.TrySave(Sample));
            Assert.Equal(content, File.ReadAllText(FilePath));
        }
    }

    [Fact]
    public void CompetingInstanceCannotOverwriteNewerState()
    {
        var first = new WindowLayoutStore(FilePath); first.Load(); first.TrySave(Sample);
        var stale = new WindowLayoutStore(FilePath); stale.Load();
        var updated = Sample with { OffsetX = 200 };
        Assert.True(first.TrySave(updated));
        Assert.False(stale.TrySave(Sample with { Width = 1100 }));
        Assert.Equal(updated, new WindowLayoutStore(FilePath).Load());
    }

    [Fact]
    public void LockedFileKeepsOriginalAndSaveCanRetry()
    {
        var store = new WindowLayoutStore(FilePath); store.Load(); store.TrySave(Sample);
        var bytes = File.ReadAllBytes(FilePath);
        using (var locked = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.False(store.TrySave(Sample with { Width = 1100 }));
        Assert.Equal(bytes, File.ReadAllBytes(FilePath));
        Assert.True(store.TrySave(Sample with { Width = 1100 }));
    }

    [Theory]
    [InlineData(96)] [InlineData(144)] [InlineData(192)]
    public void PlacementUsesMonitorRelativeDipsAndClampsOffscreenOffsets(double dpi)
    {
        var area = new Rect(-3840, -600, 3840, 2160);
        var rect = (Sample with { OffsetX = 99999, OffsetY = -99999 }).Place(area, dpi);
        Assert.Equal(area.Top, rect.Top);
        Assert.InRange(rect.Left, area.Left, area.Right - rect.Width);
        Assert.True(area.Contains(rect));
        Assert.Equal(1000 * dpi / 96, rect.Width);
    }

    [Fact]
    public void SmallerMonitorClampsSizeWithoutSavingInvalidMinimizedGeometry()
    {
        var rect = (Sample with { Width = 3000, Height = 2000 }).Place(new Rect(0, 0, 1280, 800), 96);
        Assert.Equal(new Rect(0, 0, 1280, 800), rect);
        Assert.False((Sample with { Width = double.NaN }).IsValid);
        Assert.False((Sample with { SidebarWidth = double.PositiveInfinity }).IsValid);
        Assert.False((Sample with { OffsetX = double.NegativeInfinity }).IsValid);
    }
}
