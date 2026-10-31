using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class IpScannerServiceTests
{
    private sealed class InlineProgress(Action<ScanProgress> report) : IProgress<ScanProgress>
    {
        public void Report(ScanProgress value) => report(value);
    }

    [Fact]
    public async Task ScanRangeAsync_RaisesProgress_ForEachAddress()
    {
        var probed = new ConcurrentBag<string>();
        var progress = new ConcurrentBag<ScanProgress>();
        var svc = new IpScannerService((ip, _, _) =>
        {
            probed.Add(ip);
            return new ScanResult { IpAddress = ip, IsOnline = true };
        });
        var results = await svc.ScanRangeAsync("192.0.2.1", "192.0.2.3",
            100, 8, new InlineProgress(progress.Add), CancellationToken.None);

        Assert.Equal(3, probed.Count);
        Assert.Equal(3, progress.Count);
        Assert.All(progress, p => Assert.Equal(3, p.Total));
        Assert.Equal(new[] { "192.0.2.1", "192.0.2.2", "192.0.2.3" },
            results.ConvertAll(r => r.IpAddress));
        Assert.All(results, r => Assert.True(r.IsOnline));
    }

    [Fact]
    public async Task ScanRangeAsync_RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        int calls = 0;
        var svc = new IpScannerService((ip, _, _) =>
        {
            Interlocked.Increment(ref calls);
            cts.Cancel();
            return new ScanResult { IpAddress = ip };
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            svc.ScanRangeAsync("192.0.2.1", "192.0.2.100", 100, 1, null, cts.Token));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ScanRangeAsync_RejectsReversedRangeBeforeProbing()
    {
        var svc = new IpScannerService((_, _, _) => throw new Exception("Unexpected probe"));
        Assert.Throws<ArgumentException>(() => { _ = svc.ScanRangeAsync("192.0.2.3", "192.0.2.1"); });
    }
}
