# Project context — persistent product brief

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

## Non-goals / unapproved changes

- No rewrite to Flutter/Electron/other toolkit.
- No imported code from unlicensed AV Toolbag.
- No hardware-vendor integration/API automation beyond saved web-link launching in this roadmap.
- No cloud storage, account management, credential vault or password synchronization.
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
