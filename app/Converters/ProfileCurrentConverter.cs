using System;
using System.Globalization;
using System.Windows.Data;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Converters;

// Display evidence only; never substitutes for apply preflight/verification.
public sealed class ProfileCurrentConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[1] is not AdapterInfo a || !a.IsDhcpKnown || a.IsDhcp || a.IsCurrentIpv4Manual == false) return false;
        bool Same(string? x, string? y) => string.Equals((x ?? "").Trim(), (y ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        bool Match(string ip, int? prefix, string? gateway, bool compareDns, string? primary, string? secondary) =>
            !string.IsNullOrWhiteSpace(ip) && prefix is >= 1 and <= 32 && Same(ip, a.CurrentIp)
            && prefix == IpFormat.PrefixLength(a.CurrentSubnet) && Same(gateway, a.CurrentGateway)
            && (!compareDns || !string.IsNullOrWhiteSpace(primary) && Same(primary, a.CurrentPrimaryDns) && Same(secondary, a.CurrentSecondaryDns));
        // AdapterInfo has no DNS-source policy: DHCP/automatic DNS cannot be confirmed here.
        return values[0] switch
        {
            IpProfile p when !p.IsDhcp => Match(p.IpAddress, IpFormat.PrefixLength(p.SubnetMask), p.Gateway,
                !string.IsNullOrWhiteSpace(p.PrimaryDns), p.PrimaryDns, p.SecondaryDns),
            ProjectProfile p when p.Mode == "static" && p.Ipv4 is { } ip && ip.Dns.Mode == "leave-unchanged"
                => Match(ip.Address, ip.PrefixLength, ip.Gateway, false, null, null),
            ProjectProfile p when p.Mode == "static" && p.Ipv4 is { } ip && ip.Dns.Mode == "static" && ip.Dns.Servers.Count is >= 1 and <= 2
                => Match(ip.Address, ip.PrefixLength, ip.Gateway, true, ip.Dns.Servers[0], ip.Dns.Servers.Count == 2 ? ip.Dns.Servers[1] : null),
            _ => false
        };
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
