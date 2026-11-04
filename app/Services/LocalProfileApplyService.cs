using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Services;

internal sealed record LocalApplyResult(ProjectApplyRequest Request, ProjectApplyOutcome? Outcome);
internal sealed class LocalApplyBlockedException(string code) : Exception
{ internal string Code { get; } = code; }

// One mutation contract for local/manual/hotkey/tray and project actions. No alias command runner.
internal sealed class LocalProfileApplyService(IProjectApplyBoundary boundary, IProjectApplyJournal journal,
    Func<CancellationToken, Task<IReadOnlyList<ApplyAdapterIdentity>>> enumerate,
    Func<Guid, ProjectApplyRequest> readJournal, int verificationAttempts = 10, TimeProvider? timeProvider = null)
{
    internal static LocalProfileApplyService Create()
    {
        var reader = new WindowsProjectApplyObservation(); var journal = new ProjectApplyJournal();
        return new(new WindowsProjectApplyBoundary(), journal,
            async token => await reader.EnumerateAsync(token), journal.ReadSnapshot);
    }
    internal static ApplyIntent Intent(IpProfile profile)
    {
        if (profile.IsDhcp) return new("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty);
        if (IpFormat.PrefixLength(profile.SubnetMask) is not int prefix || prefix is < 1 or > 30)
            throw new LocalApplyBlockedException("LocalApply.PrefixUnsupported");
        var dns = new[] { profile.PrimaryDns, profile.SecondaryDns }.Where(s => !string.IsNullOrWhiteSpace(s)).ToImmutableArray();
        if (string.IsNullOrWhiteSpace(profile.PrimaryDns) && dns.Length != 0)
            throw new LocalApplyBlockedException("LocalApply.PrimaryDnsRequired");
        var intent = new ApplyIntent("static", profile.IpAddress, prefix,
            string.IsNullOrWhiteSpace(profile.Gateway) ? null : profile.Gateway,
            dns.Length == 0 ? "leave-unchanged" : "static", dns);
        ProjectApplyPolicy.ValidateIntent(intent);
        return intent;
    }
    internal async Task<LocalApplyResult> RunAsync(string adapterId, IpProfile profile, HistoryEntry? recovery,
        Func<string, Task<bool>> confirm, CancellationToken cancellation,
        Func<ApplyConfirmation, Task<bool>>? confirmSummary = null)
    {
        if (!Guid.TryParse(adapterId, out var id) || id == Guid.Empty) throw new LocalApplyBlockedException("LocalApply.AdapterChanged");
        var coordinator = new ProjectApplyCoordinator(new ExplainedBoundary(boundary), journal, new NoBrowser(), timeProvider, verificationAttempts);
        ProjectApplyPlan? plan = null;
        try
        {
            bool oldHistory = recovery != null && recovery.RecoveryOperationId == null;
            bool refreshed = false;
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                if (recovery?.RecoveryOperationId is Guid operation)
                {
                    var saved = readJournal(operation);
                    if (saved.Before.Identity.Id != id) throw new LocalApplyBlockedException("LocalApply.AdapterChanged");
                    plan = await coordinator.PrepareRecoveryAsync(saved.Before, cancellation);
                }
                else
                {
                    if (recovery != null && !string.IsNullOrWhiteSpace(recovery.AdapterId)
                        && (!Guid.TryParse(recovery.AdapterId, out var historyId) || historyId != id))
                        throw new LocalApplyBlockedException("LocalApply.AdapterChanged");
                    // Resolve the deliberately selected GUID, never fall back to another NIC/name.
                    var identity = (await enumerate(cancellation)).Single(a => a.Id == id);
                    var intent = recovery == null ? Intent(profile) : Intent(new IpProfile
                    { IsDhcp = recovery.WasDhcp, IpAddress = recovery.OldIp, SubnetMask = recovery.OldSubnet, Gateway = recovery.OldGateway });
                    plan = await coordinator.PrepareProfileAsync(identity, intent, cancellation);
                }
                var review = new ApplyConfirmation(plan.Request, oldHistory, refreshed);
                cancellation.ThrowIfCancellationRequested();
                bool accepted = confirmSummary != null ? await confirmSummary(review)
                    : await confirm((refreshed ? Loc.Get("Apply.RefreshedReview") + "\n\n" : "")
                        + (oldHistory ? Loc.Get("LocalApply.OldHistory") + "\n\n" : "") + review.Details);
                if (!accepted) return new(plan.Request, null);
                var outcome = await coordinator.ExecuteAsync(plan, cancellation);
                if (outcome.State != ProjectApplyState.Rejected || outcome.MayHaveChanged || outcome.Stage != "Expired")
                    return new(plan.Request, outcome);
                // The expired plan is consumed without journaling or execution. Read
                // fresh state and require a NEW explicit consent; never reuse the old one.
                refreshed = true;
            }
        }
        finally { if (plan != null && coordinator.State == ProjectApplyState.AwaitingConfirmation) coordinator.Discard(plan); }
    }
    private sealed class NoBrowser : IBrowserLauncher
    { public void Open(string destination) => throw new InvalidOperationException("Local network actions cannot open a browser."); }
    private sealed class ExplainedBoundary(IProjectApplyBoundary inner) : IProjectApplyBoundary
    {
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity id, CancellationToken cancellation) => inner.ReadAsync(id, cancellation);
        public Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken cancellation) => inner.ExecuteAsync(request, cancellation);
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent desired, bool recovery)
        {
            try { inner.ValidateSupported(s, desired, recovery); }
            catch (InvalidOperationException)
            {
                string code = !s.ClassificationComplete ? "LocalApply.UnknownState"
                    : s.Addresses.Length != 1 || s.PersistentAddresses.Length > 1 ? "LocalApply.AddressState"
                    : s.Addresses.Any(a => !ProjectApplyPolicy.Usable(a.Address)) ? "LocalApply.AddressState"
                    : !s.Dhcp && s.DnsSource == "automatic" || desired.Mode == "static" && desired.DnsMode == "leave-unchanged" && s.DnsSource == "automatic"
                        ? "LocalApply.AutomaticDns"
                    : "LocalApply.ComplexState";
                throw new LocalApplyBlockedException(code);
            }
        }
    }
}
