using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Text;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public static class MdnsPacketParser
{
    public const int MaxPacket = 9000, MaxRecords = 128;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static bool TryParse(ReadOnlySpan<byte> packet, out ImmutableArray<MdnsRecord> records)
    {
        records = ImmutableArray<MdnsRecord>.Empty;
        if (packet.Length < 12 || packet.Length > MaxPacket) return false;
        try
        {
            var p = 0;
            U16(packet, ref p); // RFC 6762 §18.1: ID is ignored on reception.
            var flags = U16(packet, ref p);
            // QR response, standard opcode, successful RCODE. Other header flags
            // (including AA/TC) are ignored per §18; actual truncation still fails.
            if ((flags & 0xf80f) != 0x8000) return false;
            int questions = U16(packet, ref p), answers = U16(packet, ref p);
            int authorities = U16(packet, ref p), additional = U16(packet, ref p);
            int total = questions + answers + authorities + additional;
            if (total > MaxRecords) return false;
            for (var i = 0; i < questions; i++) { Name(packet, ref p); Take(packet, ref p, 4); }
            var result = ImmutableArray.CreateBuilder<MdnsRecord>();
            for (var i = questions; i < total; i++)
            {
                string owner = Name(packet, ref p);
                ushort type = U16(packet, ref p), cls = U16(packet, ref p);
                uint ttl = BinaryPrimitives.ReadUInt32BigEndian(Take(packet, ref p, 4));
                int length = U16(packet, ref p), start = p;
                Take(packet, ref p, length);
                if ((cls & 0x7fff) != 1 || !IsLocal(owner)) continue;
                int data = start; string? value = null; ushort port = 0;
                switch (type)
                {
                    case 12:
                        value = Name(packet, ref data);
                        if (data != p || !IsLocal(value)) return false;
                        break;
                    case 33:
                        Take(packet, ref data, 4); port = U16(packet, ref data);
                        value = Name(packet, ref data);
                        if (data != p || !IsLocal(value)) return false;
                        break;
                    case 1:
                    case 28:
                        if (length != (type == 1 ? 4 : 16)) return false;
                        value = new IPAddress(packet.Slice(start, length)).ToString();
                        break;
                }
                if (value != null) result.Add(new(owner, type, value, port, ttl, (cls & 0x8000) != 0));
            }
            if (p != packet.Length) return false;
            records = result.ToImmutable(); return true;
        }
        catch (Exception e) when (e is FormatException or ArgumentOutOfRangeException or DecoderFallbackException)
        { return false; }
    }

    private static bool IsLocal(string value) => value.EndsWith(".local", StringComparison.Ordinal);
    private static ushort U16(ReadOnlySpan<byte> b, ref int p) => BinaryPrimitives.ReadUInt16BigEndian(Take(b, ref p, 2));
    private static ReadOnlySpan<byte> Take(ReadOnlySpan<byte> b, ref int p, int count)
    {
        if (p < 0 || count < 0 || count > b.Length - p) throw new FormatException();
        var part = b.Slice(p, count); p += count; return part;
    }
    private static string Name(ReadOnlySpan<byte> b, ref int position)
    {
        int p = position, end = -1, pointers = 0, expanded = 1;
        var labels = new List<string>();
        while (true)
        {
            int labelStart = p;
            int n = Take(b, ref p, 1)[0];
            if (n == 0) break;
            if ((n & 0xc0) == 0xc0)
            {
                int target = ((n & 0x3f) << 8) | Take(b, ref p, 1)[0];
                if (target < 12 || target >= labelStart || ++pointers > 16) throw new FormatException();
                if (end < 0) end = p;
                p = target; continue;
            }
            if (n > 63 || (expanded += n + 1) > 255) throw new FormatException();
            var label = Utf8.GetString(Take(b, ref p, n));
            foreach (char c in label)
                if (char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format || c is '.' or '\\')
                    throw new FormatException();
            labels.Add(label.ToLowerInvariant());
        }
        position = end < 0 ? p : end;
        return string.Join(".", labels);
    }
}
