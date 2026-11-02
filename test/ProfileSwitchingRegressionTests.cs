using System.Collections.Immutable;
using NetworkProfileManager.Models;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ProfileSwitchingRegressionTests : IDisposable
{
    private readonly TestDataDirectoryFixture _dir = new();
    public void Dispose() => _dir.Dispose();

    private sealed class Surface : IProjectApplyBoundary, IProjectApplyJournal, IBrowserLauncher
    {
        internal ApplyAdapterSnapshot Current = ProjectApplyTests.Snapshot();
        internal ProjectApplyRequest? Saved;
        internal int Executes, Begins, Opens;
        internal bool Conflict, ReturnWithoutLease;
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken ct) => Task.FromResult(Current);
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent d, bool recovery) => ProjectApplyPolicy.ValidateTransition(s, d, recovery);
        public async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken ct)
        {
            Executes++;
            var result = await ProjectApplyWorker.ExecuteAsync(request, ReadAsync,
                (_, _) => Task.CompletedTask, _ => Task.FromResult(true));
            if (result.Status == ApplyExecutionStatus.Finished)
            {
                Current = ProjectApplyTests.Applied(Current, request.Desired);
                if (ReturnWithoutLease && request.Desired.Mode == "dhcp")
                    Current = WithoutLease() with { Identity = Current.Identity, DnsSource = request.Desired.DnsMode,
                        DnsServers = request.Desired.DnsServers, DnsRegistration = Current.DnsRegistration };
                if (Conflict) Current = Current with { Addresses = Current.Addresses.Select(a => a with { State = "duplicate" }).ToImmutableArray() };
            }
            return result;
        }
        public Task BeginAsync(ProjectApplyRequest r, CancellationToken ct) { Begins++; Saved = r; return Task.CompletedTask; }
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome) => Task.CompletedTask;
        public void Open(string destination) => Opens++;
        internal ProjectApplyCoordinator Coordinator() => new(this, this, this, verificationAttempts: 1);
        internal LocalProfileApplyService Local() => new(this, this,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { Current.Identity }),
            _ => Saved!, verificationAttempts: 1);
    }
    private static ApplyIntent EmptyDns() => ProjectApplyTests.Intent() with
        { DnsMode = "leave-unchanged", DnsServers = ImmutableArray<string>.Empty };
    private static ApplyAdapterSnapshot Dhcp(bool servers = true) => ProjectApplyTests.Snapshot() with
    {
        Dhcp = true, DnsSource = "automatic", DnsServers = servers ? ImmutableArray.Create("192.0.2.53", "192.0.2.54") : ImmutableArray<string>.Empty,
        Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "dhcp", "preferred")),
        PersistentAddresses = ImmutableArray<ApplyAddress>.Empty,
        Routes = ImmutableArray.Create(new ApplyRoute("0.0.0.0/0", "192.0.2.1", 0, "active", "dhcp"))
    };
    private static ApplyAdapterSnapshot Duplicate(bool fallback = true)
    {
        var s = ProjectApplyTests.Snapshot();
        var addresses = ImmutableArray.Create(s.Addresses[0] with { State = "duplicate" });
        if (fallback) addresses = addresses.Add(new("169.254.42.10", 16, "wellknown", "preferred"));
        return s with { Addresses = addresses, DnsSource = "none", DnsServers = ImmutableArray<string>.Empty,
            Routes = s.Routes.Select(r => r.Store == "persistent" ? r with { Origin = "persistent" } : r).ToImmutableArray() };
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task DhcpToStaticEmptyProfileDnsIsReviewedExplicitlyAndRecoveryRestoresAutomaticPolicy(bool servers)
    {
        var s = new Surface { Current = Dhcp(servers) }; var original = s.Current; var c = s.Coordinator();
        var plan = await c.PrepareProfileAsync(original.Identity, EmptyDns());
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
        Assert.Equal(servers ? "static" : "none", plan.Request.Desired.DnsMode);
        Assert.Equal(original.DnsServers, plan.Request.Desired.DnsServers);
        var commands = ProjectApplyPolicy.Commands(plan.Request);
        Assert.Equal(servers ? 3 : 2, commands.Count);
        Assert.Contains(servers ? "address=192.0.2.53" : "address=none", commands[1]);
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(plan)).State);
        Assert.Equal(original, s.Saved!.Before);
        Assert.Equal(original.DnsServers, s.Current.DnsServers);
        var recovery = await c.PrepareRecoveryAsync(original);
        Assert.Equal("automatic", recovery.Request.Desired.DnsMode); Assert.Empty(recovery.Request.Desired.DnsServers);
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(recovery)).State);
        Assert.True(s.Current.Dhcp); Assert.Equal("automatic", s.Current.DnsSource); Assert.Equal(0, s.Opens);
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task SwitchAwayFromDuplicateRetainsExactSourceButNeverVerifiesADuplicateTarget(bool fallback)
    {
        var s = new Surface { Current = Duplicate(fallback) }; var original = s.Current; var c = s.Coordinator();
        var plan = await c.PrepareProfileAsync(original.Identity, EmptyDns());
        Assert.Equal(original, plan.Request.Before);
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(plan)).State);
        Assert.Equal(original, s.Saved!.Before); Assert.Equal("none", s.Current.DnsSource);
        var recovery = await c.PrepareRecoveryAsync(original);
        Assert.Equal("192.0.2.10", recovery.Request.Desired.Address);
        Assert.Equal("none", recovery.Request.Desired.DnsMode);
        s.Conflict = true;
        var outcome = await c.ExecuteAsync(recovery);
        Assert.Equal(ProjectApplyState.Unverified, outcome.State); Assert.True(outcome.MayHaveChanged); Assert.Equal(0, s.Opens);
        Assert.Equal("AddressConflict", outcome.Stage);
    }

    [Fact]
    public async Task DuplicateSourceCanSwitchToDhcpAndLocalHistoryUsesConfiguredIpRatherThanFallback()
    {
        var s = new Surface { Current = Duplicate() }; var id = s.Current.Identity.Id.ToString();
        var vm = new MainViewModel(() => new() { new() { Id = id, Name = "Fixture" } }, apply: s.Local());
        try
        {
            vm.SelectedAdapter!.EditIsDhcp = true; vm.ConfirmHandler = (_, _) => Task.FromResult(true);
            await vm.ApplyAsync();
            Assert.Equal(ProjectApplyState.Complete, vm.LastApplyOutcome!.State);
            var entry = Assert.Single(new HistoryRepository().Load(id));
            Assert.Equal("192.0.2.10", entry.OldIp); Assert.NotNull(entry.RecoveryOperationId);
        }
        finally { vm.Diagnostics.Dispose(); }
    }

    [Fact]
    public async Task EmptyDnsAndOrdinaryPersistentRouteCanApplyAndRecoverWithoutInventingDns()
    {
        var s = new Surface { Current = Duplicate(false) };
        s.Current = s.Current with { Addresses = s.Current.Addresses.Select(a => a with { State = "preferred" }).ToImmutableArray() };
        var original = s.Current; var c = s.Coordinator();
        var p = await c.PrepareProfileAsync(original.Identity, EmptyDns());
        Assert.Single(ProjectApplyPolicy.Commands(p.Request)); // Leave known-empty DNS untouched.
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(p)).State);
        var recovery = await c.PrepareRecoveryAsync(original);
        Assert.Contains("address=none", ProjectApplyPolicy.Commands(recovery.Request)[1]);
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(recovery)).State);
    }

    [Theory]
    [InlineData("custom-fallback-route")][InlineData("extra-manual")][InlineData("wrong-persistent")]
    [InlineData("unknown-policy")][InlineData("unknown-active-route")][InlineData("persistent-mismatch")]
    public async Task SourceRelaxationNeverHidesUnrestorableState(string kind)
    {
        var s = new Surface { Current = Duplicate() };
        s.Current = kind switch
        {
            "custom-fallback-route" => s.Current with { Routes = s.Current.Routes.Add(new("169.254.0.0/16", "192.0.2.1", 1, "active", "netmgmt")) },
            "extra-manual" => s.Current with { Addresses = s.Current.Addresses.Add(new("198.51.100.10", 24, "manual", "preferred")) },
            "wrong-persistent" => s.Current with { PersistentAddresses = ImmutableArray.Create(new ApplyAddress("192.0.2.11", 24, "manual", "preferred")) },
            "unknown-policy" => s.Current with { ClassificationComplete = false },
            "unknown-active-route" => s.Current with { Routes = s.Current.Routes.Select(r => r.Store == "active" ? r with { Origin = "unknown" } : r).ToImmutableArray() },
            _ => s.Current with { Routes = s.Current.Routes.Select(r => r.Store == "persistent" ? r with { Metric = 2 } : r).ToImmutableArray() }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Coordinator().PrepareProfileAsync(s.Current.Identity, EmptyDns()));
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
    }

    [Fact]
    public async Task ChangedDhcpDnsAfterReviewRejectsTheFrozenRequestBeforeJournaling()
    {
        var s = new Surface { Current = Dhcp() }; var c = s.Coordinator();
        var plan = await c.PrepareProfileAsync(s.Current.Identity, EmptyDns());
        s.Current = s.Current with { DnsServers = ImmutableArray.Create("192.0.2.99") };
        Assert.Equal(ProjectApplyState.Rejected, (await c.ExecuteAsync(plan)).State);
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
    }

    [Fact]
    public void NoneDnsCannotCarryServersOrMasqueradeAsDhcpPolicy()
    {
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateIntent(ProjectApplyTests.Intent() with { DnsMode = "none" }));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateSnapshot(Dhcp(false) with { DnsSource = "none" }));
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateTransition(ProjectApplyTests.Snapshot(),
            EmptyDns() with { DnsMode = "none" }, recovery: false));
    }

    [Fact]
    public void RecoveryAlsoRequiresEvidenceForAStoredGatewayWithNoProtocol()
    {
        var s = Duplicate(false);
        s = s with { Routes = s.Routes.Where(r => r.Store == "persistent").ToImmutableArray() };
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateTransition(s, EmptyDns(), recovery: true));
    }

    private static ApplyAdapterSnapshot WithoutLease(bool apipa = true) => Dhcp(false) with
    {
        Addresses = apipa ? ImmutableArray.Create(new ApplyAddress("169.254.42.10", 16, "wellknown", "preferred")) : ImmutableArray<ApplyAddress>.Empty,
        Routes = apipa ? ImmutableArray.Create(
            new ApplyRoute("169.254.0.0/16", "0.0.0.0", 256, "active", "local"),
            new ApplyRoute("169.254.42.10/32", "0.0.0.0", 256, "active", "local"),
            new ApplyRoute("169.254.255.255/32", "0.0.0.0", 256, "active", "local")) : ImmutableArray<ApplyRoute>.Empty,
        Dad = new(1, true)
    };

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task NoLeaseToStaticWithGatewayAndEmptyDnsThenRestoreDhcpWithoutAServer(bool apipa)
    {
        var s = new Surface { Current = WithoutLease(apipa), ReturnWithoutLease = true };
        var original = s.Current; var c = s.Coordinator();
        var plan = await c.PrepareProfileAsync(original.Identity, EmptyDns() with { Gateway = "192.0.2.1" });
        Assert.Equal(original, plan.Request.Before); Assert.Equal("none", plan.Request.Desired.DnsMode);
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
        var commands = ProjectApplyPolicy.Commands(plan.Request);
        Assert.Contains("gateway=192.0.2.1", commands[0]); Assert.Contains("address=none", commands[1]);
        var result = await c.ExecuteAsync(plan);
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal(original, s.Saved!.Before);
        var recovery = await c.PrepareRecoveryAsync(original);
        Assert.Equal("dhcp", recovery.Request.Desired.Mode);
        var restored = await c.ExecuteAsync(recovery);
        Assert.Equal(ProjectApplyState.Complete, restored.State); Assert.Equal("DhcpPolicyVerified", restored.Stage);
        Assert.True(s.Current.Dhcp); Assert.Equal("automatic", s.Current.DnsSource); Assert.Equal(0, s.Opens);
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task NoLeaseLocalHistoryAndDeclinedReviewDoNotRequireAnOldUsableIp(bool apipa)
    {
        var s = new Surface { Current = WithoutLease(apipa) }; var original = s.Current;
        var profile = new IpProfile { IpAddress = "192.0.2.20", SubnetMask = "255.255.255.0", Gateway = "192.0.2.1" };
        var declined = await s.Local().RunAsync(original.Identity.Id.ToString(), profile, null, _ => Task.FromResult(false), default);
        Assert.Null(declined.Outcome); Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
        var id = original.Identity.Id.ToString();
        var vm = new MainViewModel(() => new() { new() { Id = id, Name = "Fixture" } }, apply: s.Local());
        try
        {
            vm.SelectedAdapter!.EditIp = profile.IpAddress; vm.SelectedAdapter.EditSubnet = profile.SubnetMask;
            vm.SelectedAdapter.EditGateway = profile.Gateway; vm.SelectedAdapter.EditIsDhcp = false;
            vm.ConfirmHandler = (_, _) => Task.FromResult(true);
            await vm.ApplyAsync();
            Assert.Equal(ProjectApplyState.Complete, vm.LastApplyOutcome!.State);
            var entry = Assert.Single(new HistoryRepository().Load(id));
            Assert.True(entry.WasDhcp); Assert.Equal(apipa ? "169.254.42.10" : "", entry.OldIp);
            Assert.NotNull(entry.RecoveryOperationId); Assert.Equal(original, s.Saved!.Before);
        }
        finally { vm.Diagnostics.Dispose(); }
    }

    [Fact]
    public async Task DhcpWithoutLeaseCanVerifyPolicyWithoutLaunchingBrowserOrClaimingLeaseVerification()
    {
        var s = new Surface { ReturnWithoutLease = true }; var c = s.Coordinator();
        var dhcp = new ApplyIntent("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty);
        var plan = await c.PrepareAsync(s.Current.Identity, dhcp, "https://192.0.2.44/");
        var failedOpen = await c.ExecuteAsync(plan);
        Assert.Equal(ProjectApplyState.Unverified, failedOpen.State); Assert.Equal(0, s.Opens);
        Assert.False(ProjectApplyVerification.Matches(plan.Request, s.Current));
        var policyOnly = await c.PrepareProfileAsync(s.Current.Identity, dhcp);
        var result = await c.ExecuteAsync(policyOnly);
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal("DhcpPolicyVerified", result.Stage); Assert.Equal(0, s.Opens);
    }

    [Theory]
    [InlineData("policy")][InlineData("alternate")][InlineData("manual-apipa")][InlineData("dad-disabled")]
    [InlineData("dad-unknown")][InlineData("disconnected")][InlineData("custom-route")][InlineData("default-route")]
    [InlineData("multiple")][InlineData("prefix")][InlineData("duplicate")][InlineData("dns-unknown")]
    public async Task NoLeaseSupportDoesNotAcceptAlternateOrUnclassifiedSource(string kind)
    {
        var s = new Surface { Current = WithoutLease() };
        var a = s.Current.Addresses[0];
        s.Current = kind switch
        {
            "policy" => s.Current with { ClassificationComplete = false },
            "alternate" => s.Current with { PersistentAddresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")) },
            "manual-apipa" => s.Current with { Addresses = ImmutableArray.Create(a with { Origin = "manual" }) },
            "dad-disabled" => s.Current with { Dad = new(0, true) },
            "dad-unknown" => s.Current with { Dad = null },
            "disconnected" => s.Current with { Dad = new(1, false) },
            "custom-route" => s.Current with { Routes = s.Current.Routes.Add(new("198.51.100.0/24", "169.254.42.1", 1, "active", "netmgmt")) },
            "default-route" => s.Current with { Routes = s.Current.Routes.Add(new("0.0.0.0/0", "169.254.42.1", 1, "active", "dhcp")) },
            "multiple" => s.Current with { Addresses = s.Current.Addresses.Add(new("192.0.2.10", 24, "dhcp", "preferred")) },
            "prefix" => s.Current with { Addresses = ImmutableArray.Create(a with { PrefixLength = 24 }) },
            "duplicate" => s.Current with { Addresses = ImmutableArray.Create(a with { State = "duplicate" }) },
            _ => s.Current with { DnsSource = "unknown" }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Coordinator().PrepareProfileAsync(s.Current.Identity, EmptyDns()));
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins); Assert.Equal(0, s.Opens);
    }

    [Fact]
    public async Task LeaseArrivingAfterApipaReviewInvalidatesThePlanBeforeMutation()
    {
        var s = new Surface { Current = WithoutLease() }; var c = s.Coordinator();
        var plan = await c.PrepareProfileAsync(s.Current.Identity, EmptyDns());
        s.Current = Dhcp() with { Identity = s.Current.Identity };
        Assert.Equal(ProjectApplyState.Rejected, (await c.ExecuteAsync(plan)).State);
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
    }

    [Fact]
    public async Task ApipaWithExplicitStaticDnsPreservesServersAndRestoresOriginalDhcpDnsPolicy()
    {
        var s = new Surface { Current = WithoutLease() with { DnsSource = "static", DnsServers = ImmutableArray.Create("192.0.2.53") }, ReturnWithoutLease = true };
        var original = s.Current; var c = s.Coordinator();
        var plan = await c.PrepareProfileAsync(original.Identity, EmptyDns());
        Assert.Single(ProjectApplyPolicy.Commands(plan.Request));
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(plan)).State);
        Assert.Equal(original.DnsServers, s.Current.DnsServers);
        var recovery = await c.PrepareRecoveryAsync(original);
        Assert.Equal("static", recovery.Request.Desired.DnsMode);
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(recovery)).State);
        Assert.True(s.Current.Dhcp); Assert.Equal(original.DnsServers, s.Current.DnsServers);
    }

    private sealed class Consent : IProjectApplyDialogs { public bool Confirm(string review, bool recovery) => true; }
    [Fact]
    public async Task ProjectWindowReportsPolicyOnlyResultWithoutClaimingAddressOrConnectivity()
    {
        var s = new Surface { Current = WithoutLease(), ReturnWithoutLease = true };
        var vm = new ProjectApplyViewModel(new ProjectProfile { Name = "DHCP fixture", Mode = "dhcp" }, null, s, s, s,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { s.Current.Identity }),
            () => Array.Empty<ProjectApplyRequest>(), new Consent(), verificationAttempts: 1, targetAdapterId: s.Current.Identity.Id);
        await vm.RefreshAsync(); await vm.ReviewAsync(false);
        Assert.Contains(Loc.Get("Apply.NoLeaseReview"), vm.Review);
        await vm.ApplyAsync();
        Assert.Equal(Loc.Get("Apply.DhcpPolicyVerified"), vm.Status); Assert.Equal(0, s.Opens);
    }
}
