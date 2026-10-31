using System.Collections.Immutable;
using System.IO;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ProjectApplyTests
{
    internal static ApplyAdapterSnapshot Snapshot() => new(
        new(Guid.NewGuid(), 12, "Ethernet fixture", "02-00-00-00-00-01"), false,
        ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")), "static",
        ImmutableArray.Create("192.0.2.53", "192.0.2.54"),
        ImmutableArray.Create(new ApplyRoute("0.0.0.0/0", "192.0.2.1", 1, "active", "netmgmt"),
            new ApplyRoute("0.0.0.0/0", "192.0.2.1", 1, "persistent", "netmgmt")), true,
        ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")));
    internal static ApplyIntent Intent() => new("static", "192.0.2.20", 24, null, "static", ImmutableArray.Create("192.0.2.55", "192.0.2.56"));
    internal static ApplyAdapterSnapshot Applied(ApplyAdapterSnapshot before, ApplyIntent intent)
    {
        var address = new ApplyAddress(intent.Address ?? "198.51.100.10", intent.Mode == "dhcp" ? 24 : intent.PrefixLength,
            intent.Mode == "dhcp" ? "dhcp" : "manual", "preferred");
        return before with
        {
            Dhcp = intent.Mode == "dhcp", Addresses = ImmutableArray.Create(address),
            PersistentAddresses = intent.Mode == "dhcp" ? ImmutableArray<ApplyAddress>.Empty : ImmutableArray.Create(address),
            DnsSource = intent.DnsMode == "leave-unchanged" ? before.DnsSource : intent.DnsMode,
            DnsServers = intent.DnsMode == "leave-unchanged" ? before.DnsServers : intent.DnsMode == "automatic" ? ImmutableArray<string>.Empty : intent.DnsServers,
            DnsRegistration = intent.DnsRegistration ?? before.DnsRegistration,
            Routes = intent.Gateway == null ? ImmutableArray<ApplyRoute>.Empty : ImmutableArray.Create(
                new ApplyRoute("0.0.0.0/0", intent.Gateway, intent.GatewayMetric, "active", "netmgmt"),
                new ApplyRoute("0.0.0.0/0", intent.Gateway, intent.GatewayMetric, "persistent", "netmgmt"))
        };
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Boundary : IProjectApplyBoundary
    {
        public ApplyAdapterSnapshot Current = Snapshot();
        public ApplyExecutionResult Result = new(ApplyExecutionStatus.Finished, true, "CommandsFinished");
        public int Calls, Reads;
        public bool Mutate = true, ThrowExecute, FailReadAfter;
        public Action? AfterExecute;
        public Func<ApplyAdapterSnapshot, ApplyAdapterSnapshot>? Transform;
        public TaskCompletionSource? Wait;
        public readonly Queue<ApplyAdapterSnapshot> Observations = new();
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity identity, CancellationToken cancellation)
        {
            Reads++;
            cancellation.ThrowIfCancellationRequested();
            if (FailReadAfter && Calls > 0) throw new IOException("secret fixture data must not leak");
            if (Calls > 0 && Observations.Count > 0) return Task.FromResult(Observations.Dequeue());
            return Task.FromResult(Current);
        }
        public void ValidateSupported(ApplyAdapterSnapshot snapshot, ApplyIntent desired, bool recovery)
        {
            ProjectApplyPolicy.ValidateSnapshot(snapshot, recovery);
            if (desired.Mode == "static" && desired.DnsMode == "leave-unchanged" && snapshot.DnsSource == "automatic")
                throw new InvalidOperationException();
        }
        public async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken cancellation)
        {
            Calls++;
            if (Wait != null) await Wait.Task;
            if (Mutate) Current = Transform?.Invoke(Applied(Current, request.Desired)) ?? Applied(Current, request.Desired);
            AfterExecute?.Invoke();
            if (ThrowExecute) throw new IOException("secret fixture data");
            return Result;
        }
    }
    private sealed class Journal : IProjectApplyJournal
    {
        public int Begins, Finishes;
        public bool FailBegin, FailFinish;
        public ProjectApplyRequest? Request;
        public Task BeginAsync(ProjectApplyRequest request, CancellationToken cancellation)
        { Begins++; if (FailBegin) throw new IOException(); Request = request; return Task.CompletedTask; }
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome)
        { Finishes++; if (FailFinish) throw new IOException(); return Task.CompletedTask; }
    }
    private sealed class Browser : IBrowserLauncher
    {
        public string? Destination;
        public bool Fail;
        public void Open(string destination) { if (Fail) throw new IOException("secret"); Destination = destination; }
    }
    private static (ProjectApplyCoordinator C, Boundary B, Journal J, Browser Web, Clock Time) Fixture()
    {
        var b = new Boundary(); var j = new Journal(); var web = new Browser(); var time = new Clock();
        return (new(b, j, web, time, verificationAttempts: 1), b, j, web, time);
    }
    private const string Url = "https://192.0.2.44:8443/en_US/panel?view=main#!/status";

    [Fact]
    public async Task ExactDestinationOpensOnlyAfterObservedAddressGatewayAndBothDnsMatch()
    {
        var (c, b, j, web, _) = Fixture();
        var plan = await c.PrepareAsync(b.Current.Identity, Intent(), Url);
        Assert.Equal(ProjectApplyState.AwaitingConfirmation, c.State);
        Assert.Equal(0, b.Calls); Assert.Null(web.Destination);
        var result = await c.ExecuteAsync(plan);
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal(Url, web.Destination);
        Assert.Equal(1, j.Begins); Assert.Equal(1, j.Finishes); Assert.Equal(3, b.Reads);
        Assert.True(result.MayHaveChanged);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.ExecuteAsync(plan));
    }
    [Theory]
    [InlineData("ip")][InlineData("prefix")][InlineData("gateway")][InlineData("dns-order")]
    [InlineData("dns-policy")][InlineData("tentative")][InlineData("extra-ip")][InlineData("persistent")]
    [InlineData("identity")][InlineData("unknown")][InlineData("custom-route")]
    public async Task MismatchedObservedStateNeverOpens(string mismatch)
    {
        var (c, b, _, web, _) = Fixture();
        b.Transform = a => mismatch switch
        {
            "ip" => a with { Addresses = ImmutableArray.Create(a.Addresses[0] with { Address = "192.0.2.21" }) },
            "prefix" => a with { Addresses = ImmutableArray.Create(a.Addresses[0] with { PrefixLength = 25 }) },
            "gateway" => a with { Routes = Snapshot().Routes },
            "dns-order" => a with { DnsServers = a.DnsServers.Reverse().ToImmutableArray() },
            "dns-policy" => a with { DnsSource = "automatic" },
            "tentative" => a with { Addresses = ImmutableArray.Create(a.Addresses[0] with { State = "tentative" }) },
            "extra-ip" => a with { Addresses = a.Addresses.Add(new("192.0.2.22", 24, "manual", "preferred")) },
            "persistent" => a with { PersistentAddresses = ImmutableArray<ApplyAddress>.Empty },
            "identity" => a with { Identity = a.Identity with { Id = Guid.NewGuid() } },
            "unknown" => a with { ClassificationComplete = false },
            _ => a with { Routes = a.Routes.Add(new("198.51.100.0/24", "192.0.2.1", 5, "active", "netmgmt")) }
        };
        var result = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(ProjectApplyState.Unverified, result.State); Assert.Null(web.Destination);
    }
    [Theory]
    [InlineData(ApplyExecutionStatus.ElevationDenied, false, ProjectApplyState.ElevationDenied)]
    [InlineData(ApplyExecutionStatus.Failed, true, ProjectApplyState.ApplyFailed)]
    [InlineData(ApplyExecutionStatus.Unknown, true, ProjectApplyState.ApplyFailed)]
    [InlineData(ApplyExecutionStatus.Cancelled, true, ProjectApplyState.Cancelled)]
    public async Task WorkerFailuresCannotOpenEvenIfActualStateMatches(ApplyExecutionStatus status, bool changed, ProjectApplyState expected)
    {
        var (c, b, _, web, _) = Fixture(); b.Result = new(status, changed, "FixtureStage");
        if (!changed) b.Mutate = false;
        var outcome = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(expected, outcome.State); Assert.Equal(changed, outcome.MayHaveChanged); Assert.Null(web.Destination);
    }
    [Fact]
    public async Task UncertainWorkerExceptionStillObservesPartialState()
    {
        var (c, b, _, web, _) = Fixture(); b.ThrowExecute = true;
        var result = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(ProjectApplyState.ApplyFailed, result.State); Assert.True(result.MayHaveChanged);
        Assert.NotNull(result.Observed); Assert.Null(web.Destination); Assert.DoesNotContain("secret", result.Stage);
    }
    [Fact]
    public async Task FailedObservationIsUnverified()
    {
        var (c, b, _, web, _) = Fixture(); b.FailReadAfter = true;
        var result = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(ProjectApplyState.Unverified, result.State); Assert.Null(result.Observed); Assert.Null(web.Destination);
    }
    [Theory]
    [InlineData("rename")][InlineData("replacement")][InlineData("index")][InlineData("mac")][InlineData("settings")]
    public async Task ChangedReviewedPreStateRejectsBeforeJournalAndMutation(string change)
    {
        var (c, b, j, web, _) = Fixture(); var plan = await c.PrepareAsync(b.Current.Identity, Intent(), Url);
        b.Current = change switch
        {
            "rename" => b.Current with { Identity = b.Current.Identity with { Alias = "Renamed" } },
            "replacement" => b.Current with { Identity = b.Current.Identity with { Id = Guid.NewGuid() } },
            "index" => b.Current with { Identity = b.Current.Identity with { InterfaceIndex = 13 } },
            "mac" => b.Current with { Identity = b.Current.Identity with { PhysicalAddress = "02-00-00-00-00-02" } },
            _ => b.Current with { DnsServers = ImmutableArray.Create("192.0.2.57") }
        };
        var result = await c.ExecuteAsync(plan);
        Assert.Equal(ProjectApplyState.Rejected, result.State); Assert.False(result.MayHaveChanged);
        Assert.Equal(0, b.Calls); Assert.Equal(0, j.Begins); Assert.Null(web.Destination);
    }
    [Fact]
    public async Task JournalBeginFailureAbortsButFinalFailureRetainsNetworkSuccess()
    {
        var (c, b, j, web, _) = Fixture(); j.FailBegin = true;
        var first = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(ProjectApplyState.JournalFailed, first.State); Assert.Equal(0, b.Calls); Assert.Null(web.Destination);
        j.FailBegin = false; j.FailFinish = true;
        var second = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(ProjectApplyState.Complete, second.State); Assert.True(second.JournalUpdateFailed); Assert.Equal(Url, web.Destination);
    }
    [Fact]
    public async Task BrowserFailureDoesNotReapplyOrRecover()
    {
        var (c, b, _, web, _) = Fixture(); web.Fail = true;
        var result = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(ProjectApplyState.BrowserFailed, result.State); Assert.Equal(1, b.Calls); Assert.True(result.MayHaveChanged);
    }
    [Fact]
    public async Task ExpiredDiscardedOrForeignPlansCannotApply()
    {
        var (c, b, j, _, time) = Fixture(); var plan = await c.PrepareAsync(b.Current.Identity, Intent(), Url);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.PrepareAsync(b.Current.Identity, Intent(), Url));
        time.Now = time.Now.AddMinutes(3);
        Assert.Equal(ProjectApplyState.Rejected, (await c.ExecuteAsync(plan)).State);
        plan = await c.PrepareAsync(b.Current.Identity, Intent(), Url); c.Discard(plan);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.ExecuteAsync(plan));
        var foreign = Fixture(); var other = await foreign.C.PrepareAsync(foreign.B.Current.Identity, Intent(), Url);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.ExecuteAsync(other));
        Assert.Equal(0, b.Calls); Assert.Equal(0, j.Begins);
    }
    [Fact]
    public async Task CancellationBeforeAndAfterMutationKeepsAttributionAndNeverOpens()
    {
        var (c, b, j, web, _) = Fixture(); using var cancellation = new CancellationTokenSource();
        var plan = await c.PrepareAsync(b.Current.Identity, Intent(), Url); cancellation.Cancel();
        var first = await c.ExecuteAsync(plan, cancellation.Token);
        Assert.Equal(ProjectApplyState.Cancelled, first.State); Assert.False(first.MayHaveChanged); Assert.Equal(0, j.Begins);
        using var after = new CancellationTokenSource(); b.AfterExecute = after.Cancel;
        var second = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url), after.Token);
        Assert.Equal(ProjectApplyState.Cancelled, second.State); Assert.True(second.MayHaveChanged);
        Assert.NotNull(second.Observed); Assert.Null(web.Destination);
    }
    [Fact]
    public async Task ConcurrentExecuteAndPrepareAreRejectedWithoutLosingActiveOperation()
    {
        var (c, b, _, _, _) = Fixture(); b.Wait = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var plan = await c.PrepareAsync(b.Current.Identity, Intent(), Url);
        var active = c.ExecuteAsync(plan);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.ExecuteAsync(plan));
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Throws<InvalidOperationException>(() => c.Discard(plan));
        b.Wait.SetResult(); Assert.Equal(ProjectApplyState.Complete, (await active).State); Assert.Equal(1, b.Calls);
    }
    [Fact]
    public async Task RecoveryRequiresFreshReviewAndDoesNotOpenBrowser()
    {
        var (c, b, _, web, _) = Fixture(); var original = b.Current;
        b.Current = Applied(original, Intent());
        var recovery = await c.PrepareRecoveryAsync(original);
        Assert.Equal(0, b.Calls); Assert.True(recovery.Request.Recovery); Assert.Null(recovery.Destination);
        var result = await c.ExecuteAsync(recovery);
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal("RecoveryVerified", result.Stage);
        Assert.Equal(original.Addresses, result.Observed!.Addresses); Assert.Null(web.Destination);
    }
    [Fact]
    public async Task DhcpRequiresPreferredDhcpAddressAndAutomaticPolicyNotReachabilityOrOldLease()
    {
        var (c, b, _, web, _) = Fixture();
        var intent = new ApplyIntent("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty);
        var result = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, intent, Url));
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal("198.51.100.10", result.Observed!.Addresses[0].Address);
        Assert.Equal(Url, web.Destination);
    }
    [Theory]
    [InlineData("extra-address")][InlineData("extra-gateway")][InlineData("custom-route")][InlineData("policy")]
    [InlineData("dns-source")][InlineData("unknown-persistent")][InlineData("static-auto")][InlineData("prefix31")]
    public async Task UnsupportedStatesBlockBeforeApply(string kind)
    {
        var (c, b, _, _, _) = Fixture();
        b.Current = kind switch
        {
            "extra-address" => b.Current with { Addresses = b.Current.Addresses.Add(new("192.0.2.11", 24, "manual", "preferred")) },
            "extra-gateway" => b.Current with { Routes = b.Current.Routes.Add(new("0.0.0.0/0", "192.0.2.2", 2, "active", "netmgmt")) },
            "custom-route" => b.Current with { Routes = b.Current.Routes.Add(new("198.51.100.0/24", "192.0.2.1", 2, "persistent", "netmgmt")) },
            "policy" => b.Current with { ClassificationComplete = false },
            "dns-source" => b.Current with { DnsSource = "unknown" },
            "unknown-persistent" => b.Current with { PersistentAddresses = ImmutableArray<ApplyAddress>.Empty },
            "static-auto" => b.Current with { DnsSource = "automatic" },
            _ => b.Current with { Addresses = ImmutableArray.Create(b.Current.Addresses[0] with { PrefixLength = 31 }) }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(0, b.Calls);
    }
    [Fact]
    public async Task UnsafeUrlCannotReachEnumerationOrMutation()
    {
        var (c, b, _, _, _) = Fixture();
        await Assert.ThrowsAsync<ProjectValidationException>(() => c.PrepareAsync(b.Current.Identity, Intent(), "file:///fixture"));
        Assert.Equal(0, b.Reads); Assert.Equal(0, b.Calls);
    }

    [Fact]
    public async Task VerificationWaitsForPreferredStateWithinBoundWithoutReapplying()
    {
        var b = new Boundary(); var j = new Journal(); var web = new Browser();
        var c = new ProjectApplyCoordinator(b, j, web, verificationAttempts: 2);
        var applied = Applied(b.Current, Intent());
        b.Observations.Enqueue(applied with { Addresses = ImmutableArray.Create(applied.Addresses[0] with { State = "tentative" }) });
        b.Observations.Enqueue(applied);
        var result = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, Intent(), Url));
        Assert.Equal(ProjectApplyState.Complete, result.State); Assert.Equal(1, b.Calls); Assert.Equal(4, b.Reads); Assert.Equal(Url, web.Destination);
    }
    [Theory]
    [InlineData("no-lease")][InlineData("apipa")][InlineData("tentative")][InlineData("manual")][InlineData("static-dns")]
    public async Task DhcpCannotOpenWithoutVerifiedUsableLeaseAndDnsPolicy(string kind)
    {
        var (c, b, _, web, _) = Fixture();
        b.Transform = a => kind switch
        {
            "no-lease" => a with { Addresses = ImmutableArray<ApplyAddress>.Empty },
            "apipa" => a with { Addresses = ImmutableArray.Create(a.Addresses[0] with { Address = "169.254.1.2" }) },
            "tentative" => a with { Addresses = ImmutableArray.Create(a.Addresses[0] with { State = "tentative" }) },
            "manual" => a with { Addresses = ImmutableArray.Create(a.Addresses[0] with { Origin = "manual" }) },
            _ => a with { DnsSource = "static", DnsServers = ImmutableArray.Create("192.0.2.53") }
        };
        var intent = new ApplyIntent("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty);
        var result = await c.ExecuteAsync(await c.PrepareAsync(b.Current.Identity, intent, Url));
        Assert.Equal(ProjectApplyState.Unverified, result.State); Assert.Null(web.Destination);
    }
    [Fact]
    public async Task FailedRecoveryRemainsFailedAndNeverOpens()
    {
        var (c, b, _, web, _) = Fixture(); var original = b.Current; b.Current = Applied(original, Intent());
        b.Result = new(ApplyExecutionStatus.Failed, true, "DnsSecondary");
        var result = await c.ExecuteAsync(await c.PrepareRecoveryAsync(original));
        Assert.Equal(ProjectApplyState.ApplyFailed, result.State); Assert.True(result.MayHaveChanged); Assert.Null(web.Destination);
    }
    [Fact]
    public async Task SeparateCoordinatorsCannotOverlapMutation()
    {
        var first = Fixture(); var second = Fixture();
        var p1 = await first.C.PrepareAsync(first.B.Current.Identity, Intent(), Url);
        var p2 = await second.C.PrepareAsync(second.B.Current.Identity, Intent(), Url);
        first.B.Wait = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = first.C.ExecuteAsync(p1);
        var blocked = await second.C.ExecuteAsync(p2);
        Assert.Equal(ProjectApplyState.Rejected, blocked.State); Assert.Equal("OperationActive", blocked.Stage); Assert.Equal(0, second.B.Calls);
        first.B.Wait.SetResult(); await active;
    }
}
