using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Net;
using System.Text;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class MdnsPacketTests
{
    [Fact]
    public void ParsesOriginalServiceFixtureWithAddressesAndFlush()
    {
        Assert.True(MdnsPacketParser.TryParse(MdnsFixture.Packet(), out var records));
        Assert.Equal(4, records.Length);
        Assert.Equal("panel._http._tcp.local", records[0].Value);
        Assert.Equal((ushort)8443, records[1].Port);
        Assert.True(records[1].Flush);
        Assert.Equal("192.0.2.44", records[2].Value);
        Assert.Equal("fe80::44", records[3].Value);
    }
    [Fact]
    public void AcceptsBackwardCompressedOwnersAndTargets()
    {
        var bytes = MdnsFixture.Packet().ToList();
        // Append another A record whose owner is a pointer to the first owner (_http...).
        BinaryPrimitives.WriteUInt16BigEndian(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(bytes).Slice(6), 5);
        bytes.AddRange(new byte[] { 0xc0, 12, 0, 1, 0, 1, 0, 0, 0, 120, 0, 4, 192, 0, 2, 55 });
        Assert.True(MdnsPacketParser.TryParse(bytes.ToArray(), out var records));
        Assert.Equal("_http._tcp.local", records[4].Name);
    }
    [Fact]
    public void EveryTruncationFailsWithoutPartialCacheOutput()
    {
        var bytes = MdnsFixture.Packet();
        for (int i = 0; i < bytes.Length; i++)
        {
            Assert.False(MdnsPacketParser.TryParse(bytes.AsSpan(0, i), out var records));
            Assert.Empty(records);
        }
    }
    [Theory]
    [InlineData(2, 0)] // query
    [InlineData(2, 0x8c)] // nonstandard opcode
    [InlineData(3, 1)] // RCODE
    [InlineData(6, 0xff)] // excessive records
    [InlineData(12, 64)] // extended label
    [InlineData(12, 0xff)] // out of range pointer
    [InlineData(13, 0x01)] // control label
    [InlineData(13, 0xff)] // invalid UTF8
    [InlineData(13, 0x2e)] // ambiguous dot
    [InlineData(13, 0x5c)] // ambiguous backslash
    public void RejectsMalformedEnvelopesAndNames(int at, int value)
    {
        var packet = MdnsFixture.Packet(); packet[at] = (byte)value;
        Assert.False(MdnsPacketParser.TryParse(packet, out var records)); Assert.Empty(records);
    }
    [Fact]
    public void IgnoresIdAndNonSemanticHeaderFlagsButStillChecksActualTruncation()
    {
        var packet = MdnsFixture.Packet(); packet[0] = 0x12; packet[1] = 0x34;
        packet[2] = 0x83; packet[3] = 0xf0; // QR, no AA, TC/RD/RA/Z/AD/CD
        Assert.True(MdnsPacketParser.TryParse(packet, out _));
        Assert.False(MdnsPacketParser.TryParse(packet.AsSpan(0, packet.Length - 1), out _));
    }
    [Theory]
    [InlineData(12)] // self pointer
    [InlineData(14)] // forward
    [InlineData(0)] // header
    public void RejectsInvalidCompression(int target)
    {
        var packet = MdnsFixture.Packet(); packet[12] = 0xc0; packet[13] = (byte)target;
        Assert.False(MdnsPacketParser.TryParse(packet, out _));
    }
    [Fact]
    public void MalformedRdataAndTrailingBytesCannotProducePartialRecords()
    {
        var ptr = MdnsFixture.Record("_http._tcp.local", 12, new byte[] { 0 }, ttl: 120);
        // Valid owner but PTR target outside .local and SRV too short.
        Assert.False(MdnsPacketParser.TryParse(MdnsFixture.Message(ptr), out _));
        Assert.False(MdnsPacketParser.TryParse(MdnsFixture.Message(MdnsFixture.Record("panel._http._tcp.local", 33, new byte[] { 0, 0 })), out _));
        Assert.False(MdnsPacketParser.TryParse(MdnsFixture.Message(MdnsFixture.Record("host.local", 1, new byte[3])), out _));
        Assert.False(MdnsPacketParser.TryParse(MdnsFixture.Packet().Concat(new byte[] { 0 }).ToArray(), out _));
    }
    [Fact]
    public void UnknownTxtAndNonInternetClassAreNotRetained()
    {
        Assert.True(MdnsPacketParser.TryParse(MdnsFixture.Message(MdnsFixture.Record("host.local", 16, Encoding.UTF8.GetBytes("token=NEVER_STORE"))), out var records));
        Assert.Empty(records);
        var rr = MdnsFixture.Record("host.local", 1, new byte[] { 192, 0, 2, 44 }, cls: 3);
        Assert.True(MdnsPacketParser.TryParse(MdnsFixture.Message(rr), out records)); Assert.Empty(records);
    }
    [Fact]
    public void BoundedRandomNegativePacketsDoNotThrow()
    {
        var random = new Random(6);
        for (int i = 0; i < 2000; i++)
        {
            var packet = new byte[random.Next(0, 1000)]; random.NextBytes(packet);
            MdnsPacketParser.TryParse(packet, out _);
        }
        Assert.False(MdnsPacketParser.TryParse(new byte[MdnsPacketParser.MaxPacket + 1], out _));
    }
    [Fact]
    public void ExpandedNameAndCompressionCyclesAreBounded()
    {
        var labels = string.Join(".", Enumerable.Repeat(new string('x', 63), 5)) + ".local";
        Assert.False(MdnsPacketParser.TryParse(MdnsFixture.Message(MdnsFixture.Record(labels, 1, new byte[4])), out _));
        var p = MdnsFixture.Packet(); p[14] = 0xc0; p[15] = 12;
        Assert.False(MdnsPacketParser.TryParse(p, out _));
    }
}

internal static class MdnsFixture
{
    internal static readonly MdnsInterface Nic = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006"), 6, "Fixture Ethernet", "020000000006", ImmutableArray.Create("192.0.2.10/24"));
    internal static byte[] Name(string value)
    {
        var bytes = new List<byte>();
        foreach (string label in value.Split('.')) { var b = Encoding.UTF8.GetBytes(label); bytes.Add((byte)b.Length); bytes.AddRange(b); }
        bytes.Add(0); return bytes.ToArray();
    }
    internal static byte[] Record(string name, ushort type, byte[] data, uint ttl = 120, ushort cls = 1)
    {
        var b = Name(name).ToList();
        void U16(ushort v) { b.Add((byte)(v >> 8)); b.Add((byte)v); }
        U16(type); U16(cls);
        b.AddRange(new[] { (byte)(ttl >> 24), (byte)(ttl >> 16), (byte)(ttl >> 8), (byte)ttl });
        U16((ushort)data.Length); b.AddRange(data); return b.ToArray();
    }
    internal static byte[] Message(params byte[][] records)
    {
        var b = new byte[12]; b[2] = 0x84; BinaryPrimitives.WriteUInt16BigEndian(b.AsSpan(6), (ushort)records.Length);
        return b.Concat(records.SelectMany(r => r)).ToArray();
    }
    internal static byte[] Packet(uint ttl = 120) => Message(
        Record("_http._tcp.local", 12, Name("panel._http._tcp.local"), ttl),
        Record("panel._http._tcp.local", 33, new byte[] { 0, 0, 0, 0, 0x20, 0xfb }.Concat(Name("host.local")).ToArray(), ttl, 0x8001),
        Record("host.local", 1, IPAddress.Parse("192.0.2.44").GetAddressBytes(), ttl, 0x8001),
        Record("host.local", 28, IPAddress.Parse("fe80::44").GetAddressBytes(), ttl, 0x8001));
    internal static ImmutableArray<MdnsRecord> Records(uint ttl = 120)
    { Assert.True(MdnsPacketParser.TryParse(Packet(ttl), out var r)); return r; }
    internal static MdnsDatagram Datagram() => new(Packet(), Nic.Index, MdnsDiscoverySession.Group, 5353);
}
