using System.Net;
using System.Net.Sockets;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

public class DiagnosticTcpTests
{
    private static DiagnosticPingRequest Request(string target = "192.0.2.229") => new(AdapterDiagnosticsTests.Ethernet,
        7, "001122334455", "Ethernet fixture", "192.0.2.7", target);
    [Theory]
    [InlineData(1)][InlineData(80)][InlineData(443)][InlineData(65535)]
    public async Task ConnectsExactlyOnceToExplicitSourceDestinationAndPort(int port)
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake(); var request = Request();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(request, port, default);
        Assert.True(result.Success); Assert.Equal(request, result.Request); Assert.Equal(port, result.Port);
        Assert.Equal("192.0.2.7", connector.Source); Assert.Equal("192.0.2.229", connector.Target);
        Assert.Equal(port, connector.Port); Assert.Equal(1, connector.Connects); Assert.Equal(0, platform.Resolves);
        Assert.Equal(0, platform.Sends); Assert.Equal(3, platform.BindingReads); Assert.Equal(2, platform.Predictions);
    }
    [Theory]
    [InlineData(0)][InlineData(-1)][InlineData(65536)]
    public async Task InvalidPortNeverTouchesPlatform(int port)
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(), port, default);
        Assert.Equal("Input", result.Error); Assert.Equal(0, connector.Connects); Assert.Equal(0, platform.BindingReads);
    }
    [Theory]
    [InlineData("host;echo x")][InlineData("https://device")][InlineData("127.0.0.1")][InlineData("224.0.0.1")][InlineData("::1")]
    public async Task InvalidTargetCannotReachDnsOrSocket(string target)
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(target), 443, default);
        Assert.Equal("Input", result.Error); Assert.Equal(0, platform.Resolves); Assert.Equal(0, connector.Connects);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task MissingOrOtherAdapterRouteNeverConnects(bool missing)
    {
        var platform = new DiagnosticFake { Route = missing ? null : new(8, "0.0.0.0", 0, "198.51.100.1", 2) };
        var connector = new TcpFake();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(), 443, default);
        Assert.Equal("Route", result.Error); Assert.Equal(0, connector.Connects);
    }
    [Theory]
    [InlineData("missing")][InlineData("source")][InlineData("index")][InlineData("mac")][InlineData("down")][InlineData("duplicate")]
    public async Task ChangedOrAmbiguousBindingNeverConnects(string variant)
    {
        var platform = new DiagnosticFake(); var binding = platform.Bindings[0];
        platform.Bindings = variant switch
        {
            "missing" => [], "source" => [binding with { Sources = ["192.0.2.8"] }],
            "index" => [binding with { Index = 8 }], "mac" => [binding with { Mac = "FFFFFFFFFFFF" }],
            "down" => [binding with { Connected = false }],
            _ => [binding, binding with { Id = AdapterDiagnosticsTests.Wifi, Index = 8 }]
        };
        var connector = new TcpFake();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(), 443, default);
        Assert.Equal("Changed", result.Error); Assert.Equal(0, connector.Connects);
    }
    [Fact]
    public async Task HostnameUsesOneResolvedIpv4AndNeverRetriesOtherAddresses()
    {
        var platform = new DiagnosticFake { Resolved = [IPAddress.IPv6Loopback, IPAddress.Parse("192.0.2.10"), IPAddress.Parse("192.0.2.11")] };
        var connector = new TcpFake { Observation = new("Refused", 1) };
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request("panel.local"), 80, default);
        Assert.False(result.Success); Assert.Equal("", result.Error); Assert.Equal("Refused", result.Observation!.Status);
        Assert.Equal("192.0.2.10", connector.Target); Assert.Equal(1, platform.Resolves); Assert.Equal(1, connector.Connects);
    }
    [Fact]
    public async Task NoIpv4ResolutionNeverConnects()
    {
        var platform = new DiagnosticFake { Resolved = [IPAddress.IPv6Loopback] }; var connector = new TcpFake();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request("panel.local"), 443, default);
        Assert.Equal("Resolve", result.Error); Assert.Equal(0, connector.Connects);
    }
    [Fact]
    public async Task AdapterChangeDuringResolutionNeverConnects()
    {
        var platform = new DiagnosticFake(); platform.OnRead = n => { if (n == 2) platform.Bindings = []; };
        var connector = new TcpFake();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request("panel.local"), 443, default);
        Assert.Equal("Changed", result.Error); Assert.Equal(0, connector.Connects);
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task ChangeAfterConnectionRetainsObservationButCannotClaimSuccess(bool routeChange)
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake();
        connector.AfterConnect = () => { if (routeChange) platform.Route = null; else platform.Bindings = []; };
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(), 443, default);
        Assert.False(result.Success); Assert.Equal("Changed", result.Error); Assert.Equal("Accepted", result.Observation!.Status);
    }
    [Fact]
    public async Task StopCancelsConnectorWithoutRetry()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var cancel = new CancellationTokenSource();
        var run = new DiagnosticTcpService(platform, connector).CheckAsync(Request(), 443, cancel.Token);
        await connector.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); cancel.Cancel();
        var result = await run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Cancelled", result.Error); Assert.Equal(1, connector.Connects); Assert.True(connector.Cancelled);
    }
    [Fact]
    public async Task ExpiredOperationIsDistinctFromUserCancellation()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake { Expire = true };
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(), 443, default);
        Assert.Equal("Timeout", result.Error); Assert.Equal(1, connector.Connects); Assert.False(result.Success);
    }
    [Fact]
    public async Task AlreadyCancelledDoesNotReadOrConnect()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake();
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(), 443, cancel.Token);
        Assert.Equal("Cancelled", result.Error); Assert.Equal(0, platform.BindingReads); Assert.Equal(0, connector.Connects);
    }
    [Fact]
    public async Task UnexpectedFailureIsRedacted()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake { Throw = true };
        var result = await new DiagnosticTcpService(platform, connector).CheckAsync(Request(), 443, default);
        Assert.Equal("Unavailable", result.Error); Assert.DoesNotContain("secret", result.Display);
        Assert.Equal(1, connector.Connects);
    }
    [Theory]
    [InlineData(SocketError.ConnectionRefused, "Refused")][InlineData(SocketError.TimedOut, "Timeout")]
    [InlineData(SocketError.HostUnreachable, "Unreachable")][InlineData(SocketError.NetworkUnreachable, "Unreachable")]
    [InlineData(SocketError.AccessDenied, null)]
    public void SocketErrorsHaveBoundedMeaning(SocketError error, string? expected)
        => Assert.Equal(expected, WindowsDiagnosticTcpConnector.StatusFor(error));
}

public class NetworkTestPresetTests
{
    private static AdapterDiagnosticsViewModel Create(DiagnosticFake platform, TcpFake? connector = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null) => new(platform, new(platform),
            tcp: connector == null ? null : new(platform, connector), delay: delay ?? ((_, _) => Task.CompletedTask));
    [Fact]
    public async Task SelectionAndPeerOpeningNeverProbeAndPreserveExplicitSourceRequirement()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake(); using var vm = Create(platform, connector);
        await AdapterDiagnosticsTests.Ready(vm);
        Assert.Equal(DiagnosticTestKind.PingFour, vm.SelectedTest.Kind);
        foreach (var preset in vm.Presets) { vm.SelectedTest = preset; Assert.True(vm.CanRun); }
        vm.Port = "80";
        using var peer = vm.CreatePeer(); await peer.RefreshAsync();
        Assert.Equal(vm.SelectedAdapter!.Id, peer.SelectedAdapter!.Id); Assert.Null(peer.Source); Assert.False(peer.CanRun);
        Assert.Equal(vm.SelectedTest, peer.SelectedTest); Assert.Equal("80", peer.Port); Assert.True(peer.ShowPresetCards);
        Assert.Equal(0, platform.Sends); Assert.Equal(0, platform.Resolves); Assert.Equal(0, connector.Connects);
    }
    [Fact]
    public async Task FourRequestPresetReportsStatsAndOneRequestRemainsAvailable()
    {
        var platform = new DiagnosticFake(); using var vm = Create(platform); await AdapterDiagnosticsTests.Ready(vm);
        await vm.RunSelectedTestAsync(); Assert.Equal(4, platform.Sends); Assert.Equal(4, vm.AttemptCount); Assert.Equal(4, vm.ReplyCount);
        Assert.Equal(4, vm.Results.Count); Assert.True(vm.CanSelect);
        vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.PingOnce);
        await vm.RunSelectedTestAsync(); Assert.Equal(5, platform.Sends); Assert.Single(vm.Results); Assert.Equal(1, vm.AttemptCount);
    }
    [Fact]
    public async Task ChangingPresetClearsPreviousOutcomeWithoutStartingTheNewTest()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake(); using var vm = Create(platform, connector);
        await AdapterDiagnosticsTests.Ready(vm); await vm.PingAsync(); Assert.Single(vm.Results);
        vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.TcpPort);
        Assert.Empty(vm.Results); Assert.Equal(0, vm.AttemptCount); Assert.Null(vm.TcpResult); Assert.Equal(0, connector.Connects);
        await vm.RunSelectedTestAsync(); Assert.NotNull(vm.TcpResult);
        vm.SelectedTest = vm.Presets[0]; Assert.Null(vm.TcpResult); Assert.Equal(1, connector.Connects); Assert.Equal(1, platform.Sends);
    }
    [Theory]
    [InlineData("")][InlineData("0")][InlineData("65536")][InlineData("-1")][InlineData("+80")][InlineData("80;cmd")][InlineData(" 80")]
    public async Task InvalidPortDisablesOnlyTcpWithoutAnyConnection(string port)
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake(); using var vm = Create(platform, connector);
        await AdapterDiagnosticsTests.Ready(vm); vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.TcpPort);
        vm.Port = port; Assert.False(vm.CanRun); Assert.False(vm.RunTestCommand.CanExecute(null)); Assert.NotEmpty(vm.PortError);
        await vm.RunSelectedTestAsync(); Assert.Equal(0, connector.Connects);
        vm.SelectedTest = vm.Presets[0]; Assert.True(vm.CanRun); Assert.Empty(vm.PortError);
    }
    [Fact]
    public async Task TcpRunPinsPortAndIdentityAndBlocksDuplicateRuns()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var vm = Create(platform, connector); await AdapterDiagnosticsTests.Ready(vm);
        vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.TcpPort); vm.QuickPortCommand.Execute("80");
        var run = vm.RunSelectedTestAsync(); await connector.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(vm.CanStop); Assert.False(vm.CanRun); vm.Port = "443"; vm.Target = "198.51.100.1"; vm.SelectedTest = vm.Presets[0];
        await vm.RunSelectedTestAsync(); await vm.RefreshAsync();
        connector.Gate.SetResult(new("Accepted", 3)); await run;
        Assert.True(vm.TcpResult!.Success); Assert.Equal(80, vm.TcpResult.Port); Assert.Equal("192.0.2.229", vm.TcpResult.Request.Target);
        Assert.Equal(1, connector.Connects); Assert.Equal(0, platform.Sends); Assert.Equal(1, platform.Reads); Assert.Empty(vm.Results);
    }
    [Fact]
    public async Task ContinuousHistoryIsBoundedButStatsCoverEntireRunAndStopCancelsDelay()
    {
        var platform = new DiagnosticFake();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int delays = 0;
        using var vm = Create(platform, delay: async (_, token) =>
        {
            if (++delays == 105) { reached.TrySetResult(); await Task.Delay(Timeout.Infinite, token); }
        });
        await AdapterDiagnosticsTests.Ready(vm); vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.ContinuousPing);
        var run = vm.RunSelectedTestAsync(); await reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(105, vm.AttemptCount); Assert.Equal(105, vm.ReplyCount); Assert.Equal(100, vm.Results.Count);
        vm.StopCommand.Execute(null); await run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(105, platform.Sends); Assert.True(vm.CanSelect); Assert.False(vm.CanStop);
        Assert.All(vm.Results, result => Assert.Equal("192.0.2.229", result.Request.Target));
    }
    [Fact]
    public async Task ContinuousStopsOnRouteChangeInsteadOfFallingBack()
    {
        var platform = new DiagnosticFake(); using var vm = Create(platform, delay: (_, _) => { platform.Route = null; return Task.CompletedTask; });
        await AdapterDiagnosticsTests.Ready(vm); vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.ContinuousPing);
        await vm.RunSelectedTestAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, platform.Sends); Assert.Equal(2, vm.AttemptCount); Assert.Equal("Route", vm.Results.Last().Error);
    }
    [Fact]
    public async Task DisposingContinuousOwnerCancelsDelayAndCannotSendAgain()
    {
        var platform = new DiagnosticFake(); var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = Create(platform, delay: (_, token) => { reached.TrySetResult(); return Task.Delay(Timeout.Infinite, token); });
        await AdapterDiagnosticsTests.Ready(vm); vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.ContinuousPing);
        var run = vm.RunSelectedTestAsync(); await reached.Task.WaitAsync(TimeSpan.FromSeconds(5)); vm.Dispose();
        await run.WaitAsync(TimeSpan.FromSeconds(5)); Assert.Equal(1, platform.Sends); Assert.False(vm.CanRun); Assert.False(vm.CanStop);
    }
    [Fact]
    public async Task DisposingTcpOwnerCancelsAndDropsLateResult()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var vm = Create(platform, connector); await AdapterDiagnosticsTests.Ready(vm);
        vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.TcpPort);
        var run = vm.RunSelectedTestAsync(); await connector.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); vm.Dispose();
        await run.WaitAsync(TimeSpan.FromSeconds(5)); Assert.Null(vm.TcpResult); Assert.True(connector.Cancelled); Assert.Equal(1, connector.Connects);
    }
    [Fact]
    public async Task StoppingPeerDoesNotCancelMainOperationAndActiveHeaderRemainsDiscoverable()
    {
        var platform = new DiagnosticFake(); var connector = new TcpFake { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var vm = Create(platform, connector); await AdapterDiagnosticsTests.Ready(vm);
        vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.TcpPort);
        using var peer = vm.CreatePeer(); await peer.RefreshAsync(); peer.Source = peer.Sources[1];
        string idleHeader = vm.SectionTitle;
        var mainRun = vm.RunSelectedTestAsync(); await connector.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEqual(idleHeader, vm.SectionTitle);
        var peerRun = peer.RunSelectedTestAsync(); await connector.SecondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        peer.StopCommand.Execute(null); await peerRun;
        Assert.True(vm.CanStop); Assert.False(mainRun.IsCompleted); Assert.Equal("Cancelled", peer.TcpResult!.Error);
        connector.Gate.SetResult(new("Accepted", 2)); await mainRun;
        Assert.True(vm.TcpResult!.Success); Assert.Equal(idleHeader, vm.SectionTitle);
        Assert.Equal("192.0.2.7", vm.TcpResult.Request.Source); Assert.Equal("192.0.2.8", peer.TcpResult.Request.Source);
    }
}

internal sealed class TcpFake : IDiagnosticTcpConnector
{
    internal int Connects, Port;
    internal string? Source, Target;
    internal bool Throw, Cancelled, Expire;
    internal Action? AfterConnect;
    internal DiagnosticTcpObservation Observation = new("Accepted", 2);
    internal TaskCompletionSource<DiagnosticTcpObservation>? Gate;
    internal TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource SecondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task<DiagnosticTcpObservation> ConnectAsync(IPAddress source, IPAddress destination, int port, CancellationToken cancellation)
    {
        int count = Interlocked.Increment(ref Connects);
        Source = source.ToString(); Target = destination.ToString(); Port = port; Entered.TrySetResult();
        if (count >= 2) SecondEntered.TrySetResult();
        if (Throw) throw new InvalidOperationException("secret fixture exception");
        if (Expire) throw new OperationCanceledException();
        try
        {
            var observed = Gate == null ? Observation : await Gate.Task.WaitAsync(cancellation);
            AfterConnect?.Invoke(); return observed;
        }
        catch (OperationCanceledException) { Cancelled = true; throw; }
    }
}
