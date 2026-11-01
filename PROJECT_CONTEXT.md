# Project context — persistent product brief

Current local source follow-up (ADR-047): owner-authorized [A26 audit fixes](docs/spec/AUDIT_FIXES_CONTRACT.md) unify local/project mutation safety, preserve damaged storage, add journal-backed local recovery and target .NET 10 LTS. [Evidence and OS/compatibility limits](docs/testing/AUDIT_FIXES_REVIEW.md). Existing published v1.1.1 remains .NET 8. Phase 07 AWAITING_REVIEW; no native acceptance, DATA-01 or Phase 08 advancement.

## Current diagnostic follow-up — ADR-041, 2026-11-01

Owner approved the [expanding network-test concept](docs/design/network-tests-target.png) and explicitly requested implementation. NT-01 adds descriptive Ping four/continuous/single and one-port TCP presets through the existing diagnostics MVVM/panel, inline below profiles and in an independent separate window. [Contract](docs/spec/NETWORK_TEST_PRESETS_CONTRACT.md), [local evidence](docs/testing/NETWORK_TEST_PRESETS_REVIEW.md). Phase 07 remains AWAITING_REVIEW; ADR-037 safe-only verification, previous acceptance and publication/release gates persist. No JSON/schema/network-mutation/support/Phase 08 change.

## Product name — ADR-018

The owner requested that the application name match this fork. The display/product name is **AV Network Toolkit** (repository: `AV-Network-Toolkit`). Window/title-bar resources, tray tooltip and binary product/title metadata use this name. Internal `NetworkProfileManager` assembly/executable/namespace, startup registry key and LocalAppData paths remain compatible; no data migration or network behavior change. This bounded branding follow-up does not advance Phase 03 or authorize Phase 04/live tests/publication.

## Goal

Create an **AV-oriented Windows network and device manager** by incrementally extending a GitHub fork of Network Profile Manager. The user works with AV equipment (e.g., Extron control/touch panels, DSP, Woxcon video wall devices) and regularly moves a Windows laptop between equipment-specific Ethernet subnets.

Two connected jobs:

1. Apply reusable static/DHCP network profiles (IPv4, subnet, gateway, DNS) to a **deliberately selected network adapter**, safely and quickly.
2. Save device web interfaces (including full deep URLs with paths/query/fragments) by AV project and launch them; optionally apply an associated network profile first through an explicit **Apply & Open** command.

Extra diagnostic capabilities desired from AV Network Toolbag: Link-Local addressing, mDNS discovery/listener, richer adapter/Hyper-V diagnostics and temporary DHCP for isolated networks.

## Baseline and provenance

- **Base:** https://github.com/rmzncakir/network-profile-changer
- **Feature inspiration only:** https://github.com/mefranklin6/AV-Network-Toolbag
- **Previously inspected on:** 2026-10-31; confirm facts at Phase 01 against exact fork commit.
- Base repo: C# / .NET 8 / WPF, MVVM; MIT licensed. Current published capabilities (at inspected revision): named static/DHCP adapter profiles, full IP/subnet/gateway/primary+secondary DNS, JSON import/export, scanner (ARP/ICMP/TCP), Ping, history/revert, UI/tray and Windows elevation.
- AV Toolbag: Flutter/Dart; published link-local, temporary DHCP server, mDNS listener and AV diagnostics. Its existing profile JSON holds only `name`, `address`, `prefixLength` and omits gateway/DNS; **no LICENSE found**, so use behavior only, not implementation.

## Decisions approved by user

- Fork **Network Profile Manager**, not AV Network Toolbag.
- Develop through Codex **by phases** with tasks written to files so new agent sessions do not lose context.
- Preserve base features; add missing AV tools; create project-scoped JSON profiles and saved Device Web Interfaces with Apply & Open.

## Functional priorities

P0: baseline audit, project JSON, Device Web Interfaces, Apply & Open, regression safety, release readiness.
P1: Link-Local, mDNS, adapter/Hyper-V diagnostics.
P2: temporary DHCP server (higher operational risk).

## Approved distribution formats — ADR-039, 2026-11-01

Ship two packages of the same application version: an installer as the primary download for regular use, and a portable ZIP as the additional option for field work/quick launch. Both include the required .NET runtime; users must not separately install .NET/SDK to run the supported Windows package. Same features and deliberate UAC/network controls in both; installation does not grant additional application privileges.

This approves delivery requirements, not completed artifacts or release publication. Installer technology/integration details remain implementation choices for the packaging follow-up. Preserve current LocalAppData identities/settings and explicit project files; ZIP distribution does not imply a new USB-local settings mode, automatic updater or storage migration. Verify both packages on clean Windows without preinstalled .NET before claiming readiness. Existing phase/native/release/publication gates remain.

ADR-042 owner refinement: after packaging, migrate shared user data to `%USERPROFILE%\.av-network-toolkit` through a separate reviewed backup/compatibility/recovery change (DATA-01). Current implementation and candidate packages still use LocalAppData. This approves the future destination, not completed migration or data deletion.

ADR-045 publication: owner requested version 1.1.1 with the new logo/current changes. [v1.1.1 Preview](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.1) is published from `3fd7e77e1f5b560172b7e23d33928bace9cdaa73`, both packages self-contained and [hosted CI verified](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41273489475). Native/phase acceptance remains pending; no DHCP/password/user-home migration implementation claimed. This supersedes earlier no-publication statements for this requested preview only.

## Non-goals / unapproved changes

- No rewrite to Flutter/Electron/other toolkit.
- No imported code from unlicensed AV Toolbag.
- No hardware-vendor integration/API automation beyond saved web-link launching in this roadmap.
- No cloud storage, account management, credential vault or password synchronization.
- ADR-024 permits optional device passwords in portable project JSON; this is an approved requirement, not current v1 functionality. No cloud credential synchronization or automatic device login is implied. See [product-fit review and FR-13 criteria](docs/PRODUCT_FIT_REVIEW_RU.md).
- No automatic subnet change merely because a device entry is clicked or a project is opened.
- No arbitrary background network scan, no auto-running DHCP on existing networks.
- No UI redesign; preserve existing application identity as much as possible.

## Assumptions to verify, not present as facts

- Project files use an **additional versioned JSON format**, independent of the base app's existing `%LOCALAPPDATA%` profile files.
- Existing profile limit of five per adapter stays unchanged initially; project collections may contain more, but application/import mapping must be designed explicitly.
- A device may reference a network profile but a single profile may relate to many devices.
- Profile selection uses adapter hints only, never implicitly choosing a physical NIC on another computer.
- Initial focus is IPv4 and Windows 10/11. Do not change existing IPv6 configuration.

## Sensitive information

Internal addresses shown in earlier browser screenshots are **user data**, not test examples for a public repository. Use RFC 5737 documentation addresses in sample files (192.0.2.0/24, 198.51.100.0/24, 203.0.113.0/24). Never commit real device admin URLs, credentials, passwords or private project identifiers as examples.

## Verified baseline limitations (2026-10-31)

Fork `Elguajo/AV-Network-Toolkit` at `603cf24fa0ba1819c0ec10b6a57ef5cf57c3852e` was inspected read-only. See docs/architecture/BASELINE_AND_SOURCE_MAP.md for source evidence and dispositions. Existing history does not snapshot DNS/routes/additional IPv4; empty static primary DNS leaves DNS untouched; apply lacks requested-versus-observed verification. These limits must not be advertised as complete rollback or apply guarantees.

The user authorized documentation adoption only (ADR-006). Phase 01 acceptance and Phase 02 execution remain pending. The nine-phase order, application behavior, MIT notice and production-network prohibition are unchanged. Environment preparation and safe baseline tests precede Phase 02 implementation; unresolved data-format/support choices remain proposals.

## Current authorization — 2026-10-31

ADR-008 supersedes the earlier pending-start snapshot: the owner explicitly authorized Phase 02 and its prerequisite preparation in a new chat. ADR-009 approves the concrete format package in docs/spec/PHASE02_FORMAT_REVIEW.md. 01-T06–01-T08 have verified preparation evidence; historical Phase 01 acceptance is not fabricated. No authorization for live tests, future phases, merge or push.

## Owner localization refinement — ADR-011

Turkish UI is no longer required. English remains supported; legacy `tr` preferences fall back to English without a startup settings rewrite. This explicitly supersedes the baseline bilingual preservation requirement only; other baseline capabilities remain protected. All new UI follows DESIGN.md (ADR-010).

## Phase 02 integration authorization — ADR-012

On 2026-10-31 the owner explicitly authorized committing the integration handoff, merging the verified Phase 02 branch into main and pushing main to origin. This supersedes earlier lack of publication authorization for this change only. No Phase 03, live-test, tag/release or force-push authorization is implied. Exact anchors and execution plan: STATUS.md / ADR-012.

## Repository paths — ADR-013

Owner selected lowercase `app/` and `test/`, with solution `AVNetworkToolkit.sln`. Project filenames, namespaces/assembly names, executable and LocalAppData paths retain existing identities. Current build/test commands use the new paths; historical audit/execution commands retain their original spelling. This structural refinement does not advance phases or authorize publication of the new change.

## Naming-refactor integration authorization — ADR-014

On 2026-10-31 the owner requested commit, merge to main and push of the verified lowercase-path refactor, after confirming the obsolete 00_READ_FIRST_RU.md deletion. ADR-014 authorizes that integration/deletion and supersedes the earlier pending-publication snapshot for this follow-up. Phase 03/live-test/release gates remain unchanged.

## Phase 03 authorization — ADR-015

On 2026-10-31 the owner explicitly requested Phase 03 in a new chat. 03-T01–03-T05 execution is authorized, preserving JSON v1 and DESIGN.md. Supersedes stale no-Phase-03-authorization snapshots only. No live tests, merge/push, release or Phase 04 authorization.

## Current integration and branch rule — ADR-017

Owner authorized merging/committing/pushing the current Phase 03 work to main on 2026-10-31. Approved deferred Ping planning (ADR-016) is preserved. Future work in this repository runs directly on main without creating separate branches, as now required by AGENTS.md. Earlier focused-branch/no-publication snapshots are superseded for this integration. Phase/manual/live-test and release gates remain unchanged; no Phase 04 start implied.

## Phase 04 execution and support boundary — ADR-019

Owner explicitly requested Phase 04 in a new chat and approved blocking complex/unknown adapter state before mutation. This supersedes stale Phase 04 pending-start statements only. Apply & Open now has local implementation/review evidence in docs/testing/PHASE04_REVIEW.md; AWAITING_REVIEW, not phase acceptance. Direct work on main, existing branding/internal identities/JSON/legacy behavior preserved. No new push/live-test/release or Phase 05 permission. Static automatic DNS and /31–/32 remain storable but blocked for OS apply; full recovery is never inferred from old history.

## Current publication authorization — ADR-020

Owner requested «Коммит и пуш в main» after the Phase 04 report. Current main, including branding/Phase 04/handoff and the bounded authorization metadata, may be committed/pushed ordinarily. Supersedes previous no-push snapshots for this publication only. Phase 04 manual acceptance, live isolated tests, release and Phase 05 remain separate gates; no force/history rewrite or new branches.

## Phase 05 execution — ADR-021/022

Owner explicitly started Phase 05 in a new chat on 2026-10-31 and approved explicit static DNS entry when the source uses automatic DNS. Work directly on main. Reuse Phase 04 apply/recovery for a transient manual Link-Local action; preserve JSON v1, all legacy functionality and IPv6. [Contract](docs/spec/PHASE05_IMPLEMENTATION_CONTRACT.md) documents DAD during assignment, exact DNS/recovery semantics and conservative APIPA/complex-state limits. Supersedes historical no-Phase-05-start snapshots only. No new push, live tests, release, Phase 06 or fabricated prior manual acceptance.

## Publication and Phase 06 handoff — ADR-023

Owner explicitly requested commit/push and the next phase in a new session. Authorizes current Phase 05 publication and a new local project chat executing 06-T01–06-T04 directly on main. Prior manual acceptance and OPEN P05-R01 remain; no live multicast/NIC/UAC tests, release, later phases or automatic publication of future Phase 06 changes. This supersedes earlier no-push/no-Phase-06-start snapshots only; no repeated start permission is needed.

## Phase 06 implementation evidence — awaiting owner review

Owner subsequently authorized the selected main-window follow-up with «Да делаем» (ADR-028). IDEA-01/04 have [local implementation/evidence](docs/testing/MAIN_WORKSPACE_REVIEW.md): explicit legacy/project scopes on main, detached project editor, device tab and standalone typed project apply targeting the selected GUID. No store/schema migration, Phase 06 acceptance or Phase 07 start. IDEA-02 support expansion and IDEA-03 passwords remain separate; no new live-test/publication/release authorization.

Selected-interface passive IPv4 mDNS is implemented locally with A/AAAA service presentation, explicit Start/Stop, bounded untrusted packet/cache processing and filtered safe CSV. No queries, optional device draft integration, project-format changes or network mutation. [Support contract](docs/spec/PHASE06_IMPLEMENTATION_CONTRACT.md) and [review/evidence](docs/testing/PHASE06_REVIEW.md) define limits. 542 isolated tests and 84 component captures passed; native sockets/multicast remain UNVERIFIED. P05-R01 remains OPEN; ADR-024 password permission and IDEA-01–03 remain separate follow-ups. Phase 06 is not marked complete; no automatic Phase 07 or publication.

## Main-workflow follow-up — ADR-028/029, 2026-11-01

The selected second concept is implemented locally with adapter navigation, independent local/project profile scopes, target-named per-row apply and a detached editor. [Contract](docs/spec/MAIN_WORKSPACE_CONTRACT.md), [UX review](docs/testing/UX_WORKFLOW_REVIEW.md). Owner confirmed the two levels of tabs convenient; full task/native acceptance remains pending. Normal project Save, modal close/validation/focus, explicit storage/computer-IP copy, empty/loading/error/recovery guidance reuse current style and policies. Final Release build and 124 WPF captures pass, but the latest full regression is **564 PASS / 1 FAIL / 0 skipped**: unsafe JSON acceptance in dhcpIpv4, relationship to OPEN P05-R01 UNVERIFIED. No suppression/repair in UX scope. Phase 06 still AWAITING_REVIEW; no release, new push, Phase 07, live test, password/schema or network-support expansion implied.

Owner then corrected the marked project/profile block to match the compact target (ADR-030). [Current layout/evidence](docs/testing/COMPACT_WORKSPACE_REVIEW.md): one project/file/Open/Save bar, one underlined Network profiles / Device interfaces tab row, saved-profile toolbar with right-aligned Add and local Import/Export. A compact selector preserves independent local/project sources; it supersedes the second visible tab-row placement only. Save as/Manage remain in the named overflow menu. Full gate failure above, native/manual limits, phase/security/storage boundaries and owner acceptance pending remain unchanged.

ADR-031/032 further authorize reference-matched actions and a blue current row/Selected status for profiles matching reported static adapter fields, explicitly not edit selection. [Current evidence](docs/testing/PROFILE_VISUAL_STATES_REVIEW.md): 576 full unit PASS and 140 WPF captures/0 binding errors. This supersedes the latest-run count only, not OPEN UX-R01/P05-R01 or release/native gates. Unknown DHCP/automatic-DNS source leaves Apply available; reapply/edit/delete remain in profile menus. No profile-selection persistence, supported-state expansion, JSON/worker/service change or phase acceptance.

## Phase 07 execution — ADR-034, 2026-11-01

Owner explicitly started 07-T01–07-T04 directly on main, including ADR-016 Ping. [Contract](docs/spec/PHASE07_IMPLEMENTATION_CONTRACT.md). This supersedes prior Phase 07 pending-start snapshots only; earlier owner/native acceptance and OPEN UX-R01/P05-R01 remain. No live tests, new publication/release or Phase 08 implied.

Phase 07 now has [implementation/evidence](docs/testing/PHASE07_REVIEW.md), AWAITING_REVIEW: 624 full unit PASS, 15 fake query fixtures and 160 WPF captures across focused/existing checks. Native/owner acceptance remains pending; no phase completed, live test, publication/release or Phase 08 implied.
