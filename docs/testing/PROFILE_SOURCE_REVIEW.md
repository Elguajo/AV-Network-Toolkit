# Profile source placement — UI-SOURCE-01, 2026-11-01

Owner reported that This computer looked misplaced beside the saved-profile heading. On main, base `57ff73fe47b98aaccfa4941da88bbdc9e4da13d8`, move the existing selector into a separate, visibly labelled Profile source row above the list heading/actions, in both local and project scopes. Reuse existing caption/theme/control resources and associate the visible label through AutomationProperties.LabeledBy. No selection, storage, network, dependency or phase change.

Concurrent uncommitted navigation/adapter-panel work was present at bootstrap and remains independent. Combined-tree screenshots include that navigation; this task does not implement or accept it. The scoped commit contains only the two profile panels, the `--workspace` harness entry and coupled documentation. Resolve its SHA with `git log -1 --format=%H -- docs/testing/PROFILE_SOURCE_REVIEW.md`.

## Verification

PowerShell, private SDK `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` (10.0.403). All component boundaries fake; no App startup, real network enumeration/probes/mutation, elevation or browser launch.

| Command | Observed result |
|---|---|
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/ProfileSource/ -p:IntermediateOutputPath=obj/ProfileSource/ --nologo` | PASS, 0 warnings/errors |
| `& $sdk scripts/ProjectUiSmoke/bin/ProfileSource/ProjectUiSmoke.dll TestResults/ProfileSourceWorkspace --workspace` | PASS, 44 Light/Dark 1200x850/760x520 captures, 0 binding errors; source round trip with four profiles retained in each store, source → Import → Export → Add keyboard order, five-slot limit, apply target/draft/exit/menu guards |
| `& $sdk scripts/ProjectUiSmoke/bin/ProfileSource/ProjectUiSmoke.dll TestResults/ProfileSourceFullFinal` | PASS, full existing component suite, 0 binding errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/ProfileSourceUnit/ -p:IntermediateOutputPath=obj/ProfileSourceUnit/ --logger 'trx;LogFileName=profile-source.trx' --results-directory TestResults/ProfileSourceUnit --nologo` | 723 PASS / 0 failed / 0 skipped |

Visually inspected actual local Dark normal, local Light minimum and project Dark normal captures. Label and selector form one clear source-selection group; the heading/actions form the next group. The narrow toolbar wraps using the existing responsive style. Source and actions remain reachable via scrolling and keyboard. [Actual normal local capture](../design/profile-source/local-dark.png), [actual minimum Light capture](../design/profile-source/local-minimum-light.png). These are safe synthetic fixture renders, not native Mica/assistive-technology certification. UI automation label association was inspected in XAML; manual screen-reader/high-contrast/mixed-DPI checks NOT RUN.

Retained failures/limits: first attempt used the old private .NET 8 tool root and failed SDK resolution before compilation; corrected to dotnet10. A build using the shared default obj directory hit CS2012 file locking; isolated IntermediateOutputPath then passed. Before this change, `--ux-after` failed `Empty-state refresh did not use the fixture adapter source`; no fix in this scope. First after-change full run stopped on unrelated `Forward focus traversal did not reach Open Web Interface` in ProjectWindow; cause UNVERIFIED. The final full run passed with unchanged product code; no assertion removed. Baseline full suite had also passed. These failures are not claimed fixed by source placement.

Phase 07 remains AWAITING_REVIEW; native/owner acceptance, DATA-01 and Phase 08 unchanged. Existing executable/packages/releases were not replaced. User can review the actual layout captures before any new packaging/publication work.
