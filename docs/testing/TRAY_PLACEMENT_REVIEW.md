# Tray popup placement — UI-TRAY-01

2026-11-01, Asia/Tashkent; https://github.com/Elguajo/AV-Network-Toolkit, main, source base `f7720e9`. Phase 07 remains AWAITING_REVIEW. Owner clarified that the menu containing Exit must appear above the tray icon; shutdown behavior is outside this fix.

## Cause and correction

`TrayPopupWindow.PositionNearTaskbar` always used the primary `SystemParameters.WorkArea` bottom-right corner, ignoring the icon that was clicked. The screenshot shows this displaced menu beside the hidden-icon panel.

Capture the cursor's screen position once when the tray menu is constructed. Choose that point's monitor work area; convert screen pixels to WPF DIPs using the window's composition transform. Center above the click, prefer below if there is no room above, and clamp to that monitor's usable area. Retain the existing height cap, scrolling, styling and action/exit safeguards. Initial HWND placement moves to the anchor monitor before final rendered positioning; the final height cap uses its current transform.

No network/settings/storage/UAC changes or new dependencies. Concurrent adapter filtering and navigation research changes are preserved and excluded from the scoped tray commit.

## Verification

Private SDK: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`; Windows 11 build 26100, .NET SDK 10.0.403.

- `& $sdk test test/NetworkProfileManager.Tests.csproj -c Release --filter FullyQualifiedName~TrayPopupPlacementTests --no-restore --nologo`: **9 PASS**, 0 failed/skipped. Hidden-panel anchor, left/right/bottom/top edges, secondary monitor with negative coordinates, synthetic 100/150/200% scaling.
- `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release -- TestResults/TrayPlacement --tray-placement`: **PASS**, 6 Light/Dark captures, actual HWND screen position compared with the requested anchor and monitor bounds. All three action callbacks (including Exit), close-on-action and zero binding errors checked. App startup, OS network APIs and user data are not used.
- `& $sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=tray-placement.trx' --results-directory TestResults/TrayPlacement --nologo`: **723 PASS**, 0 failed/skipped, on the combined working tree including concurrent adapter-filter tests; not a count for the isolated tray commit alone.
- Private validation Python: `scripts/validate_plan.py` **PASS** (9 phases, 49 stable tasks, schema/formats, 506 links); `scripts/test_validate_plan.py` **12 PASS**; `git diff --check` **PASS**.

The Dark capture was visually inspected: existing menu layout and readable Open window / Settings / Exit retained. Geometry is verified through HWND coordinates, not inferred from the cropped image.

API grounding: Context7 `/dotnet/wpf`, official [pixel-to-DIP conversion](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/Shared/MS/Internal/PointUtil.cs) and [HWND DPI transitions](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs).

Actual Windows Explorer hidden-icon clicks, physical mixed-DPI monitors, auto-hidden/relocated taskbars and accessibility acceptance remain **UNVERIFIED**. Unit scaling/secondary-monitor fixtures do not certify those native cases. No release, push, phase advancement or installed-binary update is claimed.
