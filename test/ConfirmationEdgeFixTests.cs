// Regression coverage for SWITCH-09–12 using fake execution boundaries.
// No real network/elevation/browser/storage actions.
using System.Collections.Immutable;
using System.Reflection;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using Xunit;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ConfirmationEdgeFixTests
{
    private sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static ApplyAdapterSnapshot Snapshot() => new(
        new(Guid.NewGuid(), 12, "Ethernet fixture", "02-00-00-00-00-01"), false,
        ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")), "static",
        ImmutableArray.Create("192.0.2.53"), ImmutableArray<ApplyRoute>.Empty, true,
        ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")), Dad: new(3, true));
    private static IpProfile Profile() => new()
    { Name = "Fixture", IpAddress = "192.0.2.20", SubnetMask = "255.255.255.0", PrimaryDns = "192.0.2.55" };

    private sealed class Surface : IProjectApplyBoundary, IProjectApplyJournal, IBrowserLauncher, IProjectApplyDialogs
    {
        internal readonly Clock Time = new();
        internal ApplyAdapterSnapshot Current = Snapshot();
        internal string ExpireAt = "";
        internal bool BrowserFails, FinishFails;
        internal int Reads, Begins, Finishes, BoundaryCalls, FakeUacPrompts, Commands, Opens, Confirms;
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken ct)
        {
            Reads++;
            if (Reads == 2 && ExpireAt == "preflight") Time.Now = Time.Now.AddSeconds(2);
            return Task.FromResult(Current);
        }
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent desired, bool recovery)
            => ProjectApplyPolicy.ValidateTransition(s, desired, recovery);
        public async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken ct)
        {
            BoundaryCalls++;
            // Same first statement as the production ProjectApplyElevation.ExecuteAsync.
            // It precedes process/pipe/UAC creation, and can throw without any OS mutation.
            ProjectApplyWire.ValidateRequest(request, Time.Now);
            if (ExpireAt == "unknown") throw new System.IO.IOException("Synthetic boundary exception");
            FakeUacPrompts++;
            if (ExpireAt == "uac" && BoundaryCalls == 1) Time.Now = Time.Now.AddMinutes(3);
            return await ProjectApplyWorker.ExecuteAsync(request, ReadAsync, (_, _) =>
            {
                Commands++;
                var d = request.Desired;
                var a = new ApplyAddress(d.Address!, d.PrefixLength, "manual", "preferred");
                Current = Current with { Addresses = ImmutableArray.Create(a), PersistentAddresses = ImmutableArray.Create(a),
                    DnsSource = d.DnsMode, DnsServers = d.DnsServers };
                if (ExpireAt == "after-address" && Commands == 1) Time.Now = Time.Now.AddMinutes(3);
                return Task.CompletedTask;
            }, _ => Task.FromResult(true), () => Time.Now);
        }
        public Task BeginAsync(ProjectApplyRequest request, CancellationToken ct)
        {
            Begins++;
            if (Begins == 1 && ExpireAt is "journal" or "journal-validation") Time.Now = Time.Now.AddSeconds(2);
            if (ExpireAt == "journal-validation") ProjectApplyWire.ValidateRequest(request, Time.Now);
            return Task.CompletedTask;
        }
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome)
        { Finishes++; return FinishFails ? Task.FromException(new System.IO.IOException("Synthetic finish failure")) : Task.CompletedTask; }
        public void Open(string destination) { Opens++; if (BrowserFails) throw new InvalidOperationException("Synthetic browser failure"); }
        public bool Confirm(string review, bool recovery) { Confirms++; return true; }
        internal LocalProfileApplyService Local() => new(this, this,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { Current.Identity }),
            _ => throw new InvalidOperationException(), 1, Time);
        internal Task<LocalApplyResult> RunLocal() => Local().RunAsync(Current.Identity.Id.ToString(), Profile(), null,
            _ =>
            {
                Confirms++;
                if (Confirms == 1 && ExpireAt is "preflight" or "journal" or "journal-validation") Time.Now = Time.Now.AddSeconds(119);
                if (Confirms > 1) Assert.Equal(0, Commands); // The old consent never starts a command.
                return Task.FromResult(true);
            }, default);
    }

    [Theory]
    [InlineData("preflight")][InlineData("journal")][InlineData("journal-validation")]
    public async Task ExpiryDuringPreflightOrJournalRequiresFreshConsentBeforeAnyExecution(string expireAt)
    {
        var s = new Surface { ExpireAt = expireAt }; var result = await s.RunLocal();
        Assert.Equal(ProjectApplyState.Complete, result.Outcome!.State);
        Assert.Equal(1, s.FakeUacPrompts); Assert.Equal(2, s.Commands); Assert.Equal(2, s.Confirms);
        Assert.Equal(expireAt == "preflight" ? 1 : 2, s.Begins);
        Assert.Equal(expireAt == "journal" ? 2 : 1, s.Finishes);
    }

    [Fact]
    public async Task Control_FreshConsentTraversesTheSameFakeBoundarySuccessfully()
    {
        var s = new Surface(); var result = await s.RunLocal();
        Assert.Equal(ProjectApplyState.Complete, result.Outcome!.State);
        Assert.Equal(1, s.FakeUacPrompts); Assert.Equal(2, s.Commands); Assert.Equal(1, s.Confirms);
    }

    [Fact]
    public async Task ExpiryDuringFakeUacRequiresNewConsentAndDoesNotReplayOldCommands()
    {
        var s = new Surface { ExpireAt = "uac" }; var result = await s.RunLocal();
        Assert.Equal(ProjectApplyState.Complete, result.Outcome!.State);
        Assert.Equal(2, s.Commands); Assert.Equal(2, s.FakeUacPrompts); Assert.Equal(2, s.Confirms);
    }

    [Fact]
    public async Task PermanentInhibitionRemainsLockedWithAccurateWorkerExitGuidance()
    {
        // Test-process static only. Restore it in finally, never change a running product process.
        var flag = typeof(NetworkMutationLease).GetField("_inhibited", BindingFlags.Static | BindingFlags.NonPublic)!;
        int original = (int)flag.GetValue(null)!;
        try
        {
            var firstWarning = ApplyOutcomeText.Failure(new(ProjectApplyState.ApplyFailed, true, null, "WorkerStillRunning"));
            Assert.Contains(Loc.Get("Apply.WorkerActive"), firstWarning);
            Assert.DoesNotContain(Loc.Get("Apply.ManualRecovery"), firstWarning);
            NetworkMutationLease.Inhibit();
            var s = new Surface(); var first = await s.RunLocal();
            s.Time.Now = s.Time.Now.AddHours(1); var later = await s.RunLocal();
            Assert.Equal(1, flag.GetValue(null)); Assert.Null(NetworkMutationLease.TryAcquire());
            Assert.Equal("WorkerInhibited", first.Outcome!.Stage); Assert.Equal("WorkerInhibited", later.Outcome!.Stage);
            Assert.Equal(Loc.Get("Apply.Refused.Inhibited"), ApplyOutcomeText.Failure(later.Outcome));
            Assert.Equal(0, s.Commands); Assert.Equal(0, s.BoundaryCalls);
        }
        finally { flag.SetValue(null, original); }
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BrowserOnlyFailurePreservesVerifiedNetworkAndAnyJournalWarning(bool finishFails)
    {
        var s = new Surface { BrowserFails = true, FinishFails = finishFails };
        var p = new ProjectProfile { Name = "Fixture", Mode = "static", Ipv4 = new()
        { Address = "192.0.2.20", PrefixLength = 24, Dns = new() { Mode = "static", Servers = new() { "192.0.2.55" } } } };
        var vm = new ProjectApplyViewModel(p, new ProjectDevice { Name = "Fixture device", WebUrl = "https://192.0.2.44/", ProfileId = p.Id },
            s, s, s, _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { s.Current.Identity }),
            () => Array.Empty<ProjectApplyRequest>(), s, verificationAttempts: 1);
        await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters.Single(); await vm.ReviewAsync(false);
        // This VM owns a real coordinator clock; align the fake worker clock after preparation.
        s.Time.Now = DateTimeOffset.UtcNow; await vm.ApplyAsync();
        Assert.Equal("192.0.2.20", s.Current.Addresses.Single().Address); Assert.Equal(2, s.Commands); Assert.Equal(1, s.Opens);
        Assert.Contains(Loc.Get("Apply.State.BrowserFailed"), vm.Status);
        Assert.DoesNotContain(Loc.Get("Apply.ManualRecovery"), vm.Status); Assert.Equal(1, s.Finishes);
        Assert.Equal(finishFails, vm.Status.Contains(Loc.Get("Apply.JournalWarning")));
    }

    [Fact]
    public async Task ExpiredElevationEntryReturnsKnownNoChangeBeforeAnyWindowsProcessWork()
    {
        // This is the actual production entry point, but the guaranteed-expired
        // request must return before pipe, identity, process or UAC creation.
        var s = new Surface();
        var request = new ProjectApplyRequest(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-1),
            s.Current, LocalProfileApplyService.Intent(Profile()));
        var result = await ProjectApplyElevation.ExecuteAsync(request, default);
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.Equal("Expired", result.Stage);
        Assert.False(result.MayHaveChanged); Assert.True(ProjectApplyElevation.ValidResult(result));
    }

    [Fact]
    public async Task MalformedExpiredPayloadIsNotClassifiedAsAnOrdinaryExpiredConsent()
    {
        var s = new Surface();
        var request = new ProjectApplyRequest(2, Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-1),
            s.Current, LocalProfileApplyService.Intent(Profile()));
        await Assert.ThrowsAnyAsync<Exception>(() => ProjectApplyElevation.ExecuteAsync(request, default));
        request = request with { Version = 1, Desired = request.Desired with { DnsMode = "untrusted" } };
        await Assert.ThrowsAnyAsync<Exception>(() => ProjectApplyElevation.ExecuteAsync(request, default));
    }

    [Fact]
    public async Task ExpiryAfterFirstCommandRemainsPartialAndNeverRequestsNewConsent()
    {
        var s = new Surface { ExpireAt = "after-address" }; var result = await s.RunLocal();
        Assert.Equal(ProjectApplyState.ApplyFailed, result.Outcome!.State); Assert.True(result.Outcome.MayHaveChanged);
        Assert.Equal("DnsPrimary", result.Outcome.Stage); Assert.Equal(1, s.Commands); Assert.Equal(1, s.Confirms);
        Assert.Contains(Loc.Get("Apply.ManualRecovery"), ApplyOutcomeText.Failure(result.Outcome));
        Assert.Equal(1, s.Begins); Assert.Equal(1, s.Finishes);
    }

    [Fact]
    public void ExpiredWireResultCannotClaimSuccessOrPartialMutation()
    {
        Assert.True(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Failed, false, "Expired")));
        foreach (var status in new[] { ApplyExecutionStatus.Finished, ApplyExecutionStatus.Cancelled, ApplyExecutionStatus.Unknown })
            Assert.False(ProjectApplyElevation.ValidResult(new(status, false, "Expired")));
        Assert.False(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Failed, true, "Expired")));
    }

    [Fact]
    public async Task ExpiryJournalFinishFailureStaysVisibleAndDoesNotStartAnotherAttempt()
    {
        var s = new Surface { ExpireAt = "journal", FinishFails = true }; var result = await s.RunLocal();
        Assert.Equal(ProjectApplyState.Rejected, result.Outcome!.State); Assert.Equal("Expired", result.Outcome.Stage);
        Assert.False(result.Outcome.MayHaveChanged); Assert.True(result.Outcome.JournalUpdateFailed);
        Assert.Equal(1, s.Confirms); Assert.Equal(1, s.Begins); Assert.Equal(1, s.Finishes); Assert.Equal(0, s.Commands);
    }

    [Fact]
    public async Task UnknownBoundaryExceptionRemainsConservativeAndNeverAutomaticallyRetries()
    {
        var s = new Surface { ExpireAt = "unknown" }; var result = await s.RunLocal();
        Assert.Equal(ProjectApplyState.ApplyFailed, result.Outcome!.State); Assert.Equal("WorkerUnknown", result.Outcome.Stage);
        Assert.True(result.Outcome.MayHaveChanged); Assert.Equal(1, s.Confirms);
        Assert.Contains(Loc.Get("Apply.ManualRecovery"), ApplyOutcomeText.Failure(result.Outcome));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task TransientActiveAndUpdateBarriersAreDistinctFromPermanentInhibition(bool update)
    {
        using var blocker = update ? NetworkMutationLease.TrySuspendForUpdate() : (IDisposable?)NetworkMutationLease.TryAcquire();
        Assert.NotNull(blocker);
        var s = new Surface(); var result = await s.RunLocal();
        Assert.Equal(ProjectApplyState.Rejected, result.Outcome!.State); Assert.False(result.Outcome.MayHaveChanged);
        Assert.Equal(update ? "UpdatePending" : "OperationActive", result.Outcome.Stage);
        Assert.Equal(Loc.Get(update ? "Apply.Refused.Update" : "Apply.Refused.Busy"), ApplyOutcomeText.Failure(result.Outcome));
        Assert.Equal(0, s.Commands); Assert.Equal(0, s.BoundaryCalls);
    }
}
