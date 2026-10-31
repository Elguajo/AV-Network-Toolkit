using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

internal static class ProjectApplyPolicy
{
    internal static ApplyIntent FromProfile(ProjectProfile profile)
    {
        _ = ProjectJson.Serialize(new NetworkProject
        {
            Project = new ProjectIdentity { Id = profile.Id + "-validation", Name = "Apply validation" },
            Profiles = new List<ProjectProfile> { profile }
        });
        return profile.Mode == "dhcp" ? new("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty)
            : new("static", profile.Ipv4!.Address, profile.Ipv4.PrefixLength, profile.Ipv4.Gateway,
                profile.Ipv4.Dns.Mode, profile.Ipv4.Dns.Servers.ToImmutableArray());
    }

    internal static void ValidateIntent(ApplyIntent intent, bool recovery = false)
    {
        if (intent == null || intent.DnsServers.IsDefault || intent.GatewayMetric < 1 || intent.GatewayMetric > 9999
            || intent.DnsRegistration is not (null or "none" or "primary" or "both"))
            throw new InvalidOperationException("Invalid intent.");
        if (intent.Mode == "dhcp")
        {
            if (intent.Address != null || intent.Gateway != null || intent.PrefixLength != 0
                || (intent.DnsMode != "automatic" && !(recovery && intent.DnsMode == "static")))
                throw new InvalidOperationException("Invalid DHCP intent.");
            ValidateDns(intent.DnsMode, intent.DnsServers);
            return;
        }
        _ = FromProfile(new ProjectProfile
        {
            Id = "profile-validation", Name = "Apply validation", Mode = intent.Mode,
            Ipv4 = new ProjectIpv4 { Address = intent.Address!, PrefixLength = intent.PrefixLength,
                Gateway = intent.Gateway, Dns = new ProjectDns { Mode = intent.DnsMode, Servers = intent.DnsServers.ToList() } }
        });
        if (intent.PrefixLength > 30 || intent.DnsMode == "automatic")
            throw new InvalidOperationException("Unsupported intent.");
    }

    internal static void ValidateSnapshot(ApplyAdapterSnapshot s, bool allowTransient = false)
    {
        if (s == null || s.Identity == null || s.Identity.Id == Guid.Empty || s.Identity.InterfaceIndex <= 0
            || string.IsNullOrWhiteSpace(s.Identity.Alias) || s.Identity.Alias.Length > 256
            || s.Identity.Alias.Any(char.IsControl) || s.Identity.PhysicalAddress == null
            || !s.ClassificationComplete || s.Addresses.IsDefault || s.PersistentAddresses.IsDefault
            || s.DnsServers.IsDefault || s.Routes.IsDefault || s.Addresses.Length > 1 || s.PersistentAddresses.Length > 1
            || (!allowTransient && s.Addresses.Length != 1) || s.Routes.Length > 32)
            throw new InvalidOperationException("Unsupported or unknown adapter state.");
        ValidateDns(s.DnsSource, s.DnsServers, observed: true);
        if (s.DnsRegistration is not ("none" or "primary" or "both")) throw new InvalidOperationException("Unknown DNS registration.");
        foreach (var a in s.Addresses.Concat(s.PersistentAddresses))
        {
            if (!Usable(a.Address) || a.PrefixLength < 1 || a.PrefixLength > 30 || !HostInSubnet(a.Address, a.Address, a.PrefixLength)
                || (a.Origin != "manual" && a.Origin != "dhcp")
                ) throw new InvalidOperationException("Unsupported address.");
        }
        if (!allowTransient && s.Addresses.Any(a => a.State != "preferred")) throw new InvalidOperationException("Address not ready.");
        if (!s.Dhcp)
        {
            if (s.DnsSource == "automatic") throw new InvalidOperationException("Static automatic DNS unsupported.");
            if (!allowTransient && (s.PersistentAddresses.Length != 1 || !SameAddress(s.Addresses[0], s.PersistentAddresses[0])
                || s.Addresses[0].Origin != "manual")) throw new InvalidOperationException("Unknown persistent address.");
        }
        else if (s.PersistentAddresses.Any(a => a.Origin == "manual") || s.Addresses.Any(a => a.Origin != "dhcp"))
            throw new InvalidOperationException("DHCP alternate address unsupported.");

        foreach (var r in s.Routes)
        {
            if (r.Store != "active" && r.Store != "persistent") throw new InvalidOperationException("Unknown route store.");
            if (r.Destination == "0.0.0.0/0")
            {
                if (!Usable(r.NextHop) || r.Metric < (s.Dhcp ? 0 : 1) || r.Metric > 9999
                    || r.Origin != (s.Dhcp ? "dhcp" : "netmgmt")) throw new InvalidOperationException("Unsupported gateway.");
            }
            else if (r.Store != "active" || r.Origin != "local" || r.NextHop != "0.0.0.0" || !IsGeneratedLocalRoute(s, r.Destination))
                throw new InvalidOperationException("Custom route unsupported.");
        }
        var active = s.Routes.Where(r => r.Destination == "0.0.0.0/0" && r.Store == "active").ToArray();
        var persistent = s.Routes.Where(r => r.Destination == "0.0.0.0/0" && r.Store == "persistent").ToArray();
        if (active.Length > 1 || persistent.Length > 1
            || (!s.Dhcp && !allowTransient && (active.Length != persistent.Length
                || active.Length == 1 && (active[0].NextHop != persistent[0].NextHop || active[0].Metric != persistent[0].Metric)))
            || (s.Dhcp && persistent.Length != 0)) throw new InvalidOperationException("Complex gateway unsupported.");
        if (!s.Dhcp && !allowTransient && active.Length == 1
            && (active[0].NextHop == s.Addresses[0].Address || !HostInSubnet(active[0].NextHop, s.Addresses[0].Address, s.Addresses[0].PrefixLength)))
            throw new InvalidOperationException("Gateway cannot be restored by the supported contract.");
    }

    internal static ApplyIntent RecoveryIntent(ApplyAdapterSnapshot snapshot)
    {
        ValidateSnapshot(snapshot);
        if (snapshot.Dhcp) return new("dhcp", null, 0, null, snapshot.DnsSource,
            snapshot.DnsSource == "automatic" ? ImmutableArray<string>.Empty : snapshot.DnsServers, DnsRegistration: snapshot.DnsRegistration);
        var a = snapshot.Addresses.Single();
        var g = snapshot.Routes.SingleOrDefault(r => r.Destination == "0.0.0.0/0" && r.Store == "active");
        return new("static", a.Address, a.PrefixLength, g?.NextHop, snapshot.DnsSource, snapshot.DnsServers, g?.Metric ?? 1, snapshot.DnsRegistration);
    }

    private static bool SameAddress(ApplyAddress a, ApplyAddress b) =>
        a.Address == b.Address && a.PrefixLength == b.PrefixLength && a.Origin == b.Origin;
    internal static bool Usable(string value)
    {
        if (!IPAddress.TryParse(value, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork || ip.ToString() != value) return false;
        var b = ip.GetAddressBytes();
        return b[0] != 0 && b[0] != 127 && b[0] < 224 && !(b[0] == 169 && b[1] == 254);
    }
    private static void ValidateDns(string mode, ImmutableArray<string> servers, bool observed = false)
    {
        if (servers.IsDefault || servers.Length > 2 || servers.Distinct().Count() != servers.Length || servers.Any(s => !Usable(s))
            || (mode != "static" && mode != "automatic" && (observed || mode != "leave-unchanged"))
            || (mode == "static" && servers.Length == 0) || (!observed && mode != "static" && servers.Length != 0))
            throw new InvalidOperationException("Unsupported DNS.");
    }

    internal static IReadOnlyList<string[]> Commands(ProjectApplyRequest request)
    {
        ValidateIntent(request.Desired, request.Recovery);
        var d = request.Desired;
        var index = "name=" + request.Before.Identity.InterfaceIndex.ToString(CultureInfo.InvariantCulture);
        var commands = new List<string[]>();
        var registration = "register=" + (d.DnsRegistration ?? request.Before.DnsRegistration);
        if (d.Mode == "dhcp") commands.Add(new[] { "interface", "ipv4", "set", "address", index, "source=dhcp" });
        else commands.Add(new[] { "interface", "ipv4", "set", "address", index, "source=static", "address=" + d.Address,
            "mask=" + Mask(d.PrefixLength), "gateway=" + (d.Gateway ?? "none"), "gwmetric=" + d.GatewayMetric, "store=persistent" });
        if (d.DnsMode == "automatic") commands.Add(new[] { "interface", "ipv4", "set", "dnsservers", index, "source=dhcp", registration, "validate=no" });
        else if (d.DnsMode == "static")
        {
            commands.Add(new[] { "interface", "ipv4", "set", "dnsservers", index, "source=static", "address=" + d.DnsServers[0], registration, "validate=no" });
            if (d.DnsServers.Length == 2) commands.Add(new[] { "interface", "ipv4", "add", "dnsservers", index,
                "address=" + d.DnsServers[1], "index=2", "validate=no" });
        }
        return commands;
    }
    private static string Mask(int prefix) => new IPAddress(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(unchecked((int)(uint.MaxValue << (32 - prefix)))))).ToString();
    private static bool IsGeneratedLocalRoute(ApplyAdapterSnapshot snapshot, string destination)
    {
        if (destination is "224.0.0.0/4" or "255.255.255.255/32") return true;
        foreach (var a in snapshot.Addresses)
        {
            if (destination == a.Address + "/32") return true;
            var bytes = IPAddress.Parse(a.Address).GetAddressBytes();
            uint address = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            uint mask = uint.MaxValue << (32 - a.PrefixLength);
            string Format(uint ip) => string.Join(".", new[] { ip >> 24, (ip >> 16) & 255, (ip >> 8) & 255, ip & 255 });
            if (destination == Format(address & mask) + "/" + a.PrefixLength || destination == Format(address | ~mask) + "/32") return true;
        }
        return false;
    }
    private static bool HostInSubnet(string host, string address, int prefix)
    {
        uint Number(string value) => BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse(value).GetAddressBytes());
        uint mask = uint.MaxValue << (32 - prefix), candidate = Number(host), network = Number(address) & mask;
        return (candidate & mask) == network && candidate != network && candidate != (network | ~mask);
    }
}
