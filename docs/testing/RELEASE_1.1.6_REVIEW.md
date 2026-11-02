# 1.1.6 Preview — combined release verification

2026-11-02, Asia/Tashkent. REL-08 / ADR-054. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, inspected clean source `d16f2764bbb9ed0ba6cc88cc3567471c39740714`. All three concurrent sessions completed before release edits or the combined run. Phase 07 remains AWAITING_REVIEW.

**Executed: [1.1.6 Preview published](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.6)** at **5bf8ece8608332c3a0685b228589b8ec30bf97c0**, [workflow 41393820449 SUCCESS](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41393820449). **SHIP WITH KNOWN RISKS, Preview only.** [Structured public evidence](evidence/published-v1.1.6.json). Preparation/pending statements below are historical; final outcomes follow them.

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

## Second hosted run — test cancellation race

Runner-order fix source 4a87309 passed a fresh local Release build (0 warnings/errors) and 886 full unit tests, but run **41393101524** failed one existing case, `MdnsSessionTests.RejectsForeignInterfaceAndInvalidPacketMetadataBeforeCaching(malformed)` (885 PASS/1 FAIL/0 skipped); packages/publication SKIPPED. The test waited for FakeReceiver.ReadCount, which increments before ReceiveAsync returns. It then stopped the session, allowing the production cancellation check to run before received/rejected counters increment. Received-but-undelivered data may correctly have zero counters; a read is not proof of processing. No packet-handling defect established.

The rejection test now waits for entry into the next receive, which occurs only after the previous packet is processed and its throttle completes; its Rejected=1/empty-cache assertions remain. A gated receiver test deterministically recreates cancellation after read but before delivery and verifies Stopped/Received=0/Rejected=0/empty cache/disposed receiver. No product mDNS code, sleep/timeout limit or assertion suppression changed. An initial attempt to await a periodic public snapshot correctly failed locally because an idle receive does not wake when only the fake clock advances; replaced that assumption with the actual next-receive boundary. Full original logs retained at TestResults/Release116/ci-second-failure.log; focused/repeated and final exact-source results follow below.

Focused verification of the test-only fix: private SDK `test test/NetworkProfileManager.Tests.csproj -c Release --artifacts-path out/Release116Mdns/artifacts --filter FullyQualifiedName~MdnsSessionTests` passed **29 tests**; the six rejection cases plus gated cancellation case passed **10 consecutive repeats (70/70)** with unchanged timeout/assertions. Actual sockets and product mDNS source were not used/changed.

## Verified final source, packages and public downloads

Final exact source **5bf8ece8608332c3a0685b228589b8ec30bf97c0**, ordinary main push and standard release workflow 41393820449 SUCCESS. No tag/release/history rewrite or failed-candidate reuse. The final two source refinements change runner/test/documentation only; `git diff 5bd7607 <release-source> -- app schemas examples scripts/ProjectUiSmoke scripts/sign_update.ps1 scripts/build_packages.ps1` is empty. The independently reviewed 286 WPF captures therefore cover byte-identical final product/UI/resource code, with source attribution retained rather than implying another render run.

| Final check | Observed outcome |
|---|---|
| Fresh `git archive` final source to out/Release116Verified/Source; private SDK `build .../AVNetworkToolkit.sln -c Release --artifacts-path out/Release116Verified/artifacts --nologo`, then `test .../test/NetworkProfileManager.Tests.csproj -c Release --artifacts-path out/Release116Verified/artifacts --no-build --no-restore --logger 'trx;LogFileName=release116-verified.trx' --results-directory TestResults/Release116/Verified --nologo` | **0 warnings/errors; 887 PASS/0 failed/skipped**. TRX SHA-256 `6014314d2ee8a5cad628f6980d6cc0a904d8081c1df8ad157d54cb8e669d9975`. |
| Final exported-source planning/schema/reference/tooling checks | **854 links, 12 tooling PASS**. Later outcome-only metadata checked separately before commit. |
| Hosted exact-source unit/build/planning | PASS; independently downloaded successful CI TRX confirms **887 PASS/0 failed/skipped**. |
| Hosted packages and inspected retained reports | Both packages/source/hash/self-contained win-x64 PASS; pinned compiler and four integrity rejection controls PASS. Runtime negative control failed as expected with global .NET unavailable; both packages' ordinary main-window startup and WPF/JSON diagnostics PASS. Install/reinstall/uninstall exit 0; locked update/uninstall rejected (7/1). Installed/ZIP payload matches; hashes for legacy source, home data, recovery and independent migration snapshot retained. |
| Public four-asset download, release/tag APIs and remote refs | Source/tag/manifest/version/channel/API size/digest/checksum PASS. Both packages and manifest byte-identical to successful CI candidates; SHA256SUMS agrees with both API and manifest. Canonical notes match public body. No signed update assets/configured key implied. |
| Public portable ZIP | Exactly eight expected flat files; fully extracted to `out/Release116Published/Portable`. Downloaded EXE reports **1.1.6.0 / 1.1.6+5bf8ece... / .NET 10.0.14 / Preview / UpdatePublicKeySha256=null**. WPF/JSON/create-only/three malformed diagnostic/four retired-worker rejections PASS. Malformed update worker exits 2 before ordinary startup/migration. No workstation installation or ordinary product launch. |
| Previous releases/tags | v1.1.1/v1.1.2/v1.1.3/v1.1.5 IDs/targets/names/bodies/channel and asset IDs/digests/sizes/timestamps/URLs unchanged; all pre-existing remote tag refs retained. |

| Public package | SHA-256 |
|---|---|
| AVNetworkToolkit-1.1.6-win-x64-portable.zip | `7347ca5076962f8872b3972503cb2f0ad7b4c8792d3d547ff43711d65e0ec0a7` |
| AVNetworkToolkit-1.1.6-win-x64-setup.exe | `139f4642f1b0e747775f945381b111e13856deec188b7ba43d7690af10b4209d` |

Final decision **SHIP WITH KNOWN RISKS** applies to the requested Preview. Production signing trust/DataCompatibility/native updater acceptance, actual personal-data/clipboard/native network/recovery/UAC/DPI/pristine-client/cross-source upgrade acceptance remain NOT RUN or separately gated. No Phase 07 completion, Phase 08 or USB implementation claimed. Follow-up evidence-only commit/push records these outcomes without moving/rebuilding the published source.
