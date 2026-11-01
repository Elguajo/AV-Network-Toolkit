using System.Net;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class DiagnosticPingTests
{
    private static DiagnosticPingRequest Request(string target = "192.0.2.229") => new(AdapterDiagnosticsTests.Ethernet,
        7, "001122334455", "Ethernet fixture", "192.0.2.7", target);
    [Fact]
    public async Task LiteralSourceRequestBypassesDnsAndRetainsPredictionAndEcho()
    {
        var fake = new DiagnosticFake(); var request = Request();
        var result = await new DiagnosticPingService(fake).SendAsync(request, default);
        Assert.True(result.Success); Assert.Equal(request, result.Request); Assert.Equal(0, fake.Resolves);
        Assert.Equal(1, fake.Sends); Assert.Equal(fake.Route, result.Route); Assert.Equal("192.0.2.229", result.Destination);
        Assert.Equal("0.0.0.0", result.Route!.NextHop); Assert.Equal(2, result.Echo!.Milliseconds);
    }
    [Theory]
    [InlineData("")][InlineData("file:///a")][InlineData("::1")][InlineData("127.0.0.1")]
    [InlineData("224.0.0.251")][InlineData("255.255.255.255")][InlineData("192.0.2")][InlineData("host;echo x")]
    public async Task InvalidTargetNeverSendsOrResolves(string target)
    {
        var fake = new DiagnosticFake(); var result = await new DiagnosticPingService(fake).SendAsync(Request(target), default);
        Assert.Equal("Input", result.Error); Assert.Equal(0, fake.Sends); Assert.Equal(0, fake.Resolves);
    }
    [Theory]
    [InlineData("missing")][InlineData("index")][InlineData("mac")][InlineData("down")][InlineData("source")][InlineData("duplicate")]
    public async Task ChangedOrAmbiguousBindingNeverFallsBack(string variant)
    {
        var fake = new DiagnosticFake(); var b = fake.Bindings[0];
        fake.Bindings = variant switch
        {
            "missing" => [], "index" => [b with { Index = 8 }], "mac" => [b with { Mac = "FFFFFFFFFFFF" }],
            "down" => [b with { Connected = false }], "source" => [b with { Sources = ["192.0.2.9"] }],
            _ => [b, b with { Id = AdapterDiagnosticsTests.Wifi, Index = 8 }]
        };
        var result = await new DiagnosticPingService(fake).SendAsync(Request(), default);
        Assert.Equal("Changed", result.Error); Assert.Equal(0, fake.Sends);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task MissingRouteOrWifiRouteBlocksEthernetProbe(bool missing)
    {
        var fake = new DiagnosticFake { Route = missing ? null : new(8, "192.0.2.0", 24, "198.51.100.1", 1) };
        var result = await new DiagnosticPingService(fake).SendAsync(Request(), default);
        Assert.Equal("Route", result.Error); Assert.Equal(0, fake.Sends);
    }
    [Fact]
    public async Task HostnameResolvesOnceAndUsesOneIpv4()
    {
        var fake = new DiagnosticFake { Resolved = [IPAddress.IPv6Loopback, IPAddress.Parse("192.0.2.229"), IPAddress.Parse("192.0.2.230")] };
        var result = await new DiagnosticPingService(fake).SendAsync(Request("panel.local"), default);
        Assert.True(result.Success); Assert.Equal("192.0.2.229", result.Destination); Assert.Equal(1, fake.Resolves); Assert.Equal(1, fake.Sends);
    }
    [Fact]
    public async Task MissingIpv4ResolutionDoesNotProbe()
    {
        var fake = new DiagnosticFake { Resolved = [IPAddress.IPv6Loopback] };
        var result = await new DiagnosticPingService(fake).SendAsync(Request("panel.local"), default);
        Assert.Equal("Resolve", result.Error); Assert.Equal(0, fake.Sends);
    }
    [Fact]
    public async Task RemovalDuringResolutionIsRejectedBeforeSend()
    {
        var fake = new DiagnosticFake(); fake.OnRead = n => { if (n == 2) fake.Bindings = []; };
        var result = await new DiagnosticPingService(fake).SendAsync(Request("panel.local"), default);
        Assert.Equal("Changed", result.Error); Assert.Equal(0, fake.Sends);
    }
    [Fact]
    public async Task RouteChangeAfterReplyMakesResultUnverified()
    {
        var fake = new DiagnosticFake(); fake.OnPredict = n => { if (n == 2) fake.Route = fake.Route! with { Index = 8 }; };
        var result = await new DiagnosticPingService(fake).SendAsync(Request(), default);
        Assert.Equal("Changed", result.Error); Assert.False(result.Success); Assert.NotNull(result.Echo); Assert.Equal(1, fake.Sends);
    }
    [Fact]
    public async Task AdapterRemovalAfterReplyMakesResultUnverified()
    {
        var fake = new DiagnosticFake(); fake.OnRead = n => { if (n == 3) fake.Bindings = []; };
        var result = await new DiagnosticPingService(fake).SendAsync(Request(), default);
        Assert.Equal("Changed", result.Error); Assert.False(result.Success); Assert.Equal(1, fake.Sends);
    }
    [Theory]
    [InlineData(11010)][InlineData(11003)][InlineData(11013)]
    public async Task IcmpStatusIsPreservedWithoutOfflineInference(int status)
    {
        var fake = new DiagnosticFake { Echo = new((uint)status, "192.0.2.1", 0) };
        var result = await new DiagnosticPingService(fake).SendAsync(Request(), default);
        Assert.False(result.Success); Assert.Equal("", result.Error); Assert.Equal((uint)status, result.Echo!.Status); Assert.Equal(1, fake.Sends);
    }
    [Fact]
    public async Task PlatformFailureIsRedactedAndDoesNotRetry()
    {
        var fake = new DiagnosticFake { ThrowSend = true };
        var result = await new DiagnosticPingService(fake).SendAsync(Request(), default);
        Assert.Equal("Unavailable", result.Error); Assert.Equal(1, fake.Sends);
    }
    [Fact]
    public async Task CancellationBeforeStartDoesNotSend()
    {
        using var cancel = new CancellationTokenSource(); cancel.Cancel(); var fake = new DiagnosticFake();
        var result = await new DiagnosticPingService(fake).SendAsync(Request(), cancel.Token);
        Assert.Equal("Cancelled", result.Error); Assert.Equal(0, fake.Sends);
    }
}
