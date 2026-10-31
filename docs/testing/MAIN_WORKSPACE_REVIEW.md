# Main workspace — IDEA-01/04 review

2026-10-31, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main; clean base `103b50d34c6f63d69271a03e8caf4a15494e3e43`. Owner authorized this selected-concept implementation («Да делаем»), recorded as ADR-028. [Contract](../spec/MAIN_WORKSPACE_CONTRACT.md). Implementation commit resolves with `git log -1 --format=%H -- app/MainWindow.xaml`; no push/release or Phase 07 execution in this follow-up.

## Outcome and surfaces

Implemented the second concept's adapter-left/profile-rows-right hierarchy with per-row target-named Apply, an independent project profile editor, project context and a device tab. Existing manual input, Ping, history and recovery remain accessible as expandable sections. Local five-slot profiles and portable project-wide profiles have explicitly different scopes. No matching by adapter alias, copying into slots, migration or schema change. Project and Manage project share one VM; closing the shared manager preserves the main document. Main/tray exit protects project and detached modal drafts. MainVM initial composition now wires its existing confirmation handler before use; underlying legacy network semantics are preserved.

Standalone project Apply has no URL/browser dependency, freezes retained validated intent, takes the selected adapter GUID and uses the existing typed apply/verify/journal/recovery flow. Its modal fixes the GUID target, cannot fall back to a same-named NIC, and invalidates prepared plans on refresh. Existing support/policy and worker/IPC contracts are unchanged. Device Open and Apply & Open remain separate; the latter uses the selected main adapter, the former does not apply anything.

Changed code: MainWindow/App composition and exit integration; Main/Project/ProjectApply VMs; profile/adapter/project/apply views; new ProjectProfilesPanel, shared ProjectProfileFields, ProjectProfileDialog and summary converter; localized resources; test-harness friend access. Test changes: three existing test classes and the isolated WPF harness. No dependency added. Schema/examples/ProjectJson/legacy NetworkService/ElevationHelper/ProfileRepository/HistoryRepository/ProjectApplyPolicy/license/CI unchanged.

## Verification actually run

Existing local tool paths: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`; `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. Windows/.NET8/WPF, no installation/restore/App startup. Separate outputs avoid interfering with existing running binaries.

| Command / check | Outcome |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/WorkspaceUnit/` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build -p:OutputPath=bin/WorkspaceUnit/ --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=workspace-reviewed.trx'` | PASS, 552 tests, 0 failed/0 skipped; 10 new cases |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/Workspace/` | PASS, 0 warnings/errors |
| `& $sdk scripts/ProjectUiSmoke/bin/Workspace/ProjectUiSmoke.dll TestResults/WorkspaceUiSmokeReviewed` | PASS, 112 PNGs total (28 main/editor additions), 0 binding errors; Light/Dark × 1200×850 and 760×520 main, modal normal/minimum |
| WPF interaction assertions | PASS: profile-only target event, changing/renaming selected NIC updates label/target, bounds after scrolling including long names, keyboard reachability, fake device Open/Apply & Open, invalid detached edit/Cancel retains record, shared-manager close retains draft, main/tray exit refuses project/modal draft loss, empty adapter state, named visible form controls |
| `& $py -B scripts/validate_plan.py` and `& $py -B scripts/test_validate_plan.py` | PASS, schema/formats/49 phase tasks/links; 10 validator tests. Updated documentation checked before commit |
| `git diff --check` / staged whitespace and protected-path diff | PASS; protected paths above unchanged |

Unit cases cover detached validation/identity/references, unsaved-editor conflict, frozen retained apply/no legacy writes, fixed GUID/no alias fallback, refresh invalidation, foreign recovery rejection, standalone journal/single-use/no browser, unchanged automatic-DNS/prefix/no-lease rejection. No unit or component executes real NIC queries/probes/sockets/UAC/commands/browser. Main rendering injects fake enumeration and apply event interception; AppPaths redirects to ignored test output. App.OnStartup, monitor, tray and services are not launched. Existing adapter-change animation is advanced to its settled endpoint only in the capture harness.

Initial checks retained: 20 focused tests PASS; first full 548 PASS before four additional policy/coordinator cases, then 552 PASS. First Main component render failed on an entry-assembly-relative icon URI; fixed to the application assembly resource. Next render exposed an unnamed adapter ListBox; added its accessible name and a Ping input name. Earlier passing renders were repeated after adding long-name bounds, modal-exit guard and settled-animation capture. A focused self-review identified the tray-exit/modal-draft gap and corrected it before final evidence. No test gate weakened, skipped or serialized.

## Visual and focused review

Reference: [approved second raster](../design/main-window-target-v2.png), [DESIGN.md](../../DESIGN.md). [Actual rendered main screen](../design/main-window-implemented-workspace.png) is a copied harness PNG with only synthetic documentation addresses. Main profile hierarchy, separate editor, scopes and theme resources verified visually in normal/minimum Dark and Light, including device/editor/error surfaces. Small windows scroll; native controls and reachable actions are preserved. This is a component render, not native Mica/full desktop certification or a claim of pixel identity. Two explicit profile scopes are the compatibility refinement versus the concept. Device interior reuses the existing directory; FR-13 credentials are still pending.

| Evidence ID / kind | Status | Source / decision |
|---|---|---|
| workspace-code / code-review | passed | Changed flow traced through shared VM/edit/apply/exit; no remaining material defect found in this bounded self-review |
| workspace-ordering / concurrency-review | passed | UI-owned modal editing, immutable apply copy, fixed GUID, stale-plan invalidation and existing mutation lease; unit/WPF cases above |
| workspace-boundary / security-review | passed | No new shell/payload/path/URL trust boundary; policy/worker/IO unchanged, profile-only coordinator validates before read/execute, no browser destination |
| workspace-visual / visual-qa | passed | Real WPF captures compared to selected hierarchy; examples only, theme/minimum states inspected |
| workspace-keyboard / accessibility-review | passed | Native control names, focus, wrapped labels, action bounds and fixed modal footer checked in component harness; partial review only |
| workspace-native / isolated acceptance | unavailable | Real NIC assignment/UAC/recovery/browser/tray/native dialogs/manual AT/high contrast/zoom NOT RUN, permission/environment gate retained |

## Risks, deviations and next action

Release decision **NO-SHIP**: OPEN P05-R01 remains a separate unresolved/release-blocking JSON-validation risk; subsequent passes do not close it. Native Windows/driver/pipe/actual browser/manual acceptance and large-catalog performance remain UNVERIFIED. Legacy apply/history preserve their narrower existing semantics; no universal rollback or post-state verification is claimed for them. Static automatic DNS, no-lease/APIPA and complex states still block the typed flow under existing policy (IDEA-02). Password storage is approved but unimplemented (IDEA-03).

No broad redesign, Phase 07 start, schema migration, live network operations, push or release. IDEA-01/04 are implementation/evidence recorded, awaiting owner UI review; no phase/task acceptance fabricated. Next safe step: owner reviews the implemented screen and separate source scopes. Then separately scope IDEA-02/03 or authorize Phase 07; preserve the Phase 06 acceptance gate and OPEN P05-R01.
