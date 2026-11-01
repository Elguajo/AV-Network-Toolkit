using System.Collections.Generic;
using System.Collections.ObjectModel;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public class MainViewModelTests : System.IDisposable
{
    // MainViewModel loads profiles and history on construction; keep that away
    // from the real %LOCALAPPDATA% data.
    private readonly TestDataDirectoryFixture _dir = new();
    public void Dispose() => _dir.Dispose();

    private static AdapterViewModel Adapter(string id) => new(
        new AdapterInfo { Id = id, Name = id },
        new ObservableCollection<ProfileViewModel>(),
        new ObservableCollection<HistoryEntry>());

    [Fact]
    public void FilterKeepsWorkingAndDisconnectedAdaptersAndDoesNotReloadOrLoseDrafts()
    {
        int reads = 0;
        var vm = new MainViewModel(() =>
        {
            reads++;
            return new() {
                new() { Id = "ethernet", Name = "Ethernet", IsConnected = false },
                new() { Id = "wifi", Name = "Wi-Fi", IsConnected = true },
                new() { Id = "vpn", Name = "Radmin VPN" },
                new() { Id = "hyperv", Name = "vEthernet" },
                new() { Id = "unknown", Name = "Unknown" },
                new() { Id = "filter", Name = "System filter", IsSystemInterface = true }
            };
        });
        Assert.Equal(6, vm.Adapters.Count);
        Assert.Equal(5, vm.VisibleAdapters.Cast<AdapterViewModel>().Count());
        var selected = vm.SelectedAdapter!;
        selected.EditIp = "192.0.2.42";
        vm.ShowAllAdapters = true;
        Assert.Equal(6, vm.VisibleAdapters.Cast<AdapterViewModel>().Count());
        vm.ShowAllAdapters = false;
        Assert.Same(selected, vm.SelectedAdapter);
        Assert.Equal("192.0.2.42", selected.EditIp);
        Assert.Equal(1, reads);
    }

    [Fact]
    public void HidingSelectedSystemInterfaceClearsTargetWithoutSelectingAnotherOnRefresh()
    {
        var vm = new MainViewModel(() => new() {
            new() { Id = "filter", Name = "System filter", IsSystemInterface = true },
            new() { Id = "ethernet", Name = "Ethernet" }
        });
        Assert.Equal("ethernet", vm.SelectedAdapter!.Info.Id);
        vm.ShowAllAdapters = true;
        vm.SelectedAdapter = vm.Adapters[0];
        vm.ShowAllAdapters = false;
        Assert.Null(vm.SelectedAdapter);
        vm.Refresh();
        Assert.Null(vm.SelectedAdapter);
        vm.ShowAllAdapters = true;
        Assert.Null(vm.SelectedAdapter);
        Assert.Equal(2, vm.Adapters.Count);
    }

    [Fact]
    public void RefreshPreservesGuidAcrossRenameAndClearsRemovedOrNewlyHiddenTarget()
    {
        var data = new List<AdapterInfo> {
            new() { Id = "one", Name = "Ethernet" }, new() { Id = "two", Name = "Other" }
        };
        var vm = new MainViewModel(() => data);
        var selected = vm.SelectedAdapter;
        data = new() { new() { Id = "one", Name = "Renamed" }, new() { Id = "two", Name = "Ethernet" } };
        vm.Refresh();
        Assert.Same(selected, vm.SelectedAdapter);
        Assert.Equal("Renamed", vm.SelectedAdapter!.Name);
        data[0].IsSystemInterface = true;
        vm.Refresh();
        Assert.Null(vm.SelectedAdapter);
        Assert.Single(vm.VisibleAdapters.Cast<AdapterViewModel>());
        vm.SelectedAdapter = vm.Adapters[1];
        data.RemoveAt(1); vm.Refresh();
        Assert.Null(vm.SelectedAdapter);
        Assert.Empty(vm.VisibleAdapters.Cast<AdapterViewModel>());
    }

    /// <summary>
    /// Refresh() re-selects the same adapter after every apply, revert and
    /// refresh. The window resets its page on this notification, so repeating
    /// it for the same adapter made the page jump to the top after each action.
    /// </summary>
    [Fact]
    public void SelectingTheSameAdapterAgain_DoesNotNotify()
    {
        var vm = new MainViewModel(() => new List<AdapterInfo>());
        var a = Adapter("a");
        vm.SelectedAdapter = a;

        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.SelectedAdapter = a;
        Assert.DoesNotContain(nameof(MainViewModel.SelectedAdapter), raised);

        vm.SelectedAdapter = Adapter("b");
        Assert.Contains(nameof(MainViewModel.SelectedAdapter), raised);
    }

    [Fact]
    public void ErrorText_WithoutContext_IsThePlainMessage()
        // The notification title already says "Error"; no second "Error:" prefix.
        => Assert.Equal("Access is denied.", MainViewModel.ErrorText(null, "Access is denied."));
}
