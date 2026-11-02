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
        if (intent.Mode == "link-local")
        {
            if (!LinkLocalAddressing.IsCandidate(intent.Address) || intent.PrefixLength != 16 || intent.Gateway != null
                || intent.GatewayMetric != 1 || intent.DnsMode is not ("leave-unchanged" or "static" or "none"))
                throw new InvalidOperationException("Invalid manual link-local intent.");
            ValidateDns(intent.DnsMode, intent.DnsServers);
            return;
        }
        ValidateDns(intent.DnsMode, intent.DnsServers);
        _ = FromProfile(new ProjectProfile
        {
            Id = "profile-validation", Name = "Apply validation", Mode = intent.Mode,
            Ipv4 = new ProjectIpv4 { Address = intent.Address!, PrefixLength = intent.PrefixLength,
                Gateway = intent.Gateway, Dns = new ProjectDns { Mode = intent.DnsMode == "none" ? "leave-unchanged" : intent.DnsMode, Servers = intent.DnsServers.ToList() } }
        });
        if (intent.PrefixLength > 30 || intent.DnsMode == "automatic")
            throw new InvalidOperationException("Unsupported intent.");
    }

    internal static void ValidateSnapshot(ApplyAdapterSnapshot s, bool allowTransient = false, bool allowLinkLocal = false,
        bool restoringDhcp = false, bool allowDhcpWithoutLease = false)
    {
        var noLease = allowDhcpWithoutLease && IsDhcpWithoutLease(s);
        if (s == null || s.Identity == null || s.Identity.Id == Guid.Empty || s.Identity.InterfaceIndex <= 0
            || string.IsNullOrWhiteSpace(s.Identity.Alias) || s.Identity.Alias.Length > 256
            || s.Identity.Alias.Any(char.IsControl) || s.Identity.PhysicalAddress == null
            || !s.ClassificationComplete || s.Addresses.IsDefault || s.PersistentAddresses.IsDefault
            || s.DnsServers.IsDefault || s.Routes.IsDefault || s.Addresses.Length > 1 || s.PersistentAddresses.Length > 1
            || (!allowTransient && !noLease && s.Addresses.Length != 1) || s.Routes.Length > 32)
            throw new InvalidOperationException("Unsupported or unknown adapter state.");
        ValidateDns(s.DnsSource, s.DnsServers, observed: true);
        if (s.Dhcp && s.DnsSource == "none") throw new InvalidOperationException("Invalid DHCP DNS policy.");
        if (s.DnsRegistration is not ("none" or "primary" or "both")) throw new InvalidOperationException("Unknown DNS registration.");
        foreach (var a in s.Addresses.Concat(s.PersistentAddresses))
        {
            var manualLinkLocal = allowLinkLocal && !s.Dhcp && LinkLocalAddressing.IsCandidate(a.Address)
                && a.PrefixLength == 16 && a.Origin == "manual";
            if ((!Usable(a.Address) && !manualLinkLocal && !noLease) || a.PrefixLength < 1 || a.PrefixLength > 30 || !HostInSubnet(a.Address, a.Address, a.PrefixLength)
                || (a.Origin != "manual" && a.Origin != "dhcp" && !noLease)
                ) throw new InvalidOperationException("Unsupported address.");
        }
        if (!allowTransient && s.Addresses.Any(a => a.State != "preferred")) throw new InvalidOperationException("Address not ready.");
        if (!s.Dhcp)
        {
            // Address assignment can fail DAD before explicit DNS replacement.
            // Only recovery TO DHCP may leave that classified intermediate state;
            // it is never accepted as a restorable static source/target.
            if (s.DnsSource == "automatic" && !(restoringDhcp && allowTransient && allowLinkLocal
                && s.Addresses.Length == 1 && LinkLocalAddressing.IsCandidate(s.Addresses[0].Address)
                && s.Addresses[0].PrefixLength == 16 && s.Addresses[0].Origin == "manual"))
                throw new InvalidOperationException("Static automatic DNS unsupported.");
            if (!allowTransient && (s.PersistentAddresses.Length != 1 || !SameAddress(s.Addresses[0], s.PersistentAddresses[0])
                || s.Addresses[0].Origin != "manual")) throw new InvalidOperationException("Unknown persistent address.");
        }
        else if (s.PersistentAddresses.Any(a => a.Origin == "manual") || s.Addresses.Any(a => a.Origin != "dhcp" && !noLease))
            throw new InvalidOperationException("DHCP alternate address unsupported.");

        foreach (var r in s.Routes)
        {
            if (r.Store != "active" && r.Store != "persistent") throw new InvalidOperationException("Unknown route store.");
            if (r.Destination == "0.0.0.0/0")
            {
                if (!Usable(r.NextHop) || r.Metric < (s.Dhcp ? 0 : 1) || r.Metric > 9999
                    || (r.Origin != (s.Dhcp ? "dhcp" : "netmgmt")
                        && !(r.Store == "persistent" && !s.Dhcp && r.Origin == "persistent"))) throw new InvalidOperationException("Unsupported gateway.");
                if (r.Origin == "persistent" && s.Routes.Count(a => a.Store == "active" && a.Destination == r.Destination
                    && a.NextHop == r.NextHop && a.Metric == r.Metric && a.Origin == "netmgmt") != 1)
                    throw new InvalidOperationException("Unmatched persistent gateway.");
            }
            else if (r.Store != "active" || r.Origin != "local" || r.NextHop != "0.0.0.0" || !IsGeneratedLocalRoute(s, r.Destination))
                throw new InvalidOperationException("Custom route unsupported.");
        }
        var active = s.Routes.Where(r => r.Destination == "0.0.0.0/0" && r.Store == "active").ToArray();
        var persistent = s.Routes.Where(r => r.Destination == "0.0.0.0/0" && r.Store == "persistent").ToArray();
        if (s.Addresses.Concat(s.PersistentAddresses).Any(a => LinkLocalAddressing.IsLinkLocal(a.Address))
            && (active.Length != 0 || persistent.Length != 0)) throw new InvalidOperationException("Link-local gateway unsupported.");
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
        ValidateSnapshot(SourceConfiguration(snapshot), allowLinkLocal: true, allowDhcpWithoutLease: true);
        if (snapshot.Dhcp) return new("dhcp", null, 0, null, snapshot.DnsSource,
            snapshot.DnsSource == "automatic" ? ImmutableArray<string>.Empty : snapshot.DnsServers, DnsRegistration: snapshot.DnsRegistration);
        var a = ConfiguredAddress(snapshot);
        var g = snapshot.Routes.SingleOrDefault(r => r.Destination == "0.0.0.0/0" && r.Store == "active");
        return LinkLocalAddressing.IsCandidate(a.Address)
            ? new("link-local", a.Address, 16, null, snapshot.DnsSource, snapshot.DnsServers, DnsRegistration: snapshot.DnsRegistration)
            : new("static", a.Address, a.PrefixLength, g?.NextHop, snapshot.DnsSource, snapshot.DnsServers, g?.Metric ?? 1, snapshot.DnsRegistration);
    }

    internal static void ValidateTransition(ApplyAdapterSnapshot snapshot, ApplyIntent desired, bool recovery)
    {
        ValidateIntent(desired, recovery);
        ValidateSnapshot(SourceConfiguration(snapshot), allowTransient: recovery,
            allowLinkLocal: recovery || desired.Mode is "link-local" or "dhcp", restoringDhcp: recovery && desired.Mode == "dhcp",
            allowDhcpWithoutLease: desired.Mode is "static" or "dhcp");
        if (!recovery && desired.DnsMode == "none" && snapshot.DnsServers.Length != 0)
            throw new InvalidOperationException("Empty profile DNS cannot clear existing servers.");
        if (desired.Mode is "static" or "link-local" && desired.DnsMode == "leave-unchanged" && snapshot.DnsSource == "automatic")
            throw new InvalidOperationException("Static automatic DNS unsupported.");
        if (desired.Mode == "link-local") LinkLocalAddressing.RequireDad(snapshot);
    }

    // Freeze the observed DNS list in the confirmed request when leaving DHCP.
    // Empty profile DNS never silently removes an existing server or invents one.
    internal static ApplyIntent ResolveIntent(ApplyAdapterSnapshot snapshot, ApplyIntent desired) =>
        snapshot.Dhcp && snapshot.DnsSource == "automatic" && !snapshot.DnsServers.IsDefault
            && desired.Mode == "static" && desired.DnsMode == "leave-unchanged"
            ? desired with { DnsMode = snapshot.DnsServers.Length == 0 ? "none" : "static", DnsServers = snapshot.DnsServers }
            : desired;

    internal static ApplyAddress ConfiguredAddress(ApplyAdapterSnapshot snapshot) =>
        snapshot.Addresses.Single(a => a.Origin != "wellknown");

    // A recoverable automatic policy, not a usable DHCP lease. Do not accept
    // alternate static configuration, multiple addresses or a default gateway.
    internal static bool IsDhcpWithoutLease(ApplyAdapterSnapshot? s) =>
        s is { Dhcp: true, Dad: { Connected: true, Transmits: > 0 } }
        && !s.Addresses.IsDefault && !s.PersistentAddresses.IsDefault && !s.Routes.IsDefault
        && s.PersistentAddresses.Length == 0 && s.Addresses.Length <= 1
        && s.Addresses.All(a => a.Origin == "wellknown" && a.State == "preferred"
            && a.PrefixLength == 16 && LinkLocalAddressing.IsCandidate(a.Address))
        && !s.Routes.Any(r => r.Destination == "0.0.0.0/0");

    private static ApplyAdapterSnapshot SourceConfiguration(ApplyAdapterSnapshot snapshot)
    {
        // Windows can retain one configured static address in Duplicate state
        // alongside its automatic APIPA fallback. Retain both in the journal and
        // pre-state comparison; validate recovery intent against the configured IP.
        // This relaxation is for the SOURCE only. Result verification stays strict.
        if (snapshot == null) throw new InvalidOperationException("Missing adapter state.");
        if (snapshot.Dhcp || snapshot.Addresses.IsDefault || snapshot.PersistentAddresses.IsDefault || snapshot.Routes.IsDefault
            || snapshot.PersistentAddresses.Length != 1) return snapshot;
        var configured = snapshot.Addresses.Where(a => a.Origin == "manual").ToArray();
        if (configured.Length != 1 || configured[0].State != "duplicate"
            || !SameAddress(configured[0], snapshot.PersistentAddresses[0])
            || snapshot.Addresses.Length > 2
            || snapshot.Addresses.Any(a => a != configured[0] && !(a.Origin == "wellknown"
                && LinkLocalAddressing.IsCandidate(a.Address) && a.PrefixLength == 16 && a.State == "preferred"))) return snapshot;
        var canonical = snapshot with { Addresses = ImmutableArray.Create(configured[0] with { State = "preferred" }) };
        // Reject custom routes even on the fallback address; only Windows' local
        // subnet/host/broadcast routes may accompany that transient fallback.
        foreach (var r in snapshot.Routes)
            if (r.Destination != "0.0.0.0/0" && (r.Store != "active" || r.Origin != "local"
                || r.NextHop != "0.0.0.0" || !IsGeneratedLocalRoute(snapshot, r.Destination)))
                throw new InvalidOperationException("Custom route unsupported.");
        return canonical with { Routes = snapshot.Routes.Where(r => r.Destination == "0.0.0.0/0"
            || IsGeneratedLocalRoute(canonical, r.Destination)).ToImmutableArray() };
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
            || (mode != "static" && mode != "automatic" && mode != "none" && (observed || mode != "leave-unchanged"))
            || (mode == "none" && servers.Length != 0)
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
        else if (d.DnsMode is "static" or "none")
        {
            commands.Add(new[] { "interface", "ipv4", "set", "dnsservers", index, "source=static", "address=" + (d.DnsMode == "none" ? "none" : d.DnsServers[0]), registration, "validate=no" });
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
