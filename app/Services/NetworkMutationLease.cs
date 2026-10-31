using System;
using System.Threading;

namespace NetworkProfileManager.Services;

// Serialize deliberate legacy/project mutations in this application instance.
internal sealed class NetworkMutationLease : IDisposable
{
    private static int _active;
    private static int _inhibited;
    private bool _disposed;
    private NetworkMutationLease() { }
    internal static NetworkMutationLease? TryAcquire()
    {
        if (Volatile.Read(ref _inhibited) != 0 || Interlocked.CompareExchange(ref _active, 1, 0) != 0) return null;
        if (Volatile.Read(ref _inhibited) == 0) return new();
        Volatile.Write(ref _active, 0);
        return null;
    }
    internal static void Inhibit() => Volatile.Write(ref _inhibited, 1);
    public void Dispose() { if (!_disposed) { _disposed = true; Volatile.Write(ref _active, 0); } }
}
