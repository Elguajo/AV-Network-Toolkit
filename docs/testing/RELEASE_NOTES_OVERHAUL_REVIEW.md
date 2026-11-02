# Release notes overhaul — owner review packet

2026-11-02, Asia/Tashkent. Repository: https://github.com/Elguajo/AV-Network-Toolkit, branch `main`, inspected clean baseline **`314bf64e152f1eebefd48ecec88e2d551c255bf0`**. Documentation only. **VALIDATED DRAFTS — owner approval required before live release-description edits.** No GitHub release was edited by this task.

## Ready for review

| Version | Rewritten | Validated | Ready to publish |
|---|---|---|---|
| [1.1.1](../releases/v1.1.1.md) | Yes | Yes | Yes, after owner approval |
| [1.1.2](../releases/v1.1.2.md) | Yes | Yes | Yes, after owner approval |
| [1.1.3](../releases/v1.1.3.md) | Yes | Yes | Yes, after owner approval |
| [1.1.5](../releases/v1.1.5.md) | Yes | Yes | Yes, after owner approval |
| [1.1.6](../releases/v1.1.6.md) | Yes | Yes | Yes, after owner approval |

These are existing published releases with **new local draft descriptions**. “Ready” does not mean the revised descriptions are published or approved. All five remain Preview. Version 1.1.4 is a local-only candidate and was left untouched; no release or version was invented.

Reusable format: [templates/RELEASE_NOTES.md](../../templates/RELEASE_NOTES.md). The release body has no H1 repeating GitHub's release title, preserving the owner's previous heading correction. Three section emojis provide navigation without decorating every heading. The plain `Known limitations` heading retains existing fragment links.

## Audit and original preservation

Fetched all pages of the live releases API, full original Markdown, repository settings and permissions. All original bodies matched their canonical local files at the baseline. Original API JSON, individual Markdown bodies, exact tag-source lookups, downloaded manifests and SHA256SUMS are retained under ignored `TestResults/ReleaseNotesOverhaul/original/`. The baseline commit also preserves all original canonical text in Git history.

| Release ID | Tag | Exact current package/tag source | Original publication, UTC |
|---|---|---|---|
| 430364237 | v1.1.1 | `3fd7e77e1f5b560172b7e23d33928bace9cdaa73` | 2026-11-01 04:36:32 |
| 430519636 | v1.1.2 | `d634a80918b429d11411d7c3afc8348d9a1d70a8` | 2026-11-01 10:53:40 |
| 430526535 | v1.1.3 | `2847ff6baaee59c813dbecb0688ffad62324b968` | 2026-11-01 11:07:01 |
| 430755509 | v1.1.5 | `9e06c48b4b433e0463c2376ba4555c44804ea010` | 2026-11-01 17:56:59 |
| 431100860 | v1.1.6 | `5bf8ece8608332c3a0685b228589b8ec30bf97c0` | 2026-11-02 05:28:04 |

Each release has four public assets: installer, portable ZIP, `SHA256SUMS.txt` and `package-manifest.json`. Downloaded **all ten small metadata/checksum files** and verified their SHA-256 against the release API; each manifest's version/source/self-contained win-x64 identity matches its current tag. Both package hashes match API digests and the checksum files. The twenty exact asset URLs are present in the appropriate draft. This task did **not** download/retest all ten binary packages; existing independent public-download/package-test reports provide that earlier evidence.

Structured metadata, original/draft body hashes, asset identities and the complete URL report: [release-notes-overhaul-2026-11-02.json](evidence/release-notes-overhaul-2026-11-02.json). The one-off validation driver and preview outputs are retained locally under `TestResults/ReleaseNotesOverhaul/`.

## Before/after summary

| Version | Before | Draft and preserved meaning |
|---|---|---|
| 1.1.1 | Long list of fork capabilities, packaging and acceptance details | Initial-fork introduction; portable AV projects, equipment links and diagnostics lead. Inherited local/scanner/history/tray/UAC work remains credited to Network Profile Manager. .NET 8, v1/no passwords, local storage, legacy incomplete recovery and native gaps retained. No artificial previous-fork compare link. |
| 1.1.2 | Audit implementation terminology and future-tense publication gates | Review local changes, recover supported DNS policy, preserve originals/drafts. Exact local Apply narrowing, atomic saves, corruption/paging/conflicts, migration retry, tray DNS, themes and .NET 10 remain explicit. The recorded diagnostic smoke issue remains unresolved **for this source**. Evidence uses actual successful publication and retains the earlier reinstall failure. |
| 1.1.3 | Engineering tray/classification details and extensive repeated release process | Focused improvements since 1.1.2: tray position and useful adapter selection. No OS removal; Show all, preserved drafts/data and GUID/cleared-target semantics retained. Sidebar remains planning only. Historical diagnostic uncertainty, installer failure and native tray/driver/DPI limits retained. |
| 1.1.5 | User-facing text, but inconsistent layout/support link and insufficiently grouped limits | Common format, clear navigation/layout/activity outcomes, one version-matched screenshot. Correction notice is the second opening callout, **before highlights**: re-download same-version packages, tray Exit, corrected source 9e06c48, no Teredo/IPv6 configuration change. 744 corrected-source tests distinguished from historical original 2b78f67 captures. Test-only hide/cancel race resolution remains test-only. |
| 1.1.6 | New features and large compatibility block, with publication prerequisites still in future tense | Prominent plaintext/v2/home-data/manual-update warning; optional passwords and deliberate verified migration lead. Preserve v1/new-file/no-downgrade, conditional clipboard cleanup, NTFS/ordinary-user/first-use consent, retained snapshots/partial copies, old-build divergence and acknowledgement. Update UI remains unavailable without production trust. Executed 887-unit/hosted evidence replaces pending wording without changing native limits. |

Extensive evidence remains linked, rather than copied into announcement prose. Evidence links are pinned to immutable commit `314bf64`, which contains the completed publication reports; version-specific contracts/changelogs/notices use their own release references. No new evidence or modern features are attributed retroactively to older binaries.

## Factual preservation ledger

| Topic | Checked preservation |
|---|---|
| Baselines and history | Initial fork 1.1.1; consecutive published comparisons 1.1.1→1.1.2→1.1.3→1.1.5→1.1.6. Current corrected 1.1.5 source/notice retained; 1.1.4 stays local-only. |
| Adapter identity and permission | Deliberate selected target, UAC, current/requested review and identity checks retained. 1.1.1 inherited local behavior is distinguished from the shared typed workflow introduced in 1.1.2. |
| DHCP/static/DNS/gateway | Shared later support refusals remain explicit: unknown/managed/complex, multiple addresses, static automatic DNS, /31–/32, no-lease/APIPA. Local blank DNS leaves unchanged; blank gateway explicitly removes the supported default after review; project static-DNS input still requires valid DNS. |
| Recovery | Exact matching journal for newer history; older history explicitly incomplete. Separately confirmed, best effort/non-atomic/can fail; DHCP restores policy, not a lease. No rollback guarantee introduced. |
| Data and migration | 1.1.1–1.1.5 LocalAppData and explicit projects unchanged. 1.1.5 private layout file/newer-format save blocking retained. Only 1.1.6 includes consented user-home migration, independent backup, original retention, NTFS/ordinary-user restrictions and old-build divergence without merge/reverse sync. |
| Passwords and update trust | No passwords in earlier versions. Only 1.1.6 has opt-in plaintext v2 passwords, separate preserved v1/new v2 file, no auto-downgrade/login, conditional clipboard cleanup with history caveat. Included updater UI is not operational automatic updating; production signing trust is absent. |
| OS/runtime/packages | 1.1.1 uses .NET 8; later versions .NET 10. Primary Microsoft OS matrices checked live. No unqualified Windows 10 support or SDK requirement for bundled packages. Unsigned status, checksums, installer UAC, complete ZIP extraction and shared per-user data retained. |
| Acceptance | Unit/component/hosted evidence is distinguished from native NIC/UAC/IPC/DAD/DNS/routes/multicast/egress/driver testing, pristine-client/cross-version installation, clipboard, migration/power loss, accessibility and physical DPI gaps. Preview/owner acceptance remains pending. |
| Credits | MIT, upstream Network Profile Manager/copyright, versioned third-party notices and no unlicensed AV Toolbag source/assets retained. |

## Media audit

- Inspected existing main-workspace and navigation captures and their source evidence. Version 1.1.5's `docs/design/navigation/idle-dark.png` is present in its **exact corrected source**. Compared Git blob bytes with the local file (SHA-256 recorded in structured evidence), visually inspected it, and linked it by full immutable source SHA. The caption identifies an isolated WPF fixture, not live network/hardware evidence.
- No newer sidebar image is used for 1.1.1–1.1.3. Their available images do not materially explain the release-specific reliability/tray changes, so no image was added merely to fill a section.
- The available update-interface captures use fake enabled/download states; they would misrepresent 1.1.6's disabled production updater. No new password/migration image was invented. Version 1.1.6 therefore has no added media.
- No existing PNG, binary or other media file was edited or generated.

## Links, rendering and second review

- **60 unique URLs: all resolved with HTTP 200**, with redirect destinations recorded and transient signed redirect-query parameters omitted. Four assets per release exactly match the public API; the one image resolves and matches the Git blob. Versioned source/contract, changelog, compare, license, notices, evidence and Microsoft compatibility links were checked. **0 unresolved included URLs/images.**
- GitHub's read-only-in-effect **Markdown rendering API** rendered all five draft bodies in GFM mode. Its returned HTML was served locally with host-like dark styling and inspected in Chromium at **1280×1000 and 390×844**: ten viewport checks, seven sections and one package table per version, one host title/no duplicate body H1, loaded screenshot, no final page overflow. This is actual GitHub-generated Markdown HTML in a **local preview**, not a published GitHub-page screenshot.
- First local 1.1.6 mobile preview overflowed on an unbroken slash-separated technical list. Replaced that wording with ordinary comma-separated phrases, then repeated using final GitHub-rendered HTML; all ten final checks passed. No product code or styling was changed.
- A second editorial pass by the **same agent**, treating original bodies as the comparison source, checked each version's factual scope, warnings, chronology, baselines, link destination and readability. This is not an independent human/subagent certification. The ledger above records the checks and corrections; no material unverified feature/test claim remains in the drafts.
- Final documentation gates: `scripts/validate_plan.py` **PASS, 915 links** (9 phases/49 stable tasks, schema/formats); `scripts/test_validate_plan.py` **12 PASS**; whitespace and protected-source checks **PASS**. Full application build/unit/native tests are **NOT RUN** for this editorial-only task. Historic numeric results are attributed to their release evidence.

## Unresolved factual/operational issues

1. **General issue reporting is unavailable.** Live repository API: `has_issues=false`, `has_discussions=false`, authenticated access has push/admin permissions. Removed the misleading Report a bug/Issue links from all five drafts; none was replaced by a guessed address or upstream tracker. The existing README/user guide still contain general issue links outside this task's scope. Owner may separately choose a real channel or approve enabling Issues. No settings were changed. The security policy is for vulnerability disclosure, not a verified substitute for general support.
2. **Historical issue wording:** 1.1.2/1.1.3 recorded DOC-R01 as unresolved; later evidence identified a test synchronization defect. Drafts explicitly scope that uncertainty to the old source and describe the later resolution only in 1.1.5. Native cancellation remains uncertified. No history was rewritten to pretend the older source included the test correction.
3. **Compatibility is version/edition dependent.** Broad historical Windows 10/11 intent is not support certification. Drafts link the applicable current Microsoft .NET 8/.NET 10 matrices and preserve ordinary Windows 10 22H2's absence for .NET 10. No compatibility result was fabricated or requirement softened.
4. **Future publication remains gated.** The current live bodies intentionally differ from these drafts. Owner approval must identify the five descriptions to update; no current approval to enable a reporting channel, alter a release title/channel/date, retag, rebuild or upload packages is inferred.

No unresolved discrepancy blocks editorial review. Operational support-channel and existing native/owner acceptance gaps remain visible rather than “fixed” through wording.

## Final validation checklist

- [x] All five currently published releases audited; full originals preserved locally and in baseline Git history.
- [x] Each important original capability, safety, compatibility and history statement mapped to the revised version or linked evidence.
- [x] First fork release handled without an invented previous version; 1.1.4 not published or edited.
- [x] Same-version 1.1.5 correction/redownload notice remains prominent; Teredo/IPv6 configuration was not disabled.
- [x] Plaintext/v2/old-build/home-data/disabled-updater restrictions prominent for 1.1.6.
- [x] Five manifests and five checksum files verified; all twenty asset URLs match metadata.
- [x] All included URLs/images resolved; image is version-correct and immutable.
- [x] GFM/wide/narrow previews and second editorial comparison complete; no duplicate H1.
- [x] Original release IDs/titles/bodies/channel/dates/targets and all twenty asset IDs/hashes/sizes/timestamps/URLs independently re-fetched and **unchanged**; tag commits unchanged.
- [x] Product source/tests/schemas/examples/tooling/workflows/changelog/README/runtime/version/unpublished 1.1.4/media unchanged by this task.
- [ ] Owner approves the proposed five descriptions.
- [ ] Revised descriptions published and independently compared afterward — **NOT RUN / intentionally gated**.

## Approved-publication procedure

After explicit owner approval, re-fetch the release list/IDs/body hashes/tag sources/assets and compare with this baseline before changing anything. If a release was superseded or its assets/source changed, rebase its draft on that evidence and return any material discrepancy for review. Otherwise, update **only each existing release body**, using the corresponding `docs/releases/v*.md` as `--notes-file`. Verify final body/file equality and unchanged identity/title/channel/source/tag/dates/assets. Do not run the release workflow or upload/rebuild/retag any package.
