using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public sealed class MdnsDiscoverySession : IDisposable
{
    public static readonly IPAddress Group = IPAddress.Parse("224.0.0.251");
    private readonly object _gate = new();
    private readonly IMdnsInterfaces _interfaces;
    private readonly IMdnsReceiverFactory _factory;
    private readonly TimeProvider _clock;
    private CancellationTokenSource? _cancel;
    private IMdnsReceiver? _receiver;
    private Task? _run;
    private bool _disposed;
    private MdnsSnapshot _snapshot = new(MdnsState.Idle, null, ImmutableArray<MdnsService>.Empty, 0, 0, 0);
    public MdnsDiscoverySession(IMdnsInterfaces interfaces, IMdnsReceiverFactory factory, TimeProvider? clock = null)
        => (_interfaces, _factory, _clock) = (interfaces, factory, clock ?? TimeProvider.System);
    public bool IsActive { get { lock (_gate) return _run != null; } }
    public MdnsSnapshot Snapshot { get { lock (_gate) return _snapshot; } }

    // Task returns when socket setup either succeeds or fails. Failure is a redacted terminal state.
    public Task StartAsync(MdnsInterface scope)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_run != null) throw new InvalidOperationException("Discovery is already active.");
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            _snapshot = new(MdnsState.Starting, scope, ImmutableArray<MdnsService>.Empty, 0, 0, 0);
            _cancel = new();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _run = Task.Run(() => RunAsync(scope, _cancel.Token, started));
            return started.Task;
        }
    }
    public async Task StopAsync()
    {
        Task? task;
        lock (_gate) { CancelLocked(); task = _run; }
        if (task != null) await task.ConfigureAwait(false);
    }
    public void Dispose()
    {
        lock (_gate) { _disposed = true; CancelLocked(); }
    }
    private void CancelLocked()
    {
        _cancel?.Cancel();
        _receiver?.Dispose();
    }
    private bool StillSelected(MdnsInterface scope) => _interfaces.Read().Count(i => scope.SameLink(i)) == 1;
    private async Task RunAsync(MdnsInterface scope, CancellationToken token, TaskCompletionSource started)
    {
        var cache = new MdnsCache();
        var begin = _clock.GetTimestamp();
        TimeSpan Elapsed() => _clock.GetElapsedTime(begin);
        var lastCheck = TimeSpan.Zero;
        var lastPublish = TimeSpan.Zero;
        var state = MdnsState.Stopped;
        long received = 0, rejected = 0;
        IMdnsReceiver? receiver = null;
        try
        {
            token.ThrowIfCancellationRequested();
            if (!StillSelected(scope)) { state = MdnsState.InterfaceChanged; return; }
            receiver = _factory.Open(scope);
            lock (_gate)
            {
                token.ThrowIfCancellationRequested(); _receiver = receiver;
            }
            if (!StillSelected(scope)) { state = MdnsState.InterfaceChanged; return; }
            token.ThrowIfCancellationRequested();
            Publish(MdnsState.Listening); started.TrySetResult();
            var buffer = new byte[MdnsPacketParser.MaxPacket];
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var now = Elapsed();
                if (now >= TimeSpan.FromMinutes(10)) { state = MdnsState.TimedOut; break; }
                if (now - lastCheck >= TimeSpan.FromSeconds(1))
                {
                    if (!StillSelected(scope)) { state = MdnsState.InterfaceChanged; break; }
                    lastCheck = now;
                }
                if (now - lastPublish >= TimeSpan.FromSeconds(1)) { Publish(MdnsState.Listening); lastPublish = now; }
                var packet = await receiver.ReceiveAsync(buffer, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (packet != null)
                {
                    received++;
                    if (packet.Truncated || packet.InterfaceIndex != scope.Index || !packet.Destination.Equals(Group)
                        || packet.SourcePort != 5353 || !MdnsPacketParser.TryParse(packet.Bytes.Span, out var records)) rejected++;
                    else cache.Accept(records, Elapsed());
                }
                // Also limits malformed/foreign floods and yields on synchronously completed receives.
                await Task.Delay(TimeSpan.FromMilliseconds(10), _clock, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { state = MdnsState.Stopped; }
        catch (ObjectDisposedException) when (token.IsCancellationRequested) { state = MdnsState.Stopped; }
        catch (Exception) { state = token.IsCancellationRequested ? MdnsState.Stopped : MdnsState.Failed; }
        finally
        {
            receiver?.Dispose();
            lock (_gate)
            {
                _receiver = null;
                _snapshot = new(state, scope, cache.Snapshot(Elapsed(), scope.Index, true), received, rejected, cache.Limited);
                _cancel?.Dispose(); _cancel = null; _run = null;
            }
            started.TrySetResult();
        }
        void Publish(MdnsState current)
        {
            var rows = cache.Snapshot(Elapsed(), scope.Index);
            lock (_gate) _snapshot = new(current, scope, rows, received, rejected, cache.Limited);
        }
    }
}
