using System.Collections.Immutable;
using System.IO;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class LinkLocalViewModelTests
{
    private sealed class Surface : IProjectApplyBoundary, IProjectApplyJournal, IBrowserLauncher, IProjectApplyDialogs
    {
        public ApplyAdapterSnapshot Snapshot = LinkLocalTests.Before();
        public ProjectApplyRequest? LastRequest;
        public readonly List<ProjectApplyRequest> Saved = new();
        public bool Consent, FailBegin, Conflict;
        public int Executes, Reads, Confirms, Browsers;
        public TaskCompletionSource? Hold;
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken token)
        { Reads++; token.ThrowIfCancellationRequested(); return Task.FromResult(Snapshot); }
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent d, bool recovery) => ProjectApplyPolicy.ValidateTransition(s, d, recovery);
        public async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken token)
        {
            Executes++; LastRequest = request;
            if (Hold != null) await Hold.Task;
            var source = Snapshot.DnsSource; var servers = Snapshot.DnsServers;
            Snapshot = ProjectApplyTests.Applied(Snapshot, request.Desired);
            if (!Conflict) return new(ApplyExecutionStatus.Finished, true, "CommandsFinished");
            Snapshot = Snapshot with { Addresses = ImmutableArray.Create(Snapshot.Addresses[0] with { State = "duplicate" }),
                DnsSource = source, DnsServers = servers };
            return new(ApplyExecutionStatus.Failed, true, "AddressConflict");
        }
        public Task BeginAsync(ProjectApplyRequest r, CancellationToken token)
        { if (FailBegin) throw new IOException(); Saved.Add(r); return Task.CompletedTask; }
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome) => Task.CompletedTask;
        public bool Confirm(string review, bool recovery) { Confirms++; return Consent; }
        public void Open(string destination) { Browsers++; throw new Exception("Manual action must not open a browser."); }
    }
    private static ProjectApplyViewModel Vm(Surface s) => new(null, null, s, s, s,
        _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { s.Snapshot.Identity }), () => s.Saved, s,
        verificationAttempts: 1, linkLocalTool: true);

    [Fact]
    public async Task GenerateSelectionAndReviewNeverMutateAndEditingInvalidatesConfirmation()
    {
        var s = new Surface(); var vm = Vm(s); await vm.RefreshAsync();
        Assert.True(vm.IsLinkLocalTool); Assert.Null(vm.SelectedAdapter);
        Assert.False(vm.ReviewCommand.CanExecute(null)); vm.GenerateAddressCommand.Execute(null);
        Assert.True(LinkLocalAddressing.IsCandidate(vm.LinkLocalAddress)); Assert.Equal(0, s.Reads);
        vm.SelectedAdapter = vm.Adapters[0]; await vm.ReviewAsync(false);
        Assert.Equal(1, s.Reads); Assert.Equal(0, s.Executes); Assert.Empty(s.Saved);
        Assert.Contains("192.0.2.10", vm.Review); Assert.Contains(vm.LinkLocalAddress + "/16", vm.Review);
        Assert.Contains("LinkLocal.DadWarning", vm.Review);
        await vm.ApplyAsync(); Assert.Equal(1, s.Confirms); Assert.Equal(0, s.Executes);
        vm.LinkLocalDns1 = "192.0.2.55"; Assert.False(vm.ApplyCommand.CanExecute(null));
        await vm.ReviewAsync(false); vm.LinkLocalAddress = "169.254.255.1";
        Assert.False(vm.ReviewCommand.CanExecute(null)); Assert.False(vm.ApplyCommand.CanExecute(null));
        Assert.Equal(0, s.Browsers);
    }

    [Fact]
    public async Task ExplicitDnsFromDhcpIsJournaledAndRecoveryReturnsToOriginalAutomaticPolicy()
    {
        var s = new Surface { Snapshot = LinkLocalTests.DhcpBefore(), Consent = true }; var vm = Vm(s);
        await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0]; vm.LinkLocalAddress = "169.254.42.10";
        await vm.ReviewAsync(false); Assert.False(vm.ApplyCommand.CanExecute(null)); Assert.Equal(0, s.Executes);
        vm.LinkLocalDns2 = "192.0.2.54"; await vm.ReviewAsync(false); Assert.False(vm.ApplyCommand.CanExecute(null));
        vm.LinkLocalDns1 = "192.0.2.53"; await vm.ReviewAsync(false); await vm.ApplyAsync();
        var request = Assert.Single(s.Saved); Assert.Equal("automatic", request.Before.DnsSource);
        Assert.Equal("static", request.Desired.DnsMode); Assert.Equal(2, request.Desired.DnsServers.Length);
        Assert.Equal("LinkLocal.Verified", vm.Status); Assert.Equal(0, s.Browsers);
        await vm.ReviewAsync(true); Assert.Contains("\nDHCP\n", vm.Review);
        Assert.Equal(1, s.Executes); await vm.ApplyAsync();
        Assert.True(s.LastRequest!.Recovery); Assert.Equal("dhcp", s.LastRequest.Desired.Mode);
        Assert.Equal("automatic", s.Snapshot.DnsSource); Assert.Equal("Apply.Recovered", vm.Status); Assert.Equal(0, s.Browsers);
    }

    [Fact]
    public async Task ExplicitReturnToDhcpRequiresItsOwnReviewAndConsent()
    {
        var s = new Surface { Consent = true }; var vm = Vm(s); await vm.RefreshAsync();
        vm.SelectedAdapter = vm.Adapters[0]; vm.LinkLocalAddress = "169.254.42.10";
        await vm.ReviewAsync(false); await vm.ApplyAsync();
        s.Consent = false; await vm.ReviewDhcpAsync(); Assert.Contains("LinkLocal.DhcpWarning", vm.Review);
        Assert.Equal(1, s.Executes); await vm.ApplyAsync(); Assert.Equal(1, s.Executes);
        s.Consent = true; await vm.ApplyAsync(); Assert.Equal(2, s.Executes);
        Assert.Equal("dhcp", s.LastRequest!.Desired.Mode); Assert.False(s.LastRequest.Recovery);
        Assert.True(s.Snapshot.Dhcp); Assert.Equal(0, s.Browsers);
    }

    [Fact]
    public async Task ConflictBeforeDnsReplacementCanRecoverOriginalAutomaticDhcpPolicy()
    {
        var s = new Surface { Consent = true, Conflict = true, Snapshot = LinkLocalTests.DhcpBefore() };
        var vm = Vm(s); await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0];
        vm.LinkLocalAddress = "169.254.42.10"; vm.LinkLocalDns1 = "192.0.2.55";
        await vm.ReviewAsync(false); await vm.ApplyAsync();
        Assert.False(s.Snapshot.Dhcp); Assert.Equal("automatic", s.Snapshot.DnsSource);
        Assert.Contains("duplicate", vm.Observed); Assert.Equal(1, s.Executes);
        s.Conflict = false; await vm.ReviewAsync(true); Assert.True(vm.ApplyCommand.CanExecute(null));
        await vm.ApplyAsync(); Assert.Equal("Apply.Recovered", vm.Status);
        Assert.True(s.Snapshot.Dhcp); Assert.Equal("automatic", s.Snapshot.DnsSource); Assert.Equal(0, s.Browsers);
    }

    [Fact]
    public async Task BusyCancellationFreezesIdentityAndInputsThenObservesWithoutBrowser()
    {
        var s = new Surface { Consent = true, Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var vm = Vm(s); await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0]; vm.LinkLocalAddress = "169.254.42.10";
        await vm.ReviewAsync(false); var pending = vm.ApplyAsync(); Assert.True(vm.IsBusy);
        var id = vm.SelectedAdapter; vm.SelectedAdapter = null; vm.LinkLocalAddress = "169.254.42.11"; vm.LinkLocalDns1 = "192.0.2.55";
        Assert.Equal(id, vm.SelectedAdapter); Assert.Equal("169.254.42.10", vm.LinkLocalAddress); Assert.Empty(vm.LinkLocalDns1);
        Assert.False(vm.ReviewDhcpCommand.CanExecute(null)); vm.Cancel(); s.Hold.SetResult(); await pending;
        Assert.False(vm.IsBusy); Assert.NotEmpty(vm.Observed); Assert.Equal(0, s.Browsers);
    }

    [Theory]
    [InlineData(true, false)][InlineData(false, true)]
    public async Task JournalFailureOrDetectedConflictCannotReportSuccessOrRetryAutomatically(bool journalFailure, bool conflict)
    {
        var s = new Surface { Consent = true, FailBegin = journalFailure, Conflict = conflict }; var vm = Vm(s);
        await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0]; vm.LinkLocalAddress = "169.254.42.10";
        await vm.ReviewAsync(false); await vm.ApplyAsync();
        Assert.Equal(journalFailure ? 0 : 1, s.Executes); Assert.Equal(0, s.Browsers);
        Assert.DoesNotContain("LinkLocal.Verified", vm.Status);
        if (conflict) { Assert.Contains("Apply.State.ApplyFailed", vm.Status); Assert.Contains("duplicate", vm.Observed); }
        Assert.False(vm.ApplyCommand.CanExecute(null));
    }
}
