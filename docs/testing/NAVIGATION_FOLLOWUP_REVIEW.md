# Layout persistence, activity marks and native DPI — review

2026-11-01 (Asia/Tashkent). Fork https://github.com/Elguajo/AV-Network-Toolkit / main. Exact implementation source: **2356decf70f22a089db2a7a99339c99bc6ac956d**. ADR-050 / [contract](../spec/NAVIGATION_FOLLOWUP_CONTRACT.md). UI-PREF-01 and UI-ACTIVITY-01 locally verified; UI-DPI-01 partially verified because only one 96-DPI physical display is available. Phase 07 AWAITING_REVIEW; existing native/phase/publication gates persist.

## Implemented behavior

- Private version-1 window_layout.json in the current LocalAppData data directory. Changed normal size/monitor-relative position, last normal/maximized state and sidebar preferred width/collapse are saved on Hide/Close; startup and identical state do not rewrite it. Minimized dimensions are never used as restart geometry. Tray restore retains the last maximized state. Disconnected monitor/oversized/offscreen placement clamps to the current work area with existing minimum size retained.
- Existing app_settings/profile/history/project storage is not changed. Read/version/bounds/size errors preserve the original and block replacement; atomic saves reuse the existing revision/lease/backup writer. Conflicting instances and IO failures preserve original data, with no shutdown/network interruption. Noncritical failure can leave an older layout; backups grow only on changed saves and retain the existing manual-retention policy. No DATA-01 migration.
- Network tests badge covers inline and separate peers, including overlapping runs and closed peers with in-flight cancellation. It clears after the last operation actually drains. mDNS badge covers actual Listening, including a hidden listening window, and clears on Stop/Close. Existing VM/session boundaries and polling are reused; accessible status text accompanies the marks. No restart/automatic start/probe is introduced.
- Modern PerMonitorV2 manifest shared by app/harness. Maximize uses current monitor/DPI, rather than primary-screen work area. Actual monitor DPI, native HWND DPI and WPF DPI agree at 96, with per-monitor awareness confirmed. Global display settings were not changed.

Files: new WindowLayoutState/WindowScreenPlacement helpers, WindowLayoutStore, MainWindow Layout/Activity partials; MainWindow constructor/tool hooks, tray restore call, localized converter/styles/marks, two read-only VM state properties, manifest, focused storage and WPF checks. [Design](../../DESIGN.md), [source-backed DPI contract](../spec/NAVIGATION_FOLLOWUP_CONTRACT.md).

## Verification on the exact committed source

Used a fresh `git archive 2356decf70f22a089db2a7a99339c99bc6ac956d` export at `TestResults/UiNav2/Source-2356dec`, with private SDK 10.0.403. This avoids old WPF intermediate BAML observed in the reused working-tree output. `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`; set process-local `DOTNET_ROOT` to that directory for the harness EXE. Apphost manifest matters: invoking the DLL through dotnet does not validate that executable's DPI awareness.

| Check / exact command | Observed result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'trx;LogFileName=layout-activity.trx' --results-directory ../Pinned/Unit --nologo` | **736 PASS / 0 failed / 0 skipped**, including 13 new storage/geometry cases |
| `scripts/ProjectUiSmoke/bin/Release/net10.0-windows/ProjectUiSmoke.exe ../Pinned/Followup --navigation-followup` | **8 Light/Dark captures PASS**, actual new-window/tray/normal/max/min/offscreen restoration, overlapping/cancelled test + mDNS lifecycle, native 96-DPI equality/awareness, 0 binding errors |
| Same EXE, `../Pinned/Navigation --navigation` | **26 captures PASS**, hover/focus/keyboard/resize/scaling and native caption checks, 0 binding errors |
| Same EXE, `../Pinned/Workspace`, no switch | **128 existing captures PASS**, 0 binding errors |
| Same EXE, `../Pinned/Diagnostics --phase07` | **64 captures PASS**, fake probes only, 0 binding errors |
| Same EXE, `../Pinned/Hide --diagnostic-hide` | **20 gated Hide/Show cycles + 2 late-echo cases PASS** |
| Same EXE, `../Pinned/Tray --tray-placement` | **6 captures/action/placement PASS**, 0 binding errors |
| Exact source app EXE, `--package-self-test` with absolute create-only output path under TestResults/UiNav2/Pinned | PASS: WPF/JSON render, source 2356dec, version 1.1.4.0 / .NET 10.0.14; no ordinary App/network startup |

Total: **232 captures**, zero binding errors. Storage checks cover absent/read-only startup, round trip/backup/no-op, corrupt/empty/null/missing fields, future version, oversized file, competing instances, locked-file retry, nonfinite values and negative-monitor/96/144/192-DPI geometry. Native tests check actual monitor-relative position as well as size; minimizing/closing does not overwrite normal geometry. Simulation and pure geometry checks at 144/192 DPI are not native display evidence.

[Structured source/TRX/self-test/PNG hashes and native observations](evidence/navigation-followup-2026-11-01.json). Retained actual, unchanged synthetic WPF renders: [running test](../design/navigation/tests-running-dark.png), [listening mDNS](../design/navigation/mdns-listening-light.png). No real IPs/projects/passwords are used.

## Review findings and corrections

Self-review covered storage compatibility/failure handling, UI/lifetime ownership, cancellation ordering, native coordinate conversions and the rendered marks. Tray reopen originally always forced Normal; it now retains maximized state. Normal geometry is tracked independently, display lookup failure is nonfatal, and closed diagnostic peers are detached only after their outstanding call drains. No further actionable defect found in this scoped review; owner/native acceptance is separate.

Early runs are retained under TestResults/UiNav2: WPF-UI Button.Icon rejects arbitrary Grid, so composite icon/badge content uses Button.Content. A legacy dpiAware manifest entry produced WFO0003; using only the supported modern 2016 dpiAwareness entry builds cleanly, without suppression. A programmatic DPI attempt and DLL-hosted run remained System-aware and failed the native assertion; the helper was removed and final apphost/manifest checked directly. GUI self-test output was initially read before the WinExe finished; its final create-only success JSON was then observed and hashed. No test/assertion/warning gate was weakened.

## Remaining coverage

Actual hardware: one 2560x1080 display, 96 DPI (100%). Actual 150%, 200% and transfer between different-DPI monitors: **NOT RUN**, no matching displays and no global display-setting change. These remain UI-DPI-01 acceptance work; current-state tests/simulations must not close them. Actual touch/Narrator/native NIC/UAC/driver acceptance remains separate; no real traffic or profile mutation was performed. Existing published release/installer/portable candidate remain unchanged, including their older source identity.
