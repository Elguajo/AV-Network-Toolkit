# Project/device opening — network mutation invariant review

2026-11-02, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, branch main, inspected source `a75a3acb4c533570ba3ce705e012011681d6e63e`; initially clean tree. OPEN-01 is a safe verification follow-up, not a new feature or phase acceptance. Phase 07 remains AWAITING_REVIEW. No product requirement, schema, supported network state or recovery guarantee changes.

## Outcome

No violation of the opening/mutation invariant found in the inspected paths. Existing code already separates catalogue loading, ordinary browser opening and explicit typed Apply & Open. No production or test code repair is needed. The existing tests provide relevant failure-path coverage; this review records fresh results rather than adding duplicate tests.

Important selection distinction: `MainViewModel.Refresh` initially highlights the first visible adapter. Main-window Apply and Apply & Open reuse that visible selected GUID, lock the apply window to it and require review plus final confirmation. They do not require a second manual NIC selection. A detached `ProjectWindow` creates an apply window without a target GUID; that window requires manual adapter selection after enumeration. Initial highlighting is not mutation, and opening a project/device never derives a target from project adapter hints. Requiring a fresh manual NIC choice for every main-window operation would be a separate product change, not an already implemented guarantee.

## Traced entry points and evidence

| Path | Current behavior / evidence |
|---|---|
| Main rail / Manage project / empty-state project action | `MainWindow.OpenProjectClick` opens or activates the shared `ProjectWindow`; no apply. |
| Main Open project button / Project window Open | Both bind `ProjectViewModel.OpenCommand`. `Open` validates via `ProjectStore.Open`, protects dirty state, then loads profiles/devices. `MainWindow.ProjectOpened_Click` only changes workspace/source tabs. `ProjectStore.Open` reads/parses/hashes local JSON and has no network-service dependency. |
| New, folder choice, Save/Save as, profile/device selection/editing, source/tabs/search/grouping | Catalogue/editor/file operations only. Selection setters never invoke apply or browser commands. `ProfileForApply` validates/deep-copies retained intent without saving drafts. Existing project/store/device tests exercise these operations with isolated IO. |
| Legacy profile JSON Import | `MainViewModel.ImportProfiles` validates incoming records, persists/updates the selected adapter's saved profile list; it never calls the mutation pipeline. This native file-dialog path was inspected, not exercised with the real picker. |
| Ordinary Open Web Interface in main and Project window | Both bind `OpenDeviceCommand`. Validates the full pending URL/reference, asks browser confirmation, passes the exact URL to `IBrowserLauncher.Open`; no apply event, probe, UAC or network worker. Selecting a device alone does not open it. |
| Device Apply & Open | `ProjectViewModel.Devices.InitializeDevices` freezes validated profile/device intent through JSON round-trip and raises only `ApplyAndOpenRequested`. Unlinked/stale references and unfinished related profile edits block it. Main routes through `ShowProjectApply`; detached Project window composes its own apply surface. |
| Standalone project profile Apply / explicit reapply menu | `ProjectProfilesPanel.Apply_Click` calls `ProfileForApply` and raises `ApplyRequested`; main passes the currently selected GUID. No browser destination. |
| Apply window entry / Refresh / Review | `ProjectApplyWindow.Create` enumerates on Loaded, never executes. `ProjectApplyViewModel` prepares an immutable plan and old/new review without mutation. No-target enumeration clears selection; fixed target binds only the given GUID and never falls back by alias. Selection/refresh/cancel invalidate plans. |
| Final Apply | `ProjectApplyViewModel.ApplyAsync` requires confirmation before coordinator execution. Coordinator rechecks identity/pre-state, acquires mutation lease and durably journals before entering the typed worker. Worker rechecks request/identity/pre-state after UAC and immediately before commands. |
| Apply → observation → browser | `ProjectApplyCoordinator.ExecuteAsync` opens only after Finished execution and matching observed configuration. Verification does not use reachability/ping. Static verification checks address/prefix, gateway and metric, DNS policy and ordered servers, persistent state and identity. DHCP requires a usable Preferred DHCP address and correct DNS policy, without fixing a lease. |

`DefaultBrowserLauncher` validates again and hands the original URL to the default document association, without shell command arguments. Browser handoff does not certify endpoint reachability or TLS/device success. Unsafe schemes and embedded credentials/tokens remain rejected.

## Negative paths and retained configuration semantics

| Scenario | Evidence / result |
|---|---|
| Declined browser confirmation, unsafe URL or invalid reference | No launch; device draft retained. `DeviceDirectoryTests` covers exact deep URLs, rejection and redacted launch errors. |
| No adapter, removed/replaced/renamed adapter, changed pre-state or expired/replayed plan | No mutation/browser; fixed GUID has no alias fallback. `ProjectApplyViewModelTests`, `ProjectApplyTests`, `ProjectApplyBoundaryTests`. |
| Review only / declined Apply confirmation | No worker call. `ManualSelectionReviewAndFinalConsentAreSeparateGates`; WPF Apply fixture. |
| UAC denied / worker failed, unknown or cancelled | No browser even if observed fields happen to match. `WorkerFailuresCannotOpenEvenIfActualStateMatches`; production error 1223 maps to ElevationDenied. Actual UAC NOT RUN. |
| Partial failure / cancellation after mutation | Stop future command stages, reread actual state, report uncertainty; no automatic rollback/browser. `PartialCommandFailureStopsRemainingCommandsAndRetainsMutationUncertainty`, cancellation/coordinator/WPF close cases. |
| Verification mismatch/read failure, tentative address, DHCP no lease/APIPA/wrong policy | Unverified; no browser. Existing mismatch/DHCP/failure cases passed. |
| Browser exception after verified apply | BrowserFailed; exactly one apply, no automatic reapply/recovery. `BrowserFailureDoesNotReapplyOrRecover`. |
| Journal write failure / late journal update failure | Initial failure aborts mutation; late failure remains a separate warning and does not rewrite the network outcome. Existing journal cases passed. |
| Recovery | Separate review/confirmation; original journal retained, no URL launch. DHCP restores policy, not historical lease. Unsupported/unknown/complex states remain blocked; no universal rollback claim. |

Opening operations preserve all OS settings by never entering mutation. Explicit static Apply intentionally uses the profile's gateway: a null gateway means `gateway=none`, visibly included in requested review, not preservation of the previous gateway. DNS `leave-unchanged` emits no DNS command and verifies previous policy/ordered servers; static DNS is explicit replacement, DHCP requests automatic address and DNS. Static automatic DNS and /31–/32 stay blocked for OS apply. Complex/multi-address/custom-route/unknown/non-restorable sources stay blocked. Generated commands use only `interface ipv4`; IPv6 is outside the mutation command set. Native preservation/recovery remains UNVERIFIED until isolated acceptance.

## Fresh validation

Private SDK `$invariantSdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`, .NET 10/WPF on Windows. Distinct output paths avoid existing app binaries. No package installation/restore, ordinary App startup, OS NIC enumeration/settings, probes, sockets/listeners, elevation or real browser/device URL used by these runs. Harness supplies fake boundaries and redirects its storage to fixture directories.

| Exact command | Observed result |
|---|---|
| `& $invariantSdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/OpenInvariant/` | PASS, 0 warnings/errors. |
| `& $invariantSdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --no-restore -p:OutputPath=bin/OpenInvariant/ --filter 'FullyQualifiedName~ProjectApply\|FullyQualifiedName~DeviceDirectory\|FullyQualifiedName~ProjectViewModel\|FullyQualifiedName~ProjectJson\|FullyQualifiedName~ProjectStore\|FullyQualifiedName~LocalProfileApply' --logger 'trx;LogFileName=open-invariant-focused.trx' --results-directory TestResults/OpenInvariant --logger 'console;verbosity=minimal'` | 237 PASS / 0 failed / 0 skipped. Table escapes represent literal filter pipes. |
| `& $invariantSdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --no-restore -p:OutputPath=bin/OpenInvariant/ --logger 'trx;LogFileName=open-invariant-full.trx' --results-directory TestResults/OpenInvariant --logger 'console;verbosity=minimal'` | 744 PASS / 0 failed / 0 skipped. |
| `& $invariantSdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/OpenInvariantUi/` | PASS, 0 warnings/errors. |
| `& $invariantSdk scripts/ProjectUiSmoke/bin/OpenInvariantUi/ProjectUiSmoke.dll TestResults/OpenInvariant/Ui` | PASS, 128 PNGs, Light/Dark normal/minimum, 0 WPF binding errors; includes project/device/apply/recovery/close/selected-target routing. |

Retained local artifacts (ignored): `TestResults/OpenInvariant/open-invariant-focused.trx`, `open-invariant-full.trx` and `Ui/`. TRX SHA-256 respectively: `32cb940fa72284d847f8fa546f7edd287334bc87c4edc1ec19248fe227b3bf9d`, `4b37bbe56f85a61997caf03b989b09b3fd0ddfd0fc5c33747381dfbb67b247ed`. No new screenshots are presented as a visual redesign or native acceptance.

## Release identity and concurrent scope

Local `git rev-parse v1.1.5`, read-only `git ls-remote origin refs/tags/v1.1.5` and `gh api repos/Elguajo/AV-Network-Toolkit/releases/tags/v1.1.5` independently returned source `9e06c48b4b433e0463c2376ba4555c44804ea010`; API reports Preview and four assets. `git diff 9e06c48 HEAD -- app test scripts` was empty at inspected a75a3ac. Existing [corrected-release evidence](RELEASE_1.1.5_REUPLOAD_REVIEW.md) remains historical package/CI/download evidence; packages were not rebuilt, downloaded or installed in this review.

After the solution/harness builds and clean source comparison, concurrent updater-owned `app/Services/ApplicationUpdate*.cs` and an edit to `NetworkMutationLease.cs` appeared, along with an independent device-password proposal. Tests above execute the retained review binaries, not a later rebuild of those concurrent changes. All are excluded from this review and commit; no claim is made about those later sources. DATA-01 and update chats retain their own authorization/state. No push, publication, phase acceptance or Phase 08 advancement.

Remaining limits: actual Windows NIC/UAC/IPC/network commands, physical traffic and browser/default-association/device behavior NOT RUN. Owner/native/phase acceptance and existing unrelated risks stay open. Safe next step is owner review of this evidence; any native test requires the separately approved isolated NIC/recovery case.
