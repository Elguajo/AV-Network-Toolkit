using System.Collections.Immutable;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ApplyConfirmationTests : IDisposable
{
    private readonly TestDataDirectoryFixture _dir = new();
    public void Dispose() => _dir.Dispose();
    private sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Surface : IProjectApplyBoundary, IProjectApplyJournal
    {
        internal ApplyAdapterSnapshot Current = ProjectApplyTests.Snapshot();
        internal int Begins, Executes;
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken ct) => Task.FromResult(Current);
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent desired, bool recovery)
            => ProjectApplyPolicy.ValidateTransition(s, desired, recovery);
        public Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken ct)
        {
            Executes++; Current = ProjectApplyTests.Applied(Current, request.Desired);
            return Task.FromResult(new ApplyExecutionResult(ApplyExecutionStatus.Finished, true, "CommandsFinished"));
        }
        public Task BeginAsync(ProjectApplyRequest request, CancellationToken ct) { Begins++; return Task.CompletedTask; }
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome) => Task.CompletedTask;
        internal LocalProfileApplyService Service(TimeProvider? clock = null) => new(this, this,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { Current.Identity }), _ => throw new InvalidOperationException(), 1, clock);
    }
    private static IpProfile Profile() => new() { IpAddress = "192.0.2.20", SubnetMask = "255.255.255.0", Gateway = "192.0.2.1" };
    private static ApplyConfirmation Review(ApplyAdapterSnapshot before, ApplyIntent desired, bool old = false)
        => new(new(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(2), before, desired), old);

    [Fact]
    public void SummaryShowsExactChangesAndKeepsIdentityAndRoutesInDetails()
    {
        var before = ProjectApplyTests.Snapshot();
        var review = Review(before, LocalProfileApplyService.Intent(Profile()));
        Assert.Equal(before.Identity.Alias, review.Adapter);
        Assert.Equal("192.0.2.10 → 192.0.2.20", review.Rows[0].Value);
        Assert.Equal(Loc.Format("Apply.UnchangedValue", "255.255.255.0"), review.Rows[1].Value);
        Assert.Equal(Loc.Format("Apply.UnchangedValue", "192.0.2.1"), review.Rows[2].Value);
        Assert.Equal(Loc.Format("Apply.UnchangedValue", "192.0.2.53, 192.0.2.54"), review.Rows[3].Value);
        Assert.Contains(before.Identity.Id.ToString(), review.Details); Assert.Contains("0.0.0.0/0", review.Details);
        Assert.DoesNotContain(before.Identity.Id.ToString(), string.Join(" ", review.Rows.Select(r => r.Value)));
    }
    [Fact]
    public void DhcpAndGatewayRemovalAreExplicitRatherThanReportedUnchanged()
    {
        var before = ProjectApplyTests.Snapshot();
        var review = Review(before, LocalProfileApplyService.Intent(new IpProfile { IsDhcp = true }));
        Assert.Contains(Loc.Get("Apply.AutomaticDhcp"), review.Rows[0].Value);
        Assert.Contains(Loc.Get("Project.DnsAutomatic"), review.Rows[3].Value);
        review = Review(before, ProjectApplyTests.Intent());
        Assert.Equal("192.0.2.1 → " + Loc.Get("Apply.None"), review.Rows[2].Value);
        Assert.Equal("192.0.2.53, 192.0.2.54 → 192.0.2.55, 192.0.2.56", review.Rows[3].Value);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void ResolvedAutomaticDnsShowsThePolicyChangeAndExactFrozenList(bool hasServers)
    {
        var before = ProjectApplyTests.Snapshot() with { Dhcp = true, DnsSource = "automatic",
            DnsServers = hasServers ? ImmutableArray.Create("192.0.2.53") : ImmutableArray<string>.Empty };
        var desired = ProjectApplyPolicy.ResolveIntent(before, LocalProfileApplyService.Intent(Profile()));
        var review = Review(before, desired);
        Assert.Equal(Loc.Get("Project.DnsAutomatic") + " → " + (hasServers ? "192.0.2.53" : Loc.Get("Apply.CompactNoDns")), review.Rows[3].Value);
    }
    [Fact]
    public void OfflineConflictAndIncompleteHistoryWarningsStayOutsideDetails()
    {
        var before = ProjectApplyTests.Snapshot() with { Dad = new(3, false) };
        before = before with { Addresses = ImmutableArray.Create(before.Addresses[0] with { State = "duplicate" }) };
        var review = Review(before, ProjectApplyTests.Intent(), true);
        Assert.Contains(Loc.Get("Apply.CompactOffline"), review.Warning);
        Assert.Contains(Loc.Get("Apply.SourceConflict"), review.Warning);
        Assert.Contains(Loc.Get("LocalApply.OldHistory"), review.Warning);
        Assert.Equal(Loc.Get("Apply.Recovery"), review.Title);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task TypedReviewRetainsExactRequestAndDeclineNeverJournalsOrMutates(bool accept)
    {
        var s = new Surface(); var before = s.Current; ApplyConfirmation? shown = null;
        var result = await s.Service().RunAsync(before.Identity.Id.ToString(), Profile(), null,
            _ => throw new Exception("Full text fallback must not replace the typed dialog."), default,
            review => { shown = review; return Task.FromResult(accept); });
        Assert.NotNull(shown); Assert.Equal(result.Request, shown.Request); Assert.Equal(before, shown.Request.Before);
        Assert.Equal(accept ? 1 : 0, s.Begins); Assert.Equal(accept ? 1 : 0, s.Executes);
        Assert.Equal(accept, result.Outcome != null);
    }
    [Theory]
    [InlineData(120)][InlineData(121)]
    public async Task ExpiredConfirmationDoesNotRunAndFreshExplicitReviewCanSucceed(int seconds)
    {
        var s = new Surface(); var clock = new Clock(); var service = s.Service(clock); int confirmations = 0;
        var result = await service.RunAsync(s.Current.Identity.Id.ToString(), Profile(), null, _ => Task.FromResult(false), default,
            _ => { confirmations++; clock.Now = clock.Now.AddSeconds(seconds); return Task.FromResult(true); });
        Assert.Equal(ProjectApplyState.Rejected, result.Outcome!.State); Assert.Equal("Expired", result.Outcome.Stage);
        Assert.False(result.Outcome.MayHaveChanged); Assert.Equal(Loc.Get("Apply.Refused.Expired"), ApplyOutcomeText.Failure(result.Outcome));
        Assert.Equal(0, s.Begins); Assert.Equal(0, s.Executes); Assert.Equal(1, confirmations);
        result = await service.RunAsync(s.Current.Identity.Id.ToString(), Profile(), null, _ => Task.FromResult(true), default);
        Assert.Equal(ProjectApplyState.Complete, result.Outcome!.State); Assert.Equal(1, s.Executes);
    }
    [Theory]
    [InlineData("Expired", "Expired")][InlineData("PreStateChanged", "Changed")][InlineData("OperationActive", "Busy")]
    [InlineData("Preflight", "Validation")][InlineData("SnapshotWrite", "Journal")][InlineData("UacDenied", "Elevation")]
    [InlineData("UntrustedStageAndSecret", "Other")]
    public void RefusalNamesItsCauseWithoutPartialChangeWarningOrRawStage(string stage, string key)
    {
        var outcome = new ProjectApplyOutcome(ProjectApplyState.Rejected, false, null, stage);
        var message = ApplyOutcomeText.Failure(outcome);
        Assert.Equal(Loc.Get("Apply.Refused." + key), message);
        Assert.DoesNotContain(Loc.Get("Apply.ManualRecovery"), message); Assert.DoesNotContain("Secret", message);
    }
    [Fact]
    public void UncertainOrPartialMutationRetainsExplicitRecoveryWarning()
    {
        foreach (var stage in new[] { "DnsPrimary", "WorkerUnknown", "AddressConflict" })
            Assert.Contains(Loc.Get("Apply.ManualRecovery"), ApplyOutcomeText.Failure(new(ProjectApplyState.ApplyFailed, true, null, stage)));
    }
}
