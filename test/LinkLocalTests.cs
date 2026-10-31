using System.Collections.Immutable;
using System.IO;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class LinkLocalTests
{
    internal static ApplyAdapterSnapshot Before() => ProjectApplyTests.Snapshot() with { Dad = new(1, true) };
    private static ApplyIntent Desired() => LinkLocalAddressing.Intent("169.254.42.10");
    private static ProjectApplyRequest Request(ApplyAdapterSnapshot? before = null, ApplyIntent? desired = null, bool recovery = false)
        => new(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(2), before ?? Before(), desired ?? Desired(), recovery);

    [Theory]
    [InlineData("169.254.1.0", true)][InlineData("169.254.254.255", true)]
    [InlineData("169.254.42.0", true)][InlineData("169.254.42.255", true)]
    [InlineData("169.254.0.1", false)][InlineData("169.254.255.1", false)]
    [InlineData("169.254.0.0", false)][InlineData("169.254.255.255", false)]
    [InlineData("169.253.1.1", false)][InlineData("192.0.2.10", false)]
    [InlineData("169.254.01.1", false)][InlineData("169.254.1", false)]
    [InlineData("169.254.1.1/16", false)][InlineData("169.254.1.1;echo", false)]
    [InlineData("fe80::1", false)][InlineData("", false)]
    public void CandidatesUseCanonicalUnreservedRangeWithOneSixteenBitSubnet(string address, bool valid)
        => Assert.Equal(valid, LinkLocalAddressing.IsCandidate(address));

    [Fact]
    public void SuggestionOnlyProducesLocalValidCandidates()
    {
        for (var i = 0; i < 64; i++) Assert.True(LinkLocalAddressing.IsCandidate(LinkLocalAddressing.GenerateCandidate()));
    }

    [Theory]
    [InlineData("gateway")][InlineData("prefix")][InlineData("reserved")][InlineData("automatic-dns")]
    [InlineData("dns-injection")][InlineData("secondary-only")][InlineData("duplicate-dns")]
    public void InvalidIntentRejectsBeforeAnyOsBoundary(string invalid)
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            var d = invalid switch
            {
                "gateway" => Desired() with { Gateway = "169.254.42.1" },
                "prefix" => Desired() with { PrefixLength = 24 },
                "reserved" => Desired() with { Address = "169.254.255.2" },
                "automatic-dns" => Desired() with { DnsMode = "automatic" },
                "secondary-only" => LinkLocalAddressing.Intent("169.254.42.10", "", "192.0.2.53"),
                "duplicate-dns" => LinkLocalAddressing.Intent("169.254.42.10", "192.0.2.53", "192.0.2.53"),
                _ => LinkLocalAddressing.Intent("169.254.42.10", "192.0.2.53;echo")
            };
            ProjectApplyWire.ValidateRequest(Request(desired: d), DateTimeOffset.UtcNow);
        });
    }

    [Theory]
    [InlineData("disabled")][InlineData("missing")][InlineData("disconnected")]
    [InlineData("unknown")][InlineData("extra-address")][InlineData("custom-route")][InlineData("automatic-apipa")]
    public async Task UnsupportedSourceBlocksBeforeCommands(string kind)
    {
        var s = Before();
        s = kind switch
        {
            "disabled" => s with { Dad = new(0, true) },
            "missing" => s with { Dad = null },
            "disconnected" => s with { Dad = new(1, false) },
            "unknown" => s with { ClassificationComplete = false },
            "extra-address" => s with { Addresses = s.Addresses.Add(new("192.0.2.11", 24, "manual", "preferred")) },
            "custom-route" => s with { Routes = s.Routes.Add(new("198.51.100.0/24", "192.0.2.1", 1, "active", "netmgmt")) },
            _ => s with { Dhcp = true, Addresses = ImmutableArray.Create(new ApplyAddress("169.254.42.20", 16, "wellknown", "preferred")),
                PersistentAddresses = ImmutableArray<ApplyAddress>.Empty, Routes = ImmutableArray<ApplyRoute>.Empty }
        };
        var reads = 0; var runs = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(Request(s), (_, _) => { reads++; return Task.FromResult(s); },
            (_, _) => { runs++; return Task.CompletedTask; }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.False(result.MayHaveChanged);
        Assert.Equal(0, reads); Assert.Equal(0, runs);
    }

    [Fact]
    public void CommandsKeepStaticDnsOrExplicitlyReplaceAutomaticDnsAndNeverInventNoGatewaySemantics()
    {
        var preserved = ProjectApplyPolicy.Commands(Request());
        var address = Assert.Single(preserved);
        Assert.Contains("name=12", address); Assert.Contains("mask=255.255.0.0", address);
        Assert.Contains("gateway=none", address); Assert.Contains("store=persistent", address);
        var before = DhcpBefore();
        Assert.Throws<InvalidOperationException>(() => ProjectApplyWire.ValidateRequest(Request(before), DateTimeOffset.UtcNow));
        var explicitDns = LinkLocalAddressing.Intent("169.254.42.10", "192.0.2.53", "192.0.2.54");
        ProjectApplyWire.ValidateRequest(Request(before, explicitDns), DateTimeOffset.UtcNow);
        var commands = ProjectApplyPolicy.Commands(Request(before, explicitDns));
        Assert.Equal(3, commands.Count); Assert.Contains("source=static", commands[1]);
        Assert.Contains("address=192.0.2.54", commands[2]);
        var recovery = ProjectApplyPolicy.RecoveryIntent(before);
        Assert.Equal("dhcp", recovery.Mode); Assert.Equal("automatic", recovery.DnsMode); Assert.Empty(recovery.DnsServers);
    }

    internal static ApplyAdapterSnapshot DhcpBefore() => Before() with
    {
        Dhcp = true, Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "dhcp", "preferred")),
        PersistentAddresses = ImmutableArray<ApplyAddress>.Empty, Routes = ImmutableArray<ApplyRoute>.Empty, DnsSource = "automatic"
    };

    [Theory]
    [InlineData("preferred", ApplyExecutionStatus.Finished, "CommandsFinished", 3)]
    [InlineData("duplicate", ApplyExecutionStatus.Failed, "AddressConflict", 1)]
    [InlineData("missing-dad", ApplyExecutionStatus.Failed, "DadVerification", 1)]
    [InlineData("unknown", ApplyExecutionStatus.Failed, "DadVerification", 1)]
    [InlineData("identity", ApplyExecutionStatus.Failed, "AdapterChanged", 1)]
    public async Task NativeDadObservedThroughFakeOsStopsDnsUntilAddressIsPreferred(string state,
        ApplyExecutionStatus expected, string stage, int count)
    {
        var r = Request(DhcpBefore(), LinkLocalAddressing.Intent("169.254.42.10", "192.0.2.53", "192.0.2.54"));
        var current = r.Before; var runs = new List<IReadOnlyList<string>>();
        var result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) => Task.FromResult(current), (args, _) =>
        {
            runs.Add(args);
            if (runs.Count == 1)
            {
                current = ProjectApplyTests.Applied(current, r.Desired);
                if (state == "duplicate") current = current with { Addresses = ImmutableArray.Create(current.Addresses[0] with { State = "duplicate" }) };
                if (state == "missing-dad") current = current with { Dad = null };
                if (state == "unknown") current = current with { ClassificationComplete = false };
                if (state == "identity") current = current with { Identity = current.Identity with { InterfaceIndex = 13 } };
            }
            return Task.CompletedTask;
        }, _ => Task.FromResult(true));
        Assert.Equal(expected, result.Status); Assert.Equal(stage, result.Stage); Assert.True(result.MayHaveChanged);
        Assert.Equal(count, runs.Count);
    }

    [Fact]
    public async Task DadMayBeTentativeBeforePreferredWithoutPingOrReselection()
    {
        var r = Request(); var current = r.Before; var reads = 0; var runs = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) =>
        {
            reads++;
            if (reads == 4) current = current with { Addresses = ImmutableArray.Create(current.Addresses[0] with { State = "preferred" }) };
            return Task.FromResult(current);
        }, (_, _) =>
        {
            runs++; current = ProjectApplyTests.Applied(current, r.Desired);
            current = current with { Addresses = ImmutableArray.Create(current.Addresses[0] with { State = "tentative" }) };
            return Task.CompletedTask;
        }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Finished, result.Status); Assert.Equal(1, runs); Assert.Equal(4, reads);
    }

    [Fact]
    public async Task UnresolvedDadHasBoundedFailureAndDoesNotChangeDns()
    {
        var r = Request(); var current = r.Before; var runs = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) => Task.FromResult(current), (_, _) =>
        {
            runs++; current = ProjectApplyTests.Applied(current, r.Desired);
            current = current with { Addresses = ImmutableArray.Create(current.Addresses[0] with { State = "tentative" }) };
            return Task.CompletedTask;
        }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.Equal("DadVerification", result.Stage); Assert.Equal(1, runs);
    }

    [Fact]
    public async Task CancellationAfterAddressStopsDnsAndDadChangesBeforeFirstCommandBlockMutation()
    {
        var r = Request(DhcpBefore(), LinkLocalAddressing.Intent("169.254.42.10", "192.0.2.53"));
        var current = r.Before; var runs = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) => Task.FromResult(current), (_, _) =>
        {
            runs++; current = ProjectApplyTests.Applied(current, r.Desired); return Task.CompletedTask;
        }, _ => Task.FromResult(runs == 0));
        Assert.Equal(ApplyExecutionStatus.Cancelled, result.Status); Assert.True(result.MayHaveChanged); Assert.Equal(1, runs);
        var reads = 0; runs = 0;
        result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) => Task.FromResult(++reads == 1 ? r.Before : r.Before with { Dad = new(0, true) }),
            (_, _) => { runs++; return Task.CompletedTask; }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.Equal("PreStateChanged", result.Stage);
        Assert.False(result.MayHaveChanged); Assert.Equal(0, runs);
    }

    [Fact]
    public void GeneratedLocalLinkLocalRoutesAreSupportedButDefaultAndCustomRoutesAreNot()
    {
        var s = ProjectApplyTests.Applied(Before(), Desired()) with
        {
            Routes = new[] { "169.254.0.0/16", "169.254.42.10/32", "169.254.255.255/32", "224.0.0.0/4", "255.255.255.255/32" }
                .Select(prefix => new ApplyRoute(prefix, "0.0.0.0", 256, "active", "local")).ToImmutableArray()
        };
        ProjectApplyPolicy.ValidateSnapshot(s, allowLinkLocal: true);
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateSnapshot(s));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateSnapshot(s with
        { Routes = s.Routes.Add(new("0.0.0.0/0", "192.0.2.1", 1, "active", "netmgmt")) }, allowLinkLocal: true));
    }

    [Fact]
    public void DhcpVerificationDoesNotClaimSuccessFromApipaOrPinPreviousLease()
    {
        var before = ProjectApplyTests.Applied(Before(), Desired());
        var r = Request(before, new("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty));
        var actual = ProjectApplyTests.Applied(before, r.Desired);
        Assert.True(ProjectApplyVerification.Matches(r, actual));
        Assert.False(ProjectApplyVerification.Matches(r, actual with
        { Addresses = ImmutableArray.Create(new ApplyAddress("169.254.42.20", 16, "dhcp", "preferred")) }));
        Assert.False(ProjectApplyVerification.Matches(r, actual with { Addresses = ImmutableArray<ApplyAddress>.Empty }));
    }

    [Fact]
    public void PartialStaticAutomaticDnsIsAllowedOnlyAsCurrentLinkLocalStateForExplicitRecoveryToDhcp()
    {
        var before = DhcpBefore();
        var partial = ProjectApplyTests.Applied(before, Desired()) with
        { Addresses = ImmutableArray.Create(new ApplyAddress("169.254.42.10", 16, "manual", "duplicate")) };
        var recovery = Request(partial, ProjectApplyPolicy.RecoveryIntent(before), recovery: true);
        ProjectApplyWire.ValidateRequest(recovery, DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.RecoveryIntent(partial));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyWire.ValidateRequest(recovery with { Recovery = false }, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyWire.ValidateRequest(recovery with { Desired = Desired() }, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyWire.ValidateRequest(recovery with
        { Before = partial with { ClassificationComplete = false } }, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyWire.ValidateRequest(recovery with
        { Before = partial with { Routes = before.Routes.Add(new("198.51.100.0/24", "192.0.2.1", 1, "active", "netmgmt")) } }, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("duplicate")][InlineData("tentative")][InlineData("disabled-dad")][InlineData("gateway")]
    [InlineData("dns")][InlineData("persistence")][InlineData("dhcp")]
    public void VerificationNeverEquatesCommandSuccessWithSupportedManualAddress(string mismatch)
    {
        var r = Request(); var actual = ProjectApplyTests.Applied(r.Before, r.Desired);
        Assert.True(ProjectApplyVerification.Matches(r, actual));
        actual = mismatch switch
        {
            "duplicate" or "tentative" => actual with { Addresses = ImmutableArray.Create(actual.Addresses[0] with { State = mismatch }) },
            "disabled-dad" => actual with { Dad = new(0, true) },
            "gateway" => actual with { Routes = r.Before.Routes },
            "dns" => actual with { DnsServers = ImmutableArray.Create("192.0.2.55") },
            "persistence" => actual with { PersistentAddresses = ImmutableArray<ApplyAddress>.Empty },
            _ => actual with { Dhcp = true }
        };
        Assert.False(ProjectApplyVerification.Matches(r, actual));
    }

    [Fact]
    public void ManualLinkLocalSnapshotsRestoreExactAddressDnsAndPolicyButApipaDoesNotMasqueradeAsManual()
    {
        var before = Before();
        var original = ProjectApplyTests.Applied(before, Desired());
        var intent = ProjectApplyPolicy.RecoveryIntent(original);
        Assert.Equal("link-local", intent.Mode); Assert.Equal(original.DnsServers, intent.DnsServers);
        var r = Request(before, intent, recovery: true);
        ProjectApplyWire.ValidateRequest(r, DateTimeOffset.UtcNow);
        Assert.True(ProjectApplyVerification.Matches(r, ProjectApplyTests.Applied(before, intent)));
        Assert.Contains("169.254.42.10", ProjectApplyViewModel.FormatSnapshot(original));
        Assert.Contains("LinkLocal.Manual", ProjectApplyViewModel.FormatSnapshot(original));
        var apipa = original with { Dhcp = true, PersistentAddresses = ImmutableArray<ApplyAddress>.Empty };
        Assert.Contains("LinkLocal.Automatic", ProjectApplyViewModel.FormatSnapshot(apipa));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.RecoveryIntent(apipa));
    }

    [Fact]
    public async Task NewJournalRetainsManualSnapshotsAndOldJournalWithoutDadStillReadsWithoutRewriting()
    {
        var root = Path.Combine(Path.GetTempPath(), "AVLinkLocalJournal_" + Guid.NewGuid().ToString("N"));
        try
        {
            var j = new ProjectApplyJournal(root); var r = Request(ProjectApplyTests.Applied(Before(), Desired()),
                new("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty));
            await j.BeginAsync(r, CancellationToken.None);
            var old = Request(ProjectApplyTests.Snapshot(), ProjectApplyTests.Intent());
            var oldBytes = ProjectApplyWire.Serialize(old).Replace(",\"Dad\":null", "");
            var path = Path.Combine(root, old.OperationId.ToString("N") + ".request.json");
            File.WriteAllText(path, oldBytes);
            var dhcp = DhcpBefore();
            var partial = ProjectApplyTests.Applied(dhcp, Desired()) with
            { Addresses = ImmutableArray.Create(new ApplyAddress("169.254.42.10", 16, "manual", "duplicate")) };
            await j.BeginAsync(Request(partial, ProjectApplyPolicy.RecoveryIntent(dhcp), recovery: true), CancellationToken.None);
            Assert.Equal(3, j.ReadSnapshots().Count); Assert.Equal(oldBytes, File.ReadAllText(path));
            Assert.Null(ProjectApplyWire.Parse<ProjectApplyRequest>(oldBytes).Before.Dad);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void NewDadFieldsAndWorkerStagesCannotDefaultToSuccessfulConflictChecking()
    {
        var json = ProjectApplyWire.Serialize(Request());
        Assert.ThrowsAny<Exception>(() => ProjectApplyWire.Parse<ProjectApplyRequest>(json.Replace(",\"Connected\":true", "")));
        var noDad = ProjectApplyWire.Parse<ProjectApplyRequest>(json.Replace(",\"Dad\":{\"Transmits\":1,\"Connected\":true}", ""));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyWire.ValidateRequest(noDad, DateTimeOffset.UtcNow));
        Assert.True(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Failed, true, "AddressConflict")));
        Assert.False(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Finished, true, "DadVerification")));
    }
}
