using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

// Optional, slower inventory must never gate source selection or running a probe.
public interface IAdapterDiagnosticsDetails : IAdapterDiagnostics
{
    Task<IReadOnlyList<DiagnosticAdapter>> ReadDetailsAsync(CancellationToken cancellation);
}

internal sealed class WindowsQuickAdapterDiagnostics : IAdapterDiagnosticsDetails
{
    public Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken cancellation)
        => Task.Run<IReadOnlyList<DiagnosticAdapter>>(() => Read(cancellation), cancellation);

    public Task<IReadOnlyList<DiagnosticAdapter>> ReadDetailsAsync(CancellationToken cancellation)
        => new WindowsAdapterDiagnostics().ReadAsync(cancellation);

    private static IReadOnlyList<DiagnosticAdapter> Read(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        var addresses = NativeRows<UnicastRow>(GetUnicastIpAddressTable, 32768);
        var routes = NativeRows<WindowsDiagnosticPingPlatform.ForwardRow>(GetIpForwardTable2, 65536);
        var result = new List<DiagnosticAdapter>();
        foreach (var nic in interfaces)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!nic.Supports(NetworkInterfaceComponent.IPv4)) continue;
            var properties = nic.GetIPProperties();
            var ipv4 = properties.GetIPv4Properties();
            if (ipv4 == null || ipv4.Index <= 0 || !Guid.TryParse(nic.Id, out var id) || id == Guid.Empty) continue;
            result.Add(Project(id, ipv4.Index, nic.Name, nic.Description, nic.GetPhysicalAddress().ToString(),
                nic.OperationalStatus.ToString(), nic.Speed.ToString(CultureInfo.InvariantCulture) + " bps",
                ipv4.IsDhcpEnabled, properties.DnsAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.ToString()).ToImmutableArray(), addresses, routes));
        }
        if (result.Count > 256 || result.Select(a => a.Id).Distinct().Count() != result.Count
            || result.Select(a => a.Index).Distinct().Count() != result.Count) throw new IOException();
        return result;
    }

    internal static DiagnosticAdapter Project(Guid id, int index, string name, string description, string mac,
        string link, string speed, bool dhcp, ImmutableArray<string> dns, IReadOnlyList<UnicastRow> addresses,
        IReadOnlyList<WindowsDiagnosticPingPlatform.ForwardRow> routes)
    {
        var ips = addresses.Where(a => a.Index == index).Select(a =>
        {
            if (a.Address.Family != 2 || a.PrefixLength > 32) throw new IOException();
            var state = Enum.IsDefined(typeof(DuplicateAddressDetectionState), (int)a.DadState)
                ? ((DuplicateAddressDetectionState)a.DadState).ToString() : "Unknown";
            return new DiagnosticAddress(a.Address.Text, a.PrefixLength, state, a.SkipAsSource != 0);
        }).ToImmutableArray();
        var routeFacts = routes.Where(r => r.Index == index).Select(r =>
        {
            if (r.Prefix.Address.Family != 2 || r.NextHop.Family != 2 || r.Prefix.Length > 32 || r.Metric > int.MaxValue)
                throw new IOException();
            return new DiagnosticRoute($"{r.Prefix.Address.Text}/{r.Prefix.Length}", r.NextHop.Text, (int)r.Metric);
        }).ToImmutableArray();
        if (ips.Length > 128 || routeFacts.Length > 4096) throw new IOException();
        // Ethernet type/name does not prove physical hardware or an enabled Hyper-V binding.
        return new(id, index, name, description, mac, "unknown", null, null, link, speed,
            dhcp ? "Enabled" : "Disabled", ips, dns, routeFacts);
    }

    private delegate uint ReadNativeTable(ushort family, out IntPtr table);
    private static T[] NativeRows<T>(ReadNativeTable read, int limit) where T : struct
    {
        IntPtr table = IntPtr.Zero;
        try
        {
            if (read(2, out table) != 0 || table == IntPtr.Zero) throw new IOException();
            return CopyRows<T>(table, limit);
        }
        finally { if (table != IntPtr.Zero) FreeMibTable(table); }
    }

    internal static T[] CopyRows<T>(IntPtr table, int limit) where T : struct
    {
        var count = Marshal.ReadInt32(table);
        if (count < 0 || count > limit) throw new IOException();
        var rows = new T[count];
        int size = Marshal.SizeOf<T>();
        // Both Windows rows contain 8-byte-aligned NET_LUID members; table header has padding.
        for (int i = 0; i < count; i++) rows[i] = Marshal.PtrToStructure<T>(IntPtr.Add(table, checked(8 + i * size)));
        return rows;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UnicastRow
    {
        public WindowsDiagnosticPingPlatform.Sockaddr Address;
        public ulong Luid;
        public uint Index, PrefixOrigin, SuffixOrigin, ValidLifetime, PreferredLifetime;
        public byte PrefixLength, SkipAsSource;
        public uint DadState, ScopeId;
        public long CreationTime;
    }
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetUnicastIpAddressTable(ushort family, out IntPtr table);
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetIpForwardTable2(ushort family, out IntPtr table);
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern void FreeMibTable(IntPtr table);
}
