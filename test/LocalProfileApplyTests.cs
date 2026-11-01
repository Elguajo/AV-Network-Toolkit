using System.Collections.Immutable;
using System.IO;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class LocalProfileApplyTests : IDisposable
{
    private readonly TestDataDirectoryFixture _dir = new();
    public void Dispose() => _dir.Dispose();
    private sealed class Surface : IProjectApplyBoundary, IProjectApplyJournal
    {
        internal ApplyAdapterSnapshot Current = ProjectApplyTests.Snapshot();
        internal ProjectApplyRequest? Executed, Begun, Saved;
        internal int Executes, Begins;
        internal bool Fail, FailFinish;
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); return Task.FromResult(Current); }
        public void ValidateSupported(ApplyAdapterSnapshot before, ApplyIntent desired, bool recovery)
            => ProjectApplyPolicy.ValidateTransition(before, desired, recovery);
        public Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken ct)
        {
            Executes++; Executed = request; Current = ProjectApplyTests.Applied(Current, request.Desired);
            return Task.FromResult(new ApplyExecutionResult(Fail ? ApplyExecutionStatus.Failed : ApplyExecutionStatus.Finished, true, "CommandsFinished"));
        }
        public Task BeginAsync(ProjectApplyRequest request, CancellationToken ct) { Begins++; Begun = request; return Task.CompletedTask; }
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome) => FailFinish ? Task.FromException(new IOException("fixture")) : Task.CompletedTask;
        internal LocalProfileApplyService Service() => new(this, this,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { Current.Identity }),
            id => Saved is { } saved && saved.OperationId == id ? saved : throw new IOException(), 1);
    }
    private static IpProfile Profile() => new() { Name = "Fixture", IpAddress = "192.0.2.20", SubnetMask = "255.255.255.0", PrimaryDns = "192.0.2.55", SecondaryDns = "192.0.2.56" };
    [Fact]
    public async Task DeclineNeverJournalsOrMutatesAndSuccessfulApplyUsesObservedOriginal()
    {
        var s = new Surface(); var before = s.Current; var service = s.Service();
        var declined = await service.RunAsync(before.Identity.Id.ToString(), Profile(), null, _ => Task.FromResult(false), default);
        Assert.Null(declined.Outcome); Assert.Equal(0, s.Begins); Assert.Equal(0, s.Executes);
        var applied = await service.RunAsync(before.Identity.Id.ToString(), Profile(), null, review =>
        { Assert.Contains("192.0.2.53", review); Assert.Contains("192.0.2.55", review); return Task.FromResult(true); }, default);
        Assert.Equal(ProjectApplyState.Complete, applied.Outcome!.State); Assert.Equal(before, applied.Request.Before);
        Assert.Equal(1, s.Begins); Assert.Equal(1, s.Executes); Assert.Equal(applied.Request, s.Begun);
    }
    [Theory]
    [InlineData("no-address")][InlineData("automatic-dns")][InlineData("multiple-addresses")][InlineData("custom-route")]
    public async Task UnsupportedSourceFailsBeforeJournalOrMutationWithSpecificFeedback(string kind)
    {
        var s = new Surface(); var profile = Profile();
        s.Current = kind switch
        {
            "no-address" => s.Current with { Addresses = ImmutableArray<ApplyAddress>.Empty },
            "automatic-dns" => s.Current with { DnsSource = "automatic" },
            "multiple-addresses" => s.Current with { Addresses = s.Current.Addresses.Add(new("198.51.100.10", 24, "manual", "preferred")) },
            _ => s.Current with { Routes = s.Current.Routes.Add(new("203.0.113.0/24", "192.0.2.1", 1, "active", "netmgmt")) }
        };
        var error = await Assert.ThrowsAsync<LocalApplyBlockedException>(() => s.Service().RunAsync(s.Current.Identity.Id.ToString(), profile, null, _ => Task.FromResult(true), default));
        Assert.StartsWith("LocalApply.", error.Code); Assert.Equal(0, s.Executes); Assert.Equal(0, s.Begins);
    }
    [Fact]
    public async Task FailedMutationKeepsOriginalJournalAndNeverClaimsComplete()
    {
        var s = new Surface { Fail = true }; var original = s.Current;
        var result = await s.Service().RunAsync(original.Identity.Id.ToString(), Profile(), null, _ => Task.FromResult(true), default);
        Assert.Equal(ProjectApplyState.ApplyFailed, result.Outcome!.State); Assert.True(result.Outcome.MayHaveChanged);
        Assert.Equal(original, s.Begun!.Before); Assert.NotNull(result.Outcome.Observed);
    }
    [Fact]
    public async Task NewHistoryRecoversOriginalDnsFromItsExactJournal()
    {
        var s = new Surface(); var before = s.Current;
        var result = await s.Service().RunAsync(before.Identity.Id.ToString(), Profile(), null, _ => Task.FromResult(true), default);
        s.Saved = result.Request;
        var entry = new HistoryEntry { AdapterId = before.Identity.Id.ToString(), RecoveryOperationId = result.Request.OperationId };
        var recovered = await s.Service().RunAsync(before.Identity.Id.ToString(), new(), entry, _ => Task.FromResult(true), default);
        Assert.Equal(ProjectApplyState.Complete, recovered.Outcome!.State); Assert.True(recovered.Request.Recovery);
        Assert.Equal(before.DnsServers, s.Current.DnsServers); Assert.Equal(before.Addresses, s.Current.Addresses);
    }
    [Fact]
    public async Task OldHistoryRequiresExplicitIncompleteRecoveryWarningAndLeavesDnsUntouched()
    {
        var s = new Surface(); var originalDns = s.Current.DnsServers;
        var entry = new HistoryEntry { AdapterId = s.Current.Identity.Id.ToString(), OldIp = "192.0.2.30", OldSubnet = "255.255.255.0" };
        var result = await s.Service().RunAsync(entry.AdapterId, new(), entry, review =>
        { Assert.Contains(Loc.Get("LocalApply.OldHistory"), review); return Task.FromResult(true); }, default);
        Assert.Equal("leave-unchanged", result.Request.Desired.DnsMode); Assert.Equal(originalDns, s.Current.DnsServers);
        Assert.Equal(ProjectApplyState.Complete, result.Outcome!.State);
    }
    [Fact]
    public async Task RecoveryNeverRetargetsAnotherAdaptersJournal()
    {
        var s = new Surface(); s.Saved = new(1, Guid.NewGuid(), DateTimeOffset.UtcNow, ProjectApplyTests.Snapshot(), ProjectApplyTests.Intent());
        await Assert.ThrowsAsync<LocalApplyBlockedException>(() => s.Service().RunAsync(s.Current.Identity.Id.ToString(), new(),
            new() { RecoveryOperationId = s.Saved.OperationId }, _ => Task.FromResult(true), default));
        Assert.Equal(0, s.Executes);
    }
    [Fact]
    public async Task MainApplyWithoutConfirmationHandlerFailsClosedAndExitCancelsPendingConsent()
    {
        var s = new Surface(); var id = s.Current.Identity.Id.ToString();
        var vm = new MainViewModel(() => new() { new() { Id = id, Name = "Fixture", CurrentIp = "192.0.2.10", CurrentSubnet = "255.255.255.0", CurrentPrimaryDns = "192.0.2.53" } }, apply: s.Service());
        await vm.ApplyAsync(); Assert.Equal(0, s.Executes); Assert.False(vm.IsLoading);
        var consent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.ConfirmHandler = (_, _) => consent.Task;
        var task = vm.ApplyAsync(); Assert.True(vm.IsLoading); Assert.False(vm.ConfirmApplicationExit());
        consent.SetResult(true); await task;
        Assert.Equal(0, s.Executes); Assert.True(vm.ConfirmApplicationExit()); vm.Diagnostics.Dispose();
    }
    [Fact]
    public async Task FinalJournalFailureDoesNotLoseVerifiedNetworkOutcome()
    {
        var s = new Surface { FailFinish = true };
        var result = await s.Service().RunAsync(s.Current.Identity.Id.ToString(), Profile(), null, _ => Task.FromResult(true), default);
        Assert.Equal(ProjectApplyState.Complete, result.Outcome!.State); Assert.True(result.Outcome.JournalUpdateFailed); Assert.NotNull(s.Begun);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task MainHistoryUsesObservedSnapshotAndStorageFailureCannotUndoVerifiedOutcome(bool failHistory)
    {
        var s = new Surface(); var before = s.Current; var id = before.Identity.Id.ToString();
        var vm = new MainViewModel(() => new() { new() { Id = id, Name = "Stale display", CurrentIp = "198.51.100.99", CurrentSubnet = "255.255.255.0" } }, apply: s.Service());
        vm.ConfirmHandler = (_, _) => Task.FromResult(true);
        vm.SelectedAdapter!.EditIsDhcp = true;
        if (failHistory) Directory.CreateDirectory(Path.Combine(_dir.Root, $"history_{id}.json"));
        await vm.ApplyAsync();
        Assert.Equal(ProjectApplyState.Complete, vm.LastApplyOutcome!.State); Assert.NotNull(s.Begun);
        Assert.Equal(before, s.Begun.Before); Assert.False(vm.IsLoading);
        if (failHistory) Assert.Empty(vm.SelectedAdapter.History);
        else
        {
            var entry = Assert.Single(new HistoryRepository().Load(id));
            Assert.Equal(before.Addresses[0].Address, entry.OldIp);
            Assert.Equal(s.Begun.OperationId, entry.RecoveryOperationId);
        }
        vm.Diagnostics.Dispose();
    }
    [Fact]
    public void OldAndExtendedHistoryArraysBothRoundTrip()
    {
        var repo = new HistoryRepository(); var operation = Guid.NewGuid();
        repo.Append("fixture", new() { OldIp = "192.0.2.10" }); repo.Append("fixture", new() { RecoveryOperationId = operation });
        var entries = repo.Load("fixture"); Assert.Equal(operation, entries[0].RecoveryOperationId); Assert.Null(entries[1].RecoveryOperationId);
    }
}
