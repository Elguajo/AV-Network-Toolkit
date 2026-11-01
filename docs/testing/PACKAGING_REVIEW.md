# REL-03 — local package candidates and pending hosted verification

2026-11-01 Asia/Tashkent, fork https://github.com/Elguajo/AV-Network-Toolkit, main. Phase 07 AWAITING_REVIEW. ADR-040 explicitly starts REL-03; REL-02 native stays deferred and REL-04/release publication is not started. [Implementation/verification contract](../spec/PACKAGING_CONTRACT.md), [exact candidate identity and hashes](evidence/rel03-package-candidates.json).

## Outcome

**LOCAL_CANDIDATES_VERIFIED / HOSTED_VERIFICATION_PENDING; NO-SHIP.** Implementation `e39fb64843244e272ba04b3c52711a8888045b3d`, supported installer lock-check/ordinary-startup script correction `d7fb0dd0557e1b73a23b0247f2444c5050410d7b`, complete compiler integrity pin `e4bd0cbe152a581ab0a315f90801e42de344439f`. Final candidates are built from **e4bd0cb**, not an uncommitted tree. Existing version **1.1.1** retained; this is not a release/version bump.

| Local candidate | Bytes | SHA-256 |
|---|---:|---|
| `out/Rel03Final/AVNetworkToolkit-1.1.1-win-x64-portable.zip` | 64143229 | `2ACA48807CB914728B10774AA3AED56C7DC352D3FFD142381F3B967A3BBE513D` |
| `out/Rel03Final/AVNetworkToolkit-1.1.1-win-x64-setup.exe` | 63657503 | `50EB857DA44A565FD4B2092B22F34138871FCFF4994D00EC8528EE073C5174BA` |

Both use the same self-contained payload: application EXE, five WPF native DLLs, base MIT LICENSE and existing third-party notices. The package diagnostic reports application 1.1.1.0, source e4bd0cb and **.NET 8.0.33**, validates the embedded schema and renders WPF offscreen. Full ZIP extraction hashes match all eight payload files. Installer compilation succeeds; **installation itself has not run on this PC**. Keep payload DLLs beside the EXE. Local candidates/build manifest are ignored output, not committed binaries or published release assets.

## What changed and focused review

- `.github/workflows/build.yml`: retain main/PR build/unit triggers; add workflow_dispatch, read-only contents permission, TRX evidence, existing planning/schema/tooling checks and a dependent windows-2022 package/verification job. Candidate artifacts upload without publishing a GitHub Release. Existing tag release workflow is unchanged and remains outside this work; REL-04 must reconcile its old ZIP-only publication before a future release.
- `scripts/build_packages.ps1`, `install_inno_setup.ps1`, `packaging/inno-setup.json`, `AVNetworkToolkit.iss`: pinned verified compiler, one self-contained publish, both same-version candidate formats, immutable output directories, source/version assertions and candidate hash manifest. Installer defaults to Program Files, adds a Start menu shortcut/uninstaller, never auto-launches or installs .NET globally, never changes startup/services/associations/user data. No forced app close/restart/reboot; locked-target update/uninstall rejects. Actual locked-target/retention behavior is pending hosted evidence, not certified by compilation.
- `app/App.xaml.cs`, `Services/PackageSelfTest.cs`: only five-line explicit diagnostic routing before normal startup plus the diagnostic implementation. Current default startup and both elevated-worker protocols stay unchanged. No real MainWindow/tray/network/store path in the local diagnostic. Absolute new-file report only, bounded fixed fixture, existing resources, exception type only, deterministic exit codes; no URL, credential, command or NIC input.
- `scripts/test_package_self_test.ps1`, `test_packaging_tools.ps1`, `RuntimeControl`, `test_packages_on_runner.ps1`: real packaged-host load and rejection tests; trusted compiler integrity controls; guarded disposable-host runtime isolation with framework-dependent negative control, both package self-tests/ordinary main-window startup, exact installed payload comparison, locked-file rejection, same-version reinstall and uninstall/data-sentinel retention. The runner script rejects this PC before mutation. Source/paths for moves are checked, runtime roots/environment restored in finally; no recursive delete or network command.
- Canonical planning/status/decisions and PACKAGING_CONTRACT record ADR-040 and the owner's later **ADR-042 / DATA-01** choice of future `%USERPROFILE%\.av-network-toolkit`. Data migration is not implemented in these packages.

Scoped code/security/concurrency self-review (no independent reviewer): no remaining actionable defect found in inspected package entry, file-write boundary, compiler hash verification, scripts, installer payload/data separation or CI permissions. No elevated worker/URL/schema/legacy history change. No secrets or real project/device data included. Existing local build tools are a trusted development boundary; guards do not make an operator-controlled CI environment an adversarial sandbox. Package self-test is not full normal UI/network acceptance. Installer races, native UAC, driver behavior and upgrade compatibility need actual evidence.

## Exact executed checks

Use existing `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`; `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`.

| Command/check | Actual result |
|---|---|
| Export git e31be982748115ef61f2ac494b99954891cfb6a7 to `TestResults/Rel03Prepared/source`, overlay only App.xaml.cs and PackageSelfTest.cs; `& $sdk test TestResults/Rel03Prepared/source/test/NetworkProfileManager.Tests.csproj --configuration Release --logger 'trx;LogFileName=rel03-prepared.trx' --results-directory TestResults/Rel03Prepared` | **681 PASS/0 failed/0 skipped**, compilation 0 warnings/errors. All 106 tracked app files match final content after CRLF/LF normalization; 39 raw newline differences, no content differences. TRX SHA-256 in evidence JSON |
| `& ./scripts/install_inno_setup.ps1`; same helper with `-CompilerPath "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/InnoSetup-6.7.4/ISCC.exe" -ValidateOnly` | PASS official download SHA and pinned compiler reuse; current-user compiler setup only, no elevated product install |
| `& ./scripts/test_packaging_tools.ps1 -Compiler "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/InnoSetup-6.7.4/ISCC.exe" -ResultsDirectory TestResults/Rel03ToolIntegrity` | PASS valid reuse + four missing/wrong-name/incomplete/corrupt tool rejections; real compiler untouched |
| PowerShell AST ParseFile on all five package scripts | PASS |
| `& ./scripts/test_packages_on_runner.ps1 -PackageDirectory out/packages` on this local PC | Expected guard rejection before any runtime move/installer/OS action; no hosted success claimed |
| `& ./scripts/build_packages.ps1 -Dotnet $sdk -OutputDirectory out/Rel03Final` at clean e4bd0cb | PASS Release self-contained publish, source/version assertion, WPF/JSON diagnostic, ZIP creation and Inno Setup 6.7.4 installer compile |
| Verify both final files against package-manifest.json; Expand-Archive to `TestResults/Rel03FinalPortable`, compare all payload file SHA-256 | PASS 2 candidate hashes, 8/8 payload files |
| `& ./scripts/test_package_self_test.ps1 -Executable TestResults/Rel03FinalPortable/NetworkProfileManager.exe -ResultsDirectory TestResults/Rel03FinalPortableChecks` | PASS load/render/JSON, existing-report rejection with byte preservation, three malformed-argument rejections; no ordinary startup/network |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py`; `git diff --check` | PASS schema/formats/49 phase tasks/links, **12 tooling tests**, whitespace |

Initial failures retained explicitly: the first installer draft used Delphi FileOpen, unsupported by Inno Pascal; compilation correctly failed before a usable installer. Replaced with documented TFileStream and recompiled successfully. PE FileVersion of official compiler components is 0.0.0.0; checking it would reject valid reuse and mislabel the manifest. A help-banner probe also returned usage/exit 1 without an engine version; replaced with official-asset-derived complete file hash pins and meaningful rejection tests. Earlier candidate folders are diagnostic history, not the final delivered identity.

## GitHub investigation and remaining gate

Observed twice: Actions run count **0**. CLI settings: enabled=true, allowed_actions=all, both build/release workflows active; default token contents read, fork=true. The five queued check suites at remote 1282fbc belong to Netlify/Vercel/Cursor/Railway/Claude, not GitHub Actions. Only one of 56 observed remote commits has a skip marker, so all absence cannot be attributed to skip-ci. No unsupported root-cause claim.

`gh workflow run build.yml --repo Elguajo/AV-Network-Toolkit --ref main` returned **HTTP 422: Workflow does not have workflow_dispatch trigger**. Prepared manual trigger addresses this observed inability to dispatch; no hosted run exists for the local change yet. Connector read permissions do not expose all workflow settings, so these checks used authenticated GitHub CLI without exposing credentials or changing repository permissions.

Fresh fetch: remote main **1282fbcea2b3e88977b43373cc6ece53698e996c**, 0 remote-only/10 local-only commits at e4bd0cb. Local chain includes reviewed JSON repair, safe planning, distribution decisions, separately committed ready-made diagnostic tests and packaging work. Evidence metadata follows. Existing owner publication gate requires authorization before ordinary main push of this concrete chain and hosted execution; no force, new branch, tag or release. Until that gate is resolved, installer/ordinary startup/runtime-negative-control/reinstall/uninstall results are **NOT RUN**, REL-03 remains incomplete and the release is **NO-SHIP**.

Hosted runtime discovery isolation is not a pristine Windows client image. Real UAC consent/denial, cross-version upgrade, manual UI/tray accessibility and all native network configuration/recovery remain **NOT RUN/UNVERIFIED**. After authorized push, observe the actual run/logs/artifacts and correct reproduced failures within REL-03; do not mark it done from this local report. Afterwards handle DATA-01 migration separately and obtain any REL-04/release decisions required by the existing order.

## Executed hosted verification and branded Preview publication — ADR-043/045

The pending-publication/hosted statements above are historical. Owner approved main push/CI, then explicitly requested v1.1.1 with the new logo/current changes. First hosted build run [41272961614](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41272961614) at 0a27b9f PASS proved the complete package sequence; it predates branding. It exposed unintended newer SDK selection and CS9264 warnings. Added global.json selecting existing SDK 8.0.427/latestPatch rather than weakening any check. Source behavior unchanged. Branded source **3fd7e77e1f5b560172b7e23d33928bace9cdaa73** then passed [release run 41273489475](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41273489475): build 0 warnings/0 errors, **681 unit PASS/0 failed/0 skipped**, **12 tooling PASS**, schema/plan checks, both package builds and all hosted verification. The manual release workflow reuses verification and publishes only its same-source artifact after manifest/hash checks. No existing tag was overwritten; the fork previously had no tags/releases.

Actually passed on disposable Windows Server: failing framework-dependent runtime control after global runtime roots were moved; both self-contained WPF/JSON diagnostics and ordinary expected main-window startup; identical ZIP/installed payload; pinned tool-integrity positive/negative cases; locked EXE update and uninstall rejection; successful same-version reinstall and uninstall preserving fixture user data; finally restoring runtime discovery roots/environment. Ordinary startup only reads local facts; no Apply/Run/Open command invoked. This is packaged/native host/installer evidence, **not** real NIC/UAC/IPC/network acceptance or a pristine Windows-client/cross-version certification.

[Published v1.1.1 Preview](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.1), both with the new logo:

| Asset | Bytes | SHA-256 |
|---|---:|---|
| AVNetworkToolkit-1.1.1-win-x64-portable.zip | 65071824 | `780B0C41E90EAECE141AB545505636974E117F15B542036BD959BB528EE89A6E` |
| AVNetworkToolkit-1.1.1-win-x64-setup.exe | 64609647 | `905758C3988A22F3A62CEDD2A98EA47CB533F1814988DCA272E63F4287DB98CB` |

`gh release download v1.1.1 --repo Elguajo/AV-Network-Toolkit --dir out/Published-v1.1.1` downloaded all four assets. Verified every file against GitHub's asset digest, the two packages against manifest/SHA256SUMS, and tag/source equality via `gh api repos/Elguajo/AV-Network-Toolkit/commits/v1.1.1 --jq .sha`. Extracted ZIP to out/Published-v1.1.1/portable; `test_package_self_test.ps1` against the downloaded EXE passed all five positive/rejection checks and reported bundled **.NET 8.0.33**, 1.1.1.0 and correct source. ExtractAssociatedIcon 32x32 is pixel-exact to the owner-approved source ICO; published MIT LICENSE SHA matches repository. [Retained actual release/CI/asset evidence](evidence/published-v1.1.1.json).

README/changelog/notes now describe the fork and Preview limitations; upstream history/MIT retained. Installer/ZIP/signing policy and incomplete native gates are explicit. **Preview publication completed by owner request; general/native ship gate still incomplete.** Phase 07 remains AWAITING_REVIEW. No user-home migration, DHCP server or device-password implementation introduced. DATA-01 remains a separate next change.
