# Reference toolbar and current-profile state — 2026-11-01

Base main `f7346833c7f8b21af4a73741a257852287679cdf`, clean bootstrap. Owner requested visually matching Import / Export / Add Profile, then the highlighted current row. The owner explicitly chose: show Selected when the profile matches the current adapter, as a status, not when a row is merely clicked. ADR-031/032; bounded IDEA-04 follow-up, Phase 06 still AWAITING_REVIEW.

## Implemented behavior and design

- Shared WPF-UI toolbar styles: 100×40 Import/Export, 150×40 Add, 15/16-point labels, contour tray-arrow icons, 20-high divider and reference spacing. Dark blue fill `#2E5E8D` and border `#3F74A1` were sampled from the approved [toolbar target](../design/compact/toolbar-target.png). Existing SymbolIcon ShareIos/Add glyphs are used; no borrowed external assets.
- The owner-directed component palette is scoped to these actions/current rows; other accents/buttons remain system-themed. Existing theme refresh updates the palette. Light links are darker; high-contrast mapping uses system text/highlight brushes. Wide headers keep actions at right; narrow headers wrap the intact group below the heading instead of squeezing labels.
- A matching row has a blue surface/outline, checkmark, stronger name and Selected status. Current configuration is shown where space permits. Selected is disabled/read-only; explicit reapply remains in the profile's named “…” menu. Project Edit/Delete retain their handlers in that menu; existing local editing/deleting/hotkeys/tray behavior remains.
- A pure display converter compares reported static IPv4 address, subnet prefix and gateway. Explicit DNS checks both primary and secondary, including removal of a secondary server. Legacy blank primary and project leave-unchanged DNS do not require a DNS change. Unknown DHCP mode, a known non-manual address, mismatched/unknown required DNS and unobservable automatic-DNS policy leave Apply available. DHCP profiles are not marked Selected: AdapterInfo cannot establish their DNS-source policy.
- The same comparison applies to local and project rows; Info change notifications refresh it after adapter updates. Changing the edited profile does not change current status. Identical presets can both match; the UI does not invent a unique last-applied profile ID or persist selection. The legacy IsActive/tray comparison is unchanged. Display matching is not worker verification, a reachability assertion, or a supported-state expansion.

## Actual control captures

| Artifact | Evidence |
|---|---|
| [Toolbar dark](../design/profile-states/toolbar-dark.png), [light](../design/profile-states/toolbar-light.png), [five-slot disabled](../design/profile-states/toolbar-disabled.png) | Live WPF actions rendered at 96 DPI |
| [Current row target](../design/profile-states/current-row-target.png) | Owner-supplied generated reference, documentation IP only |
| [Current row dark](../design/profile-states/current-row-dark.png), [minimum light](../design/profile-states/current-row-minimum-light.png) | Reported matching state, disabled Selected and available menu |
| [Project workspace](../design/profile-states/project-dark.png) | Matching behavior shared by portable profiles |

These are RenderTargetBitmap/VisualBrush captures of actual controls with synthetic data, composited over the existing surface. Geometry, palette, contour/plus roles and spacing were compared against the reference. Raster texture, font rasterization and DPI rounding are not pixel-identity guarantees; actual desktop/native accessibility remains unverified. The closeups are direct WPF visual renders, not generated replacements.

## Exact verification

`$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"` (8.0.427); `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. Windows / PowerShell, repository root.

| Command | Result |
|---|---|
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/ToolbarUnit/ --filter "FullyQualifiedName~ProfileCurrentConverterTests|FullyQualifiedName~ProfileViewModelTests|FullyQualifiedName~ThemeRefreshTests" --logger "trx;LogFileName=profile-states.trx" --results-directory TestResults/ToolbarUnit` | PASS 45/0 failed/0 skipped, including 11 new display-match cases |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/ToolbarUnit/ --logger "trx;LogFileName=profiles-final.trx" --results-directory TestResults/ToolbarUnit` | Final PASS 576/0 failed/0 skipped; does not resolve historic JSON nondeterminism |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/Toolbar/` | PASS, 0 warnings/errors |
| `& $sdk scripts/ProjectUiSmoke/bin/Toolbar/ProjectUiSmoke.dll TestResults/ProfileStatesReviewed` | PASS 128 captures, 0 binding errors, Light/Dark normal/minimum |
| `& $sdk scripts/ProjectUiSmoke/bin/Toolbar/ProjectUiSmoke.dll TestResults/ProfileStatesFocused --ux-after` | PASS 12 captures, 0 binding errors; actual modal Add/Keep, save and draft/error/close |

The real-control harness additionally checks: live palette changes on an existing window, system-brush high-contrast mapping (not actual OS high contrast), disabled Add at five slots and re-enable at four, source→Import→Export→Add focus order, both stores' disabled Selected, gateway mismatch/restoration, editing selection independence and explicit project reapply through the fake boundary. Existing target routing/save/conflict/draft/recovery/exit checks remain. No NIC query/change/probe/scan/socket/elevation/browser/DHCP/native file dialog/App startup.

Initial findings corrected: a test fixture attempted to mutate an init-only project record; the initial full suite failed 575/1 in StringResourceTests because dotted palette keys looked like localized strings. Palette keys were renamed to the established non-dotted Brush convention (including one remaining project tick key on the next run); the string gate was not changed. Failed TRXs are retained under TestResults/ToolbarUnit. Visual review corrected the glyph shape, explicit content font sizing, and native disabled rendering so Selected remains readable as status. No schema/test suppression or weakened assertion.

Focused code/design/accessibility self-review covered the scoped styles, display converter, Info notifications, theme hook, command/menu routing and tests. Native hover/press, full screen-reader/high-contrast/scaling/full-app/isolated NIC acceptance NOT RUN. Full native usability/visual acceptance still needs owner review; this is not independent certification.

## Gates and next step

JSON/schema/worker/coordinator/NetworkService/legacy persistence source paths are unchanged. OPEN UX-R01/P05-R01 and its [preserved rejection failure](evidence/ux-json-failure.txt) remain release-blocking; a green 576-case run is not a diagnosis or fix. DHCP/automatic-DNS current indication needs richer observed facts, not a guessed Selected state. Password/support work remains separate. No phase acceptance/advancement, network test, push or release. Next safe action: owner compares the actual toolbar/current-row captures and reviews the remaining native/JSON/support work separately.
