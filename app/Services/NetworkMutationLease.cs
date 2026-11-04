using System;
using System.Threading;

namespace NetworkProfileManager.Services;

// Serialize deliberate legacy/project mutations in this application instance.
internal sealed class NetworkMutationLease : IDisposable
{
    private static int _active;
    private static int _inhibited;
    private static int _updateBarrier;
    private bool _disposed;
    private NetworkMutationLease() { }
    internal static string BlockedStage => Volatile.Read(ref _inhibited) != 0 ? "WorkerInhibited"
        : Volatile.Read(ref _updateBarrier) != 0 ? "UpdatePending" : "OperationActive";
    internal static NetworkMutationLease? TryAcquire()
    {
        if (Volatile.Read(ref _inhibited) != 0 || Volatile.Read(ref _updateBarrier) != 0
            || Interlocked.CompareExchange(ref _active, 1, 0) != 0) return null;
        if (Volatile.Read(ref _inhibited) == 0 && Volatile.Read(ref _updateBarrier) == 0) return new();
        Volatile.Write(ref _active, 0);
        return null;
    }
    internal static void Inhibit() => Volatile.Write(ref _inhibited, 1);
    internal static IDisposable? TrySuspendForUpdate()
    {
        if (Interlocked.CompareExchange(ref _updateBarrier, 1, 0) != 0) return null;
        if (Volatile.Read(ref _active) == 0 && Volatile.Read(ref _inhibited) == 0) return new UpdateBarrier();
        Volatile.Write(ref _updateBarrier, 0); return null;
    }
    private sealed class UpdateBarrier : IDisposable
    {
        private int _released;
        public void Dispose() { if (Interlocked.Exchange(ref _released, 1) == 0) Volatile.Write(ref _updateBarrier, 0); }
    }
    public void Dispose() { if (!_disposed) { _disposed = true; Volatile.Write(ref _active, 0); } }
}
