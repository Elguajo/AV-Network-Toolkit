using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

public sealed class LinkLocalDisplayTests
{
    [Theory]
    [InlineData(0x02, true)][InlineData(0x05, false)][InlineData(0x15, false)][InlineData(null, false)]
    public void AdapterProjectionAnnotatesVisibilityWithoutDroppingInterfaceOrAddress(int? flags, bool system)
    {
        var adapter = new FixtureNic(false);
        var info = Assert.Single(new NetworkService().GetAdapters(new[] { adapter }, id =>
        {
            Assert.Equal(Guid.Parse(adapter.Id), id);
            return (byte?)flags;
        }));
        Assert.Equal(system, info.IsSystemInterface);
        Assert.Equal(adapter.Id, info.Id);
        Assert.Equal("169.254.42.10", info.CurrentIp);
    }

    [Theory]
    [InlineData(false, "LinkLocal.Manual")][InlineData(true, "LinkLocal.Automatic")][InlineData(null, "LinkLocal.Unknown")]
    public void RealAdapterProjectionKeepsLinkLocalAndDistinguishesDhcpPolicy(bool? dhcp, string mode)
    {
        var service = new NetworkService();
        var info = Assert.Single(service.GetAdapters(new[] { new FixtureNic(dhcp) }));
        Assert.Equal("169.254.42.10", info.CurrentIp); Assert.Equal("255.255.0.0", info.CurrentSubnet);
        Assert.Equal(dhcp.HasValue, info.IsDhcpKnown);
        Assert.True(info.IsCurrentIpv4Manual);
        var vm = new AdapterViewModel(info, new ObservableCollection<ProfileViewModel>(), new ObservableCollection<HistoryEntry>());
        Assert.Equal(info.CurrentIp, vm.CurrentIp); Assert.Equal(mode, vm.CurrentModeDisplay);
        info = new AdapterInfo { CurrentIp = "192.0.2.10", IsDhcp = true };
        var changed = new List<string?>(); vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        vm.UpdateInfo(info); Assert.Contains(nameof(vm.CurrentModeDisplay), changed); Assert.Equal("EditBar.Dhcp", vm.CurrentModeDisplay);
    }

    [Fact]
    public void UnknownAddressOriginNeverBecomesClaimedManualConfiguration()
    {
        var info = new AdapterInfo { CurrentIp = "169.254.42.10", IsDhcp = false, IsCurrentIpv4Manual = null };
        var vm = new AdapterViewModel(info, new(), new());
        Assert.Equal("LinkLocal.UnknownOrigin", vm.CurrentModeDisplay); Assert.True(vm.HasLinkLocalAddress);
    }

    // Every property is in-memory. The production projection cannot enumerate host
    // interfaces or use the disconnected registry fallback with this fixture.
    private sealed class FixtureNic(bool? dhcp) : NetworkInterface
    {
        public override string Id => "00000000-0000-0000-0000-000000000012";
        public override string Name => "Ethernet fixture";
        public override string Description => "Display-only fixture";
        public override NetworkInterfaceType NetworkInterfaceType => NetworkInterfaceType.Ethernet;
        public override OperationalStatus OperationalStatus => OperationalStatus.Up;
        public override bool IsReceiveOnly => false;
        public override bool SupportsMulticast => true;
        public override long Speed => 1_000_000_000;
        public override PhysicalAddress GetPhysicalAddress() => PhysicalAddress.Parse("020000000001");
        public override bool Supports(NetworkInterfaceComponent c) => c == NetworkInterfaceComponent.IPv4;
        public override IPInterfaceProperties GetIPProperties() => new FixtureProperties(dhcp);
        public override IPv4InterfaceStatistics GetIPv4Statistics() => throw new NotSupportedException();
    }
    private sealed class FixtureProperties(bool? dhcp) : IPInterfaceProperties
    {
        public override UnicastIPAddressInformationCollection UnicastAddresses => new FixtureUnicastList();
        public override GatewayIPAddressInformationCollection GatewayAddresses => new EmptyGateways();
        public override IPAddressCollection DnsAddresses => new EmptyAddresses();
        public override bool IsDnsEnabled => true;
        public override bool IsDynamicDnsEnabled => false;
        public override string DnsSuffix => "";
        public override IPAddressInformationCollection AnycastAddresses => throw new NotSupportedException();
        public override MulticastIPAddressInformationCollection MulticastAddresses => throw new NotSupportedException();
        public override IPAddressCollection DhcpServerAddresses => new EmptyAddresses();
        public override IPAddressCollection WinsServersAddresses => new EmptyAddresses();
        public override IPv4InterfaceProperties GetIPv4Properties() => dhcp.HasValue ? new FixtureIpv4(dhcp.Value) : throw new NetworkInformationException();
        public override IPv6InterfaceProperties GetIPv6Properties() => throw new NotSupportedException();
    }
    private sealed class FixtureIpv4(bool dhcp) : IPv4InterfaceProperties
    {
        public override int Index => 12;
        public override bool IsAutomaticPrivateAddressingActive => dhcp;
        public override bool IsAutomaticPrivateAddressingEnabled => true;
        public override bool IsDhcpEnabled => dhcp;
        public override bool IsForwardingEnabled => false;
        public override int Mtu => 1500;
        public override bool UsesWins => false;
    }
    private sealed class FixtureAddress : UnicastIPAddressInformation
    {
        public override IPAddress Address => IPAddress.Parse("169.254.42.10");
        public override IPAddress IPv4Mask => IPAddress.Parse("255.255.0.0");
        public override bool IsDnsEligible => false;
        public override bool IsTransient => false;
        public override long AddressPreferredLifetime => uint.MaxValue;
        public override long AddressValidLifetime => uint.MaxValue;
        public override long DhcpLeaseLifetime => 0;
        public override DuplicateAddressDetectionState DuplicateAddressDetectionState => DuplicateAddressDetectionState.Preferred;
        public override PrefixOrigin PrefixOrigin => PrefixOrigin.Manual;
        public override SuffixOrigin SuffixOrigin => SuffixOrigin.Manual;
    }
    private sealed class FixtureUnicastList : UnicastIPAddressInformationCollection
    {
        public override IEnumerator<UnicastIPAddressInformation> GetEnumerator()
            => ((IEnumerable<UnicastIPAddressInformation>)new[] { new FixtureAddress() }).GetEnumerator();
    }
    private sealed class EmptyGateways : GatewayIPAddressInformationCollection { }
    private sealed class EmptyAddresses : IPAddressCollection
    {
        public override IEnumerator<IPAddress> GetEnumerator() => Enumerable.Empty<IPAddress>().GetEnumerator();
    }
}
