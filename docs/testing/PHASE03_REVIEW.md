# Phase 03 review packet — awaiting owner review

Date: 2026-10-31, Asia/Tashkent. Fork: https://github.com/Elguajo/AV-Network-Toolkit. Branch: `codex/phase-03-device-web-interfaces`. Starting HEAD and independently checked remote main: `17c7014ba585fe85680fcdede51ce967f238054d`. Implementation commit: `e861c3a426f24e0103e9cc26b7e82b6e2f1e503f`. Final documentation-only handoff commit: resolve `git log -1 --format=%H -- STATUS.md`. No PR, push, merge or release.

## Outcome and changed paths

03-T01–03-T05 are implemented with local evidence, pending owner acceptance. Phase remains AWAITING_REVIEW. ADR-015 records the explicit owner request to start Phase 03; earlier Phase 02 manual limitations remain historical.

- `app/ViewModels/ProjectViewModel.Devices.cs`: existing ProjectDevice directory, independent stable draft, CRUD, search, type/profile grouping, relink/unlink and explicit open command. Shared ProjectViewModel dirty/save/close guards include device edits; referenced-profile deletion checks both retained and pending devices.
- `app/Views/DeviceDirectoryView.xaml(.cs)`, `app/Views/ProjectWindow.xaml(.cs)`, `app/Strings/En.xaml`: localized Devices tab, existing Fluent controls/styles, scrollable lists/forms, safe full destination preview, fixed actions and confirmation/warnings.
- `app/Services/BrowserLauncher.cs`, `app/Services/ProjectJson.cs`: injectable launcher, same approved URL validation policy exposed internally, exact string handed to default-browser association with no command arguments. Model comment updated; fields unchanged.
- `test/DeviceDirectoryTests.cs`, dialog fake in `test/ProjectViewModelTests.cs`, `scripts/ProjectUiSmoke/Program.cs`: isolated behavior tests and WPF surface proof. No real App startup, network/adapter services, native dialogs or browser launch.
- Canonical status/tasks/phase/machine plan/ADR, context/roadmap, DESIGN, device spec, threat model and test matrix record the authorized scope and evidence.

Unchanged: JSON v1 schema/example and ProjectStore, legacy repositories, NetworkService/netsh, UAC, scanner/ping, history/revert, MainWindow, App startup, tray, English fallback, MIT license and workflow. Protected-path diff against the starting anchor returned exit 0. Existing functionality has automated regression evidence; actual live/full-app behavior remains UNVERIFIED.

## Exact local verification

Windows 10.0.26300 x64; local SDK 8.0.427/runtime 8.0.33. PowerShell variables:

```powershell
$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"
$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"
```

| Exact command/check | Result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase03.trx'` | PASS, 308 tests, 0 failed/skipped; 22 additional device cases. Local ignored `test/TestResults/phase03.trx` |
| `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/Phase03UiSmoke` | PASS, 20 renders: Profiles plus Devices empty/selected/error/scrolled-details × Light/Dark × 900×780/700×540, 0 WPF binding errors |
| `& $py -B scripts/validate_plan.py` | PASS, 9 phases/49 stable IDs, full draft-2020-12 schema/formats/example/references/planning and 107 links |
| `& $py -B scripts/test_validate_plan.py` | PASS, 10 regression tests |
| `git diff --quiet 17c7014 -- app/Services/NetworkService.cs app/Services/ProfileRepository.cs app/Services/HistoryRepository.cs app/Helpers/ElevationHelper.cs app/MainWindow.xaml app/MainWindow.xaml.cs app/App.xaml.cs app/Views/TrayPopupWindow.xaml.cs LICENSE schemas/project.avnet.schema.json examples/project.avnet.json .github/workflows/build.yml` | PASS, exit 0; no changed protected path |
| `git diff --check` / staged whitespace check | PASS, unstaged/staged whitespace checks |

Behavior coverage: one profile/24 devices, same IP with distinct URLs, all device fields and exact escaped nested URL/custom port/query/fragment through save/open/fake launch; preserved IDs; both pending editors in Save as; failure retains original and draft; create/edit/delete confirmation; missing selection/cancel; HTTP/mismatch/unlinked flags; HTTPS/same-IP/linked case; stale editor reference blocked until relink/unlink; strict imported orphan rejection retained by existing tests; retained draft across failed validation/selection/New/Open/exit/search/group; same-named profiles form separate ID groups; profile rename updates search/group; redacted unsafe schemes/userinfo/encoded credential keys/control/whitespace; launcher exception redaction; process-boundary fake captures original FileName, UseShellExecute and empty Arguments/ArgumentList. No OS process is started by tests.

WPF surface assertions: 24 linked records, selected list row actually matches editor, selected command states, no discard during retention, declined selection restored, hidden draft survives filtering, clearing filter restores selection, grouping retains profile association, errors remain redacted, fake browser cancel/handoff, named form controls, fixed Open bounds, forward focus traversal from URL to Open, and window/application exit refusal with a device draft. English legacy `tr` fallback remains covered.

Visual inspection: regenerated normal/minimum Light/Dark captures compared to DESIGN.md; selected row/editor mismatch was found and fixed, empty text readable after adding left-pane scrolling, footer remains visible, destination/error/status wrap, lower profile/notes controls reachable by scrolling. Native Mica is composited over ApplicationBackgroundBrush in PNGs, not a desktop-backdrop certification. Programmatic focus traversal is not manual keyboard/AT certification.

NOT RUN: actual default-browser handler/navigation/certificate dialogs, endpoint reachability/HTTP/DNS/ping, live scanning/apply/revert/DHCP/UAC, real-NIC enumeration acceptance, native file/confirmation dialogs, full App/tray integration, manual keyboard/screen reader/high contrast/zoom, publish/clean-machine, CI and hardware failure tests. No unit cases skipped. No new SDK/dependency installation or real customer-data modification.

## Initial failures and corrections

- New smoke initially failed to compile because it attempted the internal store constructor; added a public browser-injection composition overload, with store injection remaining internal. Corrected smoke passes.
- Visual inspection exposed a SelectedValue/currency mismatch: panel 0 appeared selected while panel 23 was edited. A new actual SelectedItem assertion reproduced failure. Bind the presentation row explicitly with current-item synchronization disabled; final assertion, cancellation/filter/replacement scenarios and screenshots pass.
- At minimum height the initial empty/list content was clipped. Added left-pane scrolling with a bounded minimum body; the editor and footer retain existing layout/styles. Small viewports require scrolling.
- Initial device tests produced an unassigned-field warning in a fake; explicitly initialized the intended false default. Final build has no warnings. No test was weakened/skipped.
- An early combined shell write/build call was rejected by automatic tool policy before execution. Applied the same authorized scoped edits via structured apply_patch and separate commands. No blocked action remains.

## Review evidence

| ID | Kind | Status | Source / decision |
|---|---|---|---|
| P03-ARCH | codebase-explorer / solution-architecture | passed | Existing ProjectDevice/ProjectStore/VM guards/schema inspected; reuse model and controls, no new dependency or persisted format |
| P03-CODE | code-review | passed | Final changed source/data flows and tests inspected; actual selected row defect corrected, duplicate profile-name grouping made identity-aware. Self-review, no independent reviewer/subagent |
| P03-SEC | security-review | passed | URL policy unchanged; full gate before confirmation, same check at OS boundary, exact document/no arguments, no probes/apply/TLS bypass, redacted failures; fake boundary tests. Bounded review, not certification |
| P03-UI | design-system / visual-qa | passed | DESIGN.md, existing shared styles, 20 component captures and explicit row/footer assertions; localized addition with independently scrolling bodies |
| P03-A11Y | accessibility-review | inferred | Native controls/tabs, accessible names/live status, existing focus styles; programmatic forward traversal PASS. Manual keyboard/AT/high-contrast/zoom NOT RUN |
| P03-SOURCE | source-driven-implementation | passed | .NET 8 target/SDK inspected; Microsoft API contract checked for default association and nullable process handles; see sources below. Actual browser launch NOT RUN |
| P03-GATE | release-check | unavailable | NO-SHIP for merge/release: owner phase review pending; live/full-app/deployment evidence outside this authorized run. Implementation ready for owner review |

Source-driven question: how to hand a full URL to the Windows default association under net8.0-windows, and whether null Process means failure. `UseShellExecute=true` uses the OS document association, while API return can lack a new process handle. Retain exact input in FileName and do not infer endpoint success or report failure solely from null. Sources checked 2026-10-31: [Microsoft ProcessStartInfo.UseShellExecute (.NET 8)](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.useshellexecute?view=net-8.0), [Microsoft Process.Start (.NET 8)](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=net-8.0). Fake tests verify chosen local pattern; actual OS/browser behavior UNVERIFIED.

## Risks, deviations and next step

Remaining risks: external browser may normalize/navigation-redirect URL after handoff; endpoint/TLS behavior is untested; approved secret-key heuristic is conservative/incomplete; unbounded backup retention, hardware/filesystem guarantees, forced-termination draft loss, prior migration-marker attribution uncertainty and legacy persistence/history/apply limitations remain unchanged. The directory rebuilds/sorts presentation rows on search/edit; very large catalog performance is UNVERIFIED. Minimum window uses scrollbars.

Scoped refinements: group using existing type/profile fields instead of adding strict-v1 properties; retain import rejection of orphan references and offer explicit in-memory relink/unlink. Full URL preservation is proved through application-to-launcher handoff, not actual browser navigation. These are documented compatibility choices under ADR-015, not a schema change. No Phase 04 or later feature.

Next safe action: owner reviews this diff and Project/Devices UI, then decides Phase 03 acceptance. Proposed subsequent phase: Phase 04 — Apply & Open, beginning with the state machine, explicit adapter identity and requested/observed configuration/recovery specification. No advancement/merge/push/live tests without the corresponding owner authorization.
