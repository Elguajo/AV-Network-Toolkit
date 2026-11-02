# 1.1.6 Preview — combined release verification

2026-11-02, Asia/Tashkent. REL-08 / ADR-054. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, inspected clean source `d16f2764bbb9ed0ba6cc88cc3567471c39740714`. All three concurrent sessions completed before release edits or the combined run. Phase 07 remains AWAITING_REVIEW.

## Boundary and decision

Owner requested waiting for other sessions, one combined error/defect check, a new version and GitHub release. Include completed updater bebaa10, device passwords b837ec6/aef78a9 and home storage 0da74c6/8cbf88e/0905d89/d16f276. Previous published source is corrected v1.1.5 at 9e06c48. Prepare new 1.1.6 Preview; preserve all existing releases/tags/assets and ordinary main history. No production signing keys, personal-data migration, workstation installation, real network tests, phase acceptance or Phase 08.

Independent review traced ordinary startup before settings, immutable data/recovery roots, root-loss guards, v1/v2 save/overwrite/consent paths, credential-free Apply & Open, clipboard lifecycle, updater mutation barriers/exit and hosted package fixture preparation. Existing focused security/storage/concurrency evidence was checked against current source. Visual inspection found one confirmed integration defect: Updates.Hint still promised LocalAppData after DATA-01. Corrected that localized explanation without changing storage behavior. Missing production updater trust remains fail-closed; DataCompatibility/native update handoff must be reviewed before operational activation. Do not claim updater readiness from inclusion of its disabled UI.

Initial candidate 4a88af0 passed 886 unit/12 tooling/5 signing-tool cases, Release/harness builds 0 warnings/errors and default/password/storage/update/diagnostics renders. First navigation hover run timed out while local browser tooling was starting; original unmodified navigation passed separately, underlying initial foreground/pointer cause UNVERIFIED. A layout/DPI run via dotnet DLL correctly rejected process awareness=1; corrected the invocation to the manifest-bearing harness EXE, which passed. No test removed or timeout increased. One signing wrapper inspected a stale LASTEXITCODE from an expected-negative case; a fresh PowerShell subprocess independently returned exit 0 for all five cases. These preparation failures remain recorded and are not represented as passes. Final string-corrected source must repeat authoritative checks below.

## Required checks and current state

Release preparation only; publication **PENDING**. Exact-source outcomes will be recorded after observed execution: Release solution/harness builds, complete isolated unit TRX, planning/schema/link/tooling checks; WPF default/password/storage/update/diagnostic/navigation/theme surfaces; disposable hosted package compiler/runtime/startup/install/reinstall/uninstall/retention gates; independent API/tag/manifest/checksum/download/payload verification.

Preserve v1 schema/example, package dependency versions, internal assembly/EXE/registry identities, MIT notices and all current network functions. No live NIC/probes/listener/UAC/disk-root changes or installer execution on this workstation. Native personal-data/clipboard/hardware/recovery/DPI/pristine-client/cross-source acceptance remains NOT RUN. Component renders, synthetic fault tests and hosted Windows Server package checks establish narrower evidence only.

## Final local source verification

Frozen release source **5bd7607d3426b0adc3ff07f3d7005efdd7a4306d**, version 1.1.6/1.1.6.0. Exported with `git archive --format=zip --output=out/Release116Final/source.zip <full-sha>` into `out/Release116Final/Source`; all final commands below use that source. Private SDK `LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe` (10.0.403), private validation Python. No local install or ordinary App startup.

| Check | Observed result |
|---|---|
| Private SDK `build out/Release116Final/Source/AVNetworkToolkit.sln -c Release --artifacts-path out/Release116Final/artifacts --nologo` | PASS, 0 warnings/errors |
| Same SDK `test .../test/NetworkProfileManager.Tests.csproj -c Release --artifacts-path out/Release116Final/artifacts --no-build --no-restore --logger 'trx;LogFileName=release116-final.trx' --results-directory TestResults/Release116/Final --nologo` | **886 PASS / 0 failed / 0 skipped**, TRX counters independently read |
| Same SDK `build .../scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --artifacts-path out/Release116Final/artifacts --nologo` | PASS, 0 warnings/errors |
| Manifest-bearing `out/Release116Final/artifacts/bin/ProjectUiSmoke/release/ProjectUiSmoke.exe TestResults/Release116/Final/Ui/<case>` with no flag, `--device-passwords`, `--user-storage`, `--application-updates`, `--phase07`, `--navigation`, `--navigation-followup`, `--themes`, `--diagnostic-hide`; process-local DOTNET_ROOT points to private .NET 10 | All nine modes PASS, **286 captures / 0 binding errors**. Includes 26 navigation, 8 layout/activity/DPI, 20 password, 12 storage, 20 update, 64 diagnostics, 8 theme and 128 regression captures; 20 gated main/separate Hide/Show cycles and late-echo checks. No real network/clipboard/browser boundaries. |
| Final-source `scripts/validate_plan.py`, `scripts/test_validate_plan.py` | PASS, 9 phases/49 stable tasks/schema/formats/**854 links**, **12 tooling tests** |
| Unchanged signing-tool source through fresh PowerShell process, `scripts/test_update_signing.ps1 -ResultsDirectory TestResults/Release116/SigningProcess` | **5 PASS**, exit 0, disposable fixture keys removed; not production trust |
| Final-source `scripts/test_package_self_test.ps1` against the final framework-dependent build EXE, fresh SourceSelfTest output | PASS: WPF/JSON, create-only report, malformed/retired-worker rejection; **1.1.6.0 / 1.1.6+5bd7607... / .NET 10.0.14 / Preview / UpdatePublicKeySha256=null**. Actual self-contained packages require separate CI/download checks. |
| Visual inspection and notes | Masked password/minimum, storage error/minimum and update available/minimum captures inspected; corrected update copy verified in final render. Notes rendered locally at 1280x1000 and 390x844, one table/10 links and no document overflow after applying normal wrapping to the preview renderer. Local favicon 404 only; not an application/hosted GitHub render claim. |

Local retained TRX: `TestResults/Release116/Final/release116-final.trx`, SHA-256 `4b3d7a9a6cfab864f8342a08e38c310aac61fbc382e866b409a22f9781ac06b7`. Initial 4a88af0 artifacts/failures remain separate. Final native-aware UI sequence completed without browser actions alongside it. No source/test assertions or timeout were weakened.

Ordinary main push advanced origin/main from 05cac3b through 4a88af0 to 5bd7607, preserving all twelve prepared/completed commits and the final integration-copy fix. Dispatched release workflow **41392567740** at exact 5bd7607; publication/outcome remains pending until observed below.

## First hosted run — packaging harness ordering defect

Run 41392567740: build/unit/planning/tooling PASS; independently downloaded CI TRX has 886 PASS/0 failed/skipped. Both self-contained packages and compiler-integrity controls PASS. Runtime-isolation gate FAIL before installation, publication SKIPPED. The newly added `dotnet run ... --prepare-package-storage` came after `dotnet build-server shutdown`, restarting Roslyn; Move-Item then failed on locked `C:\Program Files\dotnet\sdk\10.0.403\Roslyn\bincore`. Runner cleanup identified an orphan VBCSCompiler. Full failure log retained locally at TestResults/Release116/ci-first-failure.log.

Minimal fix moves the existing checked server-shutdown command after the final fixture build and before runtime-root moves. No installation/runtime/discovery/retention assertion removed, no product/dependency/version change or workstation runtime move. Repeat the whole exact-source release workflow; failed candidates must never be published/reused. Existing local product-code/unit/WPF evidence remains attributed to 5bd7607; app/test/UI/signing source is byte-identical in this runner-only refinement.
