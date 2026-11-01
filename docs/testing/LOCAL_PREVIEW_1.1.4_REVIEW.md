# Local 1.1.4 Preview — owner manual-test candidate

2026-11-01, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, inspected base `4c0a3aa`. Owner requested a version bump with all completed changes and the local executable path. Phase 07 remains AWAITING_REVIEW; no GitHub push/release or agent live-network execution.

## Preparation

Version/file/assembly updated to 1.1.4/1.1.4.0, changelog/notes and manually dispatched release-workflow version gate synchronized. Application behavior and data formats/locations unchanged. Source commit is resolved through `git log -1 --format=%H -- app/NetworkProfileManager.csproj`; package evidence will pin that complete SHA.

## Verified local output

Built source **629c2ae2d56800693ce1683a1464e177fcc0238a**, clean main at package generation. Later evidence-only commit does not change this source identity. [Structured evidence and hashes](evidence/local-preview-1.1.4.json).

- Ready-to-run EXE: `D:\01_DEV\AV-Network-Toolkit\out\LocalPreview-1.1.4\payload\NetworkProfileManager.exe`.
- Portable ZIP: `out/LocalPreview-1.1.4/AVNetworkToolkit-1.1.4-win-x64-portable.zip`.
- Installer: `out/LocalPreview-1.1.4/AVNetworkToolkit-1.1.4-win-x64-setup.exe`.
- Source/package identity and package hashes: `out/LocalPreview-1.1.4/package-manifest.json`, `SHA256SUMS.txt`.

EXE file/assembly version **1.1.4.0**, informational version **1.1.4+629c2ae2d56800693ce1683a1464e177fcc0238a**, bundled **.NET 10.0.14** observed from both original payload and extracted ZIP. All eight payload files are hash-identical after extraction. Keep the entire payload folder beside the executable.

## Commands and results

Tools: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` (10.0.403), `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`, pinned Inno Setup 6.7.4 reused after compiler-file integrity verification. No new dependencies/tool install.

| Command / check | Observed result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release -p:OutputPath=bin/Local114/ --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build -p:OutputPath=bin/Local114/ --logger 'trx;LogFileName=local-preview-1.1.4.trx' --results-directory TestResults/LocalPreview114/Unit --nologo` | 723 PASS / 0 failed / 0 skipped |
| `scripts/build_packages.ps1 -Dotnet $sdk -Compiler <verified ISCC.exe> -OutputDirectory out/LocalPreview-1.1.4` | Self-contained publish, WPF/JSON diagnostic, source/version guard, installer and ZIP generation PASS |
| `scripts/test_package_self_test.ps1 -Executable out/LocalPreview-1.1.4/payload/NetworkProfileManager.exe -ResultsDirectory TestResults/LocalPreview114/PayloadSelfTest` | PASS WPF/JSON, create-only report, malformed invocations, four retired-worker rejection cases; no normal startup/network |
| Same script against `TestResults/LocalPreview114/Extracted/NetworkProfileManager.exe`, results `TestResults/LocalPreview114/ZipSelfTest` | Same checks PASS |
| Both package SHA-256 values against manifest; extracted ZIP vs original payload | PASS, all 8 files identical; SHA256SUMS.txt created |
| EXE FileVersionInfo vs both self-test reports/source identity | 1.1.4.0 / 1.1.4+629c2ae / .NET 10.0.14 PASS |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py`; `git diff --check` | Preparation 575 links and 12 tooling PASS; final metadata results in STATUS handoff |

Scoped diff review: version metadata and future manual-release literals/notes agree; no functional source, dependency, schema or data-path change. Existing WPF workflow/hide evidence belongs to the previous DOC-R01 verification, not a newly claimed 192-capture run for this version. No extra UI tests required by this metadata-only change.

## Owner handoff and limits

**Local manual-test candidate verified; stable/native release NO-SHIP.** Owner can run the EXE directly from the payload folder, or use the ZIP/installer deliberately. To avoid confusing two running copies, exit the older app through its tray menu first. Explorer is opened with the new EXE selected; the agent does not start ordinary application/network behavior.

Installer install/reinstall/uninstall, hosted CI and clean-client .NET discovery isolation NOT RUN for 1.1.4. Native NIC/probes/UAC/IPC/driver/manual/phase acceptance remains pending. No GitHub push/release/tag, network settings, probes, VM or installed-product change. Existing OS/unsigned/data/recovery limits are in [notes](../releases/v1.1.4.md). This task does not start DATA-01 or Phase 08.
