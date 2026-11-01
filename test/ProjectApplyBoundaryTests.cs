using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using System.IO.Pipes;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ProjectApplyBoundaryTests
{
    private static ProjectApplyRequest Request() => new(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(2),
        ProjectApplyTests.Snapshot(), ProjectApplyTests.Intent());

    [Fact]
    public async Task WorkerDerivesOnlyFixedIpv4CommandsAndRechecksIdentityForEveryStage()
    {
        var request = Request(); var commands = new List<IReadOnlyList<string>>(); var reads = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(request, (_, _) =>
        { reads++; return Task.FromResult(request.Before); }, (args, _) => { commands.Add(args); return Task.CompletedTask; }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Finished, result.Status); Assert.True(result.MayHaveChanged);
        Assert.Equal(3, commands.Count); Assert.Equal(4, reads);
        Assert.All(commands, c => { Assert.Equal("interface", c[0]); Assert.Equal("ipv4", c[1]); Assert.Contains("name=12", c); });
        Assert.Contains("gateway=none", commands[0]); Assert.Contains("mask=255.255.255.0", commands[0]);
        Assert.Contains("store=persistent", commands[0]); Assert.Contains("validate=no", commands[1]);
        Assert.Contains("address=192.0.2.56", commands[2]); Assert.Contains("index=2", commands[2]);
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)]
    public async Task PartialCommandFailureStopsRemainingCommandsAndRetainsMutationUncertainty(int failure)
    {
        var request = Request(); var calls = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(request, (_, _) => Task.FromResult(request.Before), (args, _) =>
        { if (calls++ == failure) throw new IOException("private output"); return Task.CompletedTask; }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.True(result.MayHaveChanged);
        Assert.Equal(failure + 1, calls); Assert.DoesNotContain("private", result.Stage);
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)]
    public async Task CancellationBetweenStagesStopsBeforeNextCommand(int stop)
    {
        var request = Request(); var calls = 0; var confirmations = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(request, (_, _) => Task.FromResult(request.Before), (_, _) =>
        { calls++; return Task.CompletedTask; }, _ => Task.FromResult(confirmations++ < stop));
        Assert.Equal(ApplyExecutionStatus.Cancelled, result.Status); Assert.Equal(stop > 0, result.MayHaveChanged); Assert.Equal(stop, calls);
    }
    [Theory]
    [InlineData("guid")][InlineData("alias")][InlineData("index")][InlineData("mac")][InlineData("removed")]
    public async Task IdentityFailureAfterConfirmationCannotRetargetAnotherNic(string kind)
    {
        var request = Request(); var calls = 0; var reads = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(request, (_, _) =>
        {
            if (reads++ == 0) return Task.FromResult(request.Before);
            if (kind == "removed") throw new IOException();
            var id = kind switch
            {
                "guid" => request.Before.Identity with { Id = Guid.NewGuid() },
                "alias" => request.Before.Identity with { Alias = "Different alias" },
                "index" => request.Before.Identity with { InterfaceIndex = 13 },
                _ => request.Before.Identity with { PhysicalAddress = "02-00-00-00-00-02" }
            };
            return Task.FromResult(request.Before with { Identity = id });
        }, (_, _) => { calls++; return Task.CompletedTask; }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.False(result.MayHaveChanged); Assert.Equal(0, calls);
    }
    [Theory]
    [InlineData("version")][InlineData("expired")][InlineData("future")][InlineData("id")][InlineData("empty-guid")]
    [InlineData("injection")][InlineData("unknown-dns")][InlineData("prefix32")][InlineData("static-auto")][InlineData("missing-snapshot")]
    public async Task InvalidTypedRequestIsRejectedBeforeObservationOrExecution(string kind)
    {
        var r = Request();
        r = kind switch
        {
            "version" => r with { Version = 2 }, "expired" => r with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) },
            "future" => r with { ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) }, "id" => r with { OperationId = Guid.Empty },
            "empty-guid" => r with { Before = r.Before with { Identity = r.Before.Identity with { Id = Guid.Empty } } },
            "injection" => r with { Desired = r.Desired with { Address = "192.0.2.20&calc" } },
            "unknown-dns" => r with { Desired = r.Desired with { DnsMode = "unknown" } },
            "prefix32" => r with { Desired = r.Desired with { PrefixLength = 32 } },
            "static-auto" => r with { Desired = r.Desired with { DnsMode = "automatic", DnsServers = ImmutableArray<string>.Empty } },
            _ => r with { Before = null! }
        };
        var reads = 0; var calls = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) => { reads++; return Task.FromResult(Request().Before); },
            (_, _) => { calls++; return Task.CompletedTask; }, _ => Task.FromResult(true));
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.Equal(0, reads); Assert.Equal(0, calls); Assert.False(result.MayHaveChanged);
    }
    [Fact]
    public async Task WorkerRejectsPreStateChangeAndExpiryAfterUacWithoutMutation()
    {
        var r = Request(); var calls = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) => Task.FromResult(r.Before with { DnsServers = ImmutableArray.Create("192.0.2.57") }),
            (_, _) => { calls++; return Task.CompletedTask; }, _ => Task.FromResult(true));
        Assert.Equal("PreStateChanged", result.Stage); Assert.Equal(0, calls);
        var now = DateTimeOffset.UtcNow;
        result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) => Task.FromResult(r.Before),
            (_, _) => { calls++; return Task.CompletedTask; }, _ => { now = now.AddMinutes(3); return Task.FromResult(true); }, () => now);
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.Equal(0, calls); Assert.False(result.MayHaveChanged);
    }
    [Fact]
    public void WireRejectsDuplicateUnknownAndMissingPropertiesAndCannotCarryCommandsOrUrls()
    {
        var json = ProjectApplyWire.Serialize(Request());
        var roundTrip = ProjectApplyWire.Parse<ProjectApplyRequest>(json);
        ProjectApplyWire.ValidateRequest(roundTrip, DateTimeOffset.UtcNow);
        foreach (var fragment in new[] { "\"Version\":1,", "\"Executable\":\"calc\",", "\"Url\":\"file:///fixture\",", "\"Commands\":[]," })
            Assert.ThrowsAny<Exception>(() => ProjectApplyWire.Parse<ProjectApplyRequest>("{" + fragment + json.Substring(1)));
        Assert.ThrowsAny<Exception>(() => ProjectApplyWire.ValidateRequest(ProjectApplyWire.Parse<ProjectApplyRequest>("{}"), DateTimeOffset.UtcNow));
        Assert.ThrowsAny<Exception>(() => ProjectApplyWire.Parse<ProjectApplyRequest>(new string(' ', ProjectApplyWire.MaxBytes + 1)));
        Assert.DoesNotContain("https", json); Assert.DoesNotContain("Executable", json);
    }
    [Fact]
    public async Task FramingRejectsOversizeTruncatedAndInvalidUtf8WithoutFilesOrProcesses()
    {
        using var stream = new MemoryStream();
        await ProjectApplyWire.WriteAsync(stream, Request(), CancellationToken.None); stream.Position = 0;
        var request = await ProjectApplyWire.ReadAsync<ProjectApplyRequest>(stream, CancellationToken.None);
        Assert.Equal(1, request.Version);
        foreach (var bytes in new[] { BitConverter.GetBytes(ProjectApplyWire.MaxBytes + 1), new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0, 255 } })
        {
            using var invalid = new MemoryStream(bytes);
            await Assert.ThrowsAnyAsync<Exception>(() => ProjectApplyWire.ReadAsync<ProjectApplyRequest>(invalid, CancellationToken.None));
        }
    }
    [Fact]
    public void DnsUnchangedEmitsNoDnsCommandAndDhcpExplicitlyResetsDnsWithoutProbes()
    {
        var r = Request();
        var unchanged = r with { Desired = r.Desired with { DnsMode = "leave-unchanged", DnsServers = ImmutableArray<string>.Empty } };
        Assert.Single(ProjectApplyPolicy.Commands(unchanged));
        var dhcp = r with { Desired = new("dhcp", null, 0, null, "automatic", ImmutableArray<string>.Empty) };
        var commands = ProjectApplyPolicy.Commands(dhcp); Assert.Equal(2, commands.Count);
        Assert.All(commands, c => Assert.Contains("source=dhcp", c)); Assert.Contains("validate=no", commands[1]);
    }
    [Fact]
    public void DhcpRecoveryRestoresPolicyWithoutPinningHistoricalLeaseOrServers()
    {
        var s = ProjectApplyTests.Snapshot() with
        {
            Dhcp = true, Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "dhcp", "preferred")),
            PersistentAddresses = ImmutableArray<ApplyAddress>.Empty, Routes = ImmutableArray<ApplyRoute>.Empty, DnsSource = "automatic"
        };
        var recovery = ProjectApplyPolicy.RecoveryIntent(s); ProjectApplyPolicy.ValidateIntent(recovery, true);
        Assert.Equal("dhcp", recovery.Mode); Assert.Null(recovery.Address); Assert.Empty(recovery.DnsServers);
    }
    [Fact]
    public async Task DurableJournalKeepsOriginalSnapshotAndNeverContainsDeviceUrl()
    {
        var root = Path.Combine(Path.GetTempPath(), "AVApplyJournal_" + Guid.NewGuid().ToString("N"));
        try
        {
            var j = new ProjectApplyJournal(root); var request = Request();
            await j.BeginAsync(request, CancellationToken.None);
            var path = Path.Combine(root, request.OperationId.ToString("N") + ".request.json");
            var original = File.ReadAllBytes(path);
            await j.FinishAsync(request.OperationId, new(ProjectApplyState.ApplyFailed, true, null, "DnsPrimary"));
            Assert.Equal(original, File.ReadAllBytes(path)); Assert.DoesNotContain("https", Encoding.UTF8.GetString(original));
            Assert.Equal(request.OperationId, Assert.Single(j.ReadSnapshots()).OperationId);
            await Assert.ThrowsAsync<IOException>(() => j.BeginAsync(request, CancellationToken.None));
            Assert.Equal(original, File.ReadAllBytes(path));
            File.WriteAllText(path, "{}"); Assert.Empty(j.ReadSnapshots()); Assert.Equal(1, j.UnreadableCount);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void PipeAclDeniesNetworkAndGrantsOnlyInitiatorAndStartInfoContainsNoPayload()
    {
        var sid = new SecurityIdentifier("S-1-5-21-1-2-3-1001");
        var acl = ProjectApplyElevation.CreatePipeSecurity(sid);
        Assert.True(acl.AreAccessRulesProtected);
        var rules = acl.GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<PipeAccessRule>().ToList();
        Assert.Equal(2, rules.Count);
        Assert.Contains(rules, r => r.IdentityReference.Equals(sid) && r.AccessControlType == AccessControlType.Allow);
        Assert.Contains(rules, r => r.IdentityReference.Equals(new SecurityIdentifier(WellKnownSidType.NetworkSid, null)) && r.AccessControlType == AccessControlType.Deny);
        var psi = ProjectApplyElevation.CreateStartInfo(@"C:\fixture\NetworkProfileManager.exe", "AVNetworkToolkit_fixture", 123);
        Assert.True(psi.UseShellExecute); Assert.Equal("runas", psi.Verb); Assert.Equal(System.Diagnostics.ProcessWindowStyle.Hidden, psi.WindowStyle);
        Assert.Equal(new[] { "--project-apply", "AVNetworkToolkit_fixture", "123" }, psi.ArgumentList);
        Assert.Empty(psi.Arguments);
    }
    [Fact]
    public void WorkerResponseCannotSmuggleUnknownStageOrFalseSuccess()
    {
        Assert.True(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Finished, true, "CommandsFinished")));
        Assert.False(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Finished, false, "CommandsFinished")));
        Assert.False(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Finished, true, "Address")));
        Assert.False(ProjectApplyElevation.ValidResult(new(ApplyExecutionStatus.Failed, true, "https://fixture.invalid/secret")));
        Assert.False(ProjectApplyElevation.ValidResult(new((ApplyExecutionStatus)999, true, "Address")));
    }
    [Fact]
    public void MissingBooleanCannotDefaultToStaticAndDnsRegistrationIsExplicitlyPreserved()
    {
        var r = Request(); var json = ProjectApplyWire.Serialize(r);
        Assert.ThrowsAny<Exception>(() => ProjectApplyWire.Parse<ProjectApplyRequest>(json.Replace("\"Dhcp\":false,", "")));
        r = r with { Before = r.Before with { DnsRegistration = "none" } };
        Assert.Contains("register=none", ProjectApplyPolicy.Commands(r)[1]);
        var recovery = ProjectApplyPolicy.RecoveryIntent(r.Before);
        Assert.Equal("none", recovery.DnsRegistration);
    }
    [Fact]
    public async Task RequestThatExpiresDuringFinalIdentityReadCannotStartCommand()
    {
        var r = Request(); var now = DateTimeOffset.UtcNow; var reads = 0; var calls = 0;
        var result = await ProjectApplyWorker.ExecuteAsync(r, (_, _) =>
        {
            if (++reads == 2) now = now.AddMinutes(3);
            return Task.FromResult(r.Before);
        }, (_, _) => { calls++; return Task.CompletedTask; }, _ => Task.FromResult(true), () => now);
        Assert.Equal(ApplyExecutionStatus.Failed, result.Status); Assert.False(result.MayHaveChanged); Assert.Equal(0, calls);
    }
    [Theory]
    [InlineData("192.0.2.0", "192.0.2.1")][InlineData("192.0.2.255", "192.0.2.1")]
    [InlineData("192.0.2.10", "198.51.100.1")][InlineData("192.0.2.10", "192.0.2.10")]
    public void ExistingStateThatCannotBeRestoredIsBlocked(string address, string gateway)
    {
        var s = ProjectApplyTests.Snapshot(); var a = s.Addresses[0] with { Address = address };
        s = s with { Addresses = ImmutableArray.Create(a), PersistentAddresses = ImmutableArray.Create(a),
            Routes = s.Routes.Select(r => r with { NextHop = gateway }).ToImmutableArray() };
        Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateSnapshot(s));
    }
    [Theory]
    [InlineData("dhcp", true)][InlineData("netmgmt", false)]
    public void DhcpGatewayMustBeLeaseDerivedRatherThanAnUnrecoverableManualRoute(string origin, bool supported)
    {
        var s = ProjectApplyTests.Snapshot() with { Dhcp = true,
            Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "dhcp", "preferred")),
            PersistentAddresses = ImmutableArray<ApplyAddress>.Empty,
            Routes = ImmutableArray.Create(new ApplyRoute("0.0.0.0/0", "192.0.2.1", 0, "active", origin)) };
        if (supported) ProjectApplyPolicy.ValidateSnapshot(s);
        else Assert.Throws<InvalidOperationException>(() => ProjectApplyPolicy.ValidateSnapshot(s));
    }
}
