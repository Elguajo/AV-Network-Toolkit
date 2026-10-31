using System.Collections.Immutable;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ProjectApplyViewModelTests
{
    private sealed class Surface : IProjectApplyBoundary, IProjectApplyJournal, IBrowserLauncher, IProjectApplyDialogs
    {
        public ApplyAdapterSnapshot Snapshot = ProjectApplyTests.Snapshot();
        public int Executes, Opened, Confirms;
        public bool Consent;
        public TaskCompletionSource? Wait;
        public ApplyExecutionResult Result = new(ApplyExecutionStatus.Finished, true, "Fixture");
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken cancellation) => Task.FromResult(Snapshot);
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent d, bool recovery) => ProjectApplyPolicy.ValidateSnapshot(s, recovery);
        public async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest r, CancellationToken cancellation)
        {
            Executes++; if (Wait != null) await Wait.Task;
            Snapshot = ProjectApplyTests.Applied(Snapshot, r.Desired); return Result;
        }
        public Task BeginAsync(ProjectApplyRequest r, CancellationToken cancellation) => Task.CompletedTask;
        public Task FinishAsync(Guid id, ProjectApplyOutcome o) => Task.CompletedTask;
        public void Open(string destination) => Opened++;
        public bool Confirm(string review, bool recovery) { Confirms++; return Consent; }
    }
    private static ProjectApplyViewModel Vm(Surface s)
    {
        var p = new ProjectProfile { Name = "Fixture", Mode = "static", Ipv4 = new()
        { Address = "192.0.2.20", PrefixLength = 24, Gateway = null, Dns = new() { Mode = "static", Servers = new() { "192.0.2.55" } } } };
        return new(p, new ProjectDevice { Name = "Device", WebUrl = "http://192.0.2.44/panel#!/main", Ip = "198.51.100.44", ProfileId = p.Id },
            s, s, s, _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { s.Snapshot.Identity }),
            () => new[] { new ProjectApplyRequest(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(2), s.Snapshot, ProjectApplyTests.Intent()) }, s, 1);
    }
    [Fact]
    public async Task ManualSelectionReviewAndFinalConsentAreSeparateGates()
    {
        var s = new Surface(); var vm = Vm(s);
        await vm.RefreshAsync(); Assert.Null(vm.SelectedAdapter); Assert.False(vm.ReviewCommand.CanExecute(null));
        Assert.False(vm.ApplyCommand.CanExecute(null)); Assert.Equal(0, s.Executes);
        vm.SelectedAdapter = vm.Adapters[0]; await vm.ReviewAsync(false);
        Assert.Contains("192.0.2.10", vm.Review); Assert.Contains("192.0.2.20", vm.Review);
        Assert.Contains("192.0.2.53", vm.Review); Assert.Contains("192.0.2.55", vm.Review);
        await vm.ApplyAsync(); Assert.Equal(1, s.Confirms); Assert.Equal(0, s.Executes); Assert.True(vm.ApplyCommand.CanExecute(null));
        s.Consent = true; await vm.ApplyAsync(); Assert.Equal(1, s.Executes); Assert.Equal(1, s.Opened);
        Assert.False(vm.ApplyCommand.CanExecute(null)); Assert.NotEmpty(vm.Observed);
    }
    [Fact]
    public async Task SelectionRefreshAndCancellationInvalidateConfirmation()
    {
        var s = new Surface(); var vm = Vm(s); await vm.RefreshAsync();
        vm.SelectedAdapter = vm.Adapters[0]; await vm.ReviewAsync(false); vm.Cancel();
        Assert.False(vm.ApplyCommand.CanExecute(null));
        await vm.ReviewAsync(false); await vm.RefreshAsync(); Assert.Null(vm.SelectedAdapter);
        Assert.False(vm.ApplyCommand.CanExecute(null)); Assert.Equal(0, s.Executes);
    }
    [Fact]
    public async Task BusySurfaceRetainsTargetAndCancellationObservesWithoutBrowser()
    {
        var s = new Surface { Consent = true, Wait = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var vm = Vm(s); await vm.RefreshAsync(); vm.SelectedAdapter = vm.Adapters[0];
        var selected = vm.SelectedAdapter; await vm.ReviewAsync(false); var apply = vm.ApplyAsync();
        Assert.True(vm.IsBusy); Assert.False(vm.CanSelect); vm.SelectedAdapter = null; Assert.Equal(selected, vm.SelectedAdapter);
        vm.Cancel(); s.Wait.SetResult(); await apply;
        Assert.False(vm.IsBusy); Assert.Equal(0, s.Opened); Assert.NotEmpty(vm.Observed);
    }
    [Fact]
    public async Task SavedSnapshotReadingIsReadOnlyAndRecoveryRequiresReviewAndConsent()
    {
        var s = new Surface(); var vm = Vm(s); vm.ReadSnapshotsCommand.Execute(null);
        Assert.Single(vm.Snapshots); Assert.Equal(0, s.Executes);
        vm.SelectedSnapshot = vm.Snapshots[0]; await vm.ReviewAsync(true);
        Assert.Equal(0, s.Executes); await vm.ApplyAsync(); Assert.Equal(0, s.Executes);
        s.Consent = true; await vm.ApplyAsync(); Assert.Equal(1, s.Executes); Assert.Equal(0, s.Opened);
    }
}
