using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

public class DiagnosticStartupTests
{
    [Fact]
    public async Task BasicReadBecomesUsableWhileOptionalInventoryIsBlocked()
    {
        var reader = new EnrichmentReader(); var platform = new DiagnosticFake();
        using var vm = new AdapterDiagnosticsViewModel(reader, new(platform));
        await vm.RefreshAsync(); await reader.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(vm.CanSelect); Assert.False(vm.DetailsCompletion.IsCompleted);
        vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0]; vm.Target = "192.0.2.229";
        Assert.True(vm.CanRun); await vm.PingAsync(); Assert.Equal(1, platform.Sends);
        Assert.NotEmpty(vm.DetailsNotice);
        reader.Gates[0].SetResult([AdapterDiagnosticsTests.Adapter() with { Kind = "usb" }]); await vm.DetailsCompletion;
        Assert.Equal("192.0.2.7", vm.Source); Assert.Equal("usb", vm.SelectedAdapter!.Kind);
        Assert.Empty(vm.DetailsNotice);
    }

    [Fact]
    public async Task OptionalInventoryFailureDoesNotDisableBasicTestsOrExposeException()
    {
        var reader = new EnrichmentReader(); using var vm = new AdapterDiagnosticsViewModel(reader, new(new DiagnosticFake()));
        await vm.RefreshAsync(); await reader.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        reader.Gates[0].SetException(new IOException("PRIVATE_NEVER_ECHO")); await vm.DetailsCompletion;
        vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0]; vm.Target = "192.0.2.229";
        Assert.True(vm.CanRun); Assert.NotEmpty(vm.DetailsNotice); Assert.DoesNotContain("PRIVATE", vm.DetailsNotice);
    }

    [Fact]
    public async Task LateInventoryFromOlderRefreshCannotOverwriteNewRead()
    {
        var reader = new EnrichmentReader(); using var vm = new AdapterDiagnosticsViewModel(reader, new(new DiagnosticFake()));
        await vm.RefreshAsync(); await reader.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); var old = vm.DetailsCompletion;
        await vm.RefreshAsync(); await reader.SecondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(reader.Tokens[0].IsCancellationRequested);
        reader.Gates[1].SetResult([AdapterDiagnosticsTests.Adapter() with { Name = "Current" }]); await vm.DetailsCompletion;
        reader.Gates[0].SetResult([AdapterDiagnosticsTests.Adapter() with { Name = "Stale" }]); await old;
        Assert.Equal("Current", Assert.Single(vm.Adapters).Name);
    }

    [Theory]
    [InlineData("removed")][InlineData("index")][InlineData("mac")][InlineData("source")][InlineData("hidden")]
    public async Task InventoryClearsInvalidatedSourceWithoutChoosingAnother(string change)
    {
        var reader = new EnrichmentReader(); using var vm = new AdapterDiagnosticsViewModel(reader, new(new DiagnosticFake()), hideUplinks: true);
        await vm.RefreshAsync(); await reader.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0];
        var adapter = reader.Basic[0];
        adapter = change switch {
            "index" => adapter with { Index = 99 }, "mac" => adapter with { Mac = "001122334466" },
            "source" => adapter with { Addresses = [] }, "hidden" => adapter with { HyperVUplink = true }, _ => adapter };
        reader.Gates[0].SetResult(change == "removed" ? [] : [adapter]); await vm.DetailsCompletion;
        Assert.Null(vm.Source); Assert.False(vm.CanPing);
        if (change != "source") Assert.Null(vm.SelectedAdapter);
    }

    [Fact]
    public async Task DetailsWaitForInFlightProbeAndDoNotRewriteItsRequest()
    {
        var reader = new EnrichmentReader(); var platform = new DiagnosticFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var vm = new AdapterDiagnosticsViewModel(reader, new(platform)); await vm.RefreshAsync();
        await reader.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0]; vm.Target = "192.0.2.229";
        var run = vm.PingAsync(); await platform.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        reader.Gates[0].SetResult([reader.Basic[0] with { Name = "New name" }]); await vm.DetailsCompletion;
        Assert.NotEqual("New name", vm.SelectedAdapter!.Name);
        platform.Gate.SetResult(new(0, "192.0.2.229", 2)); await run;
        Assert.Equal("New name", vm.SelectedAdapter!.Name); Assert.Equal(reader.Basic[0].Name, vm.Results[0].Request.AdapterName);
    }

    [Fact]
    public async Task CloseCancelsDetailsAndDropsUncooperativeLateCompletion()
    {
        var reader = new EnrichmentReader(); var vm = new AdapterDiagnosticsViewModel(reader, new(new DiagnosticFake()));
        await vm.RefreshAsync(); await reader.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); vm.Dispose();
        Assert.True(reader.Tokens[0].IsCancellationRequested);
        reader.Gates[0].SetResult([]); await vm.DetailsCompletion;
        Assert.Single(vm.Adapters); Assert.False(vm.CanSelect);
    }

    [Fact]
    public void NativeTableLayoutsAndPaddedFirstEntryMatchWindowsAbi()
    {
        Assert.Equal(80, Marshal.SizeOf<WindowsQuickAdapterDiagnostics.UnicastRow>());
        Assert.Equal(40, (int)Marshal.OffsetOf<WindowsQuickAdapterDiagnostics.UnicastRow>("Index"));
        Assert.Equal(61, (int)Marshal.OffsetOf<WindowsQuickAdapterDiagnostics.UnicastRow>("SkipAsSource"));
        Assert.Equal(64, (int)Marshal.OffsetOf<WindowsQuickAdapterDiagnostics.UnicastRow>("DadState"));
        var ptr = Marshal.AllocHGlobal(168);
        try
        {
            Marshal.WriteInt32(ptr, 2);
            Marshal.StructureToPtr(Address(7, 4, false), IntPtr.Add(ptr, 8), false);
            Marshal.StructureToPtr(Address(8, 2, true), IntPtr.Add(ptr, 88), false);
            var rows = WindowsQuickAdapterDiagnostics.CopyRows<WindowsQuickAdapterDiagnostics.UnicastRow>(ptr, 2);
            Assert.Equal(7u, rows[0].Index); Assert.Equal(8u, rows[1].Index); Assert.Equal(1, rows[1].SkipAsSource);
            Assert.Throws<IOException>(() => WindowsQuickAdapterDiagnostics.CopyRows<WindowsQuickAdapterDiagnostics.UnicastRow>(ptr, 1));
            Marshal.WriteInt32(ptr, -1);
            Assert.Throws<IOException>(() => WindowsQuickAdapterDiagnostics.CopyRows<WindowsQuickAdapterDiagnostics.UnicastRow>(ptr, 3));
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    [Theory]
    [InlineData(0, "Invalid")][InlineData(1, "Tentative")][InlineData(2, "Duplicate")][InlineData(3, "Deprecated")][InlineData(4, "Preferred")][InlineData(99, "Unknown")]
    public void NativeFactsPreserveDadSkipSourceDnsAndRouteMetric(uint state, string expected)
    {
        var row = Address(7, state, true);
        var route = new WindowsDiagnosticPingPlatform.ForwardRow { Index = 7, Metric = 42,
            Prefix = new() { Address = Sock("0.0.0.0"), Length = 0 }, NextHop = Sock("192.0.2.1") };
        var adapter = WindowsQuickAdapterDiagnostics.Project(AdapterDiagnosticsTests.Ethernet, 7, "fixture", "virtual Ethernet", "001122334455", "Up", "1000 bps", true,
            ["192.0.2.53"], [row], [route]);
        Assert.Equal(expected, adapter.Addresses[0].State); Assert.True(adapter.Addresses[0].SkipAsSource);
        Assert.Equal("Enabled", adapter.Dhcp); Assert.Equal("192.0.2.53", adapter.Dns[0]);
        Assert.Equal(42, adapter.Routes[0].Metric); Assert.Equal("0.0.0.0/0", adapter.Routes[0].Destination);
        Assert.Null(adapter.Physical); Assert.Null(adapter.HyperVUplink); Assert.Equal("unknown", adapter.Kind);
    }
    [Fact]
    public async Task CancelledNativeReadNeverEntersHostEnumeration()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new WindowsQuickAdapterDiagnostics().ReadAsync(cancellation.Token));
    }
    [Theory]
    [InlineData(1, false, 0)][InlineData(2, false, 0)][InlineData(3, false, 0)][InlineData(4, true, 0)][InlineData(4, false, 1)]
    public async Task NativeSourceSelectionRequiresPreferredAndNonSkip(uint state, bool skip, int expectedSources)
    {
        var adapter = WindowsQuickAdapterDiagnostics.Project(AdapterDiagnosticsTests.Ethernet, 7, "fixture", "fixture", "001122334455", "Up", "1000 bps", false,
            [], [Address(7, state, skip)], []);
        var reader = new DiagnosticFake { Adapters = [adapter] };
        using var vm = new AdapterDiagnosticsViewModel(reader, new(reader)); await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0];
        Assert.Equal(expectedSources, vm.Sources.Count);
    }
    private static WindowsDiagnosticPingPlatform.Sockaddr Sock(string ip) => WindowsDiagnosticPingPlatform.Sockaddr.From(IPAddress.Parse(ip));
    private static WindowsQuickAdapterDiagnostics.UnicastRow Address(uint index, uint state, bool skip) => new()
        { Index = index, Address = Sock("192.0.2.7"), PrefixLength = 24, DadState = state, SkipAsSource = (byte)(skip ? 1 : 0) };

    internal sealed class EnrichmentReader : IAdapterDiagnosticsDetails
    {
        internal IReadOnlyList<DiagnosticAdapter> Basic = [AdapterDiagnosticsTests.Adapter()];
        internal TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously), SecondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<IReadOnlyList<DiagnosticAdapter>>[] Gates = [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
        internal CancellationToken[] Tokens = new CancellationToken[2];
        private int _calls;
        public Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken cancellation) => Task.FromResult(Basic);
        public Task<IReadOnlyList<DiagnosticAdapter>> ReadDetailsAsync(CancellationToken cancellation)
        {
            int call = Interlocked.Increment(ref _calls) - 1; Tokens[call] = cancellation;
            (call == 0 ? Entered : SecondEntered).TrySetResult(); return Gates[call].Task;
        }
    }
}
