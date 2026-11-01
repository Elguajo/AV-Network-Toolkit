# Preview publication verification — 2026-11-01

Fork: https://github.com/Elguajo/AV-Network-Toolkit, main. Owner requested scoped commits/push of all completed changes and a new GitHub release (ADR-048). Phase 07 remains AWAITING_REVIEW; stable/native/phase acceptance is not granted by publication.

## Scope and exact sources

Preserved the existing topical commits; separately committed ignored local metadata, release version/docs and CI diagnostics. No history rewrite or forced push. Generated `.serena`/Python bytecode remains local. New navigation brief/research is planning only. Completed tray and adapter fixes retain their own commits.

| Release | Exact source | Workflow / outcome |
|---|---|---|
| v1.1.1 Preview | `3fd7e77e1f5b560172b7e23d33928bace9cdaa73` | Historical release; tag/assets preserved |
| v1.1.2 Preview | `d634a80918b429d11411d7c3afc8348d9a1d70a8` | [41299176436](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41299176436) PASS; published/downloaded/verified |
| v1.1.3 Preview | `2847ff6baaee59c813dbecb0688ffad62324b968` | [41300112524](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41300112524) PASS; published/downloaded/verified |

v1.1.2 was frozen before concurrently completed tray placement (`06d3728`) and adapter visibility (`ea7ca7a`). v1.1.3 includes both without changing an already published tag. Later independent work cannot silently change this source. Final metadata follows the verified source rather than rebuilding or moving a tag for documentation.

## Local checks and initial failures

Private SDK `$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe`, version 10.0.403; Python venv `$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe`. Windows 11 build 26100. No working-PC product installation, runtime-root moves, NIC changes/probes/listeners/UAC or VM changes.

- Initial working-tree `dotnet build AVNetworkToolkit.sln -c Release` **FAIL**: existing development EXE PID 49356 held the apphost; 10 MSB3026 retries, MSB3027/MSB3021. Tests NOT RUN after this failure. Left that process intact and exported immutable committed sources instead.
- `git archive`/`Expand-Archive` exported 9ee974d to `out/Release112-source`. `dotnet build <source>/AVNetworkToolkit.sln -c Release`: **0 warnings/errors**; full test run **687 PASS/0 failed/0 skipped**, `TestResults/Release112/release-1.1.2-local.trx`; theme/audit WPF **8 + 8 captures PASS/0 binding errors**. Plan/schema/formats/470 links and 12 tooling PASS.
- Final source 2847ff6 exported to `out/Release113-source`. `dotnet build out/Release113-source/AVNetworkToolkit.sln -c Release --nologo`: **0 warnings/errors**. `dotnet test out/Release113-source/test/NetworkProfileManager.Tests.csproj -c Release --no-build --logger 'trx;LogFileName=release-1.1.3-local.trx' --results-directory TestResults/Release113 --nologo`: **723 PASS/0 failed/0 skipped**.
- From that export, `dotnet run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release -- ../../TestResults/Release113/Tray --tray-placement`; then `--no-build -- ../../TestResults/Release113/Adapters --adapter-visibility` and `--no-build -- ../../TestResults/Release113/Workspace`: **6 + 12 + 128 actual WPF captures PASS, 0 binding errors**. NIC/probe/browser/elevation boundaries fake; tray smoke checks real HWND placement/action callbacks. Actual Explorer/mixed-DPI/auto-hidden taskbar remains unverified.
- Export `scripts/validate_plan.py` and `scripts/test_validate_plan.py`: **512 links / 12 tooling PASS**. No phase task accepted, renumbered or removed. Changed-text high-confidence credential/private-key pattern scan clear; not a complete privacy certification.

## Hosted validation and package evidence

`git push origin main:main` preserved scoped commits. `gh workflow run release.yml --ref main --repo Elguajo/AV-Network-Toolkit` triggered exact-source verification/publishing. Full tests have no exclusions/skips. Installation, global-runtime isolation and same-version reinstall/uninstall occur only inside the guarded disposable GitHub-hosted Windows runner.

First run [41298688041](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41298688041), source b8120d3: build **0 warnings/errors**, **687 unit / 12 tooling PASS**, installer/ZIP and WPF/JSON checks PASS, then reinstall **FAIL exit 7**. Publication skipped. Exception lacked the process phase. Instrumented named process steps/exit codes, unique installer logs and retained logs; PowerShell parser PASS. No assertion removed or weakened. According to [Inno Setup source](https://github.com/jrsoftware/issrc/blob/main/Projects/Src/Setup.WizardForm.pas), exit 7 indicates preparation rejected; the original transient lock/access cause is **UNVERIFIED**.

Instrumented v1.1.2 run 41299176436: **0 warnings/errors, 687 unit/0 failed/0 skipped, 494 links/12 tooling PASS**. Bundled/runtime/JSON/WPF/source identity, compiler integrity, ordinary startup, installed-vs-ZIP payload, missing global runtime negative control, locked update/uninstall rejection, same-version reinstall and uninstall retaining sentinel data all **PASS**. Runtime control exited -2147450749 when runtime discovery was unavailable; initial install/reinstall/uninstall exited 0; intentional locked update/uninstall exited 7/1. No native network/clean Windows-client or cross-version upgrade claim.

Published v1.1.2 was independently downloaded via `gh release download v1.1.2`; all four API SHA-256 digests and manifest/SHA256SUMS checked, package bytes identical to downloaded CI artifact, exact tag/source verified. Downloaded EXE diagnostic reports 1.1.2.0 / .NET 10.0.14 / source d634a80, WPF/JSON success; create-only/malformed/retired-worker checks PASS. All 8 extracted payload files present and MIT license hash matches. v1.1.1 target/assets metadata compared before/after and unchanged.

v1.1.2 ZIP SHA-256: `E88020AF1FA784904DC72E1EBED3351EE41D235673DF73C90D5ECCC81812AD99`; installer: `3C90FF98D8465F2DA4C40FF9BBC12BF5BEBF35F5DC17B5817FB5CC71C8602267`. These are historical intermediate packages; use final v1.1.3 after its independent verification is recorded.

## Final v1.1.3 evidence

**SHIP WITH KNOWN RISKS for the requested Preview; NO-SHIP as a stable/hardware-certified release.** [v1.1.3](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.3) published at 2026-11-01T11:07:01Z. Exact-source CI: **0 warnings/errors, 723 unit PASS/0 failed/0 skipped, 512 links and 12 tooling PASS**. Both package diagnostics, fixed compiler integrity, ordinary startup, installed/ZIP identity, runtime-negative-control, locked update/uninstall rejection, reinstall and uninstall data retention **PASS**. Named process exits: runtime available 0, unavailable -2147450749, initial install 0, locked update 7, locked uninstall 1, reinstall 0, uninstall 0. No check suppressed.

Downloaded `AVNetworkToolkit-package-candidates-2847ff6baaee59c813dbecb0688ffad62324b968` and `unit-results-2847ff6baaee59c813dbecb0688ffad62324b968` with `gh run download 41300112524`, then all four assets with `gh release download v1.1.3 --dir out/Published-v1.1.3`. Verified actual tag/commit equality, prerelease/non-draft state, each GitHub API SHA-256 digest, manifest/SHA256SUMS and byte identity against the verified CI packages. Both older releases' target/assets snapshots remain unchanged.

Extracted downloaded ZIP; all 8 payload files present, MIT LICENSE SHA matches repository. Exact-source `scripts/test_package_self_test.ps1 -Executable <portable>/NetworkProfileManager.exe -ResultsDirectory TestResults/Release113/PublishedSelfTest`: WPF/JSON, report create-only, three malformed diagnostic cases and four retired-worker arities **PASS**, request/result sentinels preserved. Downloaded EXE reports **1.1.3.0 / 1.1.3+2847ff6baaee59c813dbecb0688ffad62324b968 / .NET 10.0.14**, success true. No normal startup or network action on this PC.

| Published asset | Bytes | SHA-256 |
|---|---:|---|
| AVNetworkToolkit-1.1.3-win-x64-portable.zip | 68361800 | `B571DAADC7AF66A2814514DB9F3CC45F7D57082F4223B3D0473551B8C2069EE9` |
| AVNetworkToolkit-1.1.3-win-x64-setup.exe | 67795553 | `202BBD02128D0516D2250C17A16C711D2DE061AD1CC35682B7D6F4220B8FC7C2` |

Retained structured [source/run/download/result/hash evidence](evidence/published-v1.1.3.json). Local ignored outputs: `out/Release113-source`, `out/Release113-CI`, `out/Published-v1.1.3`, `TestResults/Release113`; intermediate outputs in Release112 equivalents. All product changes frozen at the verified source; later documentation-only work can follow on main without changing release bytes/tag.

Later README/user-guide commit `8523ce8` is preserved and pushed with outcome metadata; verified it changes no app/test/scripts/workflow/package/schema files relative to the release. Final real-tree planning/schema/formats/**561 links PASS**, **12 tooling PASS**, staged whitespace PASS. No source rebuild or tag movement for this documentation.

## Remaining acceptance

Preview publication does not complete Phase 07 or the native/owner gates. DOC-R01 intermittent diagnostics hide/cancel assertion, historical UX-R01/P05-R01, initial installer CI transient cause, clean-client/cross-version upgrade and manual accessibility remain open/unverified. .NET 10 official OS matrix excludes ordinary Windows 10 22H2; existing lab compatibility is unverified. Native NIC/DAD/UAC/IPC/DNS-route recovery/driver/egress and physical mixed-DPI tray classification require their existing isolated acceptance plan. No DATA-01, Phase 08, OS-component removal, credentials or support expansion. [Current release notes](../releases/v1.1.3.md).
