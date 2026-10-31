using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace NetworkProfileManager.Models;

// Separate from legacy AdapterInfo/HistoryEntry: absence must never mean "known empty".
public sealed record ApplyAdapterIdentity([property: JsonRequired] Guid Id, [property: JsonRequired] int InterfaceIndex,
    [property: JsonRequired] string Alias, [property: JsonRequired] string PhysicalAddress);
public sealed record ApplyAddress([property: JsonRequired] string Address, [property: JsonRequired] int PrefixLength,
    [property: JsonRequired] string Origin, [property: JsonRequired] string State);
public sealed record ApplyRoute([property: JsonRequired] string Destination, [property: JsonRequired] string NextHop,
    [property: JsonRequired] int Metric, [property: JsonRequired] string Store, [property: JsonRequired] string Origin);
public sealed record ApplyDadConfiguration([property: JsonRequired] int Transmits, [property: JsonRequired] bool Connected);
public sealed record ApplyAdapterSnapshot(
    [property: JsonRequired] ApplyAdapterIdentity Identity,
    [property: JsonRequired] bool Dhcp,
    [property: JsonRequired] ImmutableArray<ApplyAddress> Addresses,
    [property: JsonRequired] string DnsSource,
    [property: JsonRequired] ImmutableArray<string> DnsServers,
    [property: JsonRequired] ImmutableArray<ApplyRoute> Routes,
    [property: JsonRequired] bool ClassificationComplete,
    [property: JsonRequired] ImmutableArray<ApplyAddress> PersistentAddresses,
    [property: JsonRequired] string DnsRegistration = "primary",
    ApplyDadConfiguration? Dad = null);

public sealed record ApplyIntent(
    [property: JsonRequired] string Mode, [property: JsonRequired] string? Address,
    [property: JsonRequired] int PrefixLength, [property: JsonRequired] string? Gateway,
    [property: JsonRequired] string DnsMode, [property: JsonRequired] ImmutableArray<string> DnsServers,
    [property: JsonRequired] int GatewayMetric = 1, [property: JsonRequired] string? DnsRegistration = null);

// No process arguments, URL, filesystem paths or executable names cross this boundary.
public sealed record ProjectApplyRequest(
    [property: JsonRequired] int Version, [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] DateTimeOffset ExpiresAt, [property: JsonRequired] ApplyAdapterSnapshot Before,
    [property: JsonRequired] ApplyIntent Desired, [property: JsonRequired] bool Recovery = false);

public enum ProjectApplyState
{
    Idle, Preparing, AwaitingConfirmation, Applying, Observing, Verified, Opening,
    Complete, Cancelled, Rejected, ElevationDenied, ApplyFailed, Unverified, BrowserFailed,
    JournalFailed
}

public enum ApplyExecutionStatus { Finished, ElevationDenied, Cancelled, Failed, Unknown }
public sealed record ApplyExecutionResult(ApplyExecutionStatus Status, bool MayHaveChanged, string Stage);
public sealed record ProjectApplyOutcome(
    ProjectApplyState State, bool MayHaveChanged, ApplyAdapterSnapshot? Observed,
    string Stage, bool JournalUpdateFailed = false);

public sealed class ProjectApplyPlan
{
    internal ProjectApplyPlan(ProjectApplyRequest request, string? destination)
        => (Request, Destination) = (request, destination);
    public ProjectApplyRequest Request { get; }
    public string? Destination { get; }
}
