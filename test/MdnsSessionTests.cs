using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Channels;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class MdnsSessionTests
{
    [Fact]
    public async Task ExplicitStartStopRestartIsBoundAndDoesNotAutoselect()
    {
        using var f = new SessionFixture();
        Assert.Equal(0, f.Factory.Opened); Assert.Null(f.Session.Snapshot.Scope);
        await f.Session.StartAsync(MdnsFixture.Nic);
        Assert.Equal(MdnsState.Listening, f.Session.Snapshot.State);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.StartAsync(MdnsFixture.Nic));
        await f.Session.StopAsync(); await f.Session.StopAsync();
        Assert.True(f.Factory.Last!.Disposed); Assert.False(f.Session.IsActive);
        Assert.Equal(MdnsState.Stopped, f.Session.Snapshot.State);
        await f.Session.StartAsync(MdnsFixture.Nic);
        Assert.Equal(2, f.Factory.Opened); Assert.Empty(f.Session.Snapshot.Services);
        await f.Session.StopAsync();
        f.Session.Dispose(); await Assert.ThrowsAsync<ObjectDisposedException>(() => f.Session.StartAsync(MdnsFixture.Nic));
    }
    [Theory]
    [InlineData("id")]
    [InlineData("index")]
    [InlineData("mac")]
    [InlineData("address")]
    [InlineData("prefix")]
    [InlineData("disconnect")]
    public async Task ChangedScopeBeforeStartDoesNotOpenSocket(string change)
    {
        using var f = new SessionFixture(); f.Catalog.Current = Change(change);
        await f.Session.StartAsync(MdnsFixture.Nic);
        await f.Eventually(() => !f.Session.IsActive);
        Assert.Equal(0, f.Factory.Opened); Assert.Equal(MdnsState.InterfaceChanged, f.Session.Snapshot.State);
    }
    [Theory]
    [InlineData("id")]
    [InlineData("index")]
    [InlineData("mac")]
    [InlineData("address")]
    [InlineData("prefix")]
    [InlineData("disconnect")]
    public async Task ChangedScopeWhileIdleStopsWithoutRebind(string change)
    {
        using var f = new SessionFixture(); await f.Session.StartAsync(MdnsFixture.Nic);
        f.Catalog.Current = Change(change); f.Clock.Advance(TimeSpan.FromSeconds(2));
        f.Factory.Last!.Enqueue(null);
        await f.Eventually(() => !f.Session.IsActive);
        Assert.Equal(MdnsState.InterfaceChanged, f.Session.Snapshot.State); Assert.Equal(1, f.Factory.Opened);
        Assert.True(f.Factory.Last.Disposed);
    }
    [Fact]
    public async Task RechecksAfterBindAndRejectsChangeWithoutFallback()
    {
        using var f = new SessionFixture(); f.Factory.OnOpen = () => f.Catalog.Current = null;
        await f.Session.StartAsync(MdnsFixture.Nic); await f.Eventually(() => !f.Session.IsActive);
        Assert.Equal(MdnsState.InterfaceChanged, f.Session.Snapshot.State); Assert.True(f.Factory.Last!.Disposed);
    }
    [Fact]
    public async Task AliasRenameDoesNotMoveStableLink()
    {
        using var f = new SessionFixture(); f.Catalog.Current = MdnsFixture.Nic with { Name = "Renamed fixture" };
        await f.Session.StartAsync(MdnsFixture.Nic);
        Assert.Equal(MdnsState.Listening, f.Session.Snapshot.State); await f.Session.StopAsync();
    }
    [Theory]
    [InlineData("foreign")]
    [InlineData("unknown")]
    [InlineData("unicast")]
    [InlineData("port")]
    [InlineData("truncated")]
    [InlineData("malformed")]
    public async Task RejectsForeignInterfaceAndInvalidPacketMetadataBeforeCaching(string bad)
    {
        using var f = new SessionFixture(); await f.Session.StartAsync(MdnsFixture.Nic);
        var packet = MdnsFixture.Datagram();
        packet = bad switch
        {
            "foreign" => packet with { InterfaceIndex = 8 }, "unknown" => packet with { InterfaceIndex = 0 },
            "unicast" => packet with { Destination = IPAddress.Parse("192.0.2.10") },
            "port" => packet with { SourcePort = 53 }, "truncated" => packet with { Truncated = true },
            _ => packet with { Bytes = new byte[] { 0, 1 } }
        };
        f.Factory.Last!.Enqueue(packet);
        await f.Eventually(() => f.Factory.Last.ReadCount >= 1);
        await f.Session.StopAsync();
        Assert.Equal(1, f.Session.Snapshot.Rejected); Assert.Empty(f.Session.Snapshot.Services);
    }
    [Fact]
    public async Task AcceptedPacketGetsScopedRowsAndStopMarksEverythingStale()
    {
        using var f = new SessionFixture(); await f.Session.StartAsync(MdnsFixture.Nic);
        f.Factory.Last!.Enqueue(MdnsFixture.Datagram());
        await f.Eventually(() => f.Factory.Last.ReadCount >= 1);
        f.Clock.Advance(TimeSpan.FromSeconds(2)); f.Factory.Last.Enqueue(null);
        await f.Eventually(() => f.Session.Snapshot.Services.Length != 0);
        Assert.False(Assert.Single(f.Session.Snapshot.Services).Stale);
        await f.Session.StopAsync();
        var row = Assert.Single(f.Session.Snapshot.Services); Assert.True(row.Stale); Assert.Equal(0, row.Ttl);
    }
    [Fact]
    public async Task TenMinuteSessionDeadlineUsesClockAndDisposes()
    {
        using var f = new SessionFixture(); await f.Session.StartAsync(MdnsFixture.Nic);
        f.Clock.Advance(TimeSpan.FromMinutes(10)); f.Factory.Last!.Enqueue(null);
        await f.Eventually(() => !f.Session.IsActive);
        Assert.Equal(MdnsState.TimedOut, f.Session.Snapshot.State); Assert.True(f.Factory.Last.Disposed);
    }
    [Theory]
    [InlineData("open")]
    [InlineData("read")]
    [InlineData("catalog")]
    public async Task ErrorsAreTerminalRedactedAndNeverRetry(string stage)
    {
        using var f = new SessionFixture();
        f.Factory.Fail = stage == "open"; f.Catalog.Fail = stage == "catalog";
        await f.Session.StartAsync(MdnsFixture.Nic);
        if (stage == "read") f.Factory.Last!.FailRead();
        await f.Eventually(() => !f.Session.IsActive);
        Assert.Equal(MdnsState.Failed, f.Session.Snapshot.State);
        Assert.InRange(f.Factory.Opened, 0, 1);
        if (f.Factory.Last != null) Assert.True(f.Factory.Last.Disposed);
    }
    [Fact]
    public async Task StopDuringSynchronousOpenDisposesLateSocketWithoutListening()
    {
        using var f = new SessionFixture();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Factory.OnOpen = () => { entered.SetResult(); release.Wait(TimeSpan.FromSeconds(5)); };
        var start = f.Session.StartAsync(MdnsFixture.Nic); await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var stop = f.Session.StopAsync(); release.Set();
        await Task.WhenAll(start, stop).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(f.Factory.Last!.Disposed); Assert.Equal(MdnsState.Stopped, f.Session.Snapshot.State);
    }
    [Fact]
    public async Task FloodIsThrottledBeforeAnotherReadAndStillChecksNic()
    {
        using var f = new SessionFixture(); await f.Session.StartAsync(MdnsFixture.Nic);
        for (int i = 0; i < 1000; i++) f.Factory.Last!.Enqueue(MdnsFixture.Datagram());
        await f.WaitWithoutAdvancing(() => f.Factory.Last!.ReadCount == 1);
        await Task.Yield(); Assert.Equal(1, f.Factory.Last!.ReadCount);
        f.Catalog.Current = null; f.Clock.Advance(TimeSpan.FromSeconds(2));
        await f.Eventually(() => !f.Session.IsActive);
        Assert.Equal(MdnsState.InterfaceChanged, f.Session.Snapshot.State);
        Assert.Equal(1, f.Factory.Last.ReadCount); Assert.True(f.Clock.CreatedTimers >= 1);
    }
    private static MdnsInterface? Change(string change) => change switch
    {
        "id" => MdnsFixture.Nic with { Id = Guid.NewGuid() }, "index" => MdnsFixture.Nic with { Index = 8 },
        "mac" => MdnsFixture.Nic with { Mac = "020000000008" },
        "address" => MdnsFixture.Nic with { Ipv4 = ImmutableArray.Create("192.0.2.11/24") },
        "prefix" => MdnsFixture.Nic with { Ipv4 = ImmutableArray.Create("192.0.2.10/25") }, _ => null
    };
}

public class MdnsNativeBoundaryTests
{
    [Fact]
    public async Task ProductionSetupUsesOnlySelectedIfListAndMembershipAndPassesReceiveMetadata()
    {
        var socket = new FakeNativeSocket(); var factory = new WindowsMdnsReceiverFactory(() => socket, new DiscoveryClock());
        using var receiver = factory.Open(MdnsFixture.Nic);
        Assert.Equal(new[] { "IP:28:1", "IP:29:6", "IP:19:1", "Socket:4:1", "Socket:4098:65536", "bind:0.0.0.0:5353", "join:224.0.0.251:6" }, socket.Calls);
        var packet = await receiver.ReceiveAsync(new byte[9000], CancellationToken.None);
        Assert.NotNull(packet); Assert.Equal(6, packet.InterfaceIndex); Assert.Equal(5353, packet.SourcePort);
        Assert.Equal(MdnsDiscoverySession.Group, packet.Destination); Assert.False(packet.Truncated);
        Assert.True(MdnsPacketParser.TryParse(packet.Bytes.Span, out _));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void AnySetupFailureDisposesWithoutFallback(int failAt)
    {
        var socket = new FakeNativeSocket { FailAt = failAt };
        var factory = new WindowsMdnsReceiverFactory(() => socket, new DiscoveryClock());
        Assert.Throws<SocketException>(() => factory.Open(MdnsFixture.Nic));
        Assert.True(socket.Disposed); Assert.Equal(failAt + 1, socket.Calls.Count);
    }
    [Theory]
    [InlineData(SocketFlags.Truncated)] [InlineData(SocketFlags.ControlDataTruncated)]
    public async Task TruncatedNativeMetadataIsRejected(SocketFlags flags)
    {
        var socket = new FakeNativeSocket { Flags = flags };
        using var receiver = new WindowsMdnsReceiverFactory(() => socket, new DiscoveryClock()).Open(MdnsFixture.Nic);
        Assert.True((await receiver.ReceiveAsync(new byte[9000], CancellationToken.None))!.Truncated);
    }
    [Fact]
    public async Task NativeOversizeIsDiscardedWhileCancellationPropagates()
    {
        var socket = new FakeNativeSocket { Error = SocketError.MessageSize };
        using var receiver = new WindowsMdnsReceiverFactory(() => socket, new DiscoveryClock()).Open(MdnsFixture.Nic);
        Assert.True((await receiver.ReceiveAsync(new byte[9000], CancellationToken.None))!.Truncated);
        socket.Error = null;
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await receiver.ReceiveAsync(new byte[9000], cancel.Token));
    }
    [Fact]
    public async Task IdleReceiveDeadlineIsClockBoundedWithNoConcurrentReceive()
    {
        var socket = new FakeNativeSocket { Hold = true }; var clock = new DiscoveryClock();
        using var receiver = new WindowsMdnsReceiverFactory(() => socket, clock).Open(MdnsFixture.Nic);
        var pending = receiver.ReceiveAsync(new byte[9000], CancellationToken.None).AsTask();
        Assert.False(pending.IsCompleted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(await pending.WaitAsync(TimeSpan.FromSeconds(5))); Assert.Equal(1, socket.Reads);
    }
    private sealed class FakeNativeSocket : IMdnsSocket
    {
        public List<string> Calls = new(); public bool Disposed; public int FailAt = -1;
        public SocketFlags Flags; public SocketError? Error;
        public bool Hold; public int Reads;
        private void Call(string text) { Calls.Add(text); if (Calls.Count - 1 == FailAt) throw new SocketException((int)SocketError.OperationNotSupported); }
        public void SetOption(SocketOptionLevel level, SocketOptionName name, int value) => Call($"{level}:{(int)name}:{value}");
        public void Bind(IPEndPoint endpoint) => Call("bind:" + endpoint);
        public void Join(IPAddress group, int index) => Call($"join:{group}:{index}");
        public async ValueTask<SocketReceiveMessageFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellation)
        {
            Reads++;
            if (Hold) await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
            cancellation.ThrowIfCancellationRequested(); if (Error.HasValue) throw new SocketException((int)Error.Value);
            var bytes = MdnsFixture.Packet(); bytes.CopyTo(buffer);
            // Value type has no public constructor; construct fake metadata, never a socket.
            var info = (IPPacketInformation)Activator.CreateInstance(typeof(IPPacketInformation), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { MdnsDiscoverySession.Group, 6 }, null)!;
            return new SocketReceiveMessageFromResult { ReceivedBytes = bytes.Length,
                PacketInformation = info, RemoteEndPoint = new IPEndPoint(IPAddress.Parse("192.0.2.44"), 5353), SocketFlags = Flags };
        }
        public void Dispose() => Disposed = true;
    }
}

internal sealed class SessionFixture : IDisposable
{
    internal readonly FakeCatalog Catalog = new(); internal readonly FakeReceiverFactory Factory = new();
    internal readonly DiscoveryClock Clock = new(); internal readonly MdnsDiscoverySession Session;
    internal SessionFixture() => Session = new(Catalog, Factory, Clock);
    internal async Task Eventually(Func<bool> condition)
    {
        for (int i = 0; i < 3000 && !condition(); i++) { Clock.Advance(TimeSpan.FromMilliseconds(20)); await Task.Delay(1); }
        Assert.True(condition(), "Fake discovery did not reach the expected state.");
    }
    internal async Task WaitWithoutAdvancing(Func<bool> condition)
    {
        for (int i = 0; i < 3000 && !condition(); i++) await Task.Delay(1);
        Assert.True(condition());
    }
    public void Dispose() => Session.Dispose();
}
internal sealed class FakeCatalog : IMdnsInterfaces
{
    internal MdnsInterface? Current = MdnsFixture.Nic; internal bool Fail;
    public IReadOnlyList<MdnsInterface> Read() => Fail ? throw new InvalidOperationException("NEVER_ECHO") : Current == null ? Array.Empty<MdnsInterface>() : new[] { Current };
}
internal sealed class FakeReceiverFactory : IMdnsReceiverFactory
{
    internal FakeReceiver? Last; internal int Opened; internal bool Fail; internal Action? OnOpen;
    public IMdnsReceiver Open(MdnsInterface scope)
    {
        Opened++; if (Fail) throw new InvalidOperationException("NEVER_ECHO");
        Assert.True(scope.SameLink(MdnsFixture.Nic)); Last = new(); OnOpen?.Invoke(); return Last;
    }
}
internal sealed class FakeReceiver : IMdnsReceiver
{
    private readonly Channel<MdnsDatagram?> _queue = Channel.CreateUnbounded<MdnsDatagram?>();
    internal bool Disposed; internal int ReadCount;
    internal void Enqueue(MdnsDatagram? packet) => _queue.Writer.TryWrite(packet);
    internal void FailRead() => _queue.Writer.TryComplete(new InvalidOperationException("NEVER_ECHO"));
    public async ValueTask<MdnsDatagram?> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellation)
    {
        var packet = await _queue.Reader.ReadAsync(cancellation); Interlocked.Increment(ref ReadCount); return packet;
    }
    public void Dispose() { Disposed = true; _queue.Writer.TryComplete(); }
}
internal sealed class DiscoveryClock : TimeProvider
{
    private long _ticks; private readonly ConcurrentBag<ManualTimer> _timers = new();
    internal int CreatedTimers;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch + TimeSpan.FromTicks(GetTimestamp());
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Interlocked.Increment(ref CreatedTimers);
        var timer = new ManualTimer(this, callback, state); _timers.Add(timer); timer.Change(dueTime, period); return timer;
    }
    internal void Advance(TimeSpan amount)
    {
        Interlocked.Add(ref _ticks, amount.Ticks);
        foreach (var timer in _timers) timer.Fire(GetTimestamp());
    }
    private sealed class ManualTimer(DiscoveryClock clock, TimerCallback callback, object? state) : ITimer
    {
        private long _due = long.MaxValue;
        public bool Change(TimeSpan dueTime, TimeSpan period) { Interlocked.Exchange(ref _due, clock.GetTimestamp() + dueTime.Ticks); return true; }
        public void Fire(long now)
        {
            long due = Interlocked.Read(ref _due);
            if (due <= now && Interlocked.CompareExchange(ref _due, long.MaxValue, due) == due) callback(state);
        }
        public void Dispose() => Interlocked.Exchange(ref _due, long.MaxValue);
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
