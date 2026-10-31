using System.Globalization;
using NetworkProfileManager.Converters;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Tests;

public class ProfileCurrentConverterTests
{
    private static AdapterInfo Adapter() => new() { IsDhcpKnown = true, IsCurrentIpv4Manual = true,
        CurrentIp = "192.0.2.100", CurrentSubnet = "255.255.255.0", CurrentPrimaryDns = "192.0.2.53", CurrentSecondaryDns = "192.0.2.54" };
    private static IpProfile Legacy() => new() { IpAddress = "192.0.2.100", PrimaryDns = "192.0.2.53", SecondaryDns = "192.0.2.54" };
    private static bool Current(object profile, AdapterInfo adapter) => (bool)new ProfileCurrentConverter().Convert(new[] { profile, adapter }, typeof(bool), null!, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("ip")][InlineData("mask")][InlineData("gateway")][InlineData("primary")][InlineData("secondary")]
    [InlineData("unknownDns")][InlineData("unknownPolicy")][InlineData("dhcp")][InlineData("nonManual")]
    public void MismatchOrUnknownStateNeverShowsSelected(string field)
    {
        var a = Adapter();
        switch (field)
        {
            case "ip": a.CurrentIp = "192.0.2.101"; break;
            case "mask": a.CurrentSubnet = "255.255.0.0"; break;
            case "gateway": a.CurrentGateway = "192.0.2.1"; break;
            case "primary": a.CurrentPrimaryDns = "192.0.2.55"; break;
            case "secondary": a.CurrentSecondaryDns = "192.0.2.56"; break;
            case "unknownDns": a.CurrentPrimaryDns = ""; break;
            case "unknownPolicy": a.IsDhcpKnown = false; break;
            case "dhcp": a.IsDhcp = true; break;
            case "nonManual": a.IsCurrentIpv4Manual = false; break;
        }
        Assert.False(Current(Legacy(), a));
    }

    [Fact]
    public void LegacyDnsLeaveUnchangedAndExplicitReplacementHaveDifferentMeaning()
    {
        var a = Adapter(); var p = Legacy(); Assert.True(Current(p, a));
        p.SecondaryDns = ""; Assert.False(Current(p, a)); // Explicit primary replaces the DNS list.
        p.PrimaryDns = ""; Assert.True(Current(p, a)); // Legacy blank primary changes neither DNS server.
        p.IsDhcp = true; a.IsDhcp = true; Assert.False(Current(p, a)); // DNS source policy is unobservable here.
    }

    [Fact]
    public void ProjectProfilesComparePrefixAndExplicitDnsWithoutTreatingAutomaticAsKnown()
    {
        var a = Adapter(); var p = new ProjectProfile { Mode = "static", Ipv4 = new()
        { Address = a.CurrentIp, PrefixLength = 24, Dns = new() { Mode = "static", Servers = new() { a.CurrentPrimaryDns, a.CurrentSecondaryDns } } } };
        Assert.True(Current(p, a));
        p.Ipv4!.Dns.Servers.RemoveAt(1); Assert.False(Current(p, a));
        p = p with { Ipv4 = p.Ipv4 with { Dns = new() { Mode = "leave-unchanged" } } }; Assert.True(Current(p, a));
        p = p with { Ipv4 = p.Ipv4 with { Dns = new() { Mode = "automatic" } } }; Assert.False(Current(p, a));
        Assert.False(Current(new ProjectProfile { Mode = "dhcp" }, a));
    }
}
