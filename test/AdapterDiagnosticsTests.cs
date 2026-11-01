using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

public class AdapterDiagnosticsTests
{
    internal static readonly Guid Ethernet = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid Wifi = Guid.Parse("10000000-0000-0000-0000-000000000002");
    internal static DiagnosticAdapter Adapter(Guid? id = null, string kind = "ethernet", bool? uplink = false) => new(
        id ?? Ethernet, id == Wifi ? 8 : 7, kind, "Reported fixture", "001122334455", kind, kind != "virtual", uplink,
        "Up", "1 Gbps", "Disabled", [new("192.0.2.7", 24, "Preferred", false), new("192.0.2.8", 24, "Preferred", false)],
        ["192.0.2.53"], [new("192.0.2.0/24", "0.0.0.0", 256)]);

    [Fact]
    public async Task SourceIsExplicitAfterRefreshAndNoTrafficOnSelection()
    {
        var fake = new DiagnosticFake(); using var vm = new AdapterDiagnosticsViewModel(fake, new(fake));
        Assert.Empty(vm.Adapters); Assert.Equal(0, fake.Reads);
        await vm.RefreshAsync(); Assert.Null(vm.SelectedAdapter); Assert.False(vm.CanPing);
        vm.SelectedAdapter = vm.Adapters[0]; Assert.Null(vm.Source); Assert.Equal(2, vm.Sources.Count);
        vm.Source = vm.Sources[1]; vm.Target = "192.0.2.229"; Assert.True(vm.CanPing);
        await vm.RefreshAsync(); Assert.Equal(Ethernet, vm.SelectedAdapter!.Id); Assert.Null(vm.Source);
        Assert.Equal(0, fake.Sends); Assert.Equal(0, fake.Resolves);
    }
    [Fact]
    public async Task FilterHidesOnlyConfirmedAndNeverSelectsReplacement()
    {
        var fake = new DiagnosticFake { Adapters = [Adapter(uplink: true), Adapter(Wifi, "wifi", null)] };
        bool? saved = null; using var vm = new AdapterDiagnosticsViewModel(fake, new(fake), saveFilter: b => saved = b);
        await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0];
        vm.HideHyperVUplinks = true;
        Assert.True(saved); Assert.Single(vm.Adapters); Assert.Null(vm.SelectedAdapter); Assert.Null(vm.Source);
        Assert.Null(vm.Adapters[0].HyperVUplink); Assert.Equal(2, fake.Adapters.Count);
        vm.HideHyperVUplinks = false; Assert.Equal(2, vm.Adapters.Count); Assert.Null(vm.SelectedAdapter);
        using var restored = new AdapterDiagnosticsViewModel(fake, new(fake), hideUplinks: true);
        await restored.RefreshAsync(); Assert.Single(restored.Adapters); Assert.Equal(0, fake.Sends);
    }
    [Fact]
    public async Task MissingAdapterOrFailedRefreshClearsStaleSource()
    {
        var fake = new DiagnosticFake(); using var vm = new AdapterDiagnosticsViewModel(fake, new(fake));
        await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0];
        fake.Adapters = []; await vm.RefreshAsync(); Assert.Null(vm.SelectedAdapter); Assert.Empty(vm.Sources);
        fake.ReadError = true; await vm.RefreshAsync(); Assert.Empty(vm.Adapters); Assert.False(vm.CanPing); Assert.True(vm.CanSelect);
    }
    [Fact]
    public async Task TentativeDuplicateAndSkipAsSourceAddressesNotOffered()
    {
        var fake = new DiagnosticFake { Adapters = [Adapter() with { Addresses = [new("192.0.2.1", 24, "Tentative", false),
            new("192.0.2.2", 24, "Duplicate", false), new("192.0.2.3", 24, "Preferred", true)] }] };
        using var vm = new AdapterDiagnosticsViewModel(fake, new(fake)); await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0];
        Assert.Empty(vm.Sources); vm.Source = "192.0.2.3"; Assert.Null(vm.Source); Assert.False(vm.CanPing);
    }
    [Fact]
    public async Task InFlightRequestCannotBeRetargetedOrRepeated()
    {
        var fake = new DiagnosticFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var vm = new AdapterDiagnosticsViewModel(fake, new(fake)); await Ready(vm);
        var first = vm.PingAsync(); await fake.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(vm.CanSelect); vm.Target = "198.51.100.8"; vm.Source = "192.0.2.8"; vm.SelectedAdapter = null;
        await vm.PingAsync(); await vm.RefreshAsync();
        Assert.Equal("192.0.2.229", vm.Target); Assert.Equal("192.0.2.7", vm.Source); Assert.NotNull(vm.SelectedAdapter);
        fake.Gate.SetResult(new(0, "192.0.2.229", 2)); await first;
        Assert.Single(vm.Results); Assert.Equal("192.0.2.7", vm.Results[0].Request.Source);
        Assert.Equal(1, fake.Sends); Assert.Equal(1, fake.Reads); Assert.True(vm.CanSelect);
    }
    [Fact]
    public async Task DisposeCancelsFutureProbesAndDropsLateUiResult()
    {
        var fake = new DiagnosticFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var vm = new AdapterDiagnosticsViewModel(fake, new(fake)); await Ready(vm);
        var run = vm.PingAsync(4); await fake.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.Dispose(); fake.Gate.SetResult(new(0, "192.0.2.229", 1)); await run;
        Assert.Empty(vm.Results); Assert.Equal(1, fake.Sends); Assert.False(vm.CanSelect);
    }
    [Fact]
    public async Task StopTerminatesSeriesAndPreservesRequestIdentity()
    {
        var fake = new DiagnosticFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var vm = new AdapterDiagnosticsViewModel(fake, new(fake)); await Ready(vm);
        var run = vm.PingAsync(4); await fake.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.StopCommand.Execute(null); fake.Gate.SetResult(new(0, "192.0.2.229", 1)); await run;
        Assert.Single(vm.Results); Assert.Equal("Cancelled", vm.Results[0].Error); Assert.Equal(1, fake.Sends); Assert.True(vm.CanSelect);
    }
    internal static async Task Ready(AdapterDiagnosticsViewModel vm)
    { await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0]; vm.Target = "192.0.2.229"; }

    [Theory]
    [InlineData("ethernet")][InlineData("wifi")][InlineData("usb")][InlineData("virtual")][InlineData("hyperv")][InlineData("unknown")]
    public void ParseRetainsReportedIdentityAndType(string kind)
    {
        var expected = Adapter(kind: kind, uplink: null);
        var result = WindowsAdapterDiagnostics.Parse(JsonSerializer.Serialize(new[] { expected }));
        Assert.Single(result); Assert.Equal(expected.Id, result[0].Id); Assert.Equal(kind, result[0].Kind);
        Assert.Equal(2, result[0].Addresses.Length); Assert.Null(result[0].HyperVUplink);
    }
    [Theory]
    [InlineData("null")][InlineData("{}")] [InlineData("[{}]")]
    public void ParseRejectsIncompleteSnapshots(string json) => Assert.ThrowsAny<Exception>(() => WindowsAdapterDiagnostics.Parse(json));
    [Fact]
    public void ParseRejectsAmbiguousAndOversizeSnapshots()
    {
        Assert.ThrowsAny<Exception>(() => WindowsAdapterDiagnostics.Parse(JsonSerializer.Serialize(new[] { Adapter(), Adapter() })));
        Assert.ThrowsAny<Exception>(() => WindowsAdapterDiagnostics.Parse(new string(' ', 2_000_001)));
        Assert.ThrowsAny<Exception>(() => WindowsAdapterDiagnostics.Parse(JsonSerializer.Serialize(new[] { Adapter() with { Kind = "named-vEthernet" } })));
    }
    [Fact]
    public void NativeRouteLayoutMatchesWindowsAbiWithoutCallingOs()
    {
        Assert.Equal(28, Marshal.SizeOf<WindowsDiagnosticPingPlatform.Sockaddr>());
        Assert.Equal(32, Marshal.SizeOf<WindowsDiagnosticPingPlatform.Prefix>());
        Assert.Equal(104, Marshal.SizeOf<WindowsDiagnosticPingPlatform.ForwardRow>());
        Assert.Equal(44, (int)Marshal.OffsetOf<WindowsDiagnosticPingPlatform.ForwardRow>("NextHop"));
        Assert.Equal(84, (int)Marshal.OffsetOf<WindowsDiagnosticPingPlatform.ForwardRow>("Metric"));
        Assert.Equal(56, Marshal.SizeOf<WindowsDiagnosticPingPlatform.RouteSelectionRow>());
        Assert.Equal(16, (int)Marshal.OffsetOf<WindowsDiagnosticPingPlatform.RouteSelectionRow>("Index"));
    }
    [Fact]
    public void EchoHeaderRetainsRttResponderAndTtlWithoutDereferencingPointers()
    {
        var buffer = Marshal.AllocHGlobal(64);
        try
        {
            for (int i = 0; i < 64; i++) Marshal.WriteByte(buffer, i, 0);
            Marshal.WriteInt32(buffer, 0, BitConverter.ToInt32(IPAddress.Parse("192.0.2.229").GetAddressBytes()));
            Marshal.WriteInt32(buffer, 8, 12); Marshal.WriteByte(buffer, IntPtr.Size == 8 ? 24 : 20, 63);
            var echo = WindowsDiagnosticPingPlatform.ReadEcho(buffer);
            Assert.Equal("192.0.2.229", echo.ReplyAddress); Assert.Equal(12, echo.Milliseconds); Assert.Equal(63, echo.Ttl);
            Assert.Equal("Success", echo.StatusDisplay);
            Marshal.WriteInt32(buffer, 4, 11013);
            Assert.Null(WindowsDiagnosticPingPlatform.ReadEcho(buffer).Ttl);
            Assert.Equal("TtlExpired", WindowsDiagnosticPingPlatform.ReadEcho(buffer).StatusDisplay);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    [Fact]
    public void LargeSnapshotAndLimitAreBoundedWithoutHostEnumeration()
    {
        var adapters = Enumerable.Range(1, 256).Select(i => Adapter(Guid.Parse($"10000000-0000-0000-0000-{i:000000000000}"))
            with { Index = i }).ToArray();
        Assert.Equal(256, WindowsAdapterDiagnostics.Parse(JsonSerializer.Serialize(adapters)).Count);
        Assert.ThrowsAny<Exception>(() => WindowsAdapterDiagnostics.Parse(JsonSerializer.Serialize(adapters.Append(Adapter(Wifi)))));
    }
}

[Collection("StaticState")]
public class DiagnosticPreferenceTests : IDisposable
{
    private readonly TestDataDirectoryFixture _directory = new();
    public void Dispose() => _directory.Dispose();
    [Fact]
    public void OptInPreferenceSurvivesRestartAndPreservesOtherSettings()
    {
        var path = Path.Combine(_directory.Root, "app_settings.json");
        File.WriteAllText(path, "{\"Theme\":\"Dark\",\"Language\":\"en\",\"FutureKey\":\"keep\"}");
        var before = File.ReadAllBytes(path);
        using (var vm = AdapterDiagnosticsViewModel.Create())
        {
            Assert.False(vm.HideHyperVUplinks); Assert.Equal(before, File.ReadAllBytes(path));
            Assert.Empty(vm.Adapters); vm.HideHyperVUplinks = true;
        }
        using var restored = AdapterDiagnosticsViewModel.Create(); Assert.True(restored.HideHyperVUplinks);
        Assert.Equal("Dark", NetworkProfileManager.Helpers.AppSettingsStore.Get("Theme"));
        Assert.Equal("en", NetworkProfileManager.Helpers.AppSettingsStore.Get("Language"));
        Assert.Equal("keep", NetworkProfileManager.Helpers.AppSettingsStore.Get("FutureKey"));
    }
}

internal sealed class DiagnosticFake : IAdapterDiagnostics, IDiagnosticPingPlatform
{
    internal IReadOnlyList<DiagnosticAdapter> Adapters = [AdapterDiagnosticsTests.Adapter()];
    internal IReadOnlyList<PingBinding> Bindings = [new(AdapterDiagnosticsTests.Ethernet, 7, "00-11-22-33-44-55", true, ["192.0.2.7", "192.0.2.8"])];
    internal PredictedRoute? Route = new(7, "192.0.2.0", 24, "0.0.0.0", 256);
    internal int Reads, Sends, Resolves, BindingReads, Predictions;
    internal bool ReadError, ThrowSend;
    internal Action<int>? OnRead, OnPredict;
    internal TaskCompletionSource<DiagnosticEcho>? Gate;
    internal TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal IPAddress[] Resolved = [IPAddress.Parse("192.0.2.229")];
    internal DiagnosticEcho Echo = new(0, "192.0.2.229", 2);
    public Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken token)
    { Reads++; if (ReadError) throw new IOException(); return Task.FromResult(Adapters); }
    public IReadOnlyList<PingBinding> ReadBindings() { BindingReads++; OnRead?.Invoke(BindingReads); return Bindings; }
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken token) { Resolves++; return Task.FromResult(Resolved); }
    public PredictedRoute? Predict(IPAddress source, IPAddress target) { Predictions++; OnPredict?.Invoke(Predictions); return Route; }
    public Task<DiagnosticEcho> EchoAsync(IPAddress source, IPAddress target, CancellationToken token)
    { Sends++; Entered.TrySetResult(); if (ThrowSend) throw new IOException(); return Gate?.Task ?? Task.FromResult(Echo); }
}
