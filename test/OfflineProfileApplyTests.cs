using System.Collections.Immutable;
using System.IO;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class OfflineProfileApplyTests : IDisposable
{
    private readonly TestDataDirectoryFixture _dir = new();
    public void Dispose() => _dir.Dispose();
    private static ApplyAdapterSnapshot Static(bool active = true) => ProjectApplyTests.Snapshot() with
    {
        Dad = new(3, false),
        Addresses = active ? ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "deprecated")) : ImmutableArray<ApplyAddress>.Empty,
        Routes = ImmutableArray.Create(new ApplyRoute("0.0.0.0/0", "192.0.2.1", 256, "persistent", "persistent"))
    };
    private static ApplyAdapterSnapshot Dhcp(bool apipa) => Static(false) with
    {
        Dhcp = true, DnsSource = "automatic", DnsServers = ImmutableArray<string>.Empty,
        Addresses = apipa ? ImmutableArray.Create(new ApplyAddress("169.254.42.10", 16, "wellknown", "deprecated")) : ImmutableArray<ApplyAddress>.Empty,
        PersistentAddresses = ImmutableArray<ApplyAddress>.Empty, Routes = ImmutableArray<ApplyRoute>.Empty
    };
    private sealed class Surface : IProjectApplyBoundary, IProjectApplyJournal, IBrowserLauncher, IProjectApplyDialogs
    {
        internal ApplyAdapterSnapshot Current = Static();
        internal ProjectApplyRequest? Saved;
        internal int Executes, Begins, Opens;
        internal string ResultAddressState = "deprecated";
        internal bool Reconnect, Fail, FinishFails, Mismatch, KeepFallback, Consent = true;
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken ct) => Task.FromResult(Current);
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent d, bool recovery) => ProjectApplyPolicy.ValidateTransition(s, d, recovery);
        public async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken ct)
        {
            Executes++;
            var result = await ProjectApplyWorker.ExecuteAsync(request, ReadAsync, (_, _) => Task.CompletedTask, _ => Task.FromResult(true));
            if (result.Status != ApplyExecutionStatus.Finished) return result;
            Current = ProjectApplyTests.Applied(Current, request.Desired);
            Current = Current with
            {
                Dad = new(3, Reconnect),
                Addresses = request.Desired.Mode == "dhcp" || ResultAddressState == "absent" ? ImmutableArray<ApplyAddress>.Empty
                    : Current.Addresses.Select(a => a with { State = Reconnect ? "preferred" : ResultAddressState }).ToImmutableArray(),
                Routes = Reconnect ? Current.Routes : Current.Routes.Where(r => r.Store == "persistent").Select(r => r with { Origin = "persistent" }).ToImmutableArray()
            };
            if (Mismatch) Current = Current with { PersistentAddresses = ImmutableArray.Create(new ApplyAddress("192.0.2.99", 24, "manual", "invalid")) };
            if (KeepFallback && request.Desired.Mode == "static")
                Current = Current with { Addresses = Current.Addresses.Add(new("169.254.42.10", 16, "wellknown", "deprecated")),
                    Routes = Current.Routes.Add(new("169.254.0.0/16", "0.0.0.0", 256, "active", "local")) };
            return Fail ? new(ApplyExecutionStatus.Failed, true, "DnsPrimary") : result;
        }
        public Task BeginAsync(ProjectApplyRequest r, CancellationToken ct) { Begins++; Saved = r; return Task.CompletedTask; }
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome) => FinishFails ? Task.FromException(new IOException("fixture")) : Task.CompletedTask;
        public void Open(string destination) => Opens++;
        public bool Confirm(string review, bool recovery) => Consent;
        internal ProjectApplyCoordinator Coordinator() => new(this, this, this, verificationAttempts: 1);
        internal LocalProfileApplyService Local() => new(this, this,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { Current.Identity }), _ => Saved!, verificationAttempts: 1);
    }

    [Theory]
    [InlineData("deprecated")][InlineData("tentative")][InlineData("invalid")][InlineData("preferred")][InlineData("absent")]
    public async Task OfflineStaticConfiguresPersistentPolicyAndRecoversWithoutAReadyActiveAddress(string state)
    {
        var s = new Surface { Current = Static(state != "absent"), ResultAddressState = state };
        if (state != "absent") s.Current = s.Current with { Addresses = s.Current.Addresses.Select(a => a with { State = state }).ToImmutableArray() };
        var original = s.Current; var c = s.Coordinator();
        var plan = await c.PrepareProfileAsync(original.Identity, ProjectApplyTests.Intent());
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
        var result = await c.ExecuteAsync(plan);
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal("OfflinePolicyVerified", result.Stage);
        Assert.Equal(original, s.Saved!.Before); Assert.Equal(0, s.Opens);
        var recovery = await c.PrepareRecoveryAsync(original);
        Assert.Equal("192.0.2.10", recovery.Request.Desired.Address); Assert.Equal("192.0.2.1", recovery.Request.Desired.Gateway);
        Assert.Equal(256, recovery.Request.Desired.GatewayMetric);
        Assert.Equal(ProjectApplyState.Complete, (await c.ExecuteAsync(recovery)).State); Assert.Equal(original.DnsServers, s.Current.DnsServers);
    }

    [Theory]
    [InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
    public async Task OfflineDhcpWithOrWithoutApipaCanSaveStaticOrDhcpPolicy(bool apipa, bool toDhcp)
    {
        var s = new Surface { Current = Dhcp(apipa), ResultAddressState = "absent" };
        var desired = toDhcp ? new ApplyIntent("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty)
            : ProjectApplyTests.Intent() with { Gateway = "192.0.2.1", DnsMode = "leave-unchanged", DnsServers = ImmutableArray<string>.Empty };
        var c = s.Coordinator(); var plan = await c.PrepareProfileAsync(s.Current.Identity, desired);
        Assert.Equal(toDhcp ? "automatic" : "none", plan.Request.Desired.DnsMode);
        var result = await c.ExecuteAsync(plan);
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal("OfflinePolicyVerified", result.Stage);
        Assert.Equal(0, s.Opens);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BrowserWaitsForLinkEvenWhenWindowsRetainsPreferredAddress(bool reconnect)
    {
        var source = Static() with
        {
            Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")),
            Routes = Static().Routes.Add(new("0.0.0.0/0", "192.0.2.1", 256, "active", "netmgmt"))
        };
        var s = new Surface { Current = source, ResultAddressState = "preferred", Reconnect = reconnect };
        var c = s.Coordinator(); var plan = await c.PrepareAsync(s.Current.Identity, ProjectApplyTests.Intent(), "https://192.0.2.44/");
        var result = await c.ExecuteAsync(plan);
        Assert.Equal(reconnect ? ProjectApplyState.Complete : ProjectApplyState.Unverified, result.State);
        Assert.Equal(reconnect ? 1 : 0, s.Opens);
        if (!reconnect) Assert.Equal("OfflinePolicyVerified", result.Stage);
    }

    [Fact]
    public async Task OfflinePolicyCannotHideWorkerFailureOrIncorrectStoredFields()
    {
        foreach (var mismatch in new[] { false, true })
        {
            var s = new Surface { Current = Static(false), Fail = !mismatch, Mismatch = mismatch, ResultAddressState = "absent" };
            var c = s.Coordinator(); var result = await c.ExecuteAsync(await c.PrepareProfileAsync(s.Current.Identity, ProjectApplyTests.Intent()));
            Assert.Equal(mismatch ? ProjectApplyState.Unverified : ProjectApplyState.ApplyFailed, result.State); Assert.Equal(0, s.Opens);
        }
    }

    [Fact]
    public async Task MainHistoryUsesStoredAddressAndGatewayWhenActiveStateIsEmpty()
    {
        var s = new Surface { Current = Static(false), ResultAddressState = "absent" }; var original = s.Current; var id = original.Identity.Id.ToString();
        var vm = new MainViewModel(() => new() { new() { Id = id, Name = "Fixture", IsConnected = false } }, apply: s.Local());
        try
        {
            vm.SelectedAdapter!.EditIsDhcp = false; vm.SelectedAdapter.EditIp = "192.0.2.20"; vm.SelectedAdapter.EditSubnet = "255.255.255.0";
            vm.ConfirmHandler = (_, _) => Task.FromResult(true); await vm.ApplyAsync();
            Assert.Equal(ProjectApplyState.Complete, vm.LastApplyOutcome!.State);
            var entry = Assert.Single(new HistoryRepository().Load(id));
            Assert.Equal("192.0.2.10", entry.OldIp); Assert.Equal("192.0.2.1", entry.OldGateway);
            Assert.NotNull(entry.RecoveryOperationId); Assert.Equal(original, s.Saved!.Before);
        }
        finally { vm.Diagnostics.Dispose(); }
    }

    [Theory]
    [InlineData("unknown-link")][InlineData("unknown-policy")][InlineData("missing-persistent")][InlineData("extra-persistent")]
    [InlineData("active-mismatch")][InlineData("custom-route")][InlineData("unknown-state")][InlineData("gateway-subnet")]
    public async Task OfflineSupportDoesNotDiscardUnknownOrConflictingState(string kind)
    {
        var s = new Surface(); var a = s.Current.Addresses[0];
        s.Current = kind switch
        {
            "unknown-link" => s.Current with { Dad = null },
            "unknown-policy" => s.Current with { ClassificationComplete = false },
            "missing-persistent" => s.Current with { PersistentAddresses = ImmutableArray<ApplyAddress>.Empty },
            "extra-persistent" => s.Current with { PersistentAddresses = s.Current.PersistentAddresses.Add(new("192.0.2.11", 24, "manual", "invalid")) },
            "active-mismatch" => s.Current with { Addresses = ImmutableArray.Create(a with { Address = "192.0.2.12" }) },
            "custom-route" => s.Current with { Routes = s.Current.Routes.Add(new("198.51.100.0/24", "192.0.2.1", 1, "persistent", "netmgmt")) },
            "unknown-state" => s.Current with { Addresses = ImmutableArray.Create(a with { State = "unknown" }) },
            _ => s.Current with { Routes = s.Current.Routes.Select(r => r with { NextHop = "198.51.100.1" }).ToImmutableArray() }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Coordinator().PrepareProfileAsync(s.Current.Identity, ProjectApplyTests.Intent()));
        Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
    }

    [Fact]
    public async Task CableStateChangingAfterReviewInvalidatesThePlan()
    {
        var s = new Surface(); var c = s.Coordinator(); var plan = await c.PrepareProfileAsync(s.Current.Identity, ProjectApplyTests.Intent());
        s.Current = s.Current with { Dad = new(3, true) };
        Assert.Equal(ProjectApplyState.Rejected, (await c.ExecuteAsync(plan)).State); Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
    }

    [Fact]
    public async Task WindowReportsOfflineStorageAndKeepsJournalWarningVisible()
    {
        var s = new Surface { FinishFails = true };
        var p = new ProjectProfile { Mode = "static", Name = "Fixture", Ipv4 = new() { Address = "192.0.2.20", PrefixLength = 24, Dns = new() { Mode = "leave-unchanged" } } };
        var vm = new ProjectApplyViewModel(p, null, s, s, s,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { s.Current.Identity }), () => Array.Empty<ProjectApplyRequest>(), s,
            verificationAttempts: 1, targetAdapterId: s.Current.Identity.Id);
        await vm.RefreshAsync(); await vm.ReviewAsync(false); Assert.Contains(Loc.Get("Apply.OfflineReview"), vm.Review);
        await vm.ApplyAsync(); Assert.Contains(Loc.Get("Apply.OfflinePolicyVerified"), vm.Status);
        Assert.Contains(Loc.Get("Apply.JournalWarning"), vm.Status); Assert.Equal(0, s.Opens);
    }

    [Theory]
    [InlineData("deprecated")][InlineData("duplicate")]
    public async Task OfflineAutomaticFallbackDoesNotHideTheSingleStoredStaticConfiguration(string state)
    {
        var s = new Surface { KeepFallback = true };
        s.Current = s.Current with { Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", state),
            new ApplyAddress("169.254.42.10", 16, "wellknown", "deprecated")),
            Routes = s.Current.Routes.Add(new("169.254.0.0/16", "0.0.0.0", 256, "active", "local")) };
        var original = s.Current; var c = s.Coordinator();
        var result = await c.ExecuteAsync(await c.PrepareProfileAsync(original.Identity, ProjectApplyTests.Intent()));
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal("OfflinePolicyVerified", result.Stage);
        Assert.Equal(original, s.Saved!.Before); Assert.Equal(0, s.Opens);
    }
}
