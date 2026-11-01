# UI-NAV-01 — left navigation implementation and review

2026-11-01 (Asia/Tashkent). Fork: https://github.com/Elguajo/AV-Network-Toolkit, main. Exact implementation source: **7d62a26a376d7216f7c317d1e68759b91b10ade3**. ADR-049 authorizes local implementation and the owner's later hidden-scrollbar/hover-highlight refinement. Phase 07 remains AWAITING_REVIEW; no new publication/native-network/phase acceptance.

## Result and boundaries

- Persistent 52-unit icon rail with 36-unit action targets, explicit 20-unit existing symbols and Settings fixed at the bottom. Existing tool windows/availability remain; Link-Local is retained. Adapters is the current workspace, and launching a tool does not falsely select another page.
- Stateful title-bar sidebar toggle and Ctrl+B. Ctrl+1..5 retained. Standard ToggleButton/RadioButton peers expose toggle/current state and names; keyboard focus is visible and Tab follows visual order.
- Adapter sidebar starts at 240, resizes within 200–420 and clamps against a 500-unit workspace budget at the existing 760x520 minimum. Preferred width remains in memory; shrink/restore and resizing while hidden retain it. Selection, drafts, tab/source, scroll and running diagnostics survive collapse. The same list and view models remain alive.
- Scrollbar and resize divider are invisible at rest. List hover/focus reveals its scrollbar; divider hover/focus reveals a 2-unit accent line, while drag uses 3. A 6-unit transparent hit region, SizeWE cursor and localized drag/arrow-key guidance make resizing discoverable. Wheel and keyboard scrolling stay available; the workspace's duplicate permanent left stroke was removed.
- Localized resources, shared semantic styles and name/description tooltips. No dependency, schema, storage-path, network service, apply/recovery/UAC, version or package change. Source-selector 3fd92a2 and adapter-menu 1b5eb5e changes from parallel sessions are preserved as separate commits.

Implementation paths: [MainWindow markup](../../app/MainWindow.xaml), [view-only navigation state](../../app/MainWindow.Navigation.cs), existing constructor hook, [shared styles](../../app/Themes/Styles.xaml), [English strings](../../app/Strings/En.xaml), adapter tooltips, and the [navigation component checks](../../scripts/ProjectUiSmoke/NavigationSmoke.cs)/harness dispatch. Contract: [brief](../spec/LEFT_NAVIGATION_BRIEF.md), [source research](../spec/LEFT_NAVIGATION_RESEARCH.md), [DESIGN](../../DESIGN.md).

## Actual WPF evidence

These are unchanged PNGs captured from the exact committed source, synthetic adapter/project data, English UI and fake networking. Native Mica is composited over the existing application surface by the established harness; this is not a pixel-identical Codex recreation or a desktop-backdrop certification.

| State | Retained capture |
|---|---|
| Dark, idle scrollbar/divider hidden | [Idle](../design/navigation/idle-dark.png) |
| Same layout, real pointer over resize target | [Hover highlight](../design/navigation/hover-dark.png) |
| Light, sidebar collapsed while rail stays visible | [Collapsed](../design/navigation/collapsed-light.png) |

[Structured source/artifact hashes and results](evidence/left-navigation-2026-11-01.json). Other generated captures remain locally under `TestResults/UiNav/Pinned`.

## Verification commands and observed results

Exported immutable source with `git archive 7d62a26a376d7216f7c317d1e68759b91b10ade3` to `TestResults/UiNav/Final-7d62a26`. Commands below ran there with `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` (SDK 10.0.403). No App.Run/OnStartup, production NIC/scan/listener/probe, elevation or browser/device action.

| Command | Observed result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'trx;LogFileName=ui-nav.trx' --results-directory ../Pinned/Unit --nologo` | **723 PASS / 0 failed / 0 skipped** |
| `& $sdk scripts/ProjectUiSmoke/bin/Release/net10.0-windows/ProjectUiSmoke.dll ../Pinned/Navigation --navigation` | **26 captures PASS / 0 binding errors** |
| Same harness, output `../Pinned/Workspace`, no switch | **128 existing captures PASS / 0 binding errors** |
| Same harness, output `../Pinned/Adapters --adapter-visibility` | **20 captures PASS / 0 binding errors**, including independently changed Show all menu |
| Same harness, output `../Pinned/Diagnostics --phase07` | **64 captures PASS / 0 binding errors**; no real traffic |

Navigation assertions cover default/collapsed/wide/minimum/empty states; real UI Automation toggle and current-item patterns; keyboard order; divider arrow resizing, routed Thumb drag at both limits and Escape cancellation; preferred width after window shrink, hidden resize and restore; retained adapter/drafts/scopes/list offset; no implicit refresh/selection or probe; existing project-window activation and fake diagnostics peer; disabled Network tests with no adapter; and an explicitly started fake continuous test remaining active through sidebar hide/show.

Real pointer movement inside the fixture verifies idle/hover/exit of the divider and scrollbar and SizeWE cursor; cursor position is restored in finally. Native HWND WM_NCHITTEST verifies caption versus toggle hit regions, and caption double-click messages maximize/restore the actual WPF window. Theme checks cover Light/Dark, switching an existing window and the library's HighContrast resources. 150%/200% are **layout-transform simulations**, without changing Windows DPI; actual per-monitor DPI is separate.

## Review and red-run history

Self-review using code-review and visual-qa, with actual Light/Dark/minimum/hidden/hover/high-contrast renders. Early checks found Settings outside the intended rail Tab order: declaring a docked footer before the scrolling top actions did not give the expected traversal. The focused regression failed until the rail used a top-first Grid with an Auto footer and local keyboard grouping. Explicit icon FontSize corrected undersized toolbar glyphs. Final current/hover/focus/disabled states retain semantic resources and standard peers. No further actionable defect found in this scoped review; this is not independent owner acceptance.

An early harness compile used a nonexistent fake counter name (`Connections` instead of existing `Connects`); corrected before any successful focused run. During source isolation, a zero-context partial patch initially inserted localized strings after the XML root because an excluded parallel hunk shifted line numbers. That temporary export failed compilation; the three shared files were reconstructed from the base with explicit scoped replacements. The final verification uses an unmodified git-archive export of 7d62a26, not that temporary copy. No assertion, warning gate or acceptance requirement was weakened.

The implementation itself sends no traffic. The focused fixture explicitly starts one fake continuous echo solely to prove sidebar visibility preserves an ongoing operation; Stop remains deliberate and drains it. No native networking was run.

## Remaining acceptance and manual check

Locally verified implementation; existing Phase 07/native/OS and UX-R01/P05-R01 acceptance remains pending. DOC-R01's synchronization repair is preserved. Narrator/manual assistive technology, actual mixed-monitor DPI/touch interaction, and native NIC/UAC/driver behavior are NOT RUN. Prior installer/portable artifacts and published release have not been rebuilt by this task; a source build is not a newly published package.

Manual check: hover the adapter list and the otherwise invisible boundary, drag to resize, then hide/reveal with the top button or Ctrl+B. Confirm the selected adapter/draft remains and Settings stays reachable at minimum size. Width intentionally resets to its default on a new application window; restart persistence is outside this increment.
