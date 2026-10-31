using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.ViewModels;

// Presentation-only checks. ProjectJson remains the independent authoritative gate.
public sealed partial class ProjectProfileEditor : IDataErrorInfo
{
    private bool _showValidation;
    private static readonly string[] Fields = { nameof(Name), nameof(Mode), nameof(Address), nameof(Prefix), nameof(Gateway), nameof(DnsMode), nameof(Dns1), nameof(Dns2) };
    public string? FirstInvalidField => Fields.FirstOrDefault(p => FieldError(p).Length != 0);
    public string Error => FirstInvalidField is { } field ? FieldError(field) : "";
    public string this[string property] => _showValidation ? FieldError(property) : "";
    public void ShowValidation() { _showValidation = true; Changed(string.Empty); }

    private string FieldError(string field)
    {
        if (field == nameof(Name)) return string.IsNullOrWhiteSpace(Name) ? Loc.Get("Ux.NameError") : "";
        if (field == nameof(Mode)) return Mode is "static" or "dhcp" ? "" : Loc.Get("Ux.ModeError");
        if (!IsStatic) return "";
        var validAddress = Host(Address, out var address);
        var validPrefix = int.TryParse(Prefix, NumberStyles.None, CultureInfo.InvariantCulture, out var prefix) && prefix is >= 1 and <= 32;
        if (field == nameof(Prefix)) return validPrefix ? "" : Loc.Get("Ux.PrefixError");
        if (field == nameof(Address))
        {
            if (!validAddress) return Loc.Get("Ux.IpError");
            if ((address & 0xffff0000) == 0xa9fe0000) return Loc.Get("Ux.LinkLocalError");
            if (validPrefix && prefix <= 30 && !IsSubnetHost(address, prefix)) return Loc.Get("Ux.SubnetHostError");
        }
        if (field == nameof(Gateway) && !string.IsNullOrWhiteSpace(Gateway))
        {
            if (!Host(Gateway, out var gateway)) return Loc.Get("Ux.GatewayError");
            if (validAddress && validPrefix)
            {
                var mask = uint.MaxValue << (32 - prefix);
                if (gateway == address || (gateway & mask) != (address & mask) || prefix <= 30 && !IsSubnetHost(gateway, prefix))
                    return Loc.Get("Ux.GatewayError");
            }
        }
        if (field == nameof(DnsMode)) return DnsMode is "static" or "automatic" or "leave-unchanged" ? "" : Loc.Get("Ux.DnsModeError");
        if (IsDnsStatic && field == nameof(Dns1) && !Host(Dns1, out _)) return Loc.Get("Ux.DnsPrimaryError");
        if (IsDnsStatic && field == nameof(Dns2) && !string.IsNullOrWhiteSpace(Dns2)
            && (!Host(Dns2, out _) || Dns1.Trim() == Dns2.Trim())) return Loc.Get("Ux.DnsSecondaryError");
        return "";
    }

    private static bool Host(string value, out uint address)
    {
        address = 0; value = value.Trim();
        if (!IPAddress.TryParse(value, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork || ip.ToString() != value) return false;
        var b = ip.GetAddressBytes();
        if (b[0] is 0 or 127 || b[0] >= 224) return false;
        address = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        return true;
    }
    private static bool IsSubnetHost(uint address, int prefix)
    {
        var host = ~((uint.MaxValue) << (32 - prefix));
        return (address & host) != 0 && (address & host) != host;
    }
}
