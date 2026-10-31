using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

// Pure privileged algorithm. Only the production composition supplies OS/process access.
internal static class ProjectApplyWorker
{
    internal static async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request,
        Func<ApplyAdapterIdentity, CancellationToken, Task<ApplyAdapterSnapshot>> read,
        Func<IReadOnlyList<string>, CancellationToken, Task> run,
        Func<string, Task<bool>> continueAtBoundary,
        Func<DateTimeOffset>? now = null)
    {
        now ??= () => DateTimeOffset.UtcNow;
        var changed = false;
        var stage = "Validation";
        try
        {
            ProjectApplyWire.ValidateRequest(request, now());
            using var session = new Semaphore(1, 1, "Local\\AVNetworkToolkitApply_" + request.Before.Identity.Id.ToString("N"));
            if (!session.WaitOne(0)) return new(ApplyExecutionStatus.Failed, false, "OperationActive");
            try
            {
            var initial = await read(request.Before.Identity, CancellationToken.None);
            if (!ProjectApplyCoordinator.SameConfiguration(request.Before, initial))
                return new(ApplyExecutionStatus.Failed, false, "PreStateChanged");
            var commands = ProjectApplyPolicy.Commands(request);
            for (var i = 0; i < commands.Count; i++)
            {
                stage = i == 0 ? "Address" : i == 1 ? "DnsPrimary" : "DnsSecondary";
                if (!await continueAtBoundary(stage)) return new(ApplyExecutionStatus.Cancelled, changed, stage);
                ProjectApplyWire.ValidateRequest(request, now());
                // Re-resolve stable GUID + index + alias + physical address immediately
                // before every operation. A rename also invalidates reviewed identity.
                var current = await read(request.Before.Identity, CancellationToken.None);
                if (current.Identity != request.Before.Identity)
                    return new(ApplyExecutionStatus.Failed, changed, "AdapterChanged");
                if (i == 0 && !ProjectApplyCoordinator.SameConfiguration(request.Before, current))
                    return new(ApplyExecutionStatus.Failed, false, "PreStateChanged");
                ProjectApplyWire.ValidateRequest(request, now());
                changed = true; // A command can mutate and then fail/timeout.
                await run(commands[i], CancellationToken.None);
            }
            return new(ApplyExecutionStatus.Finished, changed, "CommandsFinished");
            }
            finally { session.Release(); }
        }
        catch { return new(ApplyExecutionStatus.Failed, changed, stage); }
    }
}
