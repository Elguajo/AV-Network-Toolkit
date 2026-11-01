using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public sealed class NetworkMonitorServiceTests
{
    [Theory]
    [InlineData(NetworkInterfaceType.Tunnel, "Teredo Tunneling Pseudo-Interface")]
    [InlineData(NetworkInterfaceType.Tunnel, "Renamed tunnel")]
    [InlineData(NetworkInterfaceType.Loopback, "Loopback")]
    public void ExcludedInterfacesNeverNotifyAcrossRepeatedStatusAndAddressChanges(NetworkInterfaceType type, string name)
    {
        var nic = new FixtureNic(type, name);
        var notifications = new List<string>();
        using var monitor = new NetworkMonitorService(() => new[] { nic }, (_, message, _) => notifications.Add(message));
        for (var cycle = 0; cycle < 5; cycle++)
        {
            nic.Status = OperationalStatus.Up;
            monitor.CheckForChanges();
            nic.Address = "192.0.2.20";
            monitor.CheckForChanges();
            nic.Status = OperationalStatus.Down;
            monitor.CheckForChanges();
        }
        Assert.Empty(notifications);
        Assert.Equal(0, nic.PropertyReads);
    }

    [Theory]
    [InlineData(NetworkInterfaceType.Ethernet, "Ethernet")]
    [InlineData(NetworkInterfaceType.Wireless80211, "Wi-Fi")]
    [InlineData(NetworkInterfaceType.Ethernet, "vEthernet (lab)")]
    [InlineData(NetworkInterfaceType.Ethernet, "Teredo named ordinary adapter")]
    public void OrdinaryInterfacesKeepUpDownAndAddressNotificationsWithoutRepeatingUnchangedState(NetworkInterfaceType type, string name)
    {
        var nic = new FixtureNic(type, name);
        var notifications = new List<(string Message, ToolTipIcon Icon)>();
        using var monitor = new NetworkMonitorService(() => new[] { nic }, (_, message, icon) => notifications.Add((message, icon)));
        Assert.Empty(notifications); // Initial snapshot is silent.
        nic.Status = OperationalStatus.Up;
        monitor.CheckForChanges();
        monitor.CheckForChanges();
        Assert.Equal((name, ToolTipIcon.Info), Assert.Single(notifications));
        nic.Address = "192.0.2.10";
        monitor.CheckForChanges();
        monitor.CheckForChanges();
        Assert.Equal(2, notifications.Count);
        Assert.Contains("192.0.2.10", notifications[1].Message);
        nic.Address = "192.0.2.20";
        monitor.CheckForChanges();
        Assert.Equal(3, notifications.Count);
        Assert.Contains("192.0.2.10", notifications[2].Message);
        Assert.Contains("192.0.2.20", notifications[2].Message);
        nic.Status = OperationalStatus.Down;
        monitor.CheckForChanges();
        monitor.CheckForChanges();
        Assert.Equal(4, notifications.Count);
        Assert.Equal((name, ToolTipIcon.Warning), notifications[3]);
    }

    [Fact]
    public void TunnelChurnDoesNotSuppressAnEthernetConnectionInTheSameSnapshot()
    {
        var tunnel = new FixtureNic(NetworkInterfaceType.Tunnel, "Teredo");
        var ethernet = new FixtureNic(NetworkInterfaceType.Ethernet, "Ethernet");
        var notifications = new List<string>();
        using var monitor = new NetworkMonitorService(() => new[] { tunnel, ethernet }, (_, message, _) => notifications.Add(message));
        tunnel.Status = ethernet.Status = OperationalStatus.Up;
        monitor.CheckForChanges();
        Assert.Equal("Ethernet", Assert.Single(notifications));
    }

    private sealed class FixtureNic(NetworkInterfaceType type, string name) : NetworkInterface
    {
        public OperationalStatus Status { get; set; } = OperationalStatus.Down;
        public string? Address { get; set; }
        public int PropertyReads { get; private set; }
        public override string Id => name;
        public override string Name => name;
        public override NetworkInterfaceType NetworkInterfaceType => type;
        public override OperationalStatus OperationalStatus => Status;
        public override IPInterfaceProperties GetIPProperties() { PropertyReads++; return new FixtureProperties(Address); }
    }

    private sealed class FixtureProperties(string? address) : IPInterfaceProperties
    {
        public override UnicastIPAddressInformationCollection UnicastAddresses => new FixtureAddresses(address);
        public override IPAddressCollection DnsAddresses => throw new NotSupportedException();
        public override GatewayIPAddressInformationCollection GatewayAddresses => throw new NotSupportedException();
        public override MulticastIPAddressInformationCollection MulticastAddresses => throw new NotSupportedException();
        public override IPAddressInformationCollection AnycastAddresses => throw new NotSupportedException();
        public override IPAddressCollection DhcpServerAddresses => throw new NotSupportedException();
        public override IPAddressCollection WinsServersAddresses => throw new NotSupportedException();
        public override bool IsDynamicDnsEnabled => false;
        public override bool IsDnsEnabled => false;
        public override string DnsSuffix => "";
        public override IPv4InterfaceProperties GetIPv4Properties() => throw new NotSupportedException();
        public override IPv6InterfaceProperties GetIPv6Properties() => throw new NotSupportedException();
    }

    private sealed class FixtureAddresses(string? address) : UnicastIPAddressInformationCollection
    {
        public override IEnumerator<UnicastIPAddressInformation> GetEnumerator()
            => (address == null ? Enumerable.Empty<UnicastIPAddressInformation>() : new[] { new FixtureAddress(address) }).GetEnumerator();
    }

    private sealed class FixtureAddress(string address) : UnicastIPAddressInformation
    {
        public override IPAddress Address => IPAddress.Parse(address);
        public override IPAddress IPv4Mask => IPAddress.Parse("255.255.255.0");
        public override bool IsDnsEligible => false;
        public override bool IsTransient => false;
        public override long AddressPreferredLifetime => uint.MaxValue;
        public override long AddressValidLifetime => uint.MaxValue;
        public override long DhcpLeaseLifetime => 0;
        public override DuplicateAddressDetectionState DuplicateAddressDetectionState => DuplicateAddressDetectionState.Preferred;
        public override PrefixOrigin PrefixOrigin => PrefixOrigin.Manual;
        public override SuffixOrigin SuffixOrigin => SuffixOrigin.Manual;
    }
}
