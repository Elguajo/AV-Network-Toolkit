# 1.1.5 Preview — publication and independent verification

2026-11-01 (Asia/Tashkent). ADR-051 / REL-06. Fork https://github.com/Elguajo/AV-Network-Toolkit / main. Exact release source: **2b78f67cb80320c975ca18ad02677f7d0913c732**. [Published 1.1.5 Preview](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.5), [successful release run 41333770039](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41333770039). Result: **SHIP WITH KNOWN RISKS, Preview only**; stable/native/phase acceptance is not claimed.

## Delivered

Ordinary main push preserved nine existing scoped commits plus 2b78f67 release metadata. Both same-source Windows x64 packages are self-contained .NET 10.0.14: installer primary and portable ZIP additional, with SHA256SUMS and package-manifest. Public API reports four complete assets. The tag dereferences to the exact checked release source. Older v1.1.1–v1.1.3 tags, release metadata/asset IDs/digests and assets are unchanged; local-only 1.1.4 remains a distinct earlier candidate.

Changes include source selector/adapter menu refinements, persistent tool rail, collapsible/resizable hover-reveal sidebar, safe private window/sidebar preferences, actual tool-activity marks and PerMonitorV2 awareness. DOC-R01's test synchronization repair is included. [Release notes and compatibility](../releases/v1.1.5.md). No code/schema/data-root/dependency changes were added during release preparation; app version/file/assembly metadata and release notes/gate were aligned to 1.1.5/1.1.5.0.

## Exact-source checks

Local commands ran from a fresh git-archive 2b78f67 export under `TestResults/Release1.1.5/Source-2b78f67`, using private SDK 10.0.403:

- `dotnet build AVNetworkToolkit.sln --configuration Release --nologo` and harness build: **0 warnings / 0 errors**.
- Full Release unit suite with retained TRX: **736 PASS / 0 failed / 0 skipped**.
- Manifest-bearing harness EXE, `--navigation-followup`, `--navigation`, then default workspace: **8 + 26 + 128 = 162 WPF captures PASS / 0 binding errors**. Fake storage/network boundaries, actual native hover/keyboard/window/current-DPI assertions; no real probes/NIC changes.
- Planning/schema/formats/local links and 12 tooling tests PASS. Final evidence-only documentation gates recorded in STATUS.

GitHub Actions at that same SHA:

| Job / gate | Observed result |
|---|---|
| verify / build-and-test | Release build **0 warnings/errors**, independently downloaded TRX **736 PASS**, **12 tooling PASS**, planning/schema/formats/**634 source links PASS** |
| verify / packages | Both packages built; pinned Inno compiler reuse + four missing/incorrect/tampered negative controls PASS |
| Runtime/package controls | Framework-dependent control fails with global runtime discovery roots unavailable; installer/portable WPF/JSON/self-tests and ordinary window startup PASS |
| Installation checks | Installed and extracted ZIP payload identical; locked update/uninstall rejection, same-version reinstall, uninstall and fixture-data retention PASS |
| publish | Source/version/hash guards PASS; v1.1.5 Preview created with all four assets |

The existing checkout/setup/upload actions produced GitHub's Node 20-to-24 compatibility annotation; jobs succeeded. This is separate from compiler warning counts. No workflow/package checks were disabled or relaxed.

## Independent public-download verification

Used `gh release download v1.1.5`, the actual release API, `gh run download 41333770039` for both package and unit artifacts, and remote tag/commit APIs. Downloaded bytes are stored under ignored `TestResults/Release1.1.5`.

- All four downloaded asset sizes/SHA-256 match GitHub API digests; both package hashes match downloaded manifest and SHA256SUMS.
- Installer, ZIP and manifest are byte-identical to the actual successful CI artifacts. Manifest Version=1.1.5, SourceCommit=2b78f67, SelfContained=true and RuntimeIdentifier=win-x64.
- ZIP contains the expected eight flat payload/license files; extracted only after validating names and hashes.
- `scripts/test_package_self_test.ps1` against the downloaded portable EXE PASS: WPF/JSON load, exact version/source/runtime, create-only report, three malformed diagnostic cases and four retired-worker rejections. Installer was not run on the working PC; its install evidence is from the disposable runner.
- Downloaded EXE identifies **1.1.5.0 / 2b78f67 / .NET 10.0.14**. [Structured API/manifest/payload/source/TRX evidence](evidence/published-v1.1.5.json).

No new release failure occurred. An attempt to read completed-job logs while the overall run was still active was unavailable; final logs were fetched after run completion. Publication evidence metadata follows in a separate ordinary main commit; it does not move/rebuild the release tag/assets.

## Retained limits

Phase 07 remains AWAITING_REVIEW; no DATA-01 or Phase 08. Actual 150%/200%/mixed-monitor DPI, touch/Narrator, native NIC/UAC/IPC/DAD/route/DNS recovery/probes/multicast/egress, physical driver/topology and pristine client/cross-version installation acceptance are NOT RUN or remain separately pending. Hosted Windows Server runtime/installation checks are not a pristine Windows-client or hardware/network certification. Current .NET 10 OS-edition, unsigned-binary, conservative Apply-state and old-history limits remain in the release notes. Historical UX-R01/P05-R01 acceptance remains open; DOC-R01 is resolved as a test defect, not a native acceptance claim.
