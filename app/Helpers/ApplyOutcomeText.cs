using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Helpers;

internal static class ApplyOutcomeText
{
    internal static string Failure(ProjectApplyOutcome outcome)
    {
        // A pre-mutation refusal must never suggest that rollback is needed.
        if (!outcome.MayHaveChanged)
        {
            string key = outcome.Stage switch
            {
                "Expired" => "Apply.Refused.Expired",
                "PreStateChanged" or "AdapterChanged" => "Apply.Refused.Changed",
                "OperationActive" => "Apply.Refused.Busy",
                "Preflight" => "Apply.Refused.Validation",
                "SnapshotWrite" => "Apply.Refused.Journal",
                "UacDenied" => "Apply.Refused.Elevation",
                "BeforeApply" when outcome.State == ProjectApplyState.Cancelled => "Apply.Refused.Cancelled",
                _ => "Apply.Refused.Other"
            };
            return Loc.Get(key);
        }
        return (outcome.Stage == "AddressConflict" ? Loc.Get("Apply.AddressConflict") : ProjectApplyViewModel.StateText(outcome.State))
            + "\n" + Loc.Get("Apply.ManualRecovery");
    }
}
