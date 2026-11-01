# Isolated Windows preparation — REL-02 / ADR-036

Current follow-up: owner requested local prerequisite installation and selected a full independent Windows 10 VMware clone (ADR-046). [Local lab preparation and exact remaining gates](LOCAL_WINDOWS_LAB.md) supersede the historical lack-of-lab facts below; no actual application network test or phase acceptance follows from provisioning.

2026-11-01 Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, JSON repair `8f9106c34481fac4f6e71ee5ee65e2bc79e0a009`; authorization `fc2589f`. Phase 07 AWAITING_REVIEW. This packet prepares the existing Phase 04/05 native plans; no production code, support policy or phase order changes.

## Owner disposition and present boundary

After a simplified environment question, owner selected **«Нет — пока ограничимся безопасными проверками»**. Native work is deferred. No test NIC/isolation/local-console/independent-recovery facts are confirmed. This is not authorization to select a working NIC or assume rollback is guaranteed. No live enumeration acceptance, App startup, UAC, network settings, browser, probes, mDNS or DHCP ran. REL-03 CI/portable ZIP and REL-04 release metadata remain deferred; the owner has not replaced native acceptance with unit evidence or approved a reduced first-release scope.

The agent can collect GUID/index/MAC, supported pre-state and prepare exact independent recovery after a lab is available; the owner need not manually provide these technical identifiers. Physical/virtual isolation and local access still need confirmation. An app journal or a promise to restore is not independent recovery. Sequential commands and hardware/provider failures can prevent automatic or manual restoration through the application.

## Safe checks actually executed

Using the existing SDK, `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`:

```powershell
& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build -p:OutputPath=bin/JsonInvestigation/ --filter 'FullyQualifiedName~ProjectApplyTests|FullyQualifiedName~ProjectApplyBoundaryTests|FullyQualifiedName~ProjectApplyViewModelTests|FullyQualifiedName~LinkLocalTests|FullyQualifiedName~LinkLocalViewModelTests' --logger 'trx;LogFileName=rel02-fake-boundaries.trx' --results-directory TestResults/IsolatedWindowsPreparation
& ./scripts/validate_apply_observation.ps1
& "$env:WINDIR/System32/WindowsPowerShell/v1.0/powershell.exe" -NoProfile -NonInteractive -File ./scripts/validate_apply_observation.ps1
```

- Unit/fake: **156 PASS / 0 failed / 0 skipped**, six seconds. Local ignored TRX: `TestResults/IsolatedWindowsPreparation/rel02-fake-boundaries.trx`. No new tests or assertions added/weakened for this preparation.
- TRX SHA-256: `487783BA311CDAB00ED60EB8426EF5EED1F0AC1BB81FDCD02F62EE784C01C858`.
- Embedded observer AST/command allowlist plus **11 shadowed OS-query fixtures PASS in PowerShell 7 and Windows PowerShell 5.1**. Actual NetTCPIP/DNS/registry commands are replaced by fixture functions; no actual observer process or network enumeration.
- Whole-suite/build evidence at the same runtime sources: [REL-01 report](JSON_VALIDATION_REVIEW.md), 631 PASS and Release build with 0 warnings/errors. Not rerun for this documentation-only packet.
- Planning: actual working tree remains FAIL for the pre-existing CODEX_START.md deletion. Exported tracked HEAD with `git archive --format=zip HEAD`, overlaid current task files in ignored `TestResults/IsolatedWindowsPreparation/plan-snapshot`; retained tracked CODEX_START.md only there. `& $py -B TestResults/IsolatedWindowsPreparation/plan-snapshot/scripts/validate_plan.py` and `& $py -B TestResults/IsolatedWindowsPreparation/plan-snapshot/scripts/test_validate_plan.py` PASS (9 phases/49 tasks/schema/formats/313 links; 10 tooling tests), with the existing Python path from REL-01. No validator change, branch/worktree or restoration in the real tree. `git diff --check` PASS.

| Required behavior | Existing automated evidence inspected and run | Native evidence |
|---|---|---|
| Profile → actual IP/prefix/gateway/both DNS → exact device URL | `ExactDestinationOpensOnlyAfterObservedAddressGatewayAndBothDnsMatch`, `MismatchedObservedStateNeverOpens` (including IP/prefix/gateway/DNS order/policy/persistence/identity) | NOT RUN |
| Deliberate standalone profile Apply, separate Open, no mutation on project/review | `ProfileOnlyApplyRetainsPolicyJournalSingleUseAndNoBrowser`, `ExplicitTargetAppliesProfileWithoutUrlAndNeverOpensBrowser`, `ManualSelectionReviewAndFinalConsentAreSeparateGates` | NOT RUN |
| Restore with fresh diff/confirmation, no browser during recovery | `RecoveryRequiresFreshReviewAndDoesNotOpenBrowser`, `SavedSnapshotReadingIsReadOnlyAndRecoveryRequiresReviewAndConsent`, `FailedRecoveryRemainsFailedAndNeverOpens`, `DhcpRecoveryRestoresPolicyWithoutPinningHistoricalLeaseOrServers` | NOT RUN |
| UAC denial | `WorkerFailuresCannotOpenEvenIfActualStateMatches(ElevationDenied, false, ElevationDenied)` and typed start-info/pipe ACL fixtures | NOT RUN: actual consent/worker/pipe |
| NIC changed/removed/disconnected | `IdentityFailureAfterConfirmationCannotRetargetAnotherNic`, `RemovedTargetWithSameAliasCannotFallBackOrRetainOldPlan`, observer `disconnected` fixture | NOT RUN: real cable/driver/removal |
| Partial command failure/unknown worker/cancellation | `PartialCommandFailureStopsRemainingCommandsAndRetainsMutationUncertainty` at each command; `UncertainWorkerExceptionStillObservesPartialState`, cancellation/failed recovery/journal fixtures | NOT RUN: real partial mutation |

These are separate component/contract checks, not a claimed complete native end-to-end journey. Tests exercise the real coordinator/policy/worker logic with fake OS execution/observations/browser/consent. Temp-file journal tests do not certify production storage or privileged IPC.

## Prepared native sequence — do not execute under the current restriction

1. Establish a disposable Windows VM with an isolated internal switch and usable VM snapshot/console, or a dedicated lab PC/NIC connected only to approved test equipment with independent local recovery. No production bridge/uplink or remote session dependent on the test NIC. Confirm the intended adapter with the owner before any mutation; names alone are insufficient. A supported fully classified static source is sufficient for the first case; do not start a DHCP server to manufacture a source lease.
2. Pin source commit/binary and OS/build/driver/user/elevation identity. Locally capture exact NIC GUID/index/MAC, full IPv4 active/persistent addresses/origins/DAD, DHCP/DNS policy/ordered servers/registration flags, active/persistent routes and baseline IPv6/unrelated-NIC controls. Retain raw data privately, outside Git; redact public evidence. If classification is unsupported/unknown, stop and record BLOCKED, not a synthetic empty snapshot.
3. Before applying, save the independent baseline and test a VM snapshot recovery route or review adapter-specific manual restoration using local console and built-in Windows tools. Generate exact recovery steps only from the confirmed snapshot; no unconditional reset, global route removal or inferred empty DNS. Keep the app's private original request/outcome journals as additional evidence, never the only route back.
4. Happy case A: deliberately select the approved NIC and saved project profile; record the displayed old/new diff and final consent. Apply only after identity/pre-state rechecks. Independently read observed IP + prefix/mask, gateway active/persistent state, DNS source/order/registration. Compare against requested intent, not netsh exit code or Ping. Only after verified assignment deliberately Open the approved lab device URL. Observe exact path/query/fragment handoff; no credentials, automatic login or TLS bypass. Separate case B exercises Apply & Open's verification-before-browser ordering. Open alone must not mutate the NIC.
5. Restore original through the explicit recovery action, fresh current diff and separate confirmation. Independently compare all supported original policy/settings, routes and unrelated IPv6/NIC controls. For DHCP restore policy and a usable lease; no claim to preserve the old lease. No lease/APIPA is unverified. Browser launch during restore is a failure. If app recovery blocks/fails, preserve evidence and use the independent recovery route; do not retry blindly.
6. Repeat from the known restored baseline for UAC **deny**, then lab-only cable disconnect/removal before confirmation and during the operation. UAC denial must leave state unchanged and never open a device. Removal must not choose another NIC. Loss after mutation must be reported uncertain/unverified with no browser and retained original snapshot. Verify actual state independently; do not presume disconnect itself prevents commands.
7. Partial failure: retain fake stage-by-stage coverage as the current evidence. A deterministic native fault-injection method is **UNVERIFIED**; before attempting it, agree a test-only method in a disposable snapshot-backed environment. Do not damage the working NIC, change global ACLs or modify production logic to create the failure. Label a future injected failure separately from a spontaneous native provider error. Always reread and recover via an explicit action/independent path; no atomic rollback claim.
8. Finish each case with captured outcome, journal and independent observation, plus restoration evidence; stop the session if any unrelated state changed, identity is ambiguous, the worker may still be running or recovery is unavailable. No probes/scanner/mDNS/DHCP are needed for assignment verification. Actual browser navigation can cause lab network traffic and is part of the future gated execution only.

## Native evidence record (all fields currently UNVERIFIED / NOT RUN)

For each case record privately: timestamp; commit/binary; Windows/driver; approved topology and NIC identity; console/recovery confirmation; complete pre-state; requested diff; consent/UAC outcome; worker stage/status; independent post-state; exact URL handoff result; original journal ID; recovery attempt/result; independent restored state; IPv6/other-NIC comparison; PASS/FAIL/BLOCKED with artifact paths. Do not convert missing evidence to PASS.

Current result: **preparation and safe checks complete; native execution deferred by owner**. No phase complete, release/CI/portable support claim or publication. Next smallest safe step is owner review of JSON repair and this evidence; resume native work only when an isolated test environment exists and the owner changes the current safe-only restriction.
