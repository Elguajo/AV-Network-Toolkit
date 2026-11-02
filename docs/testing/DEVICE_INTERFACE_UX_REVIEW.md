# Device interface UX — UI-DEVICE-01 / ADR-058

2026-11-02, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit; main; bootstrap `3085ec6`, with existing independently owned diagnostic/profile edits. Owner approved both raster concepts and multiple-device behavior. [Approved contract](../spec/DEVICE_INTERFACE_UX_CONTRACT.md). Phase 07 remains AWAITING_REVIEW.

## Result and scope

Implemented a detached Add/Edit Fluent modal with required name/URL and optional profile/password/details disclosures. The directory now shows many-device navigation and one read-only selected card, explicit Open/Edit and linked Apply & Open, password Show/Copy and confirmed Delete through the named overflow menu. Successful retention selects the row, clears a hiding search and reminds the user to save the project file. Modal Cancel and validation failures preserve retained data; owners protect detached drafts on exit. Existing project v1/v2/password/backup/browser/apply/network contracts remain unchanged.

Changed product paths: `app/Views/ProjectDeviceDialog.xaml(.cs)`, `DevicePasswordFields.xaml(.cs)`, `DeviceDirectoryView.xaml(.cs)`, Project/Main XAML and owner-close guards, `app/ViewModels/ProjectViewModel.Devices.cs`, and device-only English strings. Test paths: `test/DeviceModalTests.cs`, `scripts/ProjectUiSmoke/DevicePasswordSmoke.cs` and two scoped Program hunks. Design/decision/task/status/reference files record this approved follow-up. No package/version/dependency, schema, OS network policy, updater, storage or unrelated diagnostic fix is introduced.

## Verification isolation and retained failures

The first shared-tree build failed on concurrently incomplete `IAdapterDiagnosticsDetails`; no unrelated repair. An initial new test fake omitted two required interface members; corrected before running tests. Initial safe verification used a git archive of `3085ec6` plus this task's source/strings/Program hunks: 72 focused and 930 full unit PASS. During work the adjacent offline-adapter task committed `de02b22a6db4ef2762fa687037213adc21f6d175`. Final isolated root `out/DeviceUiFinalVerification` is an archive of that commit plus the exact task source, excluding still-uncommitted diagnostic changes. Thirteen owned source/test files hash-match the working tree; En/Program contain only this task's hunks on the archived baseline. Ignored evidence: `TestResults/DeviceUi/source-isolation-hashes.json`.

Early UI failures were retained and corrected: the main-window fixture needed its explicit fake selected adapter; scroll checks exposed focus moving to a password field during Fluent expansion's intermediate render translation. The modal now uses arranged field geometry for its outer scroll viewport, including handled descendant requests. The harness waits for render ticks before asserting actual bounds. No assertion, URL/password validation, owner-exit or regression gate was weakened. Intermediate captures/logs remain in `TestResults/DeviceUi`; final results below supersede them.

Private SDK: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`, .NET SDK 10.0.403. Set process-local `DOTNET_ROOT` to its directory for the manifest-bearing WPF harness. Commands below ran from the isolated root unless stated otherwise; no ordinary App startup, NIC/scanner/listener/process/elevation/browser or actual clipboard boundary.

| Exact check | Result |
|---|---|
| `$sdk build AVNetworkToolkit.sln -c Release --no-restore` | PASS, 0 warnings/errors |
| `$sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=final-full.trx' --results-directory <repo>/TestResults/DeviceUi --logger 'console;verbosity=minimal'` | **954 PASS / 0 failed/skipped**, 27 s |
| `$sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-restore` | PASS, 0 warnings/errors |
| `scripts/ProjectUiSmoke/bin/Release/net10.0-windows/ProjectUiSmoke.exe <repo>/TestResults/DeviceUi/FinalFocusedUi --device-passwords` | **52 captures**, Light/Dark normal/minimum; 0 binding errors; actual New/Edit/Add/Show/Copy/Remove invocation, same-ID edits, cancel/owner-exit, many-device selection, linked apply and masked automation |
| Same executable `<repo>/TestResults/DeviceUi/FinalExistingUi`, no flag | **128 existing captures**, 0 binding errors; profile/device/apply/recovery/Link-Local/adapter/mDNS/workspace/focus/exit fixture regressions PASS |

Final isolated TRX SHA-256: `4481f34d5706f7a149f3c7fcb2942d182f561a93950648fbea41e75d3cda7ffd`.

### Combined current-main verification

After the other session committed diagnostic startup work as `dadb8bba5b1e1ad78c2b33c1fc6fd5756cbab380`, the shared application/test tree contained only this task's uncommitted source. Final combined verification includes both independently committed follow-ups; earlier isolated results above are retained as attributed evidence.

- Repository-root `$sdk build AVNetworkToolkit.sln -c Release --no-restore -p:OutputPath=bin/DeviceUi/`: PASS, 0 warnings/errors.
- `$sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-build --no-restore -p:OutputPath=bin/DeviceUi/ --logger 'trx;LogFileName=combined-full.trx' --results-directory TestResults/DeviceUi --logger 'console;verbosity=minimal'`: **977 PASS / 0 failed/skipped**, 27 s.
- `$sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-restore -p:OutputPath=bin/DeviceUiFinal/`: PASS, 0 warnings/errors.
- `scripts/ProjectUiSmoke/bin/DeviceUiFinal/ProjectUiSmoke.exe <repo>/TestResults/DeviceUi/CombinedFocusedUi --device-passwords`: 52 captures, 0 binding errors, PASS. Same executable with `CombinedExistingUi` and no flag: 128 captures, 0 binding errors, PASS. All boundaries remain fake.

## Rendered comparison and review

Compared [approved Add](../design/devices/add-device-target.png) and [selected card](../design/devices/device-details-target.png) to actual WPF output:

- [Actual Add, Dark](../design/devices/actual-add-dark.png): correct name/URL priority, descriptive optional rows and fixed Add/Cancel. All optional groups initially collapsed.
- [Actual details, Dark](../design/devices/actual-details-dark.png): selected row, exact URL, Open/Edit, compact profile/password status and visible file-save feedback. Absent optional details are hidden.
- [Actual main minimum, Dark](../design/devices/actual-main-minimum-dark.png): existing rail/adapter context retained; navigation narrows and actions wrap; body remains scrollable.
- [Actual password actions, Light minimum](../design/devices/actual-password-minimum-light.png): masked input and Show/Copy/Remove are within the body viewport while Cancel/Save remain fixed. No revealed-value capture.

Preserved actual product typography, semantic colors and header/file-toolbar patterns rather than tracing illustrative mock backgrounds. No pixel-identity claim; target's core hierarchy and interactions implemented. Named native fields/disclosures, name→URL traversal, default/cancel buttons, invalid-field focus, masked password automation and reachable actions exercised. Manual Narrator, high contrast and physical/mixed DPI are UNVERIFIED.

Code review by the implementing agent traced detached validation-before-mutation, stable ID/selection/filter handling, separate disk save, safe URL confirmation, unchanged apply handoff/redaction, password removal/clipboard ownership, reveal reset and owner-exit guards. No remaining blocking defect found in this scope; not independent reviewer certification. Existing out-of-scope profile audit risks remain in their own records.

## Limits and next action

Real network mutation/probes/UAC, browser launch, real clipboard/history, installation, private-data migration, hosted/public package and owner/native acceptance **NOT RUN / UNVERIFIED**. No push, public release, next phase or phase completion. Review this local UI with synthetic data first; all personal project save/version/password consent rules still apply. Commit/package outcome and final documentation checks follow in the session handoff.

Final combined TRX SHA-256: `c85024dbdce474451ec4d290902158f7b4a56d4710e5f08c352bca24590ca65a`. Private Python `-B scripts/validate_plan.py` PASS (9 phases, 49 stable tasks, schemas/formats and 973 links before final metadata); `-B scripts/test_validate_plan.py` PASS (12). `git diff --check` PASS, line-ending notices only.

## Exact local portable — f39f341

Implementation/source: **f39f34184585d101eb2f104a3f54cc43ea23445d**, committed on main after 0e55c99's documentation-only outcome. Clean working tree before archive/publish; all prior committed offline/diagnostic fixes included. Application/test source is unchanged from the final combined 977/180-check tree. Exported this exact commit with git archive into `out/DeviceInterfaces-f39f341/Source`; no other session's uncommitted files entered the package.

From the repository, private SDK command:

```powershell
& $sdk publish out/DeviceInterfaces-f39f341/Source/app/NetworkProfileManager.csproj -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -p:PublishReadyToRun=false -p:SourceRevisionId=f39f34184585d101eb2f104a3f54cc43ea23445d -p:InformationalVersion=1.1.6-local.devices -p:ReleaseChannel=Preview -o out/DeviceInterfaces-f39f341/Portable
```

PASS, 0 warnings/errors. MIT LICENSE and existing third-party notices/dependencies retained. Hidden `--package-self-test <absolute-new-json-path>` invoked with a hidden process, exits 0 before ordinary startup; Success/JSON/WPF true, assembly 1.1.6.0, informational version `1.1.6-local.devices+f39f34184585d101eb2f104a3f54cc43ea23445d`, .NET 10.0.14, Preview, no production updater key. No ordinary launch, migration, install or NIC/clipboard/browser action.

Ready EXE: `out/DeviceInterfaces-f39f341/Portable/NetworkProfileManager.exe` with adjacent dependencies. ZIP: `out/DeviceInterfaces-f39f341/AVNetworkToolkit-1.1.6-local-devices-f39f341-portable.zip`; extracted to a fresh ZipVerified folder, **all 8 files SHA-256-identical**. ZIP SHA-256 **ca319c502e9ba20441760d8c878caeedef587e8c499e7b2057dffbfd23f22e2a**. Ignored manifest/self-test JSON kept alongside. Existing local candidates, installed application, personal data and public 1.1.6 are unchanged.

Next safe action: owner exits the older app through tray Exit and opens the complete new portable folder, then reviews Add/Edit/select/Open with synthetic devices first. Save project file separately; existing password/v2/backup consent and deliberate network actions remain. Native/owner/phase acceptance remains pending. This outcome-only metadata does not alter package source or rerun/replace unchanged unit/WPF results.
