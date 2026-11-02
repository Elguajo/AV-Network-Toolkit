using System.IO;
using System.Text.Json.Nodes;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class DevicePasswordTests : IDisposable
{
    private const string Secret = "SYNTHETIC-ONLY-quote\"\\-日本-🔑  ";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AVPasswordTests_" + Guid.NewGuid().ToString("N"));
    private string Source => Path.Combine(_root, "v1.avnet.json");
    private string Target => Path.Combine(_root, "v2.avnet.json");
    public DevicePasswordTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private static NetworkProject WithPassword(string value = Secret)
    {
        var p = ProjectJsonTests.Sample();
        return p with { SchemaVersion = 2, Devices = new() { p.Devices[0] with { Password = value } } };
    }
    private sealed class Dialogs : IProjectDialogs
    {
        public string? Open, Save;
        public bool Consent = true, Discard = true;
        public int Consents, Pickers;
        public bool Initial, Backup;
        public string? OpenPath() => Open;
        public string? SavePath(string? folder) { Pickers++; return Save; }
        public string? ChooseFolder() => null;
        public bool ConfirmDiscard() => Discard;
        public bool ConfirmDelete() => true;
        public bool ConfirmDeleteDevice() => true;
        public bool ConfirmOverwrite(string path) => true;
        public bool ConfirmOpenDevice(string destination, bool isHttp, bool hostMismatch, bool unlinked) => true;
        public bool ConfirmPasswordSave(bool initialV2, bool backupHasPassword)
        { Consents++; Initial = initialV2; Backup = backupHasPassword; return Consent; }
    }
    private sealed class Browser : IBrowserLauncher
    {
        public List<string> Opened = new();
        public void Open(string destination) => Opened.Add(destination);
    }
    private (ProjectViewModel Vm, Dialogs Dialog, Browser Browser) Open(bool v2 = false, ProjectStore? store = null)
    {
        new ProjectStore().Save(v2 ? WithPassword() : ProjectJsonTests.Sample(), Source);
        var d = new Dialogs { Open = Source, Save = Target };
        var b = new Browser();
        var vm = new ProjectViewModel(d, store ?? new ProjectStore(), b, value => value == Secret);
        vm.OpenCommand.Execute(null); vm.SelectedDevice = vm.Devices[0];
        Assert.Empty(vm.Error);
        return (vm, d, b);
    }
    private static void Add(ProjectViewModel vm, string value = Secret)
    { vm.DeviceEditor.SavePassword = true; vm.DeviceEditor.Password = value; }

    [Theory]
    [InlineData(" ")]
    [InlineData("  quote\"\\é日本🔑  ")]
    public void ExactRoundTripIncludingCloneAndUnrelatedEdits(string value)
    {
        var p = ProjectJson.Parse(ProjectJson.Serialize(WithPassword(value)));
        var editor = new ProjectDeviceEditor(p.Devices[0]);
        editor.Name = "Renamed";
        var clone = p with { Devices = new() { editor.Build() }, Project = p.Project with { Name = "Other" } };
        var result = ProjectJson.Parse(ProjectJson.Serialize(clone));
        Assert.Equal(2, result.SchemaVersion); Assert.Equal(value, result.Devices[0].Password);
        Assert.Equal("ProjectDevice { [redacted] }", result.Devices[0].ToString());
        Assert.DoesNotContain("SYNTHETIC", new ProjectSnapshot(result, Target, "revision").ToString());
    }
    [Fact]
    public void ScalarLengthLimitIncludesNonBmpWithoutCountingSurrogatesTwice()
    {
        var max = string.Concat(Enumerable.Repeat("🔑", 1024));
        Assert.Equal(max, ProjectJson.Parse(ProjectJson.Serialize(WithPassword(max))).Devices[0].Password);
        Assert.Throws<ProjectValidationException>(() => ProjectJson.Serialize(WithPassword(max + "x")));
    }
    [Theory]
    [InlineData("")]
    [InlineData("\0")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\t")]
    [InlineData("\u007f")]
    [InlineData("\u0085")]
    public void InvalidPasswordRejectsWithoutValueInException(string suffix)
    {
        var value = suffix.Length == 0 ? "" : "SYNTHETIC_SENTINEL" + suffix;
        var error = Assert.Throws<ProjectValidationException>(() => ProjectJson.Serialize(WithPassword(value)));
        Assert.DoesNotContain("SYNTHETIC_SENTINEL", error.ToString());
    }
    [Fact]
    public void InvalidManagedSurrogateIsRejectedBeforeSerializerReplacement()
        => Assert.Throws<ProjectValidationException>(() => ProjectJson.Serialize(WithPassword("SYNTHETIC" + new string('\ud800', 1))));
    [Theory]
    [InlineData("null")]
    [InlineData("123")]
    [InlineData("{}")]
    [InlineData("\"\"")]
    [InlineData("\"\\ud800\"")]
    public void UntrustedPasswordTypesAndUnicodeAreRejected(string raw)
    {
        var json = ProjectJson.Serialize(WithPassword("SYNTHETIC_REPLACE"))
            .Replace("\"SYNTHETIC_REPLACE\"", raw);
        var error = Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(json));
        Assert.DoesNotContain("SYNTHETIC_REPLACE", error.ToString());
    }
    [Fact]
    public void V1RemainsStrictAndNoPasswordIsOmittedInsteadOfWrittenAsNull()
    {
        Assert.Throws<ProjectValidationException>(() => ProjectJson.Serialize(WithPassword() with { SchemaVersion = 1 }));
        var json = ProjectJson.Serialize(ProjectJsonTests.Sample());
        Assert.DoesNotContain("password", json);
        Assert.Equal(1, ProjectJson.Parse(json).SchemaVersion);
        var v2 = WithPassword() with { Devices = ProjectJsonTests.Sample().Devices };
        Assert.Equal(2, ProjectJson.Parse(ProjectJson.Serialize(v2)).SchemaVersion);
    }
    [Fact]
    public void V2UnknownFutureAndDuplicateCredentialFieldsFailClosed()
    {
        var json = ProjectJson.Serialize(WithPassword());
        var root = JsonNode.Parse(json)!; root["devices"]![0]!["username"] = "SYNTHETIC";
        Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(root.ToJsonString()));
        Assert.Equal("Version", Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(json.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 3"))).Code);
        Assert.Equal("DuplicateProperty", Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(json.Replace("\"password\":", "\"password\": \"SYNTHETIC\", \"password\":"))).Code);
    }
    [Fact]
    public void MixedVersionConcurrentValidationCannotSkipPasswordOrDnsRules()
    {
        var v1 = ProjectJson.Serialize(ProjectJsonTests.Sample());
        var v2 = ProjectJson.Serialize(WithPassword());
        var invalid = JsonNode.Parse(v2)!;
        invalid["profiles"]![0]!["ipv4"]!["dns"]!["servers"] = new JsonArray();
        var badDns = invalid.ToJsonString();
        var badPassword = v2.Replace("\"password\":", "\"password\": null, \"unknown\":");
        Parallel.For(0, 160, i =>
        {
            if (i % 4 == 0) Assert.Equal(1, ProjectJson.Parse(v1).SchemaVersion);
            if (i % 4 == 1) Assert.Equal(Secret, ProjectJson.Parse(v2).Devices[0].Password);
            if (i % 4 == 2) Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(badDns));
            if (i % 4 == 3) Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(badPassword));
        });
    }
    [Fact]
    public void V2ExampleAcceptedAndV1ExampleRetainsVersion()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AVNetworkToolkit.sln"))) directory = directory.Parent;
        var data = ProjectJson.Parse(File.ReadAllText(Path.Combine(directory!.FullName, "examples", "project.avnet.v2.json")));
        Assert.Equal(2, data.SchemaVersion); Assert.Equal("SYNTHETIC-EXAMPLE-ONLY", data.Devices[0].Password);
        var old = ProjectJson.Parse(File.ReadAllText(Path.Combine(directory.FullName, "examples", "project.avnet.json")));
        Assert.Equal(1, old.SchemaVersion);
    }
    [Fact]
    public void InitialPasswordSaveCreatesNewV2AndPreservesOriginalV1()
    {
        var (vm, d, b) = Open(); var bytes = File.ReadAllBytes(Source);
        Add(vm); vm.SaveCommand.Execute(null);
        Assert.Empty(vm.Error); Assert.Equal(Target, vm.FilePath); Assert.False(vm.HasUnsavedChanges);
        Assert.True(d.Initial); Assert.False(d.Backup); Assert.Equal(1, d.Consents);
        Assert.Equal(bytes, File.ReadAllBytes(Source)); Assert.Equal(1, new ProjectStore().Open(Source).Data.SchemaVersion);
        var copy = new ProjectStore().Open(Target).Data;
        Assert.Equal(2, copy.SchemaVersion); Assert.Equal(Secret, copy.Devices[0].Password);
        vm.ProjectName = "Profile-only metadata"; vm.SaveCommand.Execute(null);
        Assert.Empty(vm.Error); Assert.True(d.Backup); Assert.False(d.Initial);
        Assert.Equal(Secret, new ProjectStore().Open(Target).Data.Devices[0].Password);
        Assert.Empty(b.Opened);
    }
    [Theory]
    [InlineData("same")]
    [InlineData("existing")]
    [InlineData("cancel")]
    [InlineData("denied")]
    public void InitialSaveNeverOverwritesAndCancelledConsentRetainsIntent(string variant)
    {
        var (vm, d, _) = Open(); var bytes = File.ReadAllBytes(Source);
        if (variant == "same") d.Save = Source;
        if (variant == "existing") File.WriteAllText(Target, "do not replace");
        if (variant == "cancel") d.Save = null;
        if (variant == "denied") d.Consent = false;
        Add(vm); vm.SaveCommand.Execute(null);
        Assert.Equal(Source, vm.FilePath); Assert.True(vm.HasUnsavedChanges);
        Assert.Equal(Secret, vm.DeviceEditor.Password); Assert.True(vm.DeviceEditor.SavePassword);
        Assert.Equal(bytes, File.ReadAllBytes(Source));
        if (variant == "existing") Assert.Equal("do not replace", File.ReadAllText(Target));
        else Assert.False(File.Exists(Target));
    }
    [Fact]
    public void RemovalRequiresExplicitActionAndV2PersistsWithBackupWarning()
    {
        var (vm, d, _) = Open(true); vm.DeviceEditor.Password = "";
        vm.SaveCommand.Execute(null); Assert.NotEmpty(vm.Error);
        Assert.Equal(Secret, new ProjectStore().Open(Source).Data.Devices[0].Password);
        vm.RemovePasswordCommand.Execute(null); vm.SaveCommand.Execute(null);
        Assert.Empty(vm.Error); Assert.True(d.Backup); Assert.False(d.Initial);
        var data = new ProjectStore().Open(Source).Data;
        Assert.Equal(2, data.SchemaVersion); Assert.Null(data.Devices[0].Password);
        Assert.Equal(Secret, new ProjectStore().Open(vm.BackupPath).Data.Devices[0].Password);
    }
    [Fact]
    public void RevealCopySearchAndSectionNeverDirtyOrLaunchAndApplyReceivesNoPassword()
    {
        var (vm, _, b) = Open(true); ProjectDevice? handedOff = null;
        vm.ApplyAndOpenRequested += (_, device) => handedOff = device;
        vm.RevealPasswordCommand.Execute(null); Assert.True(vm.DeviceEditor.IsPasswordRevealed);
        vm.CopyPasswordCommand.Execute(null); Assert.False(vm.HasUnsavedChanges);
        vm.DeviceSearch = Secret; Assert.True(vm.NoDeviceMatches);
        vm.DeviceGroupBy = "profile"; vm.DeviceSearch = "";
        vm.Section = 0; Assert.False(vm.DeviceEditor.IsPasswordRevealed);
        Assert.False(vm.HasUnsavedChanges); Assert.Empty(b.Opened);
        vm.ApplyAndOpenCommand.Execute(null); Assert.NotNull(handedOff); Assert.Null(handedOff.Password);
        vm.OpenDeviceCommand.Execute(null); Assert.Equal(vm.Devices[0].WebUrl, Assert.Single(b.Opened));
        Assert.DoesNotContain(Secret, vm.DeviceDestination + vm.Error + vm.DeviceNotice + vm.FileFeedback);
    }
    [Fact]
    public void DiscardAndDeniedDiscardProtectOriginalPassword()
    {
        var (vm, d, _) = Open(true); var original = vm.Devices[0];
        vm.RemovePasswordCommand.Execute(null); d.Discard = false;
        vm.NewDeviceCommand.Execute(null); Assert.Same(original, vm.SelectedDevice);
        Assert.False(vm.DeviceEditor.SavePassword); Assert.Equal(Secret, original.Password);
        d.Discard = true; vm.NewDeviceCommand.Execute(null); vm.SelectedDevice = original;
        Assert.True(vm.DeviceEditor.SavePassword); Assert.Equal(Secret, vm.DeviceEditor.Password);
        Assert.False(vm.HasUnsavedChanges);
    }
    [Theory]
    [InlineData("Serialize")]
    [InlineData("TempWrite")]
    [InlineData("Flush")]
    [InlineData("Backup")]
    [InlineData("BackupFlush")]
    [InlineData("Replace")]
    [InlineData("AtomicReplace")]
    public void EveryWriteFailurePreservesOriginalPasswordAndDraft(string failure)
    {
        var store = new ProjectStore(stage => { if (stage.ToString() == failure) throw new IOException("SYNTHETIC"); });
        var (vm, _, _) = Open(true, store); var bytes = File.ReadAllBytes(Source);
        vm.DeviceEditor.Password = "SYNTHETIC_CHANGED"; vm.SaveCommand.Execute(null);
        Assert.NotEmpty(vm.Error); Assert.True(vm.HasUnsavedChanges);
        Assert.Equal("SYNTHETIC_CHANGED", vm.DeviceEditor.Password);
        Assert.Equal(bytes, File.ReadAllBytes(Source)); Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
        Assert.Empty(Directory.GetFiles(_root, "*.lock"));
    }
    [Theory]
    [InlineData("downgrade")]
    [InlineData("upgrade")]
    [InlineData("future")]
    [InlineData("unknown")]
    public void StoreGuardsOverwriteCompatibilityBeforeAnyWrite(string variant)
    {
        var store = new ProjectStore(); var target = variant == "upgrade" ? ProjectJsonTests.Sample() : WithPassword();
        var json = ProjectJson.Serialize(target);
        if (variant == "future") json = json.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 99");
        if (variant == "unknown") json = json.Replace("\"devices\":", "\"futureCredential\": true, \"devices\":");
        File.WriteAllText(Target, json); var bytes = File.ReadAllBytes(Target);
        var output = variant == "downgrade" ? ProjectJsonTests.Sample() : WithPassword();
        Assert.Throws<ProjectValidationException>(() => store.Save(output, Target, store.RevisionAt(Target)));
        Assert.Equal(bytes, File.ReadAllBytes(Target)); Assert.Single(Directory.GetFiles(_root));
    }
    [Fact]
    public void ExternalEditAndLateReplacementRetainAllCredentialBytes()
    {
        var store = new ProjectStore(); var saved = store.Save(WithPassword(), Target);
        var original = File.ReadAllBytes(Target); var external = ProjectJson.Serialize(WithPassword("SYNTHETIC_EXTERNAL"));
        var racing = new ProjectStore(stage => { if (stage == ProjectWriteStage.AtomicReplace) File.WriteAllText(Target, external); });
        Assert.Equal("ConcurrentReplace", Assert.Throws<ProjectValidationException>(() => racing.Save(WithPassword("SYNTHETIC_NEW"), Target, saved.Revision)).Code);
        var backups = Directory.GetFiles(_root, "*.bak");
        Assert.Contains(backups, p => File.ReadAllBytes(p).SequenceEqual(original));
        Assert.Contains(backups, p => File.ReadAllText(p) == external);
        Assert.Equal("SYNTHETIC_NEW", store.Open(Target).Data.Devices[0].Password);
    }
}

public sealed class DevicePasswordClipboardTests
{
    private sealed class Backend : IPasswordClipboardBackend
    {
        public string Text = "", Marker = "";
        public uint Sequence;
        public bool Busy;
        public int Clears;
        public uint Write(string text, string marker)
        {
            if (Busy) throw new InvalidOperationException("SYNTHETIC_PROVIDER_VALUE");
            Text = text; Marker = marker; return ++Sequence;
        }
        public bool ClearIfMatches(string text, string marker, uint sequence)
        {
            if (Busy) throw new InvalidOperationException();
            if (Text != text || Marker != marker || Sequence != sequence) return false;
            Text = ""; Marker = ""; Sequence++; Clears++; return true;
        }
        public void Dispose() { }
    }
    private sealed class Timer : IDisposable
    {
        public Action Callback = null!;
        public bool Stopped;
        public void Dispose() => Stopped = true;
    }
    [Theory]
    [InlineData("unchanged")]
    [InlineData("other")]
    [InlineData("same-text")]
    [InlineData("marker")]
    [InlineData("busy")]
    [InlineData("exit")]
    public void CleanupRequiresTheExactOwnedCopyAndNeverErasesAnotherCopy(string variant)
    {
        var backend = new Backend(); var timers = new List<Timer>();
        using var service = new DevicePasswordClipboard(backend, (delay, callback) =>
        { Assert.Equal(TimeSpan.FromSeconds(30), delay); var t = new Timer { Callback = callback }; timers.Add(t); return t; });
        Assert.True(service.Copy("SYNTHETIC_FIRST"));
        if (variant == "other") { backend.Text = "User text"; backend.Sequence++; }
        if (variant == "same-text") backend.Sequence++;
        if (variant == "marker") backend.Marker = "other owner";
        if (variant == "busy") backend.Busy = true;
        if (variant == "exit") service.Dispose(); else timers[0].Callback();
        Assert.Equal(variant is "unchanged" or "exit" ? 1 : 0, backend.Clears);
    }
    [Fact]
    public void RecopyCancelsOldTimerAndStaleCallbackCannotClearTheNewCopy()
    {
        var backend = new Backend(); var timers = new List<Timer>();
        using var service = new DevicePasswordClipboard(backend, (_, callback) =>
        { var t = new Timer { Callback = callback }; timers.Add(t); return t; });
        Assert.True(service.Copy("SYNTHETIC_FIRST")); Assert.True(service.Copy("SYNTHETIC_SECOND"));
        Assert.True(timers[0].Stopped); timers[0].Callback(); Assert.Equal(0, backend.Clears);
        timers[1].Callback(); Assert.Equal(1, backend.Clears);
    }
    [Fact]
    public void FailedRecopyRetainsPreviousCleanupAndProviderExceptionIsNotExposed()
    {
        var backend = new Backend(); Action expire = null!;
        using var service = new DevicePasswordClipboard(backend, (_, callback) => { expire = callback; return new Timer(); });
        Assert.True(service.Copy("SYNTHETIC_FIRST")); backend.Busy = true;
        Assert.False(service.Copy("SYNTHETIC_SECOND")); backend.Busy = false; expire();
        Assert.Equal(1, backend.Clears); service.Dispose(); Assert.False(service.Copy("SYNTHETIC_THIRD"));
    }
    [Fact]
    public void BusyExpiryRetriesConditionallyOnExit()
    {
        var backend = new Backend(); Action expire = null!;
        var service = new DevicePasswordClipboard(backend, (_, callback) => { expire = callback; return new Timer(); });
        Assert.True(service.Copy("SYNTHETIC")); backend.Busy = true; expire(); Assert.Equal(0, backend.Clears);
        backend.Busy = false; service.Dispose(); Assert.Equal(1, backend.Clears);
    }
    [Fact]
    public void SchedulerFailureCleansOnlyTheReplacementCopy()
    {
        var backend = new Backend();
        using var service = new DevicePasswordClipboard(backend, (_, _) => throw new InvalidOperationException());
        Assert.False(service.Copy("SYNTHETIC")); Assert.Equal(1, backend.Clears);
    }
}
