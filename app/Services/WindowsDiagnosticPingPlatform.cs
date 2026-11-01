using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public sealed class WindowsDiagnosticPingPlatform : IDiagnosticPingPlatform
{
    public IReadOnlyList<PingBinding> ReadBindings() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel)
        .Select(n =>
        {
            var properties = n.GetIPProperties();
            return new PingBinding(Guid.Parse(n.Id), properties.GetIPv4Properties()?.Index ?? 0,
                n.GetPhysicalAddress().ToString(), n.OperationalStatus == OperationalStatus.Up,
                properties.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork
                    && a.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred)
                    .Select(a => a.Address.ToString()).ToImmutableArray());
        }).ToArray();

    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellation)
        => Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork, cancellation);

    public PredictedRoute? Predict(IPAddress source, IPAddress destination)
    {
        var src = Sockaddr.From(source); var dst = Sockaddr.From(destination);
        // GetBestRoute selects the OS IPv4 route, using source as its interface tie-break.
        // GetBestRoute2 requires an initialized interface: use the OS-selected index, never
        // force the requested index here (which could hide a conflicting OS route choice).
        uint error = GetBestRoute(BitConverter.ToUInt32(destination.GetAddressBytes()),
            BitConverter.ToUInt32(source.GetAddressBytes()), out var selected);
        if (error != 0 || selected.Index == 0) return null;
        error = GetBestRoute2(IntPtr.Zero, selected.Index, ref src, ref dst, 0, out var row, out _);
        return error == 0 && row.Prefix.Address.Family == 2 && row.NextHop.Family == 2 && row.Prefix.Length <= 32
            ? new((int)row.Index, row.Prefix.Address.Text, row.Prefix.Length,
            row.NextHop.Text, row.Metric) : null;
    }

    public Task<DiagnosticEcho> EchoAsync(IPAddress source, IPAddress destination, CancellationToken cancellation)
        => Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            var handle = IcmpCreateFile();
            if (handle == new IntPtr(-1) || handle == IntPtr.Zero) throw new Win32Exception();
            var reply = IntPtr.Zero;
            try
            {
                reply = Marshal.AllocHGlobal(1024);
                var data = new byte[32];
                cancellation.ThrowIfCancellationRequested();
                uint count = IcmpSendEcho2Ex(handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    BitConverter.ToUInt32(source.GetAddressBytes()), BitConverter.ToUInt32(destination.GetAddressBytes()),
                    data, (ushort)data.Length, IntPtr.Zero, reply, 1024, 2000);
                if (count == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error is >= 11000 and < 12000) return new DiagnosticEcho((uint)error, "", -1);
                    throw new Win32Exception(error);
                }
                return ReadEcho(reply);
            }
            finally { if (reply != IntPtr.Zero) Marshal.FreeHGlobal(reply); IcmpCloseHandle(handle); }
        }, cancellation);

    // Native pointer-sized layout follows .NET 8's Windows Ping implementation. Only read
    // header values and the TTL byte; never follow native data/options pointers.
    internal static DiagnosticEcho ReadEcho(IntPtr reply)
    {
        uint status = unchecked((uint)Marshal.ReadInt32(reply, 4));
        return new(status, new IPAddress(BitConverter.GetBytes(Marshal.ReadInt32(reply))).ToString(),
            unchecked((uint)Marshal.ReadInt32(reply, 8)), status == 0 ? Marshal.ReadByte(reply, IntPtr.Size == 8 ? 24 : 20) : null);
    }

    [StructLayout(LayoutKind.Explicit, Size = 28)]
    internal struct Sockaddr
    {
        [FieldOffset(0)] public ushort Family;
        [FieldOffset(4)] public uint Address;
        public string Text => new IPAddress(BitConverter.GetBytes(Address)).ToString();
        public static Sockaddr From(IPAddress address) => new() { Family = 2, Address = BitConverter.ToUInt32(address.GetAddressBytes()) };
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Prefix { public Sockaddr Address; public byte Length; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct ForwardRow
    {
        public ulong Luid; public uint Index; public Prefix Prefix; public Sockaddr NextHop;
        public byte SitePrefixLength; public uint ValidLifetime, PreferredLifetime, Metric, Protocol;
        public byte Loopback, Autoconfigure, Publish, Immortal;
        public uint Age, Origin;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct RouteSelectionRow
    {
        public uint Destination, Mask, Policy, NextHop, Index, Type, Protocol, Age, NextHopAs;
        public uint Metric1, Metric2, Metric3, Metric4, Metric5;
    }
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetBestRoute(uint destination, uint source, out RouteSelectionRow row);
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetBestRoute2(IntPtr luid, uint index, ref Sockaddr source, ref Sockaddr destination,
        uint options, out ForwardRow row, out Sockaddr bestSource);
    [DllImport("iphlpapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr IcmpCreateFile();
    [DllImport("iphlpapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern uint IcmpSendEcho2Ex(IntPtr handle, IntPtr signal, IntPtr callback, IntPtr context,
        uint source, uint destination, byte[] data, ushort size, IntPtr options, IntPtr reply, uint replySize, uint timeout);
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IcmpCloseHandle(IntPtr handle);
}
