using System.Collections.Immutable;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class MdnsCacheTests
{
    private static readonly TimeSpan Zero = TimeSpan.Zero;
    [Fact]
    public void CompleteAndIncompleteRowsUseOnlyLiveDependenciesAndExpire()
    {
        var cache = new MdnsCache(); cache.Accept(MdnsFixture.Records(10), Zero);
        var row = Assert.Single(cache.Snapshot(Zero, 6));
        Assert.False(row.Incomplete); Assert.False(row.Stale); Assert.Equal(10, row.Ttl);
        Assert.Contains("fe80::44%6", row.Addresses); Assert.Equal("8443", row.Port);
        cache.Accept(ImmutableArray.Create(MdnsFixture.Records()[0] with { Ttl = 100 }), TimeSpan.FromSeconds(2));
        row = Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(11), 6));
        Assert.True(row.Incomplete); Assert.False(row.Stale); Assert.Equal("", row.Hostname); Assert.Equal("", row.Addresses);
        cache.Snapshot(TimeSpan.FromSeconds(101), 6);
        row = Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(103), 6)); Assert.True(row.Stale); Assert.Equal(0, row.Ttl);
        Assert.Empty(cache.Snapshot(TimeSpan.FromSeconds(164), 6)); Assert.Equal(0, cache.Count);
    }
    [Fact]
    public void SrvWithoutPtrAndPtrWithoutSrvRemainIncomplete()
    {
        foreach (int n in new[] { 0, 1 })
        {
            var cache = new MdnsCache(); cache.Accept(ImmutableArray.Create(MdnsFixture.Records()[n]), Zero);
            Assert.True(Assert.Single(cache.Snapshot(Zero, 6)).Incomplete);
        }
    }
    [Fact]
    public void ContradictorySrvDoesNotChooseOneTarget()
    {
        var cache = new MdnsCache(); var records = MdnsFixture.Records();
        cache.Accept(records.Add(records[1] with { Value = "other.local" }), Zero);
        var row = Assert.Single(cache.Snapshot(Zero, 6));
        Assert.True(row.Incomplete); Assert.Empty(row.Hostname); Assert.Empty(row.Port);
    }
    [Fact]
    public void GoodbyeHasOneSecondGraceAndCanBeRescued()
    {
        var cache = new MdnsCache(); cache.Accept(MdnsFixture.Records(), Zero);
        cache.Snapshot(Zero, 6);
        cache.Accept(MdnsFixture.Records(0), TimeSpan.FromSeconds(5));
        Assert.False(Assert.Single(cache.Snapshot(TimeSpan.FromMilliseconds(5999), 6)).Stale);
        cache.Accept(MdnsFixture.Records(), TimeSpan.FromMilliseconds(5999));
        Assert.False(Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(6), 6)).Stale);
        cache.Accept(MdnsFixture.Records(0), TimeSpan.FromSeconds(7));
        Assert.True(Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(8), 6)).Stale);
    }
    [Fact]
    public void FlushPreservesSamePacketAndRecentRrsetMembersThenRemovesOld()
    {
        var cache = new MdnsCache(); var records = MdnsFixture.Records();
        cache.Accept(records, Zero);
        var replacement = records[2] with { Value = "192.0.2.45" };
        cache.Accept(ImmutableArray.Create(replacement), TimeSpan.FromSeconds(2));
        Assert.Contains("192.0.2.44", Assert.Single(cache.Snapshot(TimeSpan.FromMilliseconds(2500), 6)).Addresses);
        Assert.DoesNotContain("192.0.2.44", Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(3), 6)).Addresses);
        var second = replacement with { Value = "192.0.2.46" };
        cache.Accept(ImmutableArray.Create(replacement, second), TimeSpan.FromSeconds(5));
        var row = Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(6), 6));
        Assert.Contains("192.0.2.45", row.Addresses); Assert.Contains("192.0.2.46", row.Addresses);
        cache.Accept(ImmutableArray.Create(second), TimeSpan.FromMilliseconds(6500));
        Assert.Contains("192.0.2.45", Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(7), 6)).Addresses);
    }
    [Fact]
    public void RecordFloodAndHugeTtlCannotGrowCacheOrPreventRefresh()
    {
        var cache = new MdnsCache(); var records = MdnsFixture.Records(uint.MaxValue);
        cache.Accept(records, Zero);
        for (int i = 0; i < 3000; i++)
            cache.Accept(ImmutableArray.Create(records[2] with { Name = $"host{i}.local", Flush = false }), Zero);
        Assert.Equal(MdnsCache.MaxRecords, cache.Count); Assert.True(cache.Limited > 0);
        cache.Accept(records, TimeSpan.FromSeconds(1));
        Assert.Equal(MdnsCache.MaxTtl, Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(1), 6)).Ttl);
        cache.Snapshot(TimeSpan.FromSeconds(MdnsCache.MaxTtl + 2), 6); Assert.Equal(0, cache.Count);
    }
    [Fact]
    public void AddressFloodProducesBoundedIncompleteRowAndFlushGraceProtectsExactlyOneSecond()
    {
        var cache = new MdnsCache(); var records = MdnsFixture.Records(); cache.Accept(records, Zero);
        for (int i = 1; i < 100; i++) cache.Accept(ImmutableArray.Create(records[2] with { Value = $"192.0.2.{i}", Flush = false }), Zero);
        var row = Assert.Single(cache.Snapshot(Zero, 6));
        Assert.True(row.Incomplete); Assert.Equal(MdnsCache.MaxAddresses, row.Addresses.Split(", ").Length);
        cache = new(); cache.Accept(records, Zero);
        cache.Accept(ImmutableArray.Create(records[2] with { Value = "192.0.2.45" }), TimeSpan.FromSeconds(1));
        Assert.Contains("192.0.2.44", Assert.Single(cache.Snapshot(TimeSpan.FromSeconds(3), 6)).Addresses);
    }
    [Fact]
    public void ServiceFloodAndStoppedResultsStayBoundedAndStale()
    {
        var cache = new MdnsCache(); var ptr = MdnsFixture.Records()[0];
        for (int i = 0; i < 500; i++) cache.Accept(ImmutableArray.Create(ptr with { Value = $"panel{i}._http._tcp.local" }), Zero);
        Assert.Equal(MdnsCache.MaxServices, cache.Snapshot(Zero, 6).Length);
        var stopped = cache.Snapshot(TimeSpan.FromSeconds(1), 6, true);
        Assert.All(stopped, r => { Assert.True(r.Stale); Assert.Equal(0, r.Ttl); });
    }
    [Theory]
    [InlineData("=2+2")]
    [InlineData("  +cmd")]
    [InlineData("\t@evil")]
    [InlineData("\r\n-formula")]
    [InlineData("\u200b=hidden")]
    public void CsvFormulaTriggersAreNeutralizedAndQuoted(string instance)
    {
        var row = new MdnsService(instance, "_http._tcp.local", "host.local", "192.0.2.44", "80", 20, false, false);
        var snapshot = new MdnsSnapshot(MdnsState.Listening, MdnsFixture.Nic, ImmutableArray.Create(row), 0, 0, 0);
        var csv = MdnsCsv.Export(snapshot, snapshot.Services);
        Assert.Contains("\"'", csv); Assert.DoesNotContain('\t', csv); Assert.DoesNotContain('\u200b', csv);
        Assert.Equal(3, csv.Split("\r\n").Length); // header + data + terminator, no injected rows
    }
    [Fact]
    public void FilterAndCsvPreserveQuotesCommasAndSnapshotScope()
    {
        var row = new MdnsService("Panel, \"Main\"", "_http._tcp.local", "host.local", "192.0.2.44", "80", 0, true, true);
        Assert.Single(MdnsCsv.Filter(new[] { row }, "HOST.LOCAL"));
        Assert.Empty(MdnsCsv.Filter(new[] { row }, "other"));
        var snapshot = new MdnsSnapshot(MdnsState.Stopped, MdnsFixture.Nic, ImmutableArray.Create(row), 0, 0, 0);
        var csv = MdnsCsv.Export(snapshot, snapshot.Services);
        Assert.Contains("\"Panel, \"\"Main\"\"\"", csv); Assert.Contains(MdnsFixture.Nic.Id.ToString(), csv); Assert.Contains("Stopped", csv);
    }
}
