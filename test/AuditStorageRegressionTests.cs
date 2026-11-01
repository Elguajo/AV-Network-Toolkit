using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using NetworkProfileManager.Converters;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class AuditStorageRegressionTests : IDisposable
{
    private readonly TestDataDirectoryFixture _dir = new();
    public void Dispose() => _dir.Dispose();
    private string PathFor(string kind, string id = "fixture") => Path.Combine(_dir.Root, kind + "_" + id + ".json");
    [Theory]
    [InlineData("profiles")][InlineData("history")]
    public void CorruptionWithUnavailableBackupNeverDeletesOriginal(string kind)
    {
        var path = PathFor(kind); File.WriteAllText(path, "{broken fixture"); Directory.CreateDirectory(path + ".bak");
        Assert.Throws<InvalidDataException>(() => { if (kind == "profiles") new ProfileRepository().Load("fixture"); else new HistoryRepository().Load("fixture"); });
        Assert.Equal("{broken fixture", File.ReadAllText(path)); Assert.True(Directory.Exists(path + ".bak"));
    }
    [Fact]
    public void FailedReplacePreservesOriginalAndExistingBackup()
    {
        var repo = new ProfileRepository(); repo.Save("fixture", new() { new() { Name = "Original" } });
        var original = File.ReadAllBytes(PathFor("profiles")); File.WriteAllText(PathFor("profiles") + ".bak", "preserve");
        var fail = new ProfileRepository(() => throw new IOException("fixture")); fail.Load("fixture");
        Assert.Throws<IOException>(() => fail.Save("fixture", new() { new() { Name = "Changed" } }));
        Assert.Equal(original, File.ReadAllBytes(PathFor("profiles"))); Assert.Equal("preserve", File.ReadAllText(PathFor("profiles") + ".bak"));
        Assert.Empty(Directory.GetFiles(_dir.Root, "*.tmp"));
    }
    [Fact]
    public void StaleWriterCannotOverwriteNewerProfilesAndBackupsRemainUsable()
    {
        var first = new ProfileRepository(); first.Save("fixture", new() { new() { Name = "One" } });
        var stale = new ProfileRepository(); stale.Load("fixture");
        first.Save("fixture", new() { new() { Name = "Two" } });
        Assert.Throws<IOException>(() => stale.Save("fixture", new() { new() { Name = "Stale" } }));
        Assert.Equal("Two", Assert.Single(first.Load("fixture")).Name);
        var backup = Assert.Single(Directory.GetFiles(_dir.Root, "profiles_*.bak")); Assert.Contains("One", File.ReadAllText(backup));
    }
    [Fact]
    public void FailedAddAndEditLeaveCollectionUnchangedAndExposeRetryFeedback()
    {
        var vm = new MainViewModel(() => new() { new() { Id = "fixture", Name = "Fixture" } });
        Directory.CreateDirectory(PathFor("profiles"));
        Assert.Null(Record.Exception(() => vm.AddProfile(new() { Name = "Draft" })));
        Assert.Empty(vm.SelectedAdapter!.Profiles); Assert.NotEmpty(vm.StorageError);
        // A valid existing row remains intact when its storage path is unavailable.
        var row = new ProfileViewModel(new() { Name = "Original" }); vm.SelectedAdapter.Profiles.Add(row);
        vm.UpdateProfile(row, new() { Name = "Edited" }); Assert.Same(row, Assert.Single(vm.SelectedAdapter.Profiles));
        vm.Diagnostics.Dispose();
    }
    [Fact]
    public void RefreshReloadsExternalChangesBeforeRetryingSave()
    {
        var vm = new MainViewModel(() => new() { new() { Id = "fixture", Name = "Fixture" } });
        new ProfileRepository().Save("fixture", new() { new() { Name = "External" } });
        Assert.False(vm.TryAddProfile(new() { Name = "Draft" })); Assert.Empty(vm.SelectedAdapter!.Profiles);
        vm.Refresh(); Assert.Equal("External", Assert.Single(vm.SelectedAdapter.Profiles).Name);
        Assert.True(vm.TryAddProfile(new() { Name = "Draft" })); Assert.Equal(2, new ProfileRepository().Load("fixture").Count);
        vm.Diagnostics.Dispose();
    }
    [Fact]
    public void TransientMigrationFailureRetriesWithoutOverwritingOrDeletingSources()
    {
        var legacy = Path.Combine(_dir.Root, "legacy"); Directory.CreateDirectory(Path.Combine(legacy, "data"));
        var source = Path.Combine(legacy, "data", "profiles_fixture.json"); File.WriteAllText(source, "[]");
        using (var held = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None))
        { Assert.Equal(0, AppPaths.MigrateLegacyDataIfPresent(legacy)); Assert.False(File.Exists(Path.Combine(_dir.Root, ".migrated_v1"))); }
        Assert.Equal(1, AppPaths.MigrateLegacyDataIfPresent(legacy)); Assert.Equal("[]", File.ReadAllText(source));
        Assert.Equal("[]", File.ReadAllText(PathFor("profiles"))); Assert.Equal(0, AppPaths.MigrateLegacyDataIfPresent(legacy));
    }
    [Fact]
    public async Task OneCorruptJournalCannotHideHealthyEntriesOrEraseEvidence()
    {
        var journal = new ProjectApplyJournal(Path.Combine(_dir.Root, "journal"));
        var request = new ProjectApplyRequest(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1), ProjectApplyTests.Snapshot(), ProjectApplyTests.Intent());
        await journal.BeginAsync(request, CancellationToken.None);
        var corrupt = Path.Combine(journal.DirectoryPath, Guid.NewGuid().ToString("N") + ".request.json"); File.WriteAllText(corrupt, "{");
        Assert.Equal(request.OperationId, Assert.Single(journal.ReadSnapshots()).OperationId);
        Assert.Equal(1, journal.UnreadableCount); Assert.Equal("{", File.ReadAllText(corrupt));
        Assert.Equal(request.OperationId, journal.ReadSnapshot(request.OperationId).OperationId);
    }
    [Fact]
    public void JournalsBeyondOneThousandRemainAccessibleInBoundedPages()
    {
        var journal = new ProjectApplyJournal(Path.Combine(_dir.Root, "journal")); Directory.CreateDirectory(journal.DirectoryPath);
        var request = new ProjectApplyRequest(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1), ProjectApplyTests.Snapshot(), ProjectApplyTests.Intent());
        for (int i = 0; i < 1001; i++)
        {
            var item = request with { OperationId = Guid.NewGuid() };
            File.WriteAllText(Path.Combine(journal.DirectoryPath, item.OperationId.ToString("N") + ".request.json"), ProjectApplyWire.Serialize(item));
        }
        var first = journal.ReadSnapshots(0); Assert.Equal(1000, first.Count); Assert.True(journal.HasMore);
        var next = journal.ReadSnapshots(1000); Assert.Single(next); Assert.False(journal.HasMore);
        Assert.Equal(1001, first.Concat(next).Select(r => r.OperationId).Distinct().Count()); Assert.Equal(1001, Directory.GetFiles(journal.DirectoryPath).Length);
    }
    [Fact]
    public void TrayAndMainAgreeOnSecondaryDnsAndMissingEvidence()
    {
        var profile = new IpProfile { IpAddress = "192.0.2.10", SubnetMask = "255.255.255.0", PrimaryDns = "192.0.2.53", SecondaryDns = "192.0.2.54" };
        var adapter = new AdapterInfo { CurrentIp = profile.IpAddress, CurrentSubnet = profile.SubnetMask, CurrentPrimaryDns = profile.PrimaryDns, CurrentSecondaryDns = "192.0.2.55" };
        var vm = new ProfileViewModel(profile); vm.UpdateFromAdapter(adapter);
        Assert.False(vm.IsActive); Assert.False((bool)new ProfileCurrentConverter().Convert(new object[] { profile, adapter }, typeof(bool), "", CultureInfo.InvariantCulture));
        adapter.CurrentSecondaryDns = profile.SecondaryDns; Assert.True(vm.IsActive);
        adapter.CurrentPrimaryDns = ""; Assert.False(vm.IsActive);
    }
}
