# 1.1.5 Preview — owner-requested corrected-package re-upload

2026-11-01 (Asia/Tashkent), ADR-052 / REL-07. Fork https://github.com/Elguajo/AV-Network-Toolkit / main, inspected base 5d0cc3e. Owner requested commit/push and explicitly selected re-uploading **1.1.5**, rather than the proposed 1.1.6. Phase 07 remains AWAITING_REVIEW.

## Preparation and replacement boundary

Original release/tag source **2b78f67cb80320c975ca18ad02677f7d0913c732**, release ID **430755509**, all four existing assets/body/tag metadata backed up to ignored `TestResults/Republish115/Previous`. `gh release download v1.1.5`, release/tag APIs and file sizes/SHA-256 comparisons PASS. Historical published hashes and evidence remain in [the original review](RELEASE_1.1.5_REVIEW.md) and [original structured evidence](evidence/published-v1.1.5.json).

Include completed c580604 tunnel notification correction and 5d0cc3e local candidate handoff, plus bounded current notes/authorization metadata. No app version/runtime/dependency/schema/data-root/UI change. Same-version downloads must be replaced deliberately after tray Exit; informational source and package hashes distinguish the correction.

The existing release workflow intentionally refuses changing an existing tag's source. Keep that safety guard unchanged. For this explicitly authorized one-time replacement, use the normal push-triggered build workflow to verify the exact new source, download its successful package candidates, independently validate source/checksums/runner/TRX evidence, then move only `refs/tags/v1.1.5` with a lease against the verified old SHA. Upload those exact CI assets and matching manifest/checksums, update release body/target, and independently download/verify again. No force push to main or other tag/release mutation. Publication remains pending until those checks finish.

## Acceptance and retained limits

Required: full unit/build/tooling/planning checks; compiler integrity controls; bundled-runtime negative control and startup; installed/ZIP payload identity, locked update/uninstall rejection, reinstall/uninstall fixture retention; all four public asset API digests/manifests/checksums/CI bytes; tag/source/executable informational identity; older release identities unchanged. Record actual outcomes below after execution.

NOT RUN: native Teredo transitions/tray delivery, NIC changes/probes/UAC/DAD/IPC/route/DNS recovery/multicast/physical egress, actual 150%/200%/mixed-monitor/touch/Narrator and pristine Windows-client/cross-source upgrade acceptance. Hosted runner same-version reinstall does not prove the original published build upgrades on every client. Keep Preview, unsigned/.NET OS-edition/conservative Apply/old-history limits. No local installation or native-network testing.
