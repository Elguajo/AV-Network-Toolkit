using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class DeviceModalTests
{
    private sealed class Dialogs : IProjectDialogs
    {
        public string? OpenPath() => null;
        public string? SavePath(string? folder) => null;
        public string? ChooseFolder() => null;
        public bool ConfirmDiscard() => false;
        public bool ConfirmDelete() => true;
        public bool ConfirmDeleteDevice() => true;
        public bool ConfirmOpenDevice(string destination, bool isHttp, bool hostMismatch, bool unlinked) => false;
        public bool ConfirmOverwrite(string path) => false;
    }
    private sealed class Browser : IBrowserLauncher
    {
        public int Opens;
        public void Open(string destination) => Opens++;
    }
    private static ProjectDeviceEditor Draft(string name = "Control panel") => new(new ProjectDevice
    { Name = name, WebUrl = "http://192.0.2.20/panel?view=main#!/status" });

    [Fact]
    public void DetachedDraftChangesDoNotTouchProjectUntilCommitted()
    {
        var browser = new Browser();
        var vm = new ProjectViewModel(new Dialogs(), new ProjectStore(), browser);
        var draft = Draft();
        draft.Name = "Unsaved edit";
        draft.SavePassword = true; draft.Password = "SYNTHETIC-ONLY";
        Assert.Empty(vm.Devices);
        Assert.False(vm.HasUnsavedChanges);
        Assert.False(vm.DeviceEditor.SavePassword);
        Assert.Equal(0, browser.Opens);
        Assert.True(vm.TryStoreDevice(draft));
        Assert.Equal("Unsaved edit", Assert.Single(vm.Devices).Name);
        Assert.True(vm.HasUnsavedChanges);
        Assert.Equal(0, browser.Opens);
    }

    [Fact]
    public void AddManySelectAndEditKeepsStableIdentityAndShowsFilteredAddition()
    {
        var vm = new ProjectViewModel(new Dialogs());
        for (int i = 0; i < 24; i++) Assert.True(vm.TryStoreDevice(Draft("Panel " + i)));
        Assert.Equal(24, vm.Devices.Select(d => d.Id).Distinct().Count());
        var first = vm.Devices[0];
        vm.SelectedDevice = first;
        vm.DeviceSearch = "nothing matches";
        var draft = new ProjectDeviceEditor(first) { Name = "Edited panel" };
        Assert.True(vm.TryStoreDevice(draft));
        Assert.Equal(24, vm.Devices.Count);
        Assert.Equal(first.Id, vm.SelectedDevice!.Id);
        Assert.Equal("Edited panel", vm.SelectedDevice.Name);
        Assert.Equal("", vm.DeviceSearch);
        Assert.False(vm.NoDeviceMatches);
        Assert.True(vm.HasSelectedDevice);
        Assert.False(vm.HasNoSelectedDevice);
    }

    [Theory]
    [InlineData("", "http://192.0.2.20")]
    [InlineData("Panel", "file:///C:/SENTINEL")]
    [InlineData("Panel", "javascript:SENTINEL")]
    [InlineData("Panel", "https://device.example/?password=SENTINEL")]
    public void InvalidDraftCannotReplaceSelectedDevice(string name, string url)
    {
        var vm = new ProjectViewModel(new Dialogs());
        Assert.True(vm.TryStoreDevice(Draft()));
        var original = vm.SelectedDevice!;
        var draft = new ProjectDeviceEditor(original) { Name = name, WebUrl = url };
        Assert.False(vm.TryStoreDevice(draft));
        Assert.Same(original, vm.SelectedDevice);
        Assert.Same(original, Assert.Single(vm.Devices));
        Assert.DoesNotContain("SENTINEL", vm.Error);
        Assert.Equal(url, draft.WebUrl);
    }

    [Fact]
    public void MissingProfileAndInvalidPasswordFailWithoutLosingOriginal()
    {
        var vm = new ProjectViewModel(new Dialogs());
        Assert.True(vm.TryStoreDevice(Draft()));
        var original = vm.SelectedDevice!;
        var draft = new ProjectDeviceEditor(original) { ProfileId = "missing" };
        Assert.False(vm.TryStoreDevice(draft));
        draft.ProfileId = "";
        draft.SavePassword = true;
        Assert.False(vm.TryStoreDevice(draft));
        Assert.Same(original, vm.SelectedDevice);
        Assert.Null(original.Password);
        draft.Password = "SYNTHETIC-ONLY";
        Assert.True(vm.TryStoreDevice(draft));
        Assert.Equal("SYNTHETIC-ONLY", vm.SelectedDevice!.Password);
        var remove = new ProjectDeviceEditor(vm.SelectedDevice);
        remove.RemovePassword();
        Assert.True(vm.TryStoreDevice(remove));
        Assert.Null(vm.SelectedDevice!.Password);
    }

    [Fact]
    public void ModalCannotOverwriteAnExistingPendingDeviceDraft()
    {
        var vm = new ProjectViewModel(new Dialogs());
        Assert.True(vm.TryStoreDevice(Draft()));
        vm.DeviceEditor.Name = "Pending legacy draft";
        Assert.False(vm.TryStoreDevice(Draft("New panel")));
        Assert.Single(vm.Devices);
        Assert.Equal("Pending legacy draft", vm.DeviceEditor.Name);
    }
}
