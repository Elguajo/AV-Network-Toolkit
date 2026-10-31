# Project context — persistent product brief

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
