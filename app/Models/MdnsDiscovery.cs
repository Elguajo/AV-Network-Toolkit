using System;
using System.Collections.Immutable;
using System.Net;

namespace NetworkProfileManager.Models;

// A read-only discovery identity, deliberately independent of apply/history models.
public sealed record MdnsInterface(Guid Id, int Index, string Name, string Mac, ImmutableArray<string> Ipv4)
{
    public string Display => $"{Name} · {Index} · {Id} · {string.Join(", ", Ipv4)}";
    public bool SameLink(MdnsInterface other) => Id == other.Id && Index == other.Index && Mac == other.Mac
        && Ipv4.AsSpan().SequenceEqual(other.Ipv4.AsSpan());
}

public sealed record MdnsDatagram(ReadOnlyMemory<byte> Bytes, int InterfaceIndex, IPAddress Destination,
    int SourcePort, bool Truncated = false);

public sealed record MdnsRecord(string Name, ushort Type, string Value, ushort Port, uint Ttl, bool Flush);

public sealed record MdnsService(string Instance, string ServiceType, string Hostname, string Addresses,
    string Port, int Ttl, bool Incomplete, bool Stale)
{
    public string SearchText => $"{Instance} {ServiceType} {Hostname} {Addresses} {Port}";
}

public enum MdnsState { Idle, Starting, Listening, Stopped, InterfaceChanged, TimedOut, Failed }
public sealed record MdnsSnapshot(MdnsState State, MdnsInterface? Scope, ImmutableArray<MdnsService> Services,
    long Received, long Rejected, long Limited);
