using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public interface IMdnsInterfaces { IReadOnlyList<MdnsInterface> Read(); }
public interface IMdnsReceiver : IDisposable
{
    // Null is an idle tick. Exactly one receive is outstanding, bounded to one second.
    ValueTask<MdnsDatagram?> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellation);
}
public interface IMdnsReceiverFactory { IMdnsReceiver Open(MdnsInterface scope); }

public sealed class WindowsMdnsInterfaces : IMdnsInterfaces
{
    public IReadOnlyList<MdnsInterface> Read()
    {
        var result = new List<MdnsInterface>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || !nic.SupportsMulticast
                || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback || !Guid.TryParse(nic.Id, out var id)) continue;
            var properties = nic.GetIPProperties();
            var v4 = properties.GetIPv4Properties();
            if (v4 == null || v4.Index <= 0) continue;
            var addresses = properties.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork
                && a.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred
                && !IPAddress.IsLoopback(a.Address) && !a.Address.Equals(IPAddress.Any))
                .Select(a => $"{a.Address}/{a.PrefixLength}").OrderBy(s => s, StringComparer.Ordinal).ToImmutableArray();
            if (addresses.Length != 0) result.Add(new(id, v4.Index, nic.Name, nic.GetPhysicalAddress().ToString(), addresses));
        }
        return result;
    }
}

// Narrow native boundary lets fixtures execute production setup/receive semantics without a socket.
public interface IMdnsSocket : IDisposable
{
    void SetOption(SocketOptionLevel level, SocketOptionName name, int value);
    void Join(IPAddress group, int index);
    void Bind(IPEndPoint endpoint);
    ValueTask<SocketReceiveMessageFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellation);
}

public sealed class WindowsMdnsReceiverFactory : IMdnsReceiverFactory
{
    private readonly Func<IMdnsSocket> _create;
    private readonly TimeProvider _clock;
    public WindowsMdnsReceiverFactory() : this(() => new NativeSocket()) { }
    public WindowsMdnsReceiverFactory(Func<IMdnsSocket> create, TimeProvider? clock = null)
        => (_create, _clock) = (create, clock ?? TimeProvider.System);
    public IMdnsReceiver Open(MdnsInterface scope)
    {
        if (scope.Id == Guid.Empty || scope.Index <= 0 || scope.Ipv4.IsDefaultOrEmpty) throw new ArgumentException(nameof(scope));
        var socket = _create();
        try
        {
            // Windows SDK IP_IFLIST=28 / IP_ADD_IFLIST=29; IF_INDEX is host-order DWORD.
            // Fail closed on an unsupported option, without wildcard fallback.
            socket.SetOption(SocketOptionLevel.IP, (SocketOptionName)28, 1);
            socket.SetOption(SocketOptionLevel.IP, (SocketOptionName)29, scope.Index);
            socket.SetOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, 1);
            socket.SetOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            socket.SetOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 64 * 1024);
            socket.Bind(new IPEndPoint(IPAddress.Any, 5353));
            socket.Join(MdnsDiscoverySession.Group, scope.Index);
            return new Receiver(socket, _clock);
        }
        catch { socket.Dispose(); throw; }
    }
    private sealed class Receiver(IMdnsSocket socket, TimeProvider clock) : IMdnsReceiver
    {
        public async ValueTask<MdnsDatagram?> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellation)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(1), clock);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation, deadline.Token);
            try
            {
                var r = await socket.ReceiveAsync(buffer, timeout.Token).ConfigureAwait(false);
                if (r.RemoteEndPoint is not IPEndPoint remote || r.ReceivedBytes < 0 || r.ReceivedBytes > buffer.Length)
                    return new(ReadOnlyMemory<byte>.Empty, 0, IPAddress.Any, 0, true);
                return new(buffer.Slice(0, r.ReceivedBytes), r.PacketInformation.Interface,
                    r.PacketInformation.Address, remote.Port, (r.SocketFlags & (SocketFlags.Truncated | SocketFlags.ControlDataTruncated)) != 0);
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { return null; }
            // An oversized UDP datagram is discarded, not a fatal listener error.
            catch (SocketException e) when (e.SocketErrorCode == SocketError.MessageSize)
            { return new(ReadOnlyMemory<byte>.Empty, 0, IPAddress.Any, 0, true); }
        }
        public void Dispose() => socket.Dispose();
    }
    private sealed class NativeSocket : IMdnsSocket
    {
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public void SetOption(SocketOptionLevel level, SocketOptionName name, int value) => _socket.SetSocketOption(level, name, value);
        public void Join(IPAddress group, int index) => _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(group, index));
        public void Bind(IPEndPoint endpoint) => _socket.Bind(endpoint);
        public ValueTask<SocketReceiveMessageFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellation) =>
            _socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), cancellation);
        public void Dispose() => _socket.Dispose();
    }
}
