# 1.1.7 Preview — REL-09 / ADR-059

2026-11-04, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit; main; inspected clean synchronized base `a5c8aa1e69de643e7b449a7560f8ff5edba3a5ef`. Owner requested a GitHub Release after the local 1.1.7 build. Phase 07 remains AWAITING_REVIEW.

**Executed: [1.1.7 Preview published](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.7)** from **23f2d3a371dc13666b9b13fb57084aeb3af9eee8**, [workflow 41769507274 SUCCESS](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41769507274). **SHIP WITH KNOWN RISKS, Preview only.** [Structured evidence](evidence/published-v1.1.7.json). Preparation/pending checkpoints below are historical; final outcomes follow them.

## Boundary and preparation

Prepare new v1.1.7 Preview through the existing release workflow; preserve previous tags/releases/assets. App version remains 1.1.7. Update only workflow version/title/notes selection, product-first canonical notes, changelog/README and durable planning/evidence. No app code/schema/dependency, production signing key, local install, ordinary startup, personal-data migration, real network/UAC or phase advancement. Retain ca7d475's local packages; publish only successful workflow candidates.

Published baseline v1.1.6 at 5bf8ece. Includes completed profile-switching/APIPA/offline, diagnostic responsiveness, device modal/directory and themed discard work. [Local 977-unit/184-capture/source/package evidence](PROJECT_DISCARD_REVIEW.md) remains attributed to ca7d475. Preserve SWITCH-04–07/native/owner limits in notes; no silent fixes/support expansion. Disabled updater/manual upgrade, v2 plaintext/backups and older-reader/journal/home-storage compatibility remain explicit.

Retained all five release API records at `out/Release117/previous-releases.json` and tag refs at `previous-tags.txt`. No v1.1.7 release/tag existed. Build/release workflows active; no push-triggered runs for the local follow-ups. Explicitly dispatch release after committing/pushing source; never infer CI success from local evidence.

## Required gates and execution state

**PENDING, not published at this preparation checkpoint.** Verify final-source local build/unit/tooling and unchanged-product UI attribution; same-source hosted unit/tooling, pinned compiler integrity/negative controls, self-contained packages, runtime negative control, ordinary runner startup, install/reinstall/uninstall/locks and retained legacy/home/recovery/snapshot fixtures. Native/live/client-OS/clipboard/DPI limits are not waived.

After successful publication independently download public installer/ZIP/manifest/checksums and CI TRX/candidates; verify tag/source/version/channel/body/API sizes/digests/checksums/CI bytes, extracted EXE diagnostics and previous-release/tag preservation. Record run/source/commands/results/hashes below and append handoff before outcome-only commit/push. Never publish failed or local-only candidates as a bypass.

Expected decision if gates pass: SHIP WITH KNOWN RISKS, Preview only. Stable/hardware certification, owner/native acceptance, production signing custody/updater integration, pristine supported-client/cross-version upgrades and Phase 08 remain separate.

## Frozen local source and observed hosted progress

Exact release source **23f2d3a371dc13666b9b13fb57084aeb3af9eee8**, ordinary main commit/push. Explicitly dispatched [release workflow 41769507274](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41769507274). Publication is still PENDING at this progress checkpoint. Product/app/test/schema/example/harness diff from ca7d475 is empty.

Exported with `git archive --format=zip --output=out/Release117/source.zip <source-sha>` to `out/Release117/Source`. Using private SDK `LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe`, process-local DOTNET_ROOT, artifacts at `out/Release117/artifacts`:

- `build out/Release117/Source/AVNetworkToolkit.sln -c Release --artifacts-path <absolute-artifacts> --nologo`: PASS, 0 warnings/errors.
- `test .../test/NetworkProfileManager.Tests.csproj -c Release --artifacts-path <absolute-artifacts> --no-build --no-restore --logger 'trx;LogFileName=release117.trx' --results-directory TestResults/Release117/Unit --nologo`: **977 PASS/0 failed/skipped**; independently parsed TRX, SHA-256 `ac6a498d96dbbd9c2aabd69d5fb59d63a10f10b1a5aa9ff571b7d4d78f6e98bb`.
- Frozen `build .../scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --artifacts-path <absolute-artifacts> --nologo` then manifest-bearing `out/Release117/artifacts/bin/ProjectUiSmoke/release/ProjectUiSmoke.exe TestResults/Release117/Discard --project-discard`: PASS, 0 warnings/errors, 4 captures/0 binding errors. Other 180 existing scenarios retain their ca7d475 source attribution; app/resources/harness byte-identical in Git blobs, not claimed a repeat render.
- Private Python `-B out/Release117/Source/scripts/validate_plan.py`: PASS, 9 phases/49 tasks/schema/formats/998 links; `test_validate_plan.py`: 12 PASS.
- `gh api markdown --method POST -f mode=gfm -f context=Elguajo/AV-Network-Toolkit -F text=@docs/releases/v1.1.7.md`: PASS, GitHub renders one table/no duplicate H1, 13 links/four intended asset links, no placeholders. Initial literal `<table>` assertion rejected GitHub's valid `<table role="table">`; corrected only the inspection to parse HTML tags/attributes. No release content or gate changed to obtain success.

Hosted unit/build/planning job has completed successfully; full workflow/package/publication outcomes follow below. No network traffic/mutation, ordinary App startup, clipboard/browser, install or personal-data migration occurred on this PC.

## Final hosted/public results

Standard release workflow 41769507274 completed all verify/build, verify/packages and publish jobs successfully at exact source 23f2d3a. No failed-candidate/manual publication bypass, tag rewrite, old-release edit or quality-gate relaxation. `gh run download <run> -n unit-results-<source> -D out/Release117/CiUnit` and package-candidates artifact to CiCandidates retained CI evidence.

- Independently parsed CI TRX: **977 PASS/0 failed/skipped**; SHA-256 `692e31e74c75fba90d32b4e1ff847af12daa475f653278d8d80237863965977b`. Hosted planning/tooling PASS (12 tests). Pinned compiler reuse and all four integrity rejection cases PASS.
- Inspected runner result/process reports: expected missing-global-runtime failure, both packages' WPF/JSON and observed ordinary main-window startup PASS; initial install/same-version reinstall/uninstall exit 0; locked update/uninstall rejected with 7/1; legacy source/home data/recovery/independent backup hashes retained. Disposable Windows runner only; CrossVersionUpgrade and native networking NOT RUN, not a pristine client claim.
- Public unauthenticated downloads: all four expected assets, exact API sizes/digests, tag/commit/manifest version and source/channel/body match; ZIP/installer/manifest byte-identical to successful CI candidates; both checksum entries match. Previous five releases' IDs/body/title/targets/channel/dates/assets/digests/sizes/timestamps/URLs and all five pre-existing tag refs unchanged. Checks exclude volatile download counters.
- Public ZIP contains exactly eight expected flat files, extracted to `out/Release117/Portable`. `pwsh -NoProfile -File ./out/Release117/Source/scripts/test_package_self_test.ps1 -Executable ./out/Release117/Portable/NetworkProfileManager.exe -ResultsDirectory ./TestResults/Release117/PublicPackageVerified` exits 0: WPF/JSON/create-only, three malformed diagnostic cases and four retired-worker rejections PASS. Downloaded EXE reports **1.1.7.0 / 1.1.7+23f2d3a371dc13666b9b13fb57084aeb3af9eee8 / .NET 10.0.14 / Preview / UpdatePublicKeySha256=null**.
- Unauthenticated API queries hit the host's GitHub rate limit (403); metadata was independently fetched via authenticated gh CLI instead. Actual public asset downloads used direct unauthenticated URLs and succeeded. Initial in-process PowerShell wrapper incorrectly read LASTEXITCODE left by an expected retired-worker rejection although the diagnostic script printed PASS; fresh PowerShell process independently exited 0. Original output/evidence preserved; no script/product/assertion change or installation.

| Public artifact | SHA-256 |
|---|---|
| AVNetworkToolkit-1.1.7-win-x64-portable.zip | `2228c2ee0c901630dcb0dd7d452284c906b554750bb15dd202856895f1efb307` |
| AVNetworkToolkit-1.1.7-win-x64-setup.exe | `b76aa04428ec438c15f910a79ba51648a40ecccb161c129c2f5df7a965e85b45` |

Canonical source notes equal the published release body; GitHub Markdown rendering and four download links checked. API/public download manifests/logs/CI TRX and runner reports retained under out/Release117 and TestResults/Release117; public summary committed as evidence/published-v1.1.7.json. Final evidence-only main commit/push records results without changing the release tag/source/assets.

Final decision **SHIP WITH KNOWN RISKS** is limited to this requested Preview. OPEN SWITCH-04–07, native NIC/UAC/recovery/clipboard/physical DPI/owner acceptance, pristine-client/historical upgrades and operational update signing remain separately gated. No ordinary local startup/install/migration, real network action, Phase 07 completion or Phase 08 occurred.
