# Traceable task backlog

## Compact apply confirmation and field refusal — 2026-11-04

- [x] **UI-APPLY-01** — Simplify the pictured local apply confirmation to adapter/IP/mask/gateway/DNS, explicit Apply/Cancel and optional technical details. **State:** VERIFIED_LOCAL — source bd6c2f3, 995 full unit PASS, 32 focused + 128 existing WPF captures/0 binding errors; initial Cancel focus, opening Enter/Escape refusal, explicit Apply, offline/conflict/history warnings and unchanged mutation gates verified with fake boundaries. Exact-source portable source/runtime/WPF/JSON diagnostic and all 8 extracted ZIP-file hashes PASS. [Evidence and local artifact](docs/testing/APPLY_CONFIRMATION_REVIEW.md). Owner/native acceptance pending; no phase advancement.
- [x] **SWITCH-08** — Diagnose and handle the owner's pre-mutation refusal after confirming an offline static profile. **State:** VERIFIED_LOCAL / OWNER_REVIEW_PENDING — source b3f6f30; read-only inspection of the still-running app established Rejected / Expired / MayHaveChanged=false, all three operation-barrier flags zero. Expired consent now reads a fresh snapshot and requires a new explicit confirmation; other failures never automatically retry. Two pre-fix regressions failed as expected; **1001 full unit PASS, 44 focused + 128 existing WPF captures/0 bindings**. Exact-source local portable/runtime/source/WPF/JSON diagnostic and eight extracted ZIP-file hashes PASS. [Root-cause/evidence and local candidate](docs/testing/APPLY_CONFIRMATION_REVIEW.md). Two-minute identity/pre-state/UAC/journal/worker/verification controls retained. Native/owner acceptance pending, no real mutation or phase advancement; historical UNVERIFIED findings are superseded only for this observed refusal code.

## GitHub 1.1.7 Preview — 2026-11-04

- [x] **REL-09** — Publish owner-requested 1.1.7 Preview through the existing release workflow. **AC:** final-source unit/tooling/build, unchanged-product WPF evidence, exact-source hosted installer/ZIP/runtime/install/retention checks, independent public download/tag/body/source/hash/CI-byte validation and old-release preservation. **State:** VERIFIED_PUBLISHED — source 23f2d3a, workflow 41769507274 SUCCESS; 977 local/hosted unit/12 tooling and package/runtime/install/retention gates PASS; four public assets/digests/source/tag/notes/CI bytes/EXE independently verified, five old releases/tags preserved. [Release review and limits](docs/testing/RELEASE_1.1.7_REVIEW.md). No local install/migration/live networking, production updater trust, phase acceptance or Phase 08.

## Discard confirmation and local 1.1.7 — 2026-11-04

- [x] **UI-DISCARD-01** — Replace the pictured classic discard dialog with the existing Fluent style; preserve safe cancellation/drafts, nested owner/focus and explicit discard; commit/push main and build local 1.1.7 at the owner's request. **State:** VERIFIED_LOCAL_AND_PUSHED — source ca7d475, 977 unit PASS, builds 0 warnings/errors; 4 new + 52 device + 128 existing WPF captures/0 binding errors. Exact-source self-contained installer/ZIP, source/runtime/WPF/JSON diagnostic and 8 extracted ZIP-file hashes verified. [Evidence, paths and limits](docs/testing/PROJECT_DISCARD_REVIEW.md). No installation/live network/public Release or phase advancement; owner/native review remains pending.

## Device interface usability — 2026-11-02

- [x] **UI-DEVICE-01** — Implement the owner's approved Add/Edit modal and many-device selected-card concepts. **AC:** detached Cancel/validation/owner-exit safety, essential name/URL plus optional profile/password/details, multiple-device search/group/selection/deletion, explicit Open/Apply & Open and project-file-save semantics; preserve v1/v2/password/network contracts and verify both themes/minimum sizes. **State:** VERIFIED_LOCAL / OWNER_NATIVE_REVIEW_PENDING — 977 combined unit PASS, Release builds 0 warnings/errors, 52 focused + 128 existing WPF captures/0 binding errors; exact f39f341 self-contained portable/source/runtime/8 ZIP-file hashes verified. [Contract/targets](docs/spec/DEVICE_INTERFACE_UX_CONTRACT.md), [actual renders and retained failures](docs/testing/DEVICE_INTERFACE_UX_REVIEW.md). No real network/clipboard/browser/install/private-data migration, push/public release or phase advancement.


## Responsiveness follow-up — 2026-11-02

- [x] **PERF-AUDIT-01** — Search application startup, windows, reads/writes, wait calls, styles and close paths for similar readiness/UI defects. **Verified:** source inventory and ranked findings in [review](docs/testing/UI_RESPONSIVENESS_REVIEW.md); measured native/PowerShell comparison, other durations explicitly UNVERIFIED. No claim of exhaustive native acceptance.
- [ ] **NT-PERF-01** — Remove full inventory from Network tests readiness and fix disabled preset styling. **State:** EVIDENCE_RECORDED — native 49.7–76.4 ms versus 5114.6 ms PowerShell; 969 full unit PASS (23 new), 16 focused + 64 repeat existing WPF captures/0 binding errors; retained initial focus failure, owner/native acceptance separate. [Contract](docs/spec/PHASE07_IMPLEMENTATION_CONTRACT.md), [review](docs/testing/UI_RESPONSIVENESS_REVIEW.md).
- [ ] **PERF-02** — Main startup/Refresh: worker-owned reads, safe dispatcher publication and refresh deduplication; preserve profile/history/draft/apply semantics. **State:** OPEN, source-confirmed synchronous path, duration UNVERIFIED.
- [ ] **PERF-03** — Scanner settings: coalesce per-character writes with orderly flush and preserved atomic/revision/backup/error policy. **State:** OPEN, source-confirmed per-change storage transaction, duration UNVERIFIED.
- [ ] **PERF-04** — Apply/Link-Local: fast initial identity catalogue without deferring strict mutation preparation/verification. **State:** OPEN, PowerShell readiness dependency identified, timing UNVERIFIED.
- [ ] **PERF-05** — Project/legacy import-export/CSV: asynchronous owned file operations preserving validation, drafts, overwrite consent and revisions. **State:** OPEN, synchronous commands identified, slow-file timing UNVERIFIED.
- [ ] **PERF-06** — Storage startup: responsive preparation/consent/recovery while retaining verified root selection and migration safety. **State:** OPEN, synchronous pre-window inventory identified; no migration policy change.
- [ ] **PERF-07** — Measure/coalesce small settings/layout work without breaking external-revision protection. **State:** OPEN, repeated UI file reads/writes identified; actual duration UNVERIFIED.
- [ ] **PERF-08** — Reproduce the owner's Network tests caption→main-hide sequence on the actual Portable. **State:** OPEN / cause UNVERIFIED; owner confirms tray remains. Safe native-caption fixtures keep main visible. Real input was unavailable for the hidden window; owner stopped Computer Use with Escape. Do not claim fixed or change tray behavior speculatively.

The fixed nine-phase order and earlier acceptance/live-network/publication gates remain. Findings outside Phase 07 are recorded for scoped follow-ups, not silently implemented here.

## Field profile-switching correction — 2026-11-02

- [x] **SWITCH-03** — Permit ordinary IPv4/DHCP configuration with an unplugged cable; verify stored policy separately and prevent offline Apply & Open. **State:** IMPLEMENTED_LOCAL / FOCUSED_CHECKS_PASS — exact de02b22 source, 946 unit/23 observer fixtures per shell/28 focused renders and package/source/8 ZIP-file hashes PASS. Existing 128-render working-tree run passed; frozen full focus runs NOT PASS with competing UI harnesses (recorded), serial focus recheck/native/owner acceptance UNVERIFIED. [Contract, exact evidence and four OPEN adjacent findings](docs/testing/PROFILE_SWITCHING_AUDIT_2026-11-02.md). No native mutation, push/release or phase acceptance.
- [x] **SWITCH-AUDIT-01** — Review adjacent profile/apply/recovery refusals and messaging after the owner's field reports. **State:** FINDINGS_RECORDED — 8 isolated reproductions confirm SWITCH-04–07; [report and preserved reproduction source](docs/testing/PROFILE_SWITCHING_AUDIT_2026-11-02.md). Detection completed, repairs remain separate; native behavior not certified.
- [ ] **SWITCH-04** — Allow a classified manual Link-Local source to switch directly to an ordinary static saved profile. **State:** OPEN / NOT_IMPLEMENTED — current transition rejects it while recovery recognizes it; see audit report. Retain worker/recovery/verification safeguards.
- [ ] **SWITCH-05** — Align local Add/Edit semantic validation with typed apply: mask, host/subnet, gateway and DNS relationships. **State:** OPEN / NOT_IMPLEMENTED — five saved-but-rejected examples reproduced; preserve existing records and editable drafts.
- [ ] **SWITCH-06** — Validate primary/secondary DNS in main manual settings before enabling Apply. **State:** OPEN / NOT_IMPLEMENTED — invalid DNS currently passes IsEditValid without field error; retain empty-DNS contract.
- [ ] **SWITCH-07** — Keep final-journal warnings visible after connected recovery in the project apply window. **State:** OPEN / NOT_IMPLEMENTED — final recovery status currently overwrites the warning; original snapshot remains retained.

- [x] **SWITCH-02** — Complete the field correction for ordinary connected DHCP/APIPA or no-address → static profile with gateway and empty DNS; retain original policy/history and explicit DHCP-without-lease result without opening a browser. **State:** VERIFIED_LOCAL — exact source e299418, 922 unit PASS, 20 observer fixtures per shell/148 renders PASS, builds 0 warnings/errors; new self-contained portable source/runtime/WPF/JSON diagnostic and 8 ZIP-file hashes verified. [Contract/evidence and prior-source red reproduction](docs/testing/APIPA_PROFILE_SWITCHING_REVIEW.md). Native apply/UAC/recovery and owner acceptance UNVERIFIED; no real settings/profile edits, push/release or phase advancement.

- [x] **SWITCH-01** — Fix ordinary Windows adapter observation and empty-DNS/profile transitions after the owner's failure report. **AC:** reproduce empty persistent-store/null route protocol/empty policy-container defects; source-only duplicate/APIPA handling; reviewed DNS preservation/known-empty recovery; strict target verification, journal/consent/identity, fake-worker negative coverage, full unit/WPF/tooling checks and local portable artifact. **State:** VERIFIED_LOCAL — source e3315a2, 902 unit/18 observer fixtures per shell/12 tooling PASS, builds 0 warnings/errors, 128 existing + 12 focused WPF captures/0 binding errors; self-contained portable diagnostic/source/8 ZIP-file hashes verified. [Evidence/compatibility and retained failures](docs/testing/PROFILE_SWITCHING_REVIEW.md). Native apply/UAC/recovery and owner acceptance remain UNVERIFIED; no push/release or phase acceptance.

## Release-note overhaul — 2026-11-02

- [x] **DOC-04** — Audit and prepare standardized descriptions for every published release, with a reusable template and review packet. **AC:** current API inventory/full originals, per-version verified features/limits/history, actual assets/checksums, all included links/media/rendering, second editorial pass and protected-source/live-release preservation; stop before external publication. **State:** VALIDATED_DRAFTS / OWNER_REVIEW_PENDING — five drafts (1.1.1/1.1.2/1.1.3/1.1.5/1.1.6), 60 URLs/20 asset links/10 GFM viewport checks PASS; [report and pending approval/publication checklist](docs/testing/RELEASE_NOTES_OVERHAUL_REVIEW.md), [template](templates/RELEASE_NOTES.md). Existing live descriptions and 1.1.4 unchanged. Issues/Discussions disabled; no settings changed. This checkbox completes preparation only, not publication or Phase 07 acceptance.

## Combined new Preview — REL-08, 2026-11-02

- [x] **REL-08** — Wait for updater/password/DATA-01 sessions, review the completed combined source and publish a new Preview. **AC:** complete regression/build/tooling/WPF checks, same-source installer/ZIP, disposable hosted runtime/install/reinstall/uninstall/data-retention gates, exact source/tag/assets/download checks and handoff; retain old releases and phase/native limits. **State:** VERIFIED_PUBLISHED — [1.1.6 Preview](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.6), exact source 5bf8ece, [workflow 41393820449](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41393820449) SUCCESS; 887 local/hosted unit, 12 tooling and all package gates PASS. 286 WPF captures/0 binding errors on identical product code; public API/tag/checksums/CI bytes/downloaded EXE and previous releases verified. Corrected one stale UI hint and two harness defects without weakening checks. [Evidence and retained failures](docs/testing/RELEASE_1.1.6_REVIEW.md), [notes](docs/releases/v1.1.6.md). Production update trust/native/owner/phase acceptance remain separate; updates manual.

## Project/device opening invariant review — 2026-11-02

- [x] **OPEN-01** — Verify catalogue/device opening never applies network settings and explicit Apply & Open retains identity/consent/apply/observation/browser gates. **State:** VERIFIED_SAFE_ONLY at a75a3ac — no product defect or code change needed; 237 focused / 744 full unit PASS, 128 WPF captures / 0 binding errors, solution/harness builds 0 warnings/errors. [Evidence, main-versus-detached selection distinction and limits](docs/testing/OPENING_NETWORK_INVARIANT_REVIEW.md). No native acceptance, phase completion, DATA-01/Phase 08, updater or publication change.

## Application update investigation — 2026-11-02

- [x] **UPD-01** — Inspect current packaging, release identity, data preservation and exit lifecycle; prepare a minimal update proposal with source-integrity/Preview/consent/portable policy and acceptance checks. **State:** INVESTIGATION_RECORDED — clean main at 05cac3b, public v1.1.5 release/tag independently read as 9e06c48, official API/installer/crypto documentation consulted. [Proposal/evidence](docs/spec/APPLICATION_UPDATE_PROPOSAL.md). No updater implementation or native test claimed.
- [x] **UPD-02** — Resolve source-authenticity and portable update strategy; adopt the approved bounded implementation contract. **State:** OWNER_DECISIONS_RECORDED — owner selected signed manifests and fresh portable folders retaining the old copy. [Contract](docs/spec/APPLICATION_UPDATE_PROPOSAL.md). No production keys/secrets/push/release/install permission implied.
- [x] **UPD-03** — Implement and locally verify the selected application update flow. **State:** LOCAL_CHECKS_PASS / NOT_RELEASE_READY — 44 updater + 744 baseline = 788 isolated unit PASS; 20 focused/52 existing WPF captures, signing-tool and packaged public-key diagnostics PASS. [Evidence and limits](docs/testing/APPLICATION_UPDATE_REVIEW.md). Missing production trust fails closed; installer handoff/UAC is UNVERIFIED. Concurrent password/DATA-01 code excluded; no phase advancement.
- [ ] **UPD-04** — Prepare operational trust and native update acceptance before distribution. **State:** NOT_STARTED / SEPARATE_AUTHORIZATION_REQUIRED — concrete production-key custody/recovery and public-key bootstrap, signing/upload integration, isolated supported-Windows cross-source upgrade/UAC/second-instance/locking/data-retention evidence; then exact-source release gates and publication only if requested. Current v1.1.5 remains manual; no native outcome inferred from UPD-03.

## Owner-requested 1.1.5 re-upload — 2026-11-01

- [x] **REL-07** — Re-upload 1.1.5 with the completed tunnel notification fix. **AC:** verified original release backup, ordinary main commits/push, exact-source unit/tooling/build/package/runtime/install gates, guarded v1.1.5 tag/four-asset replacement and independent public-download verification; other releases unchanged. **State:** VERIFIED_PUBLISHED — ADR-052; source 9e06c48, workflow 41339102871 SUCCESS, local/hosted 744 unit/12 tooling and package gates PASS, four public API digests/manifest/checksums/CI bytes/downloaded EXE verified. Only owner-selected v1.1.5 tag/assets replaced; other releases unchanged. [Evidence](docs/testing/RELEASE_1.1.5_REUPLOAD_REVIEW.md).

## Tunnel notification correction — 2026-11-01

- [x] **NOTIFY-02** — Prepare a local corrected executable after the owner reported continuing Teredo Connection lost messages. **AC:** exact NOTIFY-01 source in a self-contained local EXE/ZIP, focused monitor regression and payload/ZIP diagnostics verified, explicit launch handoff without installing or publishing. **State:** VERIFIED_LOCAL — source c580604, 8 focused PASS, both package diagnostics PASS and 8 ZIP files hash-identical. [Evidence](docs/testing/TUNNEL_NOTIFICATION_REVIEW.md#local-corrected-build-after-continuing-notifications). Published 1.1.5 and installed files unchanged.

- [x] **NOTIFY-01** — Exclude tunnel lifecycle changes from tray connection notifications. **AC:** repeated Teredo/renamed-tunnel transitions stay silent; loopback exclusion and Ethernet/Wi-Fi/ordinary virtual up/down/IP notifications remain; fake-only regression reproduction and validation. **State:** VERIFIED_LOCAL — 8 focused / 744 full unit PASS, Release build 0 warnings/errors; removing only the tunnel exclusion reproduces 3 failures. [Evidence](docs/testing/TUNNEL_NOTIFICATION_REVIEW.md). Published 1.1.5 unchanged; native/phase gates remain.

## Release-note editorial follow-up — 2026-11-01

- [x] **DOC-03** — Present the latest published release as reader-oriented release notes. **AC:** outcome-led highlights, accurate changes relative to prior published version, package links/update instructions, preserved compatibility/data/recovery/Preview limits and observed verification; published body matches tracked notes without changing source/tag/assets. **State:** VERIFIED — [1.1.5 notes](docs/releases/v1.1.5.md) updated on GitHub, all four assets and exact tag source unchanged, local wide/narrow preview and documentation gates PASS. Earlier release notes and application behavior unchanged.

## Current publication — ADR-051, 2026-11-01

- [x] **REL-06** — Publish current navigation/layout/activity work as 1.1.5 Preview. **AC:** scoped commits and ordinary main push, exact-source build/unit/planning and disposable package/runtime/install/reinstall/uninstall gates, same-source installer/ZIP/checksums/manifest, independent public asset/source verification and accurate known limits. **State:** VERIFIED — [release](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.5), source 2b78f67, [CI 41333770039](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41333770039) PASS; 736 hosted/local unit, 12 tooling, 162 local WPF captures and all hosted package gates PASS. Public assets/tag/source/digests/checksums/CI bytes and downloaded EXE verified; older releases/local 1.1.4 retained. [Evidence](docs/testing/RELEASE_1.1.5_REVIEW.md). Native/DPI/phase acceptance remains separate.

## Navigation follow-up — ADR-050, 2026-11-01

- [x] **UI-PREF-01** — Remember main-window/sidebar layout across starts. **AC:** reviewed additive private versioned preference, atomic preservation/compatibility/conflict/retry, normal/max/min/tray/offscreen restoration without touching profiles. **State:** VERIFIED_LOCAL at 2356dec; 13 new storage/geometry unit cases and actual WPF restart/placement checks PASS. [Contract](docs/spec/NAVIGATION_FOLLOWUP_CONTRACT.md), [evidence](docs/testing/NAVIGATION_FOLLOWUP_REVIEW.md).
- [x] **UI-ACTIVITY-01** — Indicate actual tests/mDNS activity on the rail. **AC:** accessible marks, inline/peer aggregation, cancellation drain, mDNS start/stop/close, no automatic traffic and subscription cleanup. **State:** VERIFIED_LOCAL at 2356dec; fake-lifecycle/overlap/late-echo checks and Light/Dark captures PASS. [Evidence and limits](docs/testing/NAVIGATION_FOLLOWUP_REVIEW.md).
- [ ] **UI-DPI-01** — Verify actual 150%/200% and mixed-monitor interaction. **AC:** native HWND/WPF/monitor DPI agreement, work-area bounds and keyboard/resize/tray behavior on real target scales. **State:** PARTIALLY_VERIFIED — PerMonitorV2/96 DPI native agreement, monitor-relative 96/144/192 geometry and layout simulations PASS; actual 150%/200%/mixed-DPI transfer NOT RUN (one physical 2560x1080/96-DPI display). No global display changes. [Evidence](docs/testing/NAVIGATION_FOLLOWUP_REVIEW.md). Phase 07 and publication gates unchanged.

## Adapter-list options placement — 2026-11-01

- [x] **UI-ADAPTER-OPTIONS-01** — Move Show all adapters into the list header's More menu. **AC:** compact header, named keyboard-accessible menu/checkable item, existing filtering/drafts/selection behavior and refresh preserved; Light/Dark normal/minimum checks. **State:** VERIFIED_LOCAL — Release build 0 warnings/errors, 25 focused unit PASS, 20 focused + 128 full WPF captures/0 binding errors. [Review](docs/testing/ADAPTER_OPTIONS_REVIEW.md). Packages and Phase 07 acceptance unchanged.

## Profile-source placement — 2026-11-01

- [x] **UI-SOURCE-01** — Separate the profile-source selector from the saved-profile heading. **AC:** visibly labelled source row in both scopes; existing stores/actions/style preserved; Light/Dark normal/minimum renders, keyboard/source round trip and regression checks. **State:** VERIFIED_LOCAL — 44 focused + 128 full WPF captures/0 binding errors, 723 unit PASS, build 0 warnings/errors. [Evidence and retained failures](docs/testing/PROFILE_SOURCE_REVIEW.md). Concurrent navigation changes independent; packages unchanged, Phase 07 AWAITING_REVIEW.

## Local application for owner testing — 2026-11-01

- [x] **BUILD-01** — Bump to 1.1.4 and provide current self-contained application for owner manual testing. **AC:** synchronized version metadata/notes, pinned committed source, Release build/unit/planning checks, both local packages and extracted payload identity/WPF/JSON/self-test rejection evidence, exact executable path. **State:** VERIFIED_LOCAL — source 629c2ae, Release build 0 warnings/errors, 723 unit PASS, both packages generated, source/runtime/version and 8 extracted payload hashes plus positive/negative package checks PASS; [local review and EXE path](docs/testing/LOCAL_PREVIEW_1.1.4_REVIEW.md). No GitHub publication, installation, agent live-network test or phase advancement.

## Diagnostic hide smoke investigation — 2026-11-01

- [x] **DOC-R01** — Reproduce and resolve intermittent diagnostic hide/cancellation smoke failure. **AC:** Causal ordering evidence, deterministic main/separate-window cancellation checks, late-echo completion/no restart, negative controls with each actual handler removed, relevant regression results. **State:** VERIFIED — test synchronization defect; no product change. [Review and limits](docs/testing/DIAGNOSTIC_HIDE_REVIEW.md). Historical OPEN mentions below and in release evidence are superseded for this issue only. Owner performs manual acceptance; Phase 07 remains AWAITING_REVIEW, no live-network/publication/Phase 08 action.

## README product documentation — 2026-11-01

- [x] **DOC-02** — Rewrite the README as a product entry point with an actionable user guide. **AC:** clear AV use case, download choice/quick start, factual feature matrix, retained seven-image gallery, discoverable documentation/contribution links, existing compatibility/security/recovery/Preview limits retained. **State:** VERIFIED — local Markdig/browser preview checked at 1280x1000 and 390x844, images/anchors/tables checked; planning/schema/links and 12 tooling tests PASS. [User guide](docs/USER_GUIDE.md). Documentation only; source/release publication and Phase 07 acceptance unchanged.

## Tray menu placement — 2026-11-01

- [x] **UI-TRAY-01** — Open the tray menu above the clicked icon rather than at the primary screen corner. **AC:** capture click location, use its monitor, convert pixels/DIPs, clamp edges, preserve menu actions/style. **State:** VERIFIED locally — 9 focused unit PASS, 6 Light/Dark HWND-placement/action captures PASS/0 binding errors, 723 full unit PASS on the combined tree. [Evidence and native limits](docs/testing/TRAY_PLACEMENT_REVIEW.md). Phase 07 AWAITING_REVIEW; installed app/release unchanged.

## Adapter list filtering — 2026-11-01

- [x] **ADAPTER-LIST-01** — Hide confirmed system filters/idle built-in WAN/debug plumbing from the main/tray list by default, with Show all. **AC:** preserve working/virtual/disconnected/unknown interfaces, stored data and drafts; hiding/removing a selected target clears selection; no OS mutation/probes; regression and Light/Dark normal/minimum captures. **State:** VERIFIED locally — 714 scoped / 723 final combined unit PASS, 12 focused + 128 existing WPF captures/0 binding errors, focused repeat at actual 760x520 minimum PASS; read-only production enumeration 24 total/17 hidden/7 visible. [Evidence and limits](docs/testing/ADAPTER_VISIBILITY_REVIEW.md). Phase 07 AWAITING_REVIEW; no system removal/publication/native acceptance.

## Left navigation planning — 2026-11-01

- [x] **UI-NAV-01** — Add an icon rail and collapsible, resizable adapter sidebar. **AC:** [Brief](docs/spec/LEFT_NAVIGATION_BRIEF.md), [research/component contract](docs/spec/LEFT_NAVIGATION_RESEARCH.md) and ADR-049 hover-reveal refinement. **State:** VERIFIED_LOCAL / OWNER_REVIEW_PENDING — source 7d62a26, Release builds 0 warnings/errors, 723 unit PASS, 26 focused + 128 workspace + 20 adapter + 64 diagnostic WPF captures PASS/0 binding errors. Real hover, native caption behavior, automation/focus/tab order, resize/draft/scroll/operation preservation checked. [Evidence and limits](docs/testing/LEFT_NAVIGATION_REVIEW.md). Width is window-local; actual mixed-DPI/Narrator/native/phase acceptance remains separate. No package/publication/Phase 08 change.

## Current publication — ADR-048, 2026-11-01

- [x] **REL-05** — Commit/push all completed current project changes in scoped commits and publish the new Preview. **AC:** Preserve topical commits and old tags/assets; include audit/theme/tray/adapter fixes in one exact source, synchronized version/docs, full build/unit/planning and disposable Windows package workflow PASS, independently verified published source/tag/checksum/payload. **State:** VERIFIED — final [v1.1.3 Preview](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.3), source 2847ff6, [CI 41300112524](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41300112524): 723 unit/12 tooling PASS, build 0 warnings/errors, all hosted package gates PASS; local 146 WPF captures PASS, downloaded packages/EXE verified. Both prior releases preserved, caches excluded. [Full evidence](docs/testing/RELEASE_2026-11-01_REVIEW.md). Phase/native/clean-client acceptance and DOC-R01/other known issues remain open.

## Window appearance follow-up — 2026-11-01

- [x] **UI-THEME-01** — Fix light controls/text on a dark window background. **AC:** Existing/new and nested windows agree with Light/Dark, opaque fallback resets, native HWND agreement is checked separately from composited captures. **State:** VERIFIED locally — causal red control fails with the old ownership filter; fixed transitions PASS, 8 focused/128 existing/8 audit captures PASS, 687 unit PASS. [Evidence and limits](docs/testing/WINDOW_THEME_REVIEW.md). DOC-R01 diagnostics hide/cancellation still FAIL; no phase/native acceptance or published binary update.

## Audit remediation — ADR-047, 2026-11-01

Owner requested “Исправь все что нашел” and delegated the complex-state design choice. [Contract](docs/spec/AUDIT_FIXES_CONTRACT.md), [review](docs/testing/AUDIT_FIXES_REVIEW.md). A26-01–08 are LOCALLY_VERIFIED: atomic preservation/backup/conflict handling, independent paged recovery, typed local Apply/Revert with DNS snapshots, retired generic worker, failed-save draft retention, migration retry, shared tray DNS matching and .NET 10 migration. Release build 0 warnings/errors, 687 full unit PASS, 8 focused + 128 existing + 64 diagnostic WPF captures/0 binding errors. Package/final documentation evidence follows in STATUS. Native/owner acceptance pending; these are separate follow-up IDs, not completion of any of the 49 phase tasks. Phase 07 remains AWAITING_REVIEW, DATA-01/Phase 08 unchanged. Historical DOC-R01/UX-R01/P05-R01 remain open.

## README screenshot gallery — 2026-11-01

- [x] **DOC-01** — Add current application screenshots to README. **AC:** Actual WPF captures with synthetic fixtures, main overview and captioned feature gallery, Light/Dark examples, image provenance and valid local links. **State:** VERIFIED — seven unchanged PNG captures visually inspected; [provenance and observed harness limitation](docs/screenshots/readme/README.md). Documentation-only; Phase 07 remains AWAITING_REVIEW. Separate DOC-R01 diagnostic hide/cancellation assertion failed during capture; no product fix or native acceptance claimed.

## Repository audit / research — 2026-11-01

[A26-01–08 findings and proposed sequence](docs/testing/REPOSITORY_AUDIT_2026-11-01.md) recorded from current source and 9 isolated audit scenarios. Findings remain OPEN; no repair scope, roadmap reprioritization, runtime/storage/auth migration or new phase was approved by the audit request. Existing phase IDs/checkboxes and REL-02 native acceptance are unchanged. Prioritize owner review of data preservation/recovery availability and .NET 8 servicing before stable acceptance.

## Product mark — ADR-044, 2026-11-01

- [x] **BRAND-01** — Use the owner's supplied logo and convert it to the Windows application icon. **Verified:** original transparent PNG retained, 10 ICO sizes, existing EXE/window/title-bar/tray/installer bindings retained; Release build, 681 unit PASS and 128 WPF captures/0 binding errors. [Evidence and limits](docs/testing/BRANDING_REVIEW.md). Phase 07 remains AWAITING_REVIEW; no new package/publication/native acceptance claim.

## Network-test presets — ADR-041, 2026-11-01

Separate follow-up ID; preserve the existing 49 phase task IDs/order.

- [ ] **NT-01** — Implement the approved expanding Network tests section and ready-made diagnostic presets. **AC:** [contract](docs/spec/NETWORK_TEST_PRESETS_CONTRACT.md): explicit source/run, Ping four/continuous/single, one TCP port, descriptions, Stop/close, bounded results and independent peers; fake-boundary tests and actual Light/Dark normal/minimum WPF evidence. **State:** EVIDENCE_RECORDED — 681 full unit PASS, 50 added cases, Release builds and 64 focused + 128 existing WPF captures/0 binding errors; owner/native acceptance remains separate. [Review](docs/testing/NETWORK_TEST_PRESETS_REVIEW.md).

This diagnostic addition does not change independently authorized REL-03 (ADR-040) or start Phase 08. Phase 07 remains AWAITING_REVIEW; ADR-037 safe-only execution persists.

## Release preparation follow-ups — ADR-036, 2026-11-01

Separate from phase acceptance and the 49 phase tasks; execute in this order:

| ID | Follow-up | State |
|---|---|---|
| REL-01 | Investigate/fix UX-R01/P05-R01 with causal reproduction and meaningful regression coverage | FIXED / EVIDENCE_RECORDED and included in v1.1.1 Preview; [reproduction, cause, correction](docs/testing/JSON_VALIDATION_REVIEW.md); no historical phase/native acceptance inferred |
| REL-02 | Prepare isolated Windows profile → observed IP/prefix/gateway/DNS → device open → restore, UAC denial, NIC disconnect and partial failure | LOCAL_LAB_PREPARED / NATIVE_ACCEPTANCE_PENDING (ADR-046): SDK/Python installed, independent Windows 10 VMware clone/private disconnected NIC/checkpoint, boot/desktop observed, self-contained payload staged; [evidence and remaining payload/recovery/NIC/peer gates](docs/testing/LOCAL_WINDOWS_LAB.md). 681 unit PASS; native app/network tests NOT RUN. Earlier [156 fake tests + observer fixtures](docs/testing/ISOLATED_WINDOWS_PREPARATION.md) retained |
| REL-03 | Confirm GitHub CI and same-version installer + portable ZIP on clean Windows without preinstalled .NET (ADR-039) | HOSTED_EVIDENCE_RECORDED: [release CI](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41273489475) PASS at 3fd7e77; 681 unit, both packages/main-window launch with global runtime unavailable, install/reinstall/uninstall/retention/lock/hash checks PASS. Hosted Windows Server is not pristine-client/native acceptance; [limits](docs/testing/PACKAGING_REVIEW.md) |
| REL-04 | Update first-release README/changelog/version/description/limits/checksum | PUBLISHED_PREVIEW / VERIFIED: [v1.1.1](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.1), new logo, same-source installer/ZIP, SHA256SUMS/manifest, README/changelog/notes/limits. Version remains 1.1.1 by owner request (ADR-045); native/phase acceptance not claimed |

Phase 07 remains AWAITING_REVIEW. ADR-045 authorizes this v1.1.1 Preview publication only; earlier no-push/no-release snapshots are historical for that scope. No Phase 08, DHCP server or native network test authorized.

ADR-038 maintenance follow-up: owner confirmed CODEX_START.md deletion; retired redundant entry and enforced existing AGENTS/bootstrap/resume/handoff documents instead. Real-tree planning/schema/links PASS, 12 tooling regressions PASS; JSON scoped self-review found no new actionable defect, 60 focused PASS and 10 retained TRX hashes verified. Phase/task acceptance and native/deferred release gates unchanged.

ADR-039: owner accepted primary installer + additional portable ZIP with bundled .NET, same application version/features/UAC boundary. Requirements recorded in PROJECT_CONTEXT and RELEASE_CHECKLIST; neither package is claimed implemented/verified. No USB-local storage mode, automatic updates, new native permission or release publication implied.

**DATA-01 — ADR-042, after packaging:** owner approved the [complete home-only contract](docs/spec/DATA_STORAGE_MIGRATION_CONTRACT.md), including `%USERPROFILE%\.av-network-toolkit`, retained originals, independent snapshot, conflict/failure/retry/downgrade policy and bounded preference hardening. **VERIFIED_LOCAL / NOT_RELEASED.** [Exact-source review](docs/testing/DATA_STORAGE_MIGRATION_REVIEW.md): implementation 0da74c6 + fixes 8cbf88e/0905d89; final 0905d89 export Release solution/harness builds 0 warnings/errors, 886 full unit PASS including 50 DATA-01/root cases, 12 storage + 128 regression WPF captures/0 binding errors, 12 tooling/832 source links PASS. Runtime first-use consent still precedes personal-data copying. No personal data migrated; published packages retain LocalAppData. Hosted install/first-run/native acceptance NOT RUN; USB separate/unimplemented. Phase 07 AWAITING_REVIEW; no Phase 08/publication action.

**Current state:** Phase 07 AWAITING_REVIEW under ADR-034 (2026-11-01), directly on main, implementation `87d02ce7ba7eaaee18b45825270953081f4f69de`. 07-T01–07-T04 EVIDENCE_RECORDED: explicit diagnostic identities/types/facts/routes, opt-in saved Hyper-V uplink filter and shared adapter/source IPv4 Ping. [Review packet](docs/testing/PHASE07_REVIEW.md): 624 full unit PASS (48 added), 15 fake Windows-query fixtures, 32 focused + 128 existing WPF captures/0 binding errors. Acceptance checkboxes remain open; native OS/network acceptance NOT RUN. Earlier owner/native limits and OPEN UX-R01/P05-R01 remain; no push/release or Phase 08.

**Previous state:** Phase 06 AWAITING_REVIEW under ADR-023, directly on main (ADR-017). 06-T01–06-T04 have EVIDENCE_RECORDED: passive selected-NIC listener, bounded parser/cache/lifecycle, service UI/filter/CSV; 79 added cases, 542 total PASS and 84 WPF captures/0 binding errors. [Review packet](docs/testing/PHASE06_REVIEW.md). Acceptance checkboxes remain open; native multicast/isolated NIC tests NOT RUN. Phase 05/earlier manual limits, OPEN P05-R01 and separate IDEA-01–03 remain. No release/Phase 07/08/live tests or automatic publication of new Phase 06 changes.

**Prior state:** Phase 02 implemented and verified; AWAITING_REVIEW, not marked phase complete. 02-T02–02-T06 have EVIDENCE_RECORDED with exact commits/tests in docs/testing/PHASE02_REVIEW.md; acceptance checkboxes remain open pending owner review. 01-T06–01-T08 verified, 02-T01 approved. Owner refinements ADR-010/011 require existing design and English-only UI. Historical Phase 01 review items remain open. Stable IDs unchanged. ADR-013 adds the verified structural path refinement (`app/`, `test/`, `AVNetworkToolkit.sln`), with no new feature or phase advancement.

**Owner follow-up — ADR-018:** display/product branding corrected to AV Network Toolkit; VERIFIED by Release build, 308 isolated tests and resource/EXE/DLL metadata checks. Exact commands and containing-commit lookup in STATUS.md. Phase acceptance checkboxes and all stable IDs unchanged.

## Original-outcome follow-ups — 2026-10-31

These stable follow-up IDs are separate from the 49 phase tasks; no phase task is accepted, renumbered or removed. Evidence/criteria: [product-fit review](docs/PRODUCT_FIT_REVIEW_RU.md); owner-authorized IDEA-01/04 implementation: [workspace review](docs/testing/MAIN_WORKSPACE_REVIEW.md). IDEA-02/03 scheduling/design remain pending; Phase 06 acceptance and nine-phase order unchanged.

| ID | Follow-up | State |
|---|---|---|
| IDEA-01 | Apply a project profile without a device or browser URL; retain explicit NIC/review/UAC/verification/recovery | EVIDENCE_RECORDED (ADR-028); standalone typed apply with selected GUID, tests in MAIN_WORKSPACE_REVIEW; owner/native acceptance pending |
| IDEA-02 | Define/test the real DHCP → isolated Static workflow, including DNS/no-lease/APIPA boundaries; preserve conservative blocking until supported | GAP RECORDED; support refinement needs scoped design/review |
| IDEA-03 | FR-13: optional device-password storage in project JSON; version/compatibility, explicit UI, redaction and backups | VERIFIED_LOCAL / NATIVE_OWNER_REVIEW_PENDING — owner accepted [v2 contract](docs/spec/DEVICE_PASSWORD_PROPOSAL.md); [implementation/evidence](docs/testing/DEVICE_PASSWORD_REVIEW.md): 795 isolated unit PASS, 20 focused + 128 existing WPF captures/0 binding errors. V1 preserved, first v2 save separate, passwords masked/redacted, clipboard fake tests PASS. Final combined tree 876 unit PASS/0 failed/skipped; native clipboard/cross-PC/owner acceptance and publication remain separate |
| IDEA-04 | FR-14: coherent main-screen adapter → current state → profile/manual settings → explicit apply → observed result, with optional device Open/Apply & Open; reuse IDEA-01/02 and DESIGN.md | EVIDENCE_RECORDED (ADR-028–032); [reference actions/current state](docs/testing/PROFILE_VISUAL_STATES_REVIEW.md), 140 WPF captures/0 binding errors, 576 unit PASS. Current-state Selected confirmed by owner, full task/native acceptance pending. OPEN UX-R01/P05-R01 remains unresolved despite green run; IDEA-02/03 separate |

## Phase 01: Baseline audit (P0)

- [ ] **01-T01** — Record fork origin, exact commit, branch, license and build prerequisites. **AC:** Evidence with git commands, repo SHA and MIT notice recorded.
- [ ] **01-T02** — Map models, viewmodels, services, UAC/netsh, data paths, scanner and revert. **AC:** Source map with paths, entry points, call graph and risk notes.
- [ ] **01-T03** — Inventory all existing capabilities, supported data formats and compatibility. **AC:** Baseline preservation checklist signed off.
- [ ] **01-T04** — Run non-invasive baseline build/test/static checks where supported. **AC:** Exact commands/results/limitations; no device configuration.
- [ ] **01-T05** — Review risk surface and propose implementation boundaries for Phases 02–09. **AC:** Audited phase plan, security assumptions and open decisions documented.

- [x] **01-T06** — Prepare the SDK/build environment in a separately authorized checkpoint. **AC:** Record SDK 8 version, approved install/restore scope and baseline build command/result; no application launch, elevation or network probes.
- [x] **01-T07** — Isolate automated tests from network probes and real adapter enumeration. **AC:** Review exact safe test set; fake OS/probe boundaries or segregate integration cases; run the isolated baseline and report every exclusion as NOT RUN.
- [x] **01-T08** — Repair and verify the planning validator in a separate tooling task. **AC:** Support current plan states and task inventory; distinguish GitHub-relative links from filesystem links; retain meaningful checks and demonstrate expected valid/invalid cases.

## Phase 02: Project-scoped JSON profiles (P0)

- [x] **02-T01** — Approve independent versioned project schema and round-trip semantics. **AC:** Schema reviewed; includes project, profiles, adapter hints, device refs; no secrets.
- [ ] **02-T02** — Implement Open Project / Choose Folder / Save As with explicit path. **AC:** Open/save project JSON from arbitrary user-approved folders; never overwrite silently.
- [ ] **02-T03** — Import/export static + DHCP settings including gateway and DNS. **AC:** Round-trip preserves all specified fields and legacy profile compatibility.
- [ ] **02-T04** — Handle schema versions, corrupt files, atomic writes and backups. **AC:** Clear errors, original file preserved, missing/unknown fields handled per spec.
- [ ] **02-T05** — Add unit tests for parse/validate/export, path security and migration. **AC:** Automated test evidence; existing ProfileRepository unaffected.

- [ ] **02-T06** — Review legacy persistence remediation as a separate compatibility change. **AC:** Document direct-write and backup-failure/delete risks, recovery fixtures and proposed scoped PR; do not change legacy storage without reviewed authorization.

## Phase 03: Device Web Interfaces (P0)

- [ ] **03-T01** — Design device model with name/type/IP/URL/profile reference and group. **AC:** Explicit identity, profile association and full URL fields.
- [ ] **03-T02** — Add per-project Device Web Interfaces UI: create/edit/delete/search/group. **AC:** Manage multiple device links for one profile without UI redesign.
- [ ] **03-T03** — Launch full http/https deep URL with browser default and safe validation. **AC:** Path, query, fragment preserved; unsafe schemes and URL credentials rejected.
- [ ] **03-T04** — Handle HTTP warning, unreachable endpoints and stale profile refs. **AC:** No hidden credential handling or unsafe TLS suppression.
- [ ] **03-T05** — Automated tests for URL parsing/launch abstraction and JSON round-trip. **AC:** Tests include deep paths/fragments and malformed URLs.

## Phase 04: Apply & Open (P0)

- [ ] **04-T01** — Define explicit state machine and adapter-selection gate. **AC:** No implicit apply; user sees target adapter, requested old/new config.
- [ ] **04-T02** — Snapshot pre-change config, apply with UAC, verify actual post-state. **AC:** Settings re-read; failure clearly reported; best-effort recovery design.
- [ ] **04-T03** — Launch browser only after supported network-state verification. **AC:** Not tied to ping success; no browser launch on failed apply.
- [ ] **04-T04** — Add cancellation, error messaging, retry and manual recovery route. **AC:** All branches documented incl. UAC denied, adapter removed, partial netsh failure.
- [ ] **04-T05** — Unit tests for state machine and isolated Windows integration plan. **AC:** No real NIC changes in CI; manual NIC tests reported separately.

- [ ] **04-T06** — Review and harden the elevated request boundary for the new apply flow. **AC:** Bind typed validated request to stable NIC identity, recheck after UAC, review request/result paths and tampering, and cover rejection paths without elevation.
- [ ] **04-T07** — Review legacy history and revert limitations separately. **AC:** Define backward-compatible handling of missing DNS/policy/routes/additional-address snapshots; scope any legacy repair separately and never invent recoverable state.

## Phase 05: Link-Local addressing (P1)

- [ ] **05-T01** — Specify explicit 169.254/16 mode, selection and address constraints. **AC:** No gateway; previous config captured; DHCP mode not confused with manual link-local.
- [ ] **05-T02** — Add conflict check and controlled address choice/assignment. **AC:** Avoid claimed conflict-free guarantee from ping alone; use Windows APIs where justified.
- [ ] **05-T03** — Add restore/return-to-DHCP action and consistent UI states. **AC:** Existing profile semantics and IPv6 preserved.
- [ ] **05-T04** — Test on isolated adapter and cover failures in unit tests. **AC:** Test evidence with no live production network scanning.

- [ ] **05-T05** — Represent link-local state without hiding the selected address. **AC:** Account for current APIPA filtering in GetAdapters; distinguish automatic APIPA from manual link-local and verify display/snapshot/restore with fixtures.

## Phase 06: mDNS Discovery (P1)

- [ ] **06-T01** — Define service-discovery scope, interface binding and listener lifecycle. **AC:** Explicit start/stop; does not leak onto unintended interface.
- [ ] **06-T02** — Implement mDNS listener/discovery without copying AV Toolbag source. **AC:** Source provenance clean; bounded CPU/memory and packet parsing.
- [ ] **06-T03** — Display service type, hostname, IP, port, TTL; optional add-to-device. **AC:** Entries may be incomplete; no automatic config or trust of unverified hostnames.
- [ ] **06-T04** — Add safe export/filter, timeouts and negative tests. **AC:** CSV injection mitigated, invalid packets handled; tests via fixtures.

## Phase 07: Ethernet & Hyper-V diagnostics (P1)

- [ ] **07-T01** — Map physical, USB, Wi-Fi, virtual and Hyper-V adapters without false binding. **AC:** Unambiguous selection and stable hints; never choose adapter silently.
- [ ] **07-T02** — Add diagnostics: IPv4/DHCP/DNS/gateway/route/link status. **AC:** Clearly distinguish interface-derived vs probe-derived status.
  - Future Ping work (ADR-016): explicit Ethernet/Wi-Fi adapter and source IPv4 selection; show adapter identity, source IPv4 and route/next hop, distinguishing requested settings from observed routing. Cover both the inline Ping and the separate Ping window. Never silently fall back to another adapter or change network configuration.
- [ ] **07-T03** — Add opt-in Hyper-V uplink filtering and persistence. **AC:** Adapter hiding affects UI only, never silently disables interfaces.
- [ ] **07-T04** — Extend tests for enumeration/failures/performance. **AC:** Virtual and unplugged adapters included in fixtures; existing behavior preserved.
  - Future Ping coverage (ADR-016): simultaneous Ethernet and Wi-Fi, overlapping subnets, missing routes, multiple source addresses, adapter removal and selection changes during a request; fake all OS/probe boundaries. Live validation requires separate approval and an isolated environment.

## Phase 08: Temporary DHCP server (P2)

- [ ] **08-T01** — Threat-model and specify isolated service boundaries BEFORE implementation. **AC:** Written approval checkpoint for binding, relay, conflicts, firewall, cleanup.
- [ ] **08-T02** — Design pool, lease, router/DNS/NTP options with safe validation. **AC:** Pool excludes host/router/reserved and validates subnet/leases.
- [ ] **08-T03** — Implement explicit start/stop, adapter binding, visibility and audit logs. **AC:** No auto-start; active interface prominent; service cannot silently move NICs.
- [ ] **08-T04** — Detect existing DHCP where feasible; reliable cleanup of sockets/firewall. **AC:** Abort or warn for suspicious network; crash recovery and stop behavior tested.
- [ ] **08-T05** — Prove behavior only on isolated lab network with explicit permission. **AC:** Documented packet captures/leasing and negative tests; never use production LAN.

## Phase 09: Integration, QA & release (P0)

- [ ] **09-T01** — Run full unit, static-analysis and Windows build regression suite. **AC:** Commands, OS/build, exact commit and results documented.
- [ ] **09-T02** — Run isolated adapter tests across DHCP/static/gateway/DNS/revert/link-local. **AC:** Pass/fail matrix; never claim unperformed live tests passed.
- [ ] **09-T03** — Verify JSON compatibility, web deep-links, URL security, profiles + history. **AC:** Import/export round-trip and previous version fixtures covered.
- [ ] **09-T04** — Verify mDNS, Hyper-V diagnostics and DHCP isolated-network safety. **AC:** DHCP release blocked if service safety evidence absent.
- [ ] **09-T05** — Prepare changelog, MIT notices, tagged release and reproducible artifacts. **AC:** Release checklist approved; unsigned-binary risks documented.
