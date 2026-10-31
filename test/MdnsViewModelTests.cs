using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public class MdnsViewModelTests
{
    [Fact]
    public async Task ConstructionRefreshSelectionAndFilteringNeverOpenSocket()
    {
        using var f = new SessionFixture(); var export = new Export();
        using var vm = new MdnsDiscoveryViewModel(f.Catalog, f.Session, export);
        Assert.Empty(vm.Interfaces); Assert.Null(vm.SelectedInterface); Assert.Equal(0, f.Factory.Opened);
        await vm.RefreshAsync(); Assert.Single(vm.Interfaces); Assert.Null(vm.SelectedInterface);
        Assert.False(vm.StartCommand.CanExecute(null)); Assert.False(vm.ExportCommand.CanExecute(null));
        vm.SelectedInterface = vm.Interfaces[0]; vm.Filter = "panel";
        Assert.True(vm.StartCommand.CanExecute(null)); Assert.Equal(0, f.Factory.Opened); Assert.Equal(0, export.Calls);
        await vm.StartAsync(); Assert.False(vm.CanSelect); Assert.True(vm.StopCommand.CanExecute(null));
        vm.SelectedInterface = null; Assert.NotNull(vm.SelectedInterface);
        await vm.RefreshAsync(); Assert.Equal(1, f.Factory.Opened);
        await vm.StopAsync(); Assert.True(vm.CanSelect);
        await vm.RefreshAsync(); Assert.Null(vm.SelectedInterface); Assert.False(vm.StartCommand.CanExecute(null));
    }
    [Fact]
    public async Task ResultSelectionFilteringAndExplicitExportCannotLaunchOrChangeNic()
    {
        using var f = new SessionFixture(); var export = new Export();
        using var vm = new MdnsDiscoveryViewModel(f.Catalog, f.Session, export);
        await vm.RefreshAsync(); vm.SelectedInterface = vm.Interfaces[0]; await vm.StartAsync();
        f.Factory.Last!.Enqueue(MdnsFixture.Datagram());
        await f.Eventually(() => f.Factory.Last.ReadCount >= 1);
        f.Clock.Advance(TimeSpan.FromSeconds(2)); f.Factory.Last.Enqueue(null);
        await f.Eventually(() => f.Session.Snapshot.Services.Length != 0);
        vm.Poll(); var originalRow = Assert.Single(vm.Rows); var originalCollection = vm.Rows; Assert.Equal(0, export.Calls);
        f.Clock.Advance(TimeSpan.FromSeconds(2)); f.Factory.Last.Enqueue(null);
        await f.Eventually(() => f.Session.Snapshot.Services[0].Ttl < originalRow.Service.Ttl);
        vm.Poll(); Assert.Same(originalCollection, vm.Rows); Assert.Same(originalRow, Assert.Single(vm.Rows));
        vm.Filter = "no match"; Assert.Empty(vm.Rows); Assert.False(vm.ExportCommand.CanExecute(null));
        vm.Filter = "PANEL"; vm.ExportCommand.Execute(null);
        Assert.Equal(1, export.Calls); Assert.Contains("192.0.2.44", export.Csv); Assert.Equal(1, f.Factory.Opened);
        await vm.StopAsync(); Assert.True(Assert.Single(vm.Rows).Service.Stale);
        vm.ExportCommand.Execute(null); Assert.Contains("Stopped", export.Csv);
        export.Fail = true; vm.ExportCommand.Execute(null); Assert.DoesNotContain("NEVER_ECHO", vm.Notice);
        var notice = vm.Notice; vm.Poll(); Assert.NotEmpty(notice); Assert.Equal(notice, vm.Notice);
    }
    [Fact]
    public async Task InterfaceLossRequiresFreshSelectionAndDisposeCancelsPendingReceiver()
    {
        using var f = new SessionFixture(); using var vm = new MdnsDiscoveryViewModel(f.Catalog, f.Session, new Export());
        await vm.RefreshAsync(); vm.SelectedInterface = vm.Interfaces[0]; await vm.StartAsync();
        f.Catalog.Current = null; f.Clock.Advance(TimeSpan.FromSeconds(2)); f.Factory.Last!.Enqueue(null);
        await f.Eventually(() => !f.Session.IsActive); vm.Poll();
        Assert.Null(vm.SelectedInterface); Assert.False(vm.StartCommand.CanExecute(null));
        f.Catalog.Current = MdnsFixture.Nic; await vm.RefreshAsync(); vm.SelectedInterface = vm.Interfaces[0]; await vm.StartAsync();
        vm.Dispose(); await f.Eventually(() => !f.Session.IsActive);
        Assert.True(f.Factory.Last!.Disposed); Assert.False(vm.StartCommand.CanExecute(null));
    }
    [Fact]
    public async Task CatalogFailureIsRedactedAndCloseDuringRefreshCannotReviveWindow()
    {
        using var f = new SessionFixture(); using var vm = new MdnsDiscoveryViewModel(f.Catalog, f.Session, new Export());
        f.Catalog.Fail = true; await vm.RefreshAsync(); Assert.Empty(vm.Interfaces); Assert.DoesNotContain("NEVER_ECHO", vm.Notice);
        Assert.True(vm.HasNotice); var notice = vm.Notice; vm.Poll(); Assert.Equal(notice, vm.Notice);
        vm.Filter = new string('x', 1024); Assert.Equal(256, vm.Filter.Length);
        vm.Dispose(); await vm.RefreshAsync(); Assert.False(vm.CanSelect);
    }
    private sealed class Export : IMdnsExport
    {
        internal int Calls; internal string Csv = ""; internal bool Fail;
        public bool Save(string csv) { Calls++; if (Fail) throw new Exception("NEVER_ECHO"); Csv = csv; return true; }
    }
}
