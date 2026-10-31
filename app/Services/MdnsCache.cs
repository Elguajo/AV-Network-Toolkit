using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

// Owned by one session worker; no callbacks or per-packet UI work.
public sealed class MdnsCache
{
    public const int MaxRecords = 1024, MaxServices = 256, MaxAddresses = 16, MaxTtl = 4500;
    private sealed record Entry(MdnsRecord Record, TimeSpan Seen, TimeSpan Expires);
    private readonly Dictionary<(string, ushort, string, ushort), Entry> _records = new();
    private readonly Dictionary<string, (MdnsService Row, TimeSpan Seen)> _recent = new(StringComparer.Ordinal);
    public long Limited { get; private set; }
    public int Count => _records.Count;

    public void Accept(ImmutableArray<MdnsRecord> records, TimeSpan now)
    {
        Expire(now);
        // Protect all records in this packet, and records received in the last second.
        var keys = records.Select(Key).ToHashSet();
        var flushing = records.Where(r => r.Flush && r.Ttl != 0 && r.Type != 12).Select(r => (r.Name, r.Type)).ToHashSet();
        foreach (var pair in _records.ToArray())
            if (flushing.Contains((pair.Value.Record.Name, pair.Value.Record.Type)) && !keys.Contains(pair.Key)
                && now - pair.Value.Seen > TimeSpan.FromSeconds(1))
                _records[pair.Key] = pair.Value with { Expires = Min(pair.Value.Expires, now + TimeSpan.FromSeconds(1)) };
        foreach (var r in records)
        {
            var key = Key(r);
            if (r.Ttl == 0)
            {
                if (_records.TryGetValue(key, out var old)) _records[key] = old with { Expires = Min(old.Expires, now + TimeSpan.FromSeconds(1)) };
                continue;
            }
            if (!_records.ContainsKey(key) && _records.Count >= MaxRecords) { Limited++; continue; }
            _records[key] = new(r, now, now + TimeSpan.FromSeconds(Math.Min(r.Ttl, MaxTtl)));
        }
    }

    public ImmutableArray<MdnsService> Snapshot(TimeSpan now, int index, bool stopped = false)
    {
        Expire(now);
        var all = _records.Values.ToArray();
        var byName = all.ToLookup(e => e.Record.Name, StringComparer.Ordinal);
        var ptrs = all.Where(e => e.Record.Type == 12).ToLookup(e => e.Record.Value, StringComparer.Ordinal);
        var instances = all.Where(e => e.Record.Type == 12 && IsServiceType(e.Record.Name)
            && e.Record.Value.EndsWith("." + e.Record.Name, StringComparison.Ordinal))
            .Select(e => (Instance: e.Record.Value, Type: e.Record.Name))
            .Concat(all.Where(e => e.Record.Type == 33).Select(e => (Instance: e.Record.Name, Type: ServiceType(e.Record.Name))))
            .Where(e => IsServiceType(e.Type)).Distinct().OrderBy(e => e.Instance, StringComparer.Ordinal).ToArray();
        int count = 0;
        foreach (var item in instances)
        {
            if (count++ >= MaxServices) break;
            var dependencies = byName[item.Instance].Where(e => e.Record.Type == 33).ToArray();
            var ptr = ptrs[item.Instance].Where(e => e.Record.Name == item.Type).ToArray();
            var srv = dependencies.Length == 1 ? dependencies[0] : null;
            string host = srv?.Record.Value ?? "", port = srv?.Record.Port.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
            var addresses = byName[host].Where(e => e.Record.Type is 1 or 28).ToArray();
            var used = ptr.Concat(dependencies).Concat(addresses).ToArray();
            int ttl = used.Length == 0 ? 0 : (int)Math.Max(0, Math.Ceiling(used.Min(e => (e.Expires - now).TotalSeconds)));
            var row = new MdnsService(item.Instance, item.Type, host,
                string.Join(", ", addresses.Select(e => ScopedAddress(e.Record.Value, index)).Distinct().OrderBy(s => s, StringComparer.Ordinal).Take(MaxAddresses)),
                port, stopped ? 0 : ttl, srv == null || srv.Record.Port == 0 || ptr.Length == 0 || addresses.Length == 0 || addresses.Length > MaxAddresses, stopped);
            if (!_recent.ContainsKey(item.Instance) && _recent.Count >= MaxServices)
            {
                var victim = _recent.OrderBy(e => e.Value.Seen).First().Key;
                _recent.Remove(victim);
            }
            _recent[item.Instance] = (row, now);
        }
        var active = instances.Take(MaxServices).Select(e => e.Instance).ToHashSet(StringComparer.Ordinal);
        foreach (var key in _recent.Keys.ToArray())
        {
            var old = _recent[key];
            if (now - old.Seen >= TimeSpan.FromSeconds(60)) { _recent.Remove(key); continue; }
            if (!active.Contains(key) || stopped) _recent[key] = (old.Row with { Stale = true, Ttl = 0 }, old.Seen);
        }
        return _recent.Values.Select(v => v.Row).OrderBy(r => r.Instance, StringComparer.Ordinal).ToImmutableArray();
    }
    private void Expire(TimeSpan now)
    { foreach (var key in _records.Where(p => p.Value.Expires <= now).Select(p => p.Key).ToArray()) _records.Remove(key); }
    private static (string, ushort, string, ushort) Key(MdnsRecord r) => (r.Name, r.Type, r.Value, r.Port);
    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
    private static string ScopedAddress(string value, int index)
    {
        var address = IPAddress.Parse(value);
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv6LinkLocal) address.ScopeId = index;
        return address.ToString();
    }
    private static string ServiceType(string name)
    {
        int dot = name.IndexOf('.');
        return dot < 0 ? "" : name.Substring(dot + 1);
    }
    private static bool IsServiceType(string name)
    {
        var labels = name.Split('.');
        return labels.Length == 3 && labels[0].StartsWith("_", StringComparison.Ordinal) && labels[0].Length > 1
            && (labels[1] == "_tcp" || labels[1] == "_udp") && labels[2] == "local";
    }
}
