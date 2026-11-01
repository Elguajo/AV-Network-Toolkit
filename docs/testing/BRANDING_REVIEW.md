# Product logo and application icon — BRAND-01 / ADR-044

2026-11-01, Windows, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, branch main, bootstrap `e4bd0cbe152a581ab0a315f90801e42de344439f`. Concurrent packaging documentation/publication commits through `0a27b9fc2efd8c762ae189ca0c841b1a3e10f888` were preserved. Phase 07 remains AWAITING_REVIEW.

Owner requested the supplied image as the new logo/application icon and explicitly allowed ICO conversion. Original `Glossy Cyan-Blue Ribbon Lettermark.png` copied byte-for-byte to `app/Assets/logo.png` (1402 x 1122, 32-bit ARGB); no generated replacement or third-party artwork. PNG SHA-256: `795e9f3ac900bd3ba1c93b244502a1c125500d521897efd3593ba2be5833cd72`.

`scripts/update_brand_icon.ps1` converts this source with Windows System.Drawing into transparent square frames, preserving aspect ratio, centered with 2% padding on each horizontal edge. Sizes: 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 px. ICO SHA-256: `f8779c177d30721605870147d468aad35e5daf35686d1f8c4768d08eb0be3fb4`. PNG is also embedded as a WPF resource. Existing ApplicationIcon, main Window.Icon, title-bar ImageIcon, tray EXE/resource extraction and installer SetupIconFile bindings continue to use `Assets/icon.ico`; no runtime code or layout changed.

## Verification actually executed

Existing local tools; no installation:

```powershell
$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"
$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"
& scripts/update_brand_icon.ps1
& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/BrandingVerified/
& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --no-restore -p:OutputPath=bin/BrandingVerified/ --logger 'trx;LogFileName=branding.trx' --results-directory TestResults/Branding
& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/BrandingUi/
& $sdk scripts/ProjectUiSmoke/bin/BrandingUi/ProjectUiSmoke.dll TestResults/Branding/Ui
& $py -B scripts/validate_plan.py
& $py -B scripts/test_validate_plan.py
git diff --check
```

- Both Release builds PASS, 0 warnings/errors. Full isolated suite **681 PASS / 0 failed / 0 skipped**. TRX SHA-256: `71cf5ae68657d9691d8b33f15564aa241c8b30bb0c6f3ede9b4f65b3127444c7`.
- **128 WPF captures / 0 binding errors**, Light/Dark, normal/minimum; injected fake network/apply/browser boundaries, no App startup. Inspected actual dark main-window capture and dark/light logo + extracted EXE icon preview under `TestResults/Branding`.
- BinaryReader parsed all 10 ICO entries; System.Drawing decoded every PNG frame: expected dimensions, nonempty visible pixels, transparent corners PASS. Source/download SHA equality PASS. Repeat conversion produced identical ICO SHA PASS. Icon.ExtractAssociatedIcon on the built EXE PASS, visually inspected.
- Planning/schema/link validation and 12 tooling regressions PASS; whitespace PASS (final run recorded in STATUS).

Native full App/tray/Explorer icon cache/high-DPI/manual accessibility NOT RUN. Tray and installer use the verified existing resource paths, but installer/ZIP candidates built before this change retain their older icon until rebuilt. No new packages, publication, live NIC/probes/UAC/browser tests or phase acceptance claimed. Network/storage/JSON/history/scanner/apply code unchanged in this scope.

Next safe step: review the asset diff/actual capture; future package builds will consume the new icon. Branding commit lookup: `git log -1 --format=%H -- app/Assets/logo.png`.
