using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class DeviceDirectoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AVDevices_" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_root, "project.avnet.json");
    public DeviceDirectoryTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);

    private sealed class Dialogs : IProjectDialogs
    {
        public string? Open, Save;
        public bool Discard = false, Delete, Confirm;
        public int OpenPrompts;
        public string? Destination;
        public bool Http, Mismatch, Unlinked;
        public string? OpenPath() => Open;
        public string? SavePath(string? folder) => Save;
        public string? ChooseFolder() => null;
        public bool ConfirmDiscard() => Discard;
        public bool ConfirmDelete() => Delete;
        public bool ConfirmOverwrite(string path) => true;
        public bool ConfirmDeleteDevice() => Delete;
        public bool ConfirmOpenDevice(string destination, bool isHttp, bool hostMismatch, bool unlinked)
        {
            OpenPrompts++; Destination = destination; Http = isHttp; Mismatch = hostMismatch; Unlinked = unlinked;
            return Confirm;
        }
    }
    private sealed class Browser : IBrowserLauncher
    {
        public readonly List<string> Opened = new();
        public Exception? Error;
        public void Open(string destination) { if (Error != null) throw Error; Opened.Add(destination); }
    }
    private (ProjectViewModel Vm, Dialogs Dialog, Browser Browser) OpenSample()
    {
        new ProjectStore().Save(ProjectJsonTests.Sample(), FilePath);
        var dialog = new Dialogs { Open = FilePath };
        var browser = new Browser();
        var vm = new ProjectViewModel(dialog, new ProjectStore(), browser);
        vm.OpenCommand.Execute(null);
        Assert.Empty(vm.Error);
        Assert.False(vm.HasUnsavedChanges);
        return (vm, dialog, browser);
    }

    [Fact]
    public void TwentyFourDevicesShareProfileAndIpWithDistinctUrlsAndStableIds()
    {
        var (vm, dialog, browser) = OpenSample();
        for (int i = 1; i < 24; i++)
        {
            vm.NewDeviceCommand.Execute(null);
            vm.DeviceEditor.Name = "Panel " + i;
            vm.DeviceEditor.Ip = "192.0.2.20";
            vm.DeviceEditor.WebUrl = $"https://192.0.2.20:8443/en_US/panel?view={i}&lang=en#!/main";
            vm.DeviceEditor.ProfileId = "static";
            vm.DeviceEditor.Type = "Panel";
            vm.KeepDeviceCommand.Execute(null);
            Assert.Empty(vm.Error);
        }
        var ids = vm.Devices.Select(d => d.Id).ToList();
        Assert.Equal(24, ids.Distinct().Count());
        dialog.Save = Path.Combine(_root, "copy.avnet.json");
        vm.SaveAsCommand.Execute(null);
        Assert.Empty(vm.Error);
        var data = new ProjectStore().Open(dialog.Save).Data;
        Assert.Equal(ids, data.Devices.Select(d => d.Id));
        Assert.All(data.Devices, d => Assert.Equal("static", d.ProfileId));
        Assert.All(data.Devices, d => Assert.Equal("192.0.2.20", d.Ip));
        Assert.Equal(vm.Devices.Select(d => d.WebUrl), data.Devices.Select(d => d.WebUrl));
        Assert.Empty(browser.Opened);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void DeviceEditsAndBothPendingEditorsAreIncludedInSaveAs()
    {
        var (vm, dialog, _) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        var id = vm.DeviceEditor.Id;
        vm.DeviceEditor.Name = "Renamed panel";
        vm.DeviceEditor.Notes = "Portable notes";
        vm.DeviceEditor.Manufacturer = "Example";
        vm.SelectedProfile = vm.Profiles[0];
        vm.Editor.Name = "Renamed profile";
        dialog.Save = FilePath;
        vm.SaveAsCommand.Execute(null);
        Assert.Empty(vm.Error);
        var data = new ProjectStore().Open(FilePath).Data;
        Assert.Equal(id, data.Devices[0].Id);
        Assert.Equal("Renamed panel", data.Devices[0].Name);
        Assert.Equal("Portable notes", data.Devices[0].Notes);
        Assert.Equal("Example", data.Devices[0].Manufacturer);
        Assert.Equal("Renamed profile", data.Profiles[0].Name);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void InvalidDeviceDraftAndDeclinedSelectionNewOpenExitStayIntact()
    {
        var (vm, dialog, _) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        var selected = vm.SelectedDevice;
        var id = vm.DeviceEditor.Id;
        vm.DeviceEditor.WebUrl = "file:///C:/NEVER_ECHO";
        vm.KeepDeviceCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.DoesNotContain("NEVER_ECHO", vm.Error);
        Assert.Equal(id, vm.DeviceEditor.Id);
        Assert.StartsWith("https:", vm.Devices[0].WebUrl);
        vm.SelectedDevice = null;
        vm.NewDeviceCommand.Execute(null);
        vm.NewCommand.Execute(null);
        vm.OpenCommand.Execute(null);
        Assert.Same(selected, vm.SelectedDevice);
        Assert.Equal(id, vm.DeviceEditor.Id);
        Assert.Equal("file:///C:/NEVER_ECHO", vm.DeviceEditor.WebUrl);
        Assert.False(vm.CanDiscard());
        dialog.Save = Path.Combine(_root, "invalid.json");
        vm.SaveAsCommand.Execute(null);
        Assert.False(File.Exists(dialog.Save));
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void SearchAndGroupingDoNotDiscardHiddenDraftOrMarkSavedDocumentDirty()
    {
        var (vm, _, _) = OpenSample();
        vm.DeviceSearch = "missing";
        vm.DeviceGroupBy = "profile";
        Assert.True(vm.NoDeviceMatches);
        Assert.False(vm.HasUnsavedChanges);
        vm.DeviceSearch = "STATIC NETWORK";
        // Sample profile name is read from its current record, never cached.
        vm.DeviceSearch = vm.Profiles[0].Name.ToUpperInvariant();
        Assert.Single(vm.DeviceView.Cast<DeviceDirectoryEntry>());
        vm.SelectedDevice = vm.Devices[0];
        vm.DeviceEditor.Name = "Draft";
        var id = vm.DeviceEditor.Id;
        vm.DeviceSearch = "no match";
        Assert.Equal(id, vm.DeviceEditor.Id);
        Assert.Equal("Draft", vm.DeviceEditor.Name);
        Assert.NotNull(vm.SelectedDevice);
        vm.DeviceSearch = "#!/MAIN";
        Assert.Single(vm.DeviceView.Cast<DeviceDirectoryEntry>());
    }

    [Fact]
    public void SameNamedProfilesKeepSeparateGroupsAndSearchRefreshesAfterRename()
    {
        var (vm, _, _) = OpenSample();
        vm.SelectedProfile = vm.Profiles[1];
        vm.Editor.Name = vm.Profiles[0].Name;
        vm.KeepProfileCommand.Execute(null);
        vm.NewDeviceCommand.Execute(null);
        vm.DeviceEditor.Name = "Second";
        vm.DeviceEditor.WebUrl = "https://198.51.100.20/";
        vm.DeviceEditor.ProfileId = vm.Profiles[1].Id;
        vm.KeepDeviceCommand.Execute(null);
        vm.DeviceGroupBy = "profile";
        Assert.Equal(2, vm.DeviceView.Groups!.Count);
        Assert.Equal(2, vm.DeviceView.Cast<DeviceDirectoryEntry>().Select(r => r.Group.Id).Distinct().Count());
        vm.Editor.Name = "Unique new name";
        vm.KeepProfileCommand.Execute(null);
        vm.DeviceSearch = "unique new";
        Assert.Equal("Second", Assert.Single(vm.DeviceView.Cast<DeviceDirectoryEntry>()).Device.Name);
    }

    [Fact]
    public void FailedSaveRetainsDeviceAndDraftIdentityAndOriginalFile()
    {
        new ProjectStore().Save(ProjectJsonTests.Sample(), FilePath);
        var original = File.ReadAllBytes(FilePath);
        var dialog = new Dialogs { Open = FilePath, Save = FilePath };
        var vm = new ProjectViewModel(dialog, new ProjectStore(_ => throw new IOException()), new Browser());
        vm.OpenCommand.Execute(null);
        vm.SelectedDevice = vm.Devices[0];
        var id = vm.DeviceEditor.Id;
        vm.DeviceEditor.Name = "Retained draft";
        vm.SaveAsCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.True(vm.HasUnsavedChanges);
        Assert.Equal(id, vm.DeviceEditor.Id);
        Assert.Equal("Retained draft", vm.DeviceEditor.Name);
        Assert.Equal(original, File.ReadAllBytes(FilePath));
    }

    [Fact]
    public void ConfirmedDiscardAndOpenReplaceDraftOnlyAfterUserDecision()
    {
        var (vm, dialog, browser) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        var id = vm.DeviceEditor.Id;
        vm.DeviceEditor.Name = "Draft";
        dialog.Discard = true;
        vm.NewDeviceCommand.Execute(null);
        Assert.Null(vm.SelectedDevice);
        Assert.NotEqual(id, vm.DeviceEditor.Id);
        Assert.Empty(vm.DeviceEditor.Name);
        vm.DeviceEditor.Name = "New draft";
        vm.OpenCommand.Execute(null);
        Assert.Null(vm.SelectedDevice);
        Assert.Single(vm.Devices);
        Assert.Empty(vm.DeviceEditor.Name);
        Assert.False(vm.HasUnsavedChanges);
        Assert.Empty(browser.Opened);
        Assert.Equal(0, dialog.OpenPrompts);
    }

    [Fact]
    public void DeleteIsConfirmedAndUnlinkAllowsExplicitProfileDeletion()
    {
        var (vm, dialog, _) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        vm.DeleteDeviceCommand.Execute(null);
        Assert.Single(vm.Devices);
        vm.SelectedProfile = vm.Profiles[0];
        dialog.Delete = true;
        vm.DeleteProfileCommand.Execute(null);
        Assert.Equal(2, vm.Profiles.Count);
        vm.UnlinkDeviceCommand.Execute(null);
        vm.KeepDeviceCommand.Execute(null);
        Assert.Null(vm.Devices[0].ProfileId);
        vm.DeleteProfileCommand.Execute(null);
        Assert.Single(vm.Profiles);
        vm.DeleteDeviceCommand.Execute(null);
        Assert.Empty(vm.Devices);
        Assert.Null(vm.SelectedDevice);
        Assert.False(vm.DeleteDeviceCommand.CanExecute(null));
        Assert.False(vm.OpenDeviceCommand.CanExecute(null));
    }

    [Fact]
    public void PendingDeviceReferenceAlsoProtectsProfileFromDeletion()
    {
        var (vm, dialog, _) = OpenSample();
        vm.DeviceEditor.ProfileId = vm.Profiles[1].Id;
        vm.SelectedProfile = vm.Profiles[1];
        dialog.Delete = true;
        vm.DeleteProfileCommand.Execute(null);
        Assert.Equal(2, vm.Profiles.Count);
        Assert.NotEmpty(vm.Error);
    }

    [Fact]
    public void StaleDraftReferenceBlocksSaveAndOpenUntilExplicitRelinkOrUnlink()
    {
        var (vm, dialog, browser) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        vm.DeviceEditor.ProfileId = "gone";
        vm.OpenDeviceCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.Equal(0, dialog.OpenPrompts);
        Assert.Empty(browser.Opened);
        Assert.Equal("gone", vm.DeviceEditor.ProfileId);
        vm.KeepDeviceCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        vm.DeviceEditor.ProfileId = vm.Profiles[1].Id;
        vm.KeepDeviceCommand.Execute(null);
        Assert.Empty(vm.Error);
        Assert.Equal(vm.Profiles[1].Id, vm.Devices[0].ProfileId);
        vm.UnlinkDeviceCommand.Execute(null);
        vm.KeepDeviceCommand.Execute(null);
        Assert.Empty(vm.Error);
        Assert.Null(vm.Devices[0].ProfileId);
    }

    [Fact]
    public void OpenRequiresSelectionAndConfirmationAndUsesExactPendingDestination()
    {
        var (vm, dialog, browser) = OpenSample();
        vm.OpenDeviceCommand.Execute(null);
        Assert.Equal(0, dialog.OpenPrompts);
        vm.SelectedDevice = vm.Devices[0];
        var url = "http://198.51.100.21:8443/a%2Fb/panel?view=main&lang=en#!/main";
        vm.DeviceEditor.WebUrl = url;
        vm.UnlinkDeviceCommand.Execute(null);
        vm.OpenDeviceCommand.Execute(null);
        Assert.Empty(browser.Opened);
        Assert.Equal(url, dialog.Destination);
        Assert.True(dialog.Http);
        Assert.True(dialog.Mismatch);
        Assert.True(dialog.Unlinked);
        dialog.Confirm = true;
        vm.OpenDeviceCommand.Execute(null);
        Assert.Equal(url, Assert.Single(browser.Opened));
        Assert.NotEmpty(vm.DeviceNotice);
        Assert.True(vm.HasUnsavedChanges);
        Assert.NotEqual(url, vm.Devices[0].WebUrl); // Opening never silently keeps edits.
        Assert.Equal("192.0.2.20", vm.DeviceEditor.Ip);
    }

    [Fact]
    public void HttpsSameHostAndLinkedProfileDoNotRaiseUnrelatedWarnings()
    {
        var (vm, dialog, _) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        vm.OpenDeviceCommand.Execute(null);
        Assert.False(dialog.Http);
        Assert.False(dialog.Mismatch);
        Assert.False(dialog.Unlinked);
    }

    [Theory]
    [InlineData("file:///C:/NEVER_ECHO")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:NEVER_ECHO@192.0.2.20/")]
    [InlineData("https://192.0.2.20/?%2574oken=NEVER_ECHO")]
    [InlineData("https://192.0.2.20/#!/route?api-key=NEVER_ECHO")]
    [InlineData("https://192.0.2.20/#sessionid=NEVER_ECHO")]
    [InlineData("https://192.0.2.20/ bad")]
    [InlineData("https://192.0.2.20/#%0a")]
    public void UnsafeDraftCannotBePreviewedConfirmedLaunchedOrSaved(string url)
    {
        var (vm, dialog, browser) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        vm.DeviceEditor.WebUrl = url;
        dialog.Confirm = true;
        vm.OpenDeviceCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.DoesNotContain("NEVER_ECHO", vm.Error);
        Assert.DoesNotContain("NEVER_ECHO", vm.DeviceDestination);
        Assert.NotEqual(url, vm.DeviceDestination);
        Assert.Equal(0, dialog.OpenPrompts);
        Assert.Empty(browser.Opened);
        vm.KeepDeviceCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.NotEqual(url, vm.Devices[0].WebUrl);
    }

    [Fact]
    public void LauncherFailureIsRedactedAndRetainsDraft()
    {
        var (vm, dialog, browser) = OpenSample();
        vm.SelectedDevice = vm.Devices[0];
        vm.DeviceEditor.Name = "Draft";
        dialog.Confirm = true;
        browser.Error = new Win32Exception("NEVER_ECHO");
        vm.OpenDeviceCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.DoesNotContain("NEVER_ECHO", vm.Error);
        Assert.Empty(vm.DeviceNotice);
        Assert.Equal("Draft", vm.DeviceEditor.Name);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void DefaultLauncherPassesOriginalStringAsDocumentWithoutCommandArguments()
    {
        var url = "https://192.0.2.20:8443/a%2Fb?view=main&lang=en#!/main";
        ProcessStartInfo? captured = null;
        var launcher = new DefaultBrowserLauncher(info => { captured = info; return null; });
        launcher.Open(url); // A shell handoff can succeed without a new process handle.
        Assert.NotNull(captured);
        Assert.Equal(url, captured.FileName);
        Assert.True(captured.UseShellExecute);
        Assert.Empty(captured.Arguments);
        Assert.Empty(captured.ArgumentList);
        captured = null;
        Assert.Throws<ProjectValidationException>(() => launcher.Open("file:///C:/bad"));
        Assert.Null(captured);
    }

    [Fact]
    public void ApplyAndOpenIsSeparateAndFreezesLinkedIntentWithoutKeepingDrafts()
    {
        var (vm, dialog, browser) = OpenSample();
        int applyRequests = 0; ProjectProfile? requested = null; ProjectDevice? device = null;
        vm.ApplyAndOpenRequested += (p, d) => { applyRequests++; requested = p; device = d; };
        vm.SelectedDevice = vm.Devices[0];
        Assert.Equal(0, applyRequests);
        dialog.Confirm = true; vm.OpenDeviceCommand.Execute(null);
        Assert.Equal(0, applyRequests); Assert.Single(browser.Opened);
        vm.DeviceEditor.Name = "Pending device name";
        vm.ApplyAndOpenCommand.Execute(null);
        Assert.Empty(vm.Error); Assert.Equal(1, applyRequests);
        Assert.Equal(vm.DeviceEditor.ProfileId, requested!.Id); Assert.Equal("Pending device name", device!.Name);
        Assert.NotEqual(device.Name, vm.Devices[0].Name); Assert.True(vm.HasUnsavedChanges);
        vm.DeviceEditor.ProfileId = ""; vm.ApplyAndOpenCommand.Execute(null);
        Assert.NotEmpty(vm.Error); Assert.Equal(1, applyRequests);
    }

    [Fact]
    public void PendingRelatedProfileEditMustBeKeptExplicitlyBeforeApply()
    {
        var (vm, _, _) = OpenSample(); int requests = 0;
        vm.ApplyAndOpenRequested += (_, _) => requests++;
        vm.SelectedDevice = vm.Devices[0];
        vm.SelectedProfile = vm.Profiles.Single(p => p.Id == vm.DeviceEditor.ProfileId);
        vm.Editor.Name = "Pending profile"; vm.ApplyAndOpenCommand.Execute(null);
        Assert.NotEmpty(vm.Error); Assert.Equal(0, requests); Assert.Equal("Pending profile", vm.Editor.Name);
        vm.KeepProfileCommand.Execute(null); vm.ApplyAndOpenCommand.Execute(null);
        Assert.Empty(vm.Error); Assert.Equal(1, requests);
    }
}
