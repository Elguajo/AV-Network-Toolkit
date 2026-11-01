# Ready-made network tests — local review

2026-11-01, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, clean bootstrap `2ccf8cd668ab0e27ef98f93b4db87afaf6ec4c67`. ADR-041 / NT-01. Local implementation and evidence recorded; owner/native acceptance pending. Phase 07 stays AWAITING_REVIEW. Resolve the implementation commit using `git log -1 --format=%H -- app/Services/DiagnosticTcpService.cs`; STATUS.md pins it after commit.

## Outcome and changed surfaces

The existing expanding diagnostics section below profiles/manual settings is now Network tests. Choose a named test and read its description, explicitly select adapter/source and destination, then Run test. Four-request Ping is the default; Continuous Ping, one-port TCP and the original single-request Ping are available. No shell or command typing. Opening, expanding, changing a preset and refreshing send no probes. A running suffix remains visible when the section is collapsed.

The existing separate window uses the same view model/panel and descriptive selectable cards. Its independent operation can be stopped/closed without cancelling a main-window run. Run/Stop/Close stay fixed at the bottom at minimum size. The main block uses a compact ComboBox. TCP shows a port field, 80/443 shortcuts and associated validation. Changing tests clears the previous outcome to prevent an old Ping result being mistaken for a TCP result.

Production TCP uses one IPv4 source-bound socket and one resolved endpoint, a three-second connect budget and ten-second service budget, no application payload or endpoint/interface retries. It preserves requested adapter/source/target/port, OS-predicted route and observed outcome. Accepted, refused, timed out and unreachable are distinguished; unknown exceptions are redacted. Identity/route changes make the outcome unverified. Actual egress and Windows socket behavior still require isolated native acceptance. [Contract and authoritative .NET 8 sources](../spec/NETWORK_TEST_PRESETS_CONTRACT.md).

Continuous Ping has one operation at a time, about one second between completed attempts, cancellable delay and a 100-row result limit. Attempt/reply/missing-response/RTT aggregates cover the whole run; these are diagnostic attempts, not packet-capture counters. Stop preserves the summary; disposal cancels future work and drops late UI results. Hiding either window cancels its active test, including the existing main close-to-tray behavior. Existing source/route checks, TTL/RTT, facts/routes and optional Hyper-V filter remain.

Changed path groups:

- `app/Models/DiagnosticTestPreset.cs`, `app/Services/DiagnosticTcpService.cs`: fixed preset catalogue and immutable TCP results; small injectable service and production connector.
- `app/ViewModels/AdapterDiagnosticsViewModel.cs`: selected preset/port, guarded Run/Stop, independent peers, bounded continuous results/summary and discoverable active header.
- `app/Views/AdapterDiagnosticsPanel.xaml`, `PingWindow.xaml`, `PingWindow.xaml.cs`, `app/MainWindow.xaml`, `MainWindow.xaml.cs`, `app/Strings/En.xaml`: localized shared UI, existing semantic styles, cards, fixed footer and stop-on-hide without changing tray behavior.
- `test/NetworkTestPresetTests.cs`: **50 added cases**; `test/AdapterDiagnosticsTests.cs`: correct fake read/prediction counters so they count calls even without a callback. No production platform changes.
- `scripts/ProjectUiSmoke/DiagnosticsSmoke.cs`: real WPF selector/card/port/error/keyboard/expansion/stop/close regression coverage with fake OS/network boundaries.
- ADR/task/context/design/status/contract/review/test evidence and the original concept plus selected actual captures. No schema, persistence or migration changes.

## Exact verification

Windows, PowerShell 7, existing .NET SDK 8.0.427. From repository root:

`$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`

`$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`

The system PATH `dotnet --version` reported no SDK; the established local SDK above was used, without installation or environment changes.

| Command | Observed result |
|---|---|
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/NetworkTestsFinal/ --filter 'FullyQualifiedName~DiagnosticTcpTests\|FullyQualifiedName~NetworkTestPresetTests\|FullyQualifiedName~DiagnosticPingTests\|FullyQualifiedName~AdapterDiagnosticsTests' --logger 'trx;LogFileName=presets-focused-final.trx' --results-directory TestResults/NetworkTests` | **97 PASS / 0 failed / 0 skipped**, build 0 warnings/errors |
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/NetworkTestsVerified/` | PASS, 0 warnings/errors on final source |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --no-restore -p:OutputPath=bin/NetworkTestsVerified/ --logger 'trx;LogFileName=presets-full-verified.trx' --results-directory TestResults/NetworkTests` | **681 PASS / 0 failed / 0 skipped**, final full regression, including repaired JSON concurrency cases |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/NetworkTestsUiTray/` | PASS, 0 warnings/errors, including final stop-on-hide integration |
| `& $sdk scripts/ProjectUiSmoke/bin/NetworkTestsUiTray/ProjectUiSmoke.dll TestResults/NetworkTests/UiTray --phase07` | **64 actual WPF captures / 0 binding errors**, Light/Dark, window 700×860/480×440, main 1200×850/760×520; actual main/window Hide→Show stops the fake continuous run without restarting. No native calls/probes |
| `& $sdk scripts/ProjectUiSmoke/bin/NetworkTestsUiTray/ProjectUiSmoke.dll TestResults/NetworkTests/UiRegressionTray` | **128 existing WPF captures / 0 binding errors**: project/device/apply/link-local/mDNS/main workflows, including final MainWindow event integration |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py`; `git diff --check` | Final actual working tree PASS 9 phases/49 tasks/schema/formats/351 links; **12 tooling PASS**; whitespace PASS, no gate weakening. Link count includes separately preserved concurrent documentation |

Protected-source comparison against bootstrap: `git diff --exit-code 2ccf8cd -- app/Services/DiagnosticPingService.cs app/Services/WindowsDiagnosticPingPlatform.cs app/Services/NetworkService.cs app/Services/ProfileRepository.cs app/Services/HistoryRepository.cs app/Services/ProjectJson.cs app/Services/ProjectApplyCoordinator.cs app/Services/ProjectApplyWorker.cs app/Services/ProjectApplyPolicy.cs app/Services/WindowsMdnsReceiver.cs app/Services/MdnsDiscoverySession.cs app/Services/LinkLocalAddressing.cs app/Services/BrowserLauncher.cs schemas app/app.manifest app/App.xaml.cs` — **PASS, no differences**. Full unit suite has no exclusions.

This comparison passed before concurrent REL-03 package work changed App.xaml.cs and added PackageSelfTest/packaging/scripts. Those changes and its separate planning hunks are excluded from the NT-01 commit; no package behavior is certified here. Concurrent distribution decision `a81f7d5` is preserved. Diagnostic authorization uses ADR-041 to avoid ADR-039/040 reserved for that separate work.

To verify the feature independently of the concurrently changing tree, exported its scoped staged Git tree `6257ca7552e7ed8e34f20a14a232d9579defaa81` with `git archive --format=zip --output=TestResults/NetworkTests/scoped-source-final.zip <tree>` and expanded into ignored `TestResults/NetworkTests/ScopedSourceFinal`. No branch/worktree and no external startup/package changes. `& $sdk restore TestResults/NetworkTests/ScopedSourceFinal/AVNetworkToolkit.sln --ignore-failed-sources` PASS with existing dependencies. From that snapshot root: `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/ScopedFinal/ --logger 'trx;LogFileName=presets-scoped-final.trx' --results-directory 'D:\DEV\NETWORKING\AV-Network-Toolkit\TestResults\NetworkTests'` — **681 PASS / 0 failed / 0 skipped**, build 0 warnings/errors, including final main/window stop-on-hide source. Subsequent changes are evidence metadata only. Scoped snapshot plan PASS 343 links and **12 tooling PASS**; final actual working-tree planning is recorded in STATUS.md. This validates the isolated feature source, not the other chat's package code.

## Failure/correction trail

- First focused run: **90 PASS / 4 FAIL**. The fake's `OnRead?.Invoke(++BindingReads)` and equivalent prediction callback did not increment counters when callbacks were absent. The new success assertions exposed this instrumentation defect. Moved counter increments outside optional callbacks; next focused run **94 PASS**. This is test instrumentation, not a repaired production network defect.
- An intermediate full run before the last three cases: **678 PASS**.
- First expanded full run: **680 PASS / 1 FAIL** in the new two-window test. It cancelled the peer before its Task.Run worker necessarily reached the fake connector, then incorrectly assumed a connector result existed. Added an explicit second-entry signal and atomic fake call counter so cancellation tests an established in-flight operation. No sleeps, suppression, production serialization or weakened assertion. Reviewed and final full runs both **681 PASS**. Captured TRX hashes are retained below.
- Actual visual inspection found stale Ping output after switching to TCP and excessive nested card padding. Cleared outcomes on preset change, added regression coverage and used one semantic card surface per item. Added a checkmark, associated port error text, scroll clipping and fixed footer; final focused captures passed. Final integration review found that main's existing close-to-tray path hides rather than disposes the window; added Stop on IsVisibleChanged for both owners and verified actual WPF Hide→Show with fake continuous probes. Tray behavior remains unchanged and invisible tests cannot continue.

[TRX outcomes and SHA-256](evidence/network-test-presets.txt) retain the failed and successful local artifacts. Full TRX files remain under ignored TestResults.

## Specialist review evidence

| id | kind | status | source / decision |
|---|---|---|---|
| NT-ARCH | codebase-explorer / solution-architecture | passed | Existing shared panel, immutable request/route and fake boundaries reused; no package, schema, worker or storage change. Contract above |
| NT-SOURCE | source-driven-implementation | passed | net8.0 Socket.Bind and cancellable ConnectAsync endpoint overload confirmed in primary Microsoft/.NET sources linked in contract; no hostname/multi-address connect fallback |
| NT-UNIT | testing | passed | 50 new behavioral cases, 97 focused PASS and final 681 full PASS; source/route/DNS/input/port errors, cancellation, peer isolation, disposal and bounded continuous history |
| NT-UI | design-code / visual-qa | passed | Approved concept and DESIGN.md compared to actual 64 final WPF captures. Compact inline selector and window cards, selected non-color cue, readable wrapped descriptions, error and scroll/fixed footer states |
| NT-A11Y | accessibility-review | passed | Focusable adapter/source/cards and Close, native ListBox selection semantics, named inputs/cards, port-associated HelpText/live validation; actual WPF harness checks. This is bounded automated evidence only |
| NT-CONCURRENCY | concurrency-review | passed | UI owner freezes request/port before first await; re-entry guards; Stop and dispose cancel only that owner; late output dropped; deterministic concurrent peer and continuous-delay tests |
| NT-SECURITY | security-review | passed | Untrusted host/source/port validated at service boundary; one explicit connection, no shell/process/URL launch/payload/credentials, fixed error categories, socket/linked-token disposal; reviewed changed code and negative tests |
| NT-CODE | code-review | passed | Scoped self-review of diff/callers/results/failure handling; observed stale-output issue fixed and covered; no remaining actionable defect found in this scope. Not independent owner acceptance |
| NT-NATIVE | native / manual AT / packet egress | unavailable | ADR-037: no isolated lab/native permission established; socket routing/cancellation, actual ICMP/TCP/DNS/driver/UAC/browser/full App/tray, screen reader/high contrast/scaling NOT RUN |
| NT-RELEASE | release-check | unavailable | **NO-SHIP**: owner/native/earlier release gates remain, no publication/release authorization. Local feature reviewable, not a release candidate |

No broad redesign or phase-order deviation. The extra single-request preset preserves baseline behavior. DNS/traceroute remain outside the approved first increment. Main minimum-size body/results and window body scroll; exact raster pixel identity and complete manual accessibility are not claimed.

## Actual captures and next action

- [Main, collapsed section](../design/network-tests/main-collapsed-dark.png)
- [Main, expanded TCP preset](../design/network-tests/main-expanded-dark.png)
- [Separate window, TCP ready](../design/network-tests/window-tcp-dark.png)
- [Minimum light window, invalid port](../design/network-tests/window-port-error-minimum-light.png)

These are component captures rendered with the actual WPF controls and fake data, not ImageGen output or live network results. The concept remains [separate](../design/network-tests-target.png). Owner should review the section placement/cards/fields and local diff next. No new phase is proposed for automatic execution; Phase 07 and prior native acceptance remain pending, Phase 08 requires its separate owner decision. No push, real NIC changes, traffic, UAC or external launch performed.
