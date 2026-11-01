# 1.1.5 Preview — owner-requested corrected-package re-upload

2026-11-01 (Asia/Tashkent), ADR-052 / REL-07. Fork https://github.com/Elguajo/AV-Network-Toolkit / main, inspected base 5d0cc3e. Owner requested commit/push and explicitly selected re-uploading **1.1.5**, rather than the proposed 1.1.6. Phase 07 remains AWAITING_REVIEW.

Executed **2026-11-02** (Asia/Tashkent): [1.1.5 Preview re-uploaded](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.5), exact source **9e06c48b4b433e0463c2376ba4555c44804ea010**, [workflow 41339102871 SUCCESS](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41339102871). **SHIP WITH KNOWN RISKS, Preview only**. [Structured evidence](evidence/republished-v1.1.5.json). Preparation below is historical; final outcomes follow it.

## Preparation and replacement boundary

Original release/tag source **2b78f67cb80320c975ca18ad02677f7d0913c732**, release ID **430755509**, all four existing assets/body/tag metadata backed up to ignored `TestResults/Republish115/Previous`. `gh release download v1.1.5`, release/tag APIs and file sizes/SHA-256 comparisons PASS. Historical published hashes and evidence remain in [the original review](RELEASE_1.1.5_REVIEW.md) and [original structured evidence](evidence/published-v1.1.5.json).

Include completed c580604 tunnel notification correction and 5d0cc3e local candidate handoff, plus bounded current notes/authorization metadata. No app version/runtime/dependency/schema/data-root/UI change. Same-version downloads must be replaced deliberately after tray Exit; informational source and package hashes distinguish the correction.

The existing release workflow intentionally refuses changing an existing tag's source. Keep that safety guard unchanged. For this explicitly authorized one-time replacement, use the normal push-triggered build workflow to verify the exact new source, download its successful package candidates, independently validate source/checksums/runner/TRX evidence, then move only `refs/tags/v1.1.5` with a lease against the verified old SHA. Upload those exact CI assets and matching manifest/checksums, update release body/target, and independently download/verify again. No force push to main or other tag/release mutation. Publication remains pending until those checks finish.

## Acceptance and retained limits

Required: full unit/build/tooling/planning checks; compiler integrity controls; bundled-runtime negative control and startup; installed/ZIP payload identity, locked update/uninstall rejection, reinstall/uninstall fixture retention; all four public asset API digests/manifests/checksums/CI bytes; tag/source/executable informational identity; older release identities unchanged. Record actual outcomes below after execution.

NOT RUN: native Teredo transitions/tray delivery, NIC changes/probes/UAC/DAD/IPC/route/DNS recovery/multicast/physical egress, actual 150%/200%/mixed-monitor/touch/Narrator and pristine Windows-client/cross-source upgrade acceptance. Hosted runner same-version reinstall does not prove the original published build upgrades on every client. Keep Preview, unsigned/.NET OS-edition/conservative Apply/old-history limits. No local installation or native-network testing.

## Executed checks and publication

Ordinary `git push origin main:main` advanced 0507357 to 9e06c48, including the existing two scoped commits; no branch-history rewrite. No push-triggered run was returned by GitHub, so explicitly dispatched `gh workflow run build.yml --ref main`, observed run 41339102871 at exact SHA 9e06c48. All original workflow guards/checks were retained; release.yml unchanged and not dispatched for this one-time replacement.

| Check | Observed outcome |
|---|---|
| Local private SDK 10.0.403 `build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/Republish115/ -p:IntermediateOutputPath=obj/Republish115/ --nologo`, then `test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --no-restore` with the same paths and retained TRX | 0 warnings/errors; 744 PASS / 0 failed / 0 skipped at frozen 9e06c48 source. |
| Hosted build-and-test, independently downloaded `unit-results-9e06c48...` TRX | 0 build warnings/errors; 744 PASS / 0 failed/skipped, all 8 monitor cases present and passed; planning/schema/formats/675 links and 12 tooling tests PASS. |
| Hosted packages | Both packages built; pinned compiler reuse and 4 missing/incorrect/tampered controls PASS; global-runtime-unavailable negative control/startup, identical installed/portable payload, locked update/uninstall rejection, reinstall/uninstall original fixture retention PASS. |
| Download `AVNetworkToolkit-package-candidates-9e06c48...` from successful run; inspect manifest/runner/payload reports and both package hashes | Exact source/version/self-contained win-x64 identity PASS; all runner outcomes verified. Eight flat expected ZIP files checked. Candidate portable complete package self-test PASS locally without ordinary startup. |
| Notes validation | Markdown parsed; known-limitations fragment and four exact existing release asset URLs present. No new UI layout/capture run; original capture evidence remains historical. |

Before external replacement, live release ID/body/target/channel and all old asset IDs/digests/sizes matched the verified backup, and CI success/source was checked again. `git push origin 9e06c48...:refs/tags/v1.1.5 --force-with-lease=refs/tags/v1.1.5:2b78f67...` moved only that explicitly authorized tag. Local tag synchronized using compare-and-swap `git update-ref`. `gh release upload v1.1.5 <four verified files> --clobber` and `gh release edit --target <full source> --notes-file docs/releases/v1.1.5.md --prerelease --verify-tag` succeeded. Release ID 430755509/title/Preview preserved; new target/body/source and four asset identities intentionally replaced. The version stays 1.1.5/1.1.5.0; no installer was run on this PC.

Independently re-downloaded all four public assets. Each size/SHA-256 matches GitHub API; both packages and manifest/checksums are byte-identical to verified CI candidates. Downloaded manifest SourceCommit=9e06c48, Version=1.1.5, SelfContained=true, RuntimeIdentifier=win-x64. Both package hashes match SHA256SUMS and manifest. All 8 extracted public payload files match the checked candidate. Downloaded EXE complete package self-test PASS: WPF/JSON, create-only report, 3 malformed invocations and 4 retired-worker rejections, identity **1.1.5.0 / 1.1.5+9e06c48b4b433e0463c2376ba4555c44804ea010 / .NET 10.0.14**. API body equals canonical notes (trailing newline ignored); tag/commit API and remote refs confirm exact source.

| Re-uploaded package | SHA-256 |
|---|---|
| AVNetworkToolkit-1.1.5-win-x64-portable.zip | `1e3b4e2552ef08bd472a783178f92a056b01d2d0754f3268e66a7109ebe153fe` |
| AVNetworkToolkit-1.1.5-win-x64-setup.exe | `1b120a432c9a0794ae8694831aad5de4dcfb651f11e3828d81ec08219f21589c` |

Compared older v1.1.1/v1.1.2/v1.1.3 release IDs, targets, titles, bodies and all asset IDs/digests/sizes/timestamps before/after: unchanged. Their remote tags remain 3fd7e77/d634a80/2847ff6. Original v1.1.5 backup and historical evidence remain preserved; only the explicitly authorized same-version release was replaced. No release failure or gate relaxation occurred. GitHub's existing Node 20-to-24 action annotation remains separate from zero compiler warnings. Final evidence documentation/ordinary push follows without rebuilding/moving the release source again.
