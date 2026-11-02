using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class UserStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AvData01-" + Guid.NewGuid().ToString("N"));
    private readonly UserStorageLocations _paths;
    public UserStorageTests()
    {
        Directory.CreateDirectory(_root);
        _paths = new(Path.Combine(_root, "home"), Path.Combine(_root, "legacy"), Path.Combine(_root, "exe"), Path.Combine(_root, "backups"));
        Directory.CreateDirectory(_paths.Executable);
        AppPaths.ResetForTests();
    }
    public void Dispose() { AppPaths.ResetForTests(); Directory.Delete(_root, true); }
    private string Seed(string relative, string text = "[]")
    {
        var path = Path.Combine(_paths.Legacy, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false)); return path;
    }
    private StorageRootContext Open(Func<StorageReview, bool>? consent = null, Action<StorageStep>? before = null)
        => new UserStorage(_paths, before).Open(consent ?? (_ => true));

    [Fact]
    public void FreshStartAndRepeatedStartDoNotInventPreferencesOrRecopy()
    {
        var first = Open(_ => throw new Exception("Empty install must not request migration consent."));
        Assert.Empty(Directory.GetFiles(first.Data)); Assert.Empty(Directory.GetFiles(first.Recovery));
        var marker = File.ReadAllBytes(Path.Combine(first.Root, "storage-root.json"));
        Open(_ => throw new Exception("Repeated start must not request consent."));
        Assert.Equal(marker, File.ReadAllBytes(Path.Combine(first.Root, "storage-root.json")));
        Assert.Single(Directory.GetDirectories(_paths.Backups));
        Assert.False(Directory.Exists(_paths.Legacy));
    }
    [Fact]
    public void CancelHasNoDestinationOrBackupAndLeavesSourceExact()
    {
        var source = Seed("data/profiles_orphan-adapter.json", " [ ] \r\n"); var bytes = File.ReadAllBytes(source);
        Assert.Throws<OperationCanceledException>(() => Open(_ => false));
        Assert.False(Directory.Exists(_paths.Home)); Assert.False(Directory.Exists(_paths.Backups));
        Assert.Equal(bytes, File.ReadAllBytes(source));
    }
    [Fact]
    public void EveryStoreBackupAndUnknownKeySurvivesWithNoIdentityMapping()
    {
        Seed("data/profiles__legacy_adapter_.json", " [ {\"Id\":\"stable\",\"Name\":\"Fixture\",\"Gateway\":\"\",\"PrimaryDns\":\"\"} ]\r\n");
        Seed("data/history_disconnected.json", "[]");
        Seed("data/app_settings.json", "{\"Language\":\"tr\",\"Theme\":\"Dark\",\"FutureKey\":\"keep\"}");
        Seed("data/scanner_settings.json", "{\"StartIp\":\"192.0.2.1\",\"UseArp\":true,\"FutureKey\":\"keep\"}");
        Seed("data/window_layout.json", JsonConvert.SerializeObject(new[] { new WindowLayoutState(1, "fixture", 5, 6, 1000, 700, false, 240, false) }));
        Seed("data/application_updates.json", "[{\"Version\":1,\"AutomaticChecks\":false,\"IncludePreview\":true,\"LastCheck\":null}]");
        Seed("data/profiles__legacy_adapter_.json.fixture.bak", "damaged old evidence");
        Seed("data/orphan.tmp", "unfinished"); Seed("data/inactive.lock", "stale"); Seed("data/unknown/nested.bin", "opaque");
        Seed("apply-recovery/broken.request.json", "{broken"); Seed("apply-recovery/orphan.outcome.json", "{unknown");
        Seed("updates/download.partial", "not user data");
        var original = Directory.GetFiles(_paths.Legacy, "*", SearchOption.AllDirectories).ToDictionary(p => Path.GetRelativePath(_paths.Legacy, p), File.ReadAllBytes);
        var result = Open();
        foreach (var file in original)
        {
            Assert.Equal(file.Value, File.ReadAllBytes(Path.Combine(_paths.Legacy, file.Key)));
            if (file.Key.StartsWith("updates") || file.Key.EndsWith(".tmp") || file.Key.EndsWith(".lock")) continue;
            Assert.Equal(file.Value, File.ReadAllBytes(Path.Combine(result.Root, file.Key)));
        }
        Assert.False(Directory.Exists(Path.Combine(result.Root, "updates")));
        Assert.False(File.Exists(Path.Combine(result.Data, "orphan.tmp")));
        var snapshot = Assert.Single(Directory.GetDirectories(_paths.Backups));
        Assert.Equal("unfinished", File.ReadAllText(Path.Combine(snapshot, "local/data/orphan.tmp")));
        Assert.Equal("stale", File.ReadAllText(Path.Combine(snapshot, "local/data/inactive.lock")));
        var journal = new ProjectApplyJournal(result.Recovery); Assert.Empty(journal.ReadSnapshots()); Assert.Equal(1, journal.UnreadableCount);
        using var identity = WindowsIdentity.GetCurrent();
        var acl = new DirectoryInfo(snapshot).GetAccessControl();
        Assert.True(acl.AreAccessRulesProtected); Assert.Equal(identity.User, acl.GetOwner(typeof(SecurityIdentifier)));
    }
    [Fact]
    public void HistoricalRecoveryPointerAndExpiredSnapshotRemainReadable()
    {
        var request = new ProjectApplyRequest(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddYears(-1), ProjectApplyTests.Snapshot(), ProjectApplyTests.Intent());
        Seed("data/history_orphan.json", JsonConvert.SerializeObject(new[] { new HistoryEntry { RecoveryOperationId = request.OperationId, AdapterId = request.Before.Identity.Id.ToString() } }));
        var journal = Seed("apply-recovery/" + request.OperationId.ToString("N") + ".request.json", ProjectApplyWire.Serialize(request));
        StorageReview? review = null;
        var result = Open(r => { review = r; return true; });
        Assert.Equal(0, review!.UnavailableRecovery);
        var restored = new ProjectApplyJournal(result.Recovery).ReadSnapshot(request.OperationId);
        Assert.Equal(request.OperationId, restored.OperationId); Assert.Equal(request.Before.Identity, restored.Before.Identity);
        Assert.Equal(File.ReadAllBytes(journal), File.ReadAllBytes(Path.Combine(result.Recovery, Path.GetFileName(journal))));
    }
    [Fact]
    public void AlreadyMissingRecoveryIsDisclosedAndNeverInvented()
    {
        Seed("data/history_fixture.json", JsonConvert.SerializeObject(new[] { new HistoryEntry { RecoveryOperationId = Guid.NewGuid() } }));
        StorageReview? review = null; var result = Open(r => { review = r; return true; });
        Assert.Equal(1, review!.UnavailableRecovery); Assert.Empty(Directory.GetFiles(result.Recovery));
    }
    [Theory]
    [InlineData("data/profiles_bad.json", "{broken")]
    [InlineData("data/history_bad.json", "[null]")]
    [InlineData("data/app_settings.json", "{\"Theme\":4}")]
    [InlineData("data/scanner_settings.json", "{\"TimeoutMs\":\"wrong\"}")]
    [InlineData("data/window_layout.json", "[{\"Version\":99}]")]
    [InlineData("data/application_updates.json", "[{\"Version\":99}]")]
    [InlineData("data/APP_SETTINGS.JSON", "{\"Theme\":4}")]
    [InlineData("data/WINDOW_LAYOUT.JSON", "[{\"Version\":99}]")]
    public void DamagedOrFutureActiveDataIsBackedUpButNeverPromoted(string file, string text)
    {
        var source = Seed(file, text);
        Assert.ThrowsAny<Exception>(() => Open()); Assert.False(Directory.Exists(_paths.Home));
        Assert.Equal(text, File.ReadAllText(source));
        var snapshot = Assert.Single(Directory.GetDirectories(_paths.Backups));
        Assert.Equal(text, File.ReadAllText(Path.Combine(snapshot, "local", file)));
    }
    [Fact]
    public void UnknownExistingHomeIsNeverAdoptedEvenWhenEmpty()
    {
        Seed("data/profiles_fixture.json"); Directory.CreateDirectory(_paths.Home);
        Assert.Throws<IOException>(() => Open()); Assert.Empty(Directory.GetFileSystemEntries(_paths.Home));
        Assert.False(Directory.Exists(_paths.Backups));
    }
    [Fact]
    public void LegacyDivergenceNeedsConsentAndPreservesBothVersions()
    {
        var source = Seed("data/profiles_fixture.json", "[]"); var home = Open();
        File.WriteAllText(source, " [ ] "); var originalMarker = File.ReadAllBytes(Path.Combine(home.Root, "storage-root.json"));
        Assert.Throws<OperationCanceledException>(() => Open(_ => false));
        Assert.Equal(originalMarker, File.ReadAllBytes(Path.Combine(home.Root, "storage-root.json")));
        Open(r => { Assert.True(r.Diverged); return true; });
        Assert.Equal("[]", File.ReadAllText(Path.Combine(home.Data, "profiles_fixture.json")));
        Assert.Equal(" [ ] ", File.ReadAllText(source)); Assert.Equal(2, Directory.GetDirectories(_paths.Backups).Length);
        Open(_ => throw new Exception("Already acknowledged."));
    }
    [Fact]
    public void DeletedLegacySourceIsADivergenceAndHomeEditsDoNotTriggerReimport()
    {
        var source = Seed("data/profiles_fixture.json"); var home = Open();
        File.WriteAllText(Path.Combine(home.Data, "profiles_fixture.json"), " [ ] ");
        Open(_ => throw new Exception("Only home data changed."));
        File.Delete(source);
        Assert.Throws<OperationCanceledException>(() => Open(_ => false));
    }
    [Theory]
    [InlineData((int)StorageStep.Inventory)] [InlineData((int)StorageStep.Snapshot)] [InlineData((int)StorageStep.Copy)]
    [InlineData((int)StorageStep.Flush)] [InlineData((int)StorageStep.Validate)] [InlineData((int)StorageStep.Metadata)]
    [InlineData((int)StorageStep.Publish)] [InlineData((int)StorageStep.Readback)]
    public void EveryInterruptionRestartsWithoutPartialActivationOrSourceChanges(int failureValue)
    {
        var failure = (StorageStep)failureValue;
        var source = Seed("data/profiles_fixture.json");
        Assert.Throws<IOException>(() => Open(before: s => { if (s == failure) throw new IOException("injected IO failure"); }));
        Assert.Equal("[]", File.ReadAllText(source));
        if (failure != StorageStep.Readback) Assert.False(Directory.Exists(_paths.Home));
        var context = Open(); context.EnsureAvailable(); Assert.Equal("[]", File.ReadAllText(Path.Combine(context.Data, "profiles_fixture.json")));
    }
    [Fact]
    public void ConcurrentDestinationAndSourceChangeCannotBeOverwritten()
    {
        var source = Seed("data/profiles_fixture.json");
        Assert.Throws<IOException>(() => Open(before: s => { if (s == StorageStep.Publish) File.WriteAllText(source, " [ ] "); }));
        Assert.False(Directory.Exists(_paths.Home));
        Assert.Throws<IOException>(() => Open(before: s => { if (s == StorageStep.Publish) { Directory.CreateDirectory(_paths.Home); File.WriteAllText(Path.Combine(_paths.Home, "owner.txt"), "keep"); } }));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(_paths.Home, "owner.txt")));
    }
    [Fact]
    public void BusyProcessAndLockedFileBlockWithoutCopying()
    {
        var source = Seed("data/profiles_fixture.json");
        Assert.Throws<IOException>(() => new UserStorage(_paths, otherInstance: () => true).Open(_ => true));
        using (var locked = new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.Throws<IOException>(() => Open());
        Assert.False(Directory.Exists(_paths.Home)); Assert.False(Directory.Exists(_paths.Backups));
    }
    [Fact]
    public void ExeOnlyImportIsExplicitAndUnfinishedHybridIsRejected()
    {
        var exeData = Path.Combine(_paths.Executable, "data"); Directory.CreateDirectory(exeData);
        File.WriteAllText(Path.Combine(exeData, "profiles_fixture.json"), "[]");
        File.WriteAllText(Path.Combine(_paths.Executable, "app_settings.json"), "{\"Theme\":\"Dark\"}");
        Seed("data/profiles_other.json");
        Assert.Throws<IOException>(() => Open()); Assert.False(Directory.Exists(_paths.Home));
        File.Delete(Path.Combine(_paths.Legacy, "data/profiles_other.json"));
        StorageReview? review = null; var context = Open(r => { review = r; return true; });
        Assert.Equal(_paths.Executable, review!.Source); Assert.True(File.Exists(Path.Combine(context.Data, "profiles_fixture.json")));
        Assert.Contains("Dark", File.ReadAllText(Path.Combine(context.Data, "app_settings.json")));
    }
    [Fact]
    public void CompletedOldImportDoesNotResurrectUnrelatedExeFiles()
    {
        Seed("data/profiles_fixture.json"); Seed("data/.migrated_v1", "historical marker");
        Directory.CreateDirectory(Path.Combine(_paths.Executable, "data"));
        File.WriteAllText(Path.Combine(_paths.Executable, "data/profiles_old.json"), "[]");
        var context = Open(); Assert.False(File.Exists(Path.Combine(context.Data, "profiles_old.json")));
    }
    [Fact]
    public void RootLossDoesNotRecreateOrFallbackAndDefaultJournalUsesSameContext()
    {
        var context = Open(); AppPaths.Initialize(context);
        var updatePreferences = new ApplicationUpdatePreferences();
        Assert.Equal(context.Recovery, new ProjectApplyJournal().DirectoryPath); Assert.Equal(context.Data, AppPaths.DataDirectory);
        var displaced = _paths.Home + "-displaced"; Directory.Move(_paths.Home, displaced);
        Assert.Throws<IOException>(() => AppPaths.DataDirectory);
        Assert.False(AppSettingsStore.TrySet("Theme", "Light")); Assert.False(Directory.Exists(_paths.Home));
        Assert.False(updatePreferences.Save(updatePreferences.Current with { AutomaticChecks = true }));
        Assert.False(Directory.Exists(_paths.Home));
        Assert.False(Directory.Exists(_paths.Legacy));
    }
    [Fact]
    public void MarkerVersionOrDuplicatesAreRejectedWithoutTouchingHome()
    {
        var home = Open(); var marker = Path.Combine(home.Root, "storage-root.json");
        var obj = JObject.Parse(File.ReadAllText(marker)); obj["Version"] = 999; File.WriteAllText(marker, obj.ToString());
        Assert.Throws<IOException>(() => Open()); Assert.Equal(999, (int)JObject.Parse(File.ReadAllText(marker))["Version"]!);
        File.WriteAllText(marker, "{\"Version\":1,\"Version\":1}"); Assert.Throws<IOException>(() => Open());
    }
    [Fact]
    public void ObjectSettingsPreserveUnknownKeysBackupAndExternalConflict()
    {
        var path = Seed("data/scanner_settings.json", "{\"StartIp\":\"192.0.2.1\",\"Unknown\":42}");
        var store = new PreservedObjectFile(path); var values = store.Load(); values["StartIp"] = "192.0.2.2"; store.Save(values);
        Assert.Equal(42, (int)JObject.Parse(File.ReadAllText(path))["Unknown"]!);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.bak"));
        File.WriteAllText(path, "{\"Unknown\":43}"); Assert.Throws<IOException>(() => store.Save(values)); Assert.Contains("43", File.ReadAllText(path));
    }
    [Fact]
    public void AppAndScannerCorruptionCannotBeOverwrittenByLaterPreferences()
    {
        var data = Path.Combine(_root, "fixture"); AppPaths.OverrideDataDirectory(data);
        var app = Path.Combine(data, "app_settings.json"); File.WriteAllText(app, "{broken");
        Assert.Null(AppSettingsStore.Get("Theme")); Assert.False(AppSettingsStore.TrySet("Theme", "Light")); Assert.Equal("{broken", File.ReadAllText(app));
        var scanner = Path.Combine(data, "scanner_settings.json"); File.WriteAllText(scanner, "{broken");
        var vm = new IpScannerViewModel(); vm.StartIp = "192.0.2.1"; Assert.Equal("{broken", File.ReadAllText(scanner));
    }

    [Fact]
    public void ScannerRetainsDraftAndUnknownValuesWhenAnotherInstanceSaves()
    {
        var data = Path.Combine(_root, "scanner-fixture"); AppPaths.OverrideDataDirectory(data);
        var path = Path.Combine(data, "scanner_settings.json");
        File.WriteAllText(path, "{\"StartIp\":\"192.0.2.1\",\"FutureKey\":42}");
        var vm = new IpScannerViewModel(); vm.StartIp = "192.0.2.2";
        Assert.Equal(42, (int)JObject.Parse(File.ReadAllText(path))["FutureKey"]!);
        File.WriteAllText(path, "{\"StartIp\":\"192.0.2.3\"}");
        vm.StartIp = "192.0.2.4"; Assert.Equal("192.0.2.4", vm.StartIp);
        Assert.Equal("192.0.2.3", (string?)JObject.Parse(File.ReadAllText(path))["StartIp"]);
    }
    [Fact]
    public void LockedSettingsAndFailedBackupCannotClaimSuccess()
    {
        var path = Seed("data/app_settings.json", "{\"Theme\":\"Dark\"}");
        var store = new PreservedObjectFile(path); var value = store.Load(); value["Theme"] = "Light";
        using (var lease = new FileStream(path + ".lock", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            Assert.Throws<IOException>(() => store.Save(value));
        Assert.Contains("Dark", File.ReadAllText(path));
    }
    [Fact]
    public void ConcurrentBootstrapsDoNotPublishTwoRoots()
    {
        Seed("data/profiles_fixture.json");
        var context = Open(_ =>
        {
            Exception? secondFailure = null;
            var second = new System.Threading.Thread(() => secondFailure = Record.Exception(() => Open()));
            second.Start(); Assert.True(second.Join(5000)); Assert.IsType<IOException>(secondFailure);
            return true;
        });
        context.EnsureAvailable(); Assert.Single(Directory.GetDirectories(_paths.Backups));
    }
    [Fact]
    public void ReadonlySourceAndInjectedAccessFailurePreserveOriginals()
    {
        var source = Seed("data/profiles_fixture.json"); File.SetAttributes(source, FileAttributes.ReadOnly);
        try
        {
            Assert.Throws<UnauthorizedAccessException>(() => Open(before: s => { if (s == StorageStep.Snapshot) throw new UnauthorizedAccessException(); }));
            Assert.False(Directory.Exists(_paths.Home)); Assert.Equal("[]", File.ReadAllText(source));
            Open(); Assert.Equal(FileAttributes.ReadOnly, File.GetAttributes(source) & FileAttributes.ReadOnly);
        }
        finally { File.SetAttributes(source, FileAttributes.Normal); }
    }
    [Fact]
    public void ReparseAndOverlappingLocationsAreRejected()
    {
        Seed("data/profiles_fixture.json");
        var overlap = _paths with { Home = Path.Combine(_paths.Legacy, "data", "home") };
        Assert.Throws<IOException>(() => new UserStorage(overlap).Open(_ => true));
        var link = Path.Combine(_paths.Legacy, "data", "linked");
        // Junction creation on local NTFS requires no elevation; only synthetic temp paths are passed.
        var start = new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell/v1.0/powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-Command");
        start.ArgumentList.Add("$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path '" + link.Replace("'", "''")
            + "' -Value '" + _paths.Executable.Replace("'", "''") + "' | Out-Null");
        using var process = System.Diagnostics.Process.Start(start)!;
        Assert.True(process.WaitForExit(10000)); Assert.Equal(0, process.ExitCode);
        Assert.ThrowsAny<Exception>(() => Open());
        Directory.Delete(link);
        Assert.False(Directory.Exists(_paths.Home));
    }
    [Theory]
    [InlineData("null")][InlineData("{\"Theme\":\"Dark\",\"Theme\":\"Light\"}")]
    public void InvalidObjectShapesAndDuplicatesAreNeverSavedAsDefaults(string text)
    {
        var path = Seed("data/app_settings.json", text); var store = new PreservedObjectFile(path);
        Assert.ThrowsAny<Exception>(() => store.Load()); Assert.Throws<IOException>(() => store.Save(new JObject()));
        Assert.Equal(text, File.ReadAllText(path));
    }

    [Theory]
    [InlineData("backup")][InlineData("staging")][InlineData("manifest")][InlineData("record")]
    public void LateTamperingCannotActivateAnUnverifiedBackupOrRoot(string target)
    {
        var source = Seed("data/profiles_fixture.json");
        Assert.Throws<IOException>(() => Open(before: step =>
        {
            if (step != StorageStep.Publish) return;
            var staging = Assert.Single(Directory.GetDirectories(_root, "home.staging-*"));
            var backup = Assert.Single(Directory.GetDirectories(_paths.Backups));
            var path = target switch
            {
                "backup" => Path.Combine(backup, "local/data/profiles_fixture.json"),
                "staging" => Path.Combine(staging, "data/profiles_fixture.json"),
                "manifest" => Path.Combine(backup, "snapshot.json"),
                _ => Path.Combine(staging, "storage-root.json")
            };
            File.WriteAllText(path, " [ ] ");
        }));
        Assert.False(Directory.Exists(_paths.Home)); Assert.Equal("[]", File.ReadAllText(source));
    }

    [Fact]
    public void MigrationPrivilegeGateDoesNotChangeOrdinaryCommittedRootStartup()
    {
        Seed("data/profiles_fixture.json");
        Assert.Throws<IOException>(() => new UserStorage(_paths, beforeMigration: () => throw new IOException("ordinary user required")).Open(_ => true));
        Assert.False(Directory.Exists(_paths.Home));
        Open();
        new UserStorage(_paths, beforeMigration: () => throw new Exception("A committed unchanged root needs no migration privilege gate.")).Open(_ => true);
    }
    [Fact]
    public void InvalidUtf8AndOversizeActiveArraysArePreservedWithoutPromotion()
    {
        var file = Seed("data/profiles_fixture.json"); File.WriteAllBytes(file, new byte[] { 0xff, 0xfe, 0xff });
        Assert.ThrowsAny<Exception>(() => Open()); Assert.Equal(new byte[] { 0xff, 0xfe, 0xff }, File.ReadAllBytes(file));
        File.WriteAllBytes(file, new byte[8 * 1024 * 1024 + 1]);
        Assert.ThrowsAny<Exception>(() => Open()); Assert.False(Directory.Exists(_paths.Home));
        Assert.Equal(8 * 1024 * 1024 + 1, new FileInfo(file).Length);
    }
    [Fact]
    public void LargeOpaqueFilesAreStreamedWithoutTruncation()
    {
        var file = Seed("data/large-opaque.bin");
        var bytes = new byte[8 * 1024 * 1024 + 13]; Array.Fill(bytes, (byte)0x5a); File.WriteAllBytes(file, bytes);
        var home = Open();
        Assert.Equal(StorageSafety.HashFile(file), StorageSafety.HashFile(Path.Combine(home.Data, "large-opaque.bin")));
        Assert.Equal(bytes.Length, new FileInfo(Path.Combine(home.Data, "large-opaque.bin")).Length);
    }
    [Fact]
    public void RecoveryFeedbackShowsKnownRootsWithoutLeakingExceptionData()
    {
        const string privateValue = "invented-private-fixture-value";
        var message = StorageStartup.FailureMessage(new IOException(privateValue), _paths);
        Assert.Contains(_paths.Home, message); Assert.Contains(_paths.Legacy, message); Assert.Contains(_paths.Backups, message);
        Assert.DoesNotContain(privateValue, message);
    }
}
