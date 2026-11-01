# Window theme synchronization — UI-THEME-01

2026-11-01, Asia/Tashkent. Owner requested repair after a Project/Devices screenshot showed black text and light inputs on a dark background. Phase 07 remains AWAITING_REVIEW. Source base: `e35971ad5f80721064e082cfb97de646b5672b30`, main, https://github.com/Elguajo/AV-Network-Toolkit. This follow-up changes window appearance synchronization and its isolated render harness only; no network, data, UAC, layout, dependency or release change.

## Causes and reproduction

Two reproducible paths produced the mismatched appearance:

1. The render harness replaced ThemesDictionary directly. WPF brushes became Light while ApplicationThemeManager still cached Dark. A newly opened ProjectWindow reported foreground `#E4000000`, transparent background, cached theme Dark and actual HWND immersive-dark attribute `1`. This reproduces the screenshot's color combination; it does not establish which interaction produced the owner's particular screenshot.
2. Production refresh skipped owned FluentWindows. WPF-UI updates native backgrounds only one ownership level deep and leaves an owned window's opaque fallback brush untouched. A root → Project → Settings fixture, with the library's RemoveBackdrop called on Settings before changing Dark to Light, retained background `#FF202020` and native-dark `1` with light-theme foreground `#E4000000`.

The [WPF-UI 4.2.0 theme manager](https://github.com/lepoco/wpfui/blob/4.2.0/src/Wpf.Ui/Appearance/ApplicationThemeManager.cs), [owner update](https://github.com/lepoco/wpfui/blob/4.2.0/src/Wpf.Ui/Appearance/WindowBackgroundManager.cs) and [fallback restoration](https://github.com/lepoco/wpfui/blob/4.2.0/src/Wpf.Ui/Controls/Window/WindowBackdrop.cs) explain these independent brush/native states.

## Correction

App.RefreshWindowThemes refreshes every open FluentWindow, including owned dialogs and deeper descendants, through the existing WPF-UI background manager. System/Light/Dark selection and existing palette/Mica remain unchanged. The harness now switches themes through ApplicationThemeManager.Apply and the same application refresh, rather than replacing the color dictionary independently.

WindowThemeSmoke checks Dark → Light → Dark → Light, opaque fallback reset, already open root/project/nested settings and freshly opened nested Project windows. It verifies WPF foreground/background and reads DWMWA_USE_IMMERSIVE_DARK_MODE from real HWNDs on Windows 11. Eight unique PNGs cover Project profiles, Devices at normal/minimum dimensions and Settings in both themes. PNGs composite the native backdrop as existing captures do; actual native theme agreement is checked separately, not inferred from PNGs.

Retained minimum-size Devices captures: [Light](evidence/window-themes/light.png), [Dark](evidence/window-themes/dark.png). Both visually inspected: readable labels, normal field surfaces and reachable footer actions. Layouts are preserved.

## Observed verification

Environment: Windows 11 build 26100, SDK 10.0.403, current .NET 10 target (the runtime migration belongs to concurrent audit remediation, not this fix).

Use `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` from the repository root:

- `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release -- TestResults/WindowThemes --themes`: PASS, 8 unique captures, 0 binding errors; actual native attributes agree with text.
- Causal red control: isolated git-archive of e35971a plus this harness/extracted refresh, restoring only the original `w != Current.MainWindow && w.Owner == null` predicate: FAIL, `ThemeSettingsWindow: stale opaque background after theme change`. Corrected predicate passes the same test. Export under `%TEMP%/av-theme-red-*`; no checkout reset or user-file overwrite.
- `& $sdk test AVNetworkToolkit.sln -c Release --no-restore --logger 'trx;LogFileName=window-themes.trx'`: 687 PASS, 0 FAIL, 0 skipped. Release app/test build succeeded with no warnings/errors observed.
- `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-build -- TestResults/WindowThemesFull`: PASS, 128 captures, 0 binding errors, fake NIC/apply/socket/elevation/browser boundaries.
- `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-build -- TestResults/WindowThemesAudit --audit-fixes`: PASS, 8 captures, 0 binding errors.
- `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-build -- TestResults/WindowThemesDiagnostics --phase07`: FAIL after 9 captures, `DiagnosticsSmoke.cs:201`, `Hidden diagnostic window continued sending`. Previously recorded DOC-R01 failure; not suppressed or repaired in theme scope.
- Planning/schema/formats/local links: `python scripts/validate_plan.py` PASS (458 links); `python scripts/test_validate_plan.py` 12 PASS, using the private validation venv. `git diff --check` PASS.

Focused self-review with code-review found no additional material defect in this diff. Full phase/native networking/real device/UAC tests NOT RUN. Manual System theme events, high contrast, other Windows builds/drivers and the exact original desktop interaction remain UNVERIFIED. No installed binary or published release was replaced.
