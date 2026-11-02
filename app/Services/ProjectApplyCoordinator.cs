using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

// Implementations must independently validate support, identity and the reviewed
// pre-state at the elevated boundary. The coordinator is not an authorization boundary.
public interface IProjectApplyBoundary
{
    Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity identity, CancellationToken cancellation);
    void ValidateSupported(ApplyAdapterSnapshot snapshot, ApplyIntent desired, bool recovery);
    Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken cancellation);
}

public interface IProjectApplyJournal
{
    // Must durably retain the recovery snapshot before any mutation is requested.
    Task BeginAsync(ProjectApplyRequest request, CancellationToken cancellation);
    Task FinishAsync(Guid operationId, ProjectApplyOutcome outcome);
}

/// <summary>
/// One confirmed plan, one attempt. No OS implementation is selected implicitly.
/// All dependencies are explicit so unit tests cannot reach a real NIC or browser.
/// </summary>
public sealed class ProjectApplyCoordinator(
    IProjectApplyBoundary boundary, IProjectApplyJournal journal, IBrowserLauncher browser,
    TimeProvider? timeProvider = null, int verificationAttempts = 10)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private int _busy;
    private ProjectApplyPlan? _pending;
    public ProjectApplyState State { get; private set; } = ProjectApplyState.Idle;

    public async Task<ProjectApplyPlan> PrepareAsync(ApplyAdapterIdentity identity, ApplyIntent desired,
        string destination, CancellationToken cancellation = default)
        => await PrepareCoreAsync(identity, desired, destination ?? throw new ArgumentNullException(nameof(destination)), false, cancellation);

    public async Task<ProjectApplyPlan> PrepareRecoveryAsync(ApplyAdapterSnapshot original, CancellationToken cancellation = default)
        => await PrepareCoreAsync(original.Identity, ProjectApplyPolicy.RecoveryIntent(original), null, true, cancellation);

    public Task<ProjectApplyPlan> PrepareProfileAsync(ApplyAdapterIdentity identity, ApplyIntent desired,
        CancellationToken cancellation = default)
        => PrepareCoreAsync(identity, desired, null, false, cancellation);

    public async Task<ProjectApplyPlan> PrepareManualAsync(ApplyAdapterIdentity identity, ApplyIntent desired,
        CancellationToken cancellation = default)
    {
        if (desired.Mode is not ("link-local" or "dhcp")) throw new InvalidOperationException("Invalid manual action.");
        return await PrepareCoreAsync(identity, desired, null, false, cancellation);
    }

    private async Task<ProjectApplyPlan> PrepareCoreAsync(ApplyAdapterIdentity identity, ApplyIntent desired,
        string? destination, bool recovery, CancellationToken cancellation)
    {
        Enter();
        try
        {
            if (_pending != null && _pending.Request.ExpiresAt > _time.GetUtcNow())
                throw new InvalidOperationException("Confirmation pending.");
            _pending = null;
            State = ProjectApplyState.Preparing;
            if (!recovery && destination != null) ProjectJson.ValidateUrl(destination);
            ProjectApplyPolicy.ValidateIntent(desired, recovery);
            cancellation.ThrowIfCancellationRequested();
            var before = await boundary.ReadAsync(identity, cancellation);
            if (before.Identity != identity) throw new InvalidOperationException("Adapter changed.");
            desired = ProjectApplyPolicy.ResolveIntent(before, desired);
            boundary.ValidateSupported(before, desired, recovery);
            cancellation.ThrowIfCancellationRequested();
            var request = new ProjectApplyRequest(1, Guid.NewGuid(), _time.GetUtcNow().AddMinutes(2), before, desired, recovery);
            _pending = new ProjectApplyPlan(request, destination);
            State = ProjectApplyState.AwaitingConfirmation;
            return _pending;
        }
        catch (OperationCanceledException) { State = ProjectApplyState.Cancelled; throw; }
        catch { if (_pending == null) State = ProjectApplyState.Rejected; throw; }
        finally { Exit(); }
    }

    public void Discard(ProjectApplyPlan plan)
    {
        Enter();
        try
        {
            if (!ReferenceEquals(_pending, plan)) throw new InvalidOperationException("Stale plan.");
            _pending = null;
            State = ProjectApplyState.Cancelled;
        }
        finally { Exit(); }
    }

    public async Task<ProjectApplyOutcome> ExecuteAsync(ProjectApplyPlan plan, CancellationToken cancellation = default)
    {
        Enter();
        try
        {
            if (!ReferenceEquals(_pending, plan)) throw new InvalidOperationException("Stale plan.");
            _pending = null; // Consume before the first await; failures cannot replay a confirmation.
            var request = plan.Request;
            using var mutationLease = NetworkMutationLease.TryAcquire();
            if (mutationLease == null) return End(ProjectApplyState.Rejected, false, null, "OperationActive");
            if (request.ExpiresAt <= _time.GetUtcNow()) return End(ProjectApplyState.Rejected, false, null, "Expired");
            if (cancellation.IsCancellationRequested) return End(ProjectApplyState.Cancelled, false, null, "BeforeApply");

            ApplyAdapterSnapshot before;
            try
            {
                before = await boundary.ReadAsync(request.Before.Identity, cancellation);
                if (!SameConfiguration(request.Before, before)) return End(ProjectApplyState.Rejected, false, before, "PreStateChanged");
                boundary.ValidateSupported(before, request.Desired, request.Recovery);
            }
            catch (OperationCanceledException) { return End(ProjectApplyState.Cancelled, false, null, "BeforeApply"); }
            catch { return End(ProjectApplyState.Rejected, false, null, "Preflight"); }

            try { await journal.BeginAsync(request, cancellation); }
            catch (OperationCanceledException) { return End(ProjectApplyState.Cancelled, false, before, "BeforeApply"); }
            catch { return End(ProjectApplyState.JournalFailed, false, before, "SnapshotWrite"); }

            if (cancellation.IsCancellationRequested)
                return await Finish(request, End(ProjectApplyState.Cancelled, false, before, "BeforeApply"));
            State = ProjectApplyState.Applying;
            ApplyExecutionResult execution;
            try { execution = await boundary.ExecuteAsync(request, cancellation); }
            // An exception after entering the privileged boundary cannot establish no change.
            catch { execution = new(ApplyExecutionStatus.Unknown, true, "WorkerUnknown"); }

            State = ProjectApplyState.Observing;
            ApplyAdapterSnapshot? observed = null;
            // Deliberately independent of user cancellation: partial changes still need observation.
            using (var observationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                for (var attempt = 0; attempt < Math.Max(1, verificationAttempts); attempt++)
                {
                    try
                    {
                        observed = await boundary.ReadAsync(request.Before.Identity, observationTimeout.Token);
                        if (execution.Status != ApplyExecutionStatus.Finished || ProjectApplyVerification.Matches(request, observed)
                            || plan.Destination == null && ProjectApplyVerification.MatchesDhcpPolicyWithoutLease(request, observed)) break;
                        if (attempt + 1 < verificationAttempts)
                            await Task.Delay(TimeSpan.FromMilliseconds(500), _time, observationTimeout.Token);
                    }
                    catch { break; } // Unknown state is never success; never expose raw errors.
                }
            }

            ProjectApplyOutcome outcome;
            if (execution.Status == ApplyExecutionStatus.ElevationDenied && !execution.MayHaveChanged)
                outcome = End(ProjectApplyState.ElevationDenied, false, observed, execution.Stage);
            else if (execution.Status == ApplyExecutionStatus.Cancelled || cancellation.IsCancellationRequested)
                outcome = End(ProjectApplyState.Cancelled, execution.MayHaveChanged, observed, execution.Stage);
            else if (execution.Status != ApplyExecutionStatus.Finished)
                outcome = End(ProjectApplyState.ApplyFailed, execution.MayHaveChanged, observed, execution.Stage);
            else if (observed != null && plan.Destination == null && ProjectApplyVerification.MatchesDhcpPolicyWithoutLease(request, observed))
                outcome = End(ProjectApplyState.Complete, execution.MayHaveChanged, observed, "DhcpPolicyVerified");
            else if (observed == null || !ProjectApplyVerification.Matches(request, observed))
                outcome = End(ProjectApplyState.Unverified, execution.MayHaveChanged, observed,
                    observed?.Addresses.Any(a => a.Address == request.Desired.Address && a.State == "duplicate") == true
                        ? "AddressConflict" : "Verification");
            else
            {
                State = ProjectApplyState.Verified;
                // Cancellation must not hand off a URL even if the just-completed apply verified.
                if (cancellation.IsCancellationRequested)
                    outcome = End(ProjectApplyState.Cancelled, execution.MayHaveChanged, observed, "BeforeBrowser");
                else if (request.Recovery || plan.Destination == null)
                    outcome = End(ProjectApplyState.Complete, execution.MayHaveChanged, observed,
                        request.Recovery ? "RecoveryVerified" : "NetworkVerified");
                else
                {
                    State = ProjectApplyState.Opening;
                    try
                    {
                        browser.Open(plan.Destination!);
                        outcome = End(ProjectApplyState.Complete, execution.MayHaveChanged, observed, "BrowserHandoff");
                    }
                    catch { outcome = End(ProjectApplyState.BrowserFailed, execution.MayHaveChanged, observed, "BrowserHandoff"); }
                }
            }
            return await Finish(request, outcome);
        }
        finally { Exit(); }
    }

    private ProjectApplyOutcome End(ProjectApplyState state, bool changed, ApplyAdapterSnapshot? observed, string stage)
    {
        State = state;
        return new(state, changed, observed, stage);
    }
    private async Task<ProjectApplyOutcome> Finish(ProjectApplyRequest request, ProjectApplyOutcome outcome)
    {
        try { await journal.FinishAsync(request.OperationId, outcome); return outcome; }
        catch { return outcome with { JournalUpdateFailed = true }; }
    }
    private void Enter()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) throw new InvalidOperationException("Operation active.");
    }
    private void Exit() => Volatile.Write(ref _busy, 0);

    internal static bool SameConfiguration(ApplyAdapterSnapshot a, ApplyAdapterSnapshot b) =>
        a.Identity == b.Identity && a.ClassificationComplete == b.ClassificationComplete && a.Dhcp == b.Dhcp
        && a.DnsSource == b.DnsSource && a.Addresses.SequenceEqual(b.Addresses)
        && a.DnsServers.SequenceEqual(b.DnsServers) && a.Routes.SequenceEqual(b.Routes)
        && a.DnsRegistration == b.DnsRegistration
        && a.Dad == b.Dad
        && a.PersistentAddresses.SequenceEqual(b.PersistentAddresses);
}

internal static class ProjectApplyVerification
{
    // Only the coordinator's browser-free branch may use this narrower result.
    internal static bool MatchesDhcpPolicyWithoutLease(ProjectApplyRequest request, ApplyAdapterSnapshot actual)
    {
        var d = request.Desired;
        if (d.Mode != "dhcp" || !ProjectApplyPolicy.IsDhcpWithoutLease(actual) || !actual.ClassificationComplete
            || actual.Identity != request.Before.Identity || actual.DnsSource != d.DnsMode
            || actual.DnsRegistration != (d.DnsRegistration ?? request.Before.DnsRegistration)) return false;
        try { ProjectApplyPolicy.ValidateSnapshot(actual, allowDhcpWithoutLease: true); }
        catch { return false; }
        return d.DnsMode == "automatic" || d.DnsMode == "static" && actual.DnsServers.SequenceEqual(d.DnsServers);
    }

    internal static bool Matches(ProjectApplyRequest request, ApplyAdapterSnapshot actual)
    {
        var desired = request.Desired;
        if (!actual.ClassificationComplete || actual.Identity != request.Before.Identity
            || actual.DnsRegistration != (desired.DnsRegistration ?? request.Before.DnsRegistration)) return false;
        try
        {
            ProjectApplyPolicy.ValidateSnapshot(actual, allowLinkLocal: desired.Mode == "link-local");
            if (desired.Mode == "link-local") LinkLocalAddressing.RequireDad(actual);
        }
        catch { return false; }
        if (desired.Mode == "dhcp")
            return actual.Dhcp && actual.DnsSource == desired.DnsMode && actual.Addresses.Length == 1
                && ProjectApplyPolicy.Usable(actual.Addresses[0].Address)
                && actual.Addresses[0].Origin == "dhcp" && actual.Addresses[0].State == "preferred"
                && (desired.DnsMode != "static" || actual.DnsServers.SequenceEqual(desired.DnsServers));
        if (desired.Mode is not ("static" or "link-local") || actual.Dhcp || actual.Addresses.Length != 1) return false;
        var address = actual.Addresses[0];
        if (address.Address != desired.Address || address.PrefixLength != desired.PrefixLength
            || address.Origin != "manual" || address.State != "preferred") return false;
        var gateways = actual.Routes.Where(r => r.Destination == "0.0.0.0/0" && r.Store == "active").ToArray();
        if (desired.Gateway == null ? gateways.Length != 0 : gateways.Length != 1 || gateways[0].NextHop != desired.Gateway || gateways[0].Metric != desired.GatewayMetric)
            return false;
        return desired.DnsMode switch
        {
            "static" => actual.DnsSource == "static" && actual.DnsServers.SequenceEqual(desired.DnsServers),
            "automatic" => actual.DnsSource == "automatic",
            "none" => actual.DnsSource == "none" && actual.DnsServers.Length == 0,
            "leave-unchanged" => actual.DnsSource == request.Before.DnsSource
                && actual.DnsServers.SequenceEqual(request.Before.DnsServers),
            _ => false
        };
    }
}
