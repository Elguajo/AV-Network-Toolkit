using System;
using System.Runtime.InteropServices;

namespace NetworkProfileManager.Services;

internal static class WindowsAdapterVisibility
{
    internal static bool IsSystemInterface(string description, byte? flags, bool hasIpv4)
    {
        // FilterInterface is bit 1, not the ConnectorPresent or NotMediaConnected bits.
        if ((flags.GetValueOrDefault() & 0x02) != 0) return true;
        // Built-in idle WAN/debug plumbing is not a profile target. A connected RAS
        // interface with IPv4 stays visible. Never classify by the user's alias.
        return !hasIpv4 && (description.StartsWith("WAN Miniport (", StringComparison.OrdinalIgnoreCase)
            || description.Equals("Microsoft Kernel Debug Network Adapter", StringComparison.OrdinalIgnoreCase));
    }

    internal static byte? ReadFlags(Guid id)
    {
        try
        {
            if (ConvertInterfaceGuidToLuid(ref id, out var luid) != 0) return null;
            var row = new InterfaceRow { Luid = luid };
            return GetIfEntry2(ref row) == 0 && row.Id == id ? row.Flags : null;
        }
        catch (DllNotFoundException) { return null; }
        catch (EntryPointNotFoundException) { return null; }
    }

    // MIB_IF_ROW2, netioapi.h: 8-byte alignment, WCHAR[257] aliases/descriptions,
    // 32-byte physical-address arrays. Reserve the ENTIRE native output, including
    // counters we do not read. There are no pointers; layout is identical on x86/x64.
    // https://learn.microsoft.com/windows/win32/api/netioapi/ns-netioapi-mib_if_row2
    [StructLayout(LayoutKind.Explicit, Size = 1352)]
    internal struct InterfaceRow
    {
        [FieldOffset(0)] public ulong Luid;
        [FieldOffset(12)] public Guid Id;
        [FieldOffset(1152)] public byte Flags;
    }

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint ConvertInterfaceGuidToLuid(ref Guid id, out ulong luid);
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetIfEntry2(ref InterfaceRow row);
}
