# Adapter-list options placement — UI-ADAPTER-OPTIONS-01

2026-11-01, Asia/Tashkent; https://github.com/Elguajo/AV-Network-Toolkit, main, base `3fd92a2`. Owner reported that the separate Show all adapters row was visually and logically misplaced. Reuse the existing subtle More button/context-menu pattern: header, Refresh and More in one row, Show all adapters as a checkable menu item. Preserve its original help text and session-only ShowAllAdapters binding. Menu data context comes from its placement target; opening it changes no settings. No new library, palette, persistence, network operation or phase advancement.

Concurrent navigation and adapter-tooltip edits remain outside this commit. Combined-tree captures include that independent navigation. Resolve this task's commit with `git log -1 --format=%H -- docs/testing/ADAPTER_OPTIONS_REVIEW.md`.

## Observed checks

Private `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` (10.0.403), PowerShell:

| Command | Result |
|---|---|
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/AdapterOptions/ -p:IntermediateOutputPath=obj/AdapterOptions/ --nologo` | PASS, 0 warnings/errors |
| `& $sdk scripts/ProjectUiSmoke/bin/AdapterOptions/ProjectUiSmoke.dll TestResults/AdapterOptionsVerified --adapter-visibility` | PASS, 20 Light/Dark normal/minimum captures, 0 binding errors |
| `& $sdk scripts/ProjectUiSmoke/bin/AdapterOptions/ProjectUiSmoke.dll TestResults/AdapterOptionsFull` | PASS, 128 existing component captures, 0 binding errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/AdapterOptionsUnit/ -p:IntermediateOutputPath=obj/AdapterOptionsUnit/ --filter 'FullyQualifiedName~AdapterVisibilityTests\|FullyQualifiedName~MainViewModelTests' --logger 'trx;LogFileName=adapter-options.trx' --results-directory TestResults/AdapterOptionsUnit --nologo` | 25 PASS / 0 failed / 0 skipped; pipe is literal, unescaped in the actual PowerShell filter |

The focused harness opens the actual menu, checks its view model and accessible names, focuses the header button and item, invokes the item through UI Automation, reopens it and verifies the retained checkmark. Showing all retains the selected model/draft and performs no refresh; hiding the selected system interface clears selection without choosing another target. Existing refresh/filter unit cases pass. Full unit suite NOT RUN again for this presentation change; the preceding source-placement task recorded 723 PASS.

Visually inspected header and checked popup in both themes, normal/minimum. [Dark header](../design/adapter-options/header-dark.png), [checked menu](../design/adapter-options/menu-dark.png). Popup screenshots render its actual presentation root after the standard opening animation; rendering the ContextMenu alone initially produced clipped/compressed artifacts, so those captures were rejected. No product animation/theme was changed. Existing WPF/Mica capture and manual screen-reader/high-contrast/mixed-DPI limits apply. Tests inject fake OS/browser/IO boundaries; native startup/traffic/network mutation/UAC NOT RUN.

No package/EXE/release updated. Phase 07 remains AWAITING_REVIEW; native/owner acceptance, DATA-01 and Phase 08 unchanged. Owner can review the actual placement captures.
