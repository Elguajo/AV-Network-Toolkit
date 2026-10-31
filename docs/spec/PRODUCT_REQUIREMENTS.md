# Product requirements (proposal, subject to review)

## Priority P0: project context

- **FR-01** Create/open/save a single project file in a user-selected folder, with explicit path shown. No automatic writes to existing files during import.
- **FR-02** Persist `static`, `dhcp` profiles with IPv4, CIDR, optional gateway, DNS policy and adapter **hint**; retain existing base AppData format intact.
- **FR-03** Import and export must preserve exact semantically meaningful fields, stable IDs and device-to-profile references; no hidden defaults that change NIC settings.
- **FR-04** Project files must be human-readable versioned JSON, portable between computers. Missing adapter mapping must force human selection.
- **FR-05** Record full URLs for multiple devices per project, including paths, queries and fragments. Associate zero/one profile per device, many devices per profile.
- **FR-06** Browser Open does not apply a network profile. Explicit Apply & Open verifies selected adapter/current result before browser launch.
- **FR-07** Existing base features must be preserved and remain fully tested.

## Priority P1: engineering tools

- **FR-08** Explicit Link-Local management (`169.254.0.0/16`, practical valid host constraints); conflict checks and a safe return path.
- **FR-09** mDNS listener by selected interface: show discovered service type/target/address/port, optionally create a device entry only on user action.
- **FR-10** Diagnostics of physical/USB/virtual/Hyper-V interfaces, status, gateway/DNS/DHCP and chosen routing; never auto-select a different NIC based on name match alone.

## Priority P2: temporary DHCP server

- **FR-11** User must explicitly choose an isolated NIC, pool, subnet and start/stop. Confirm possible existing DHCP and conflicts; no automatic DHCP on corporate/production LAN.
- **FR-12** Have a reliable stop/recovery strategy for socket/firewall/lease state, visible activity and an audit log. Disable release if safety evidence is missing.

## Nonfunctional

- **NFR-01** Least privilege; UAC only for actions requiring elevation.
- **NFR-02** Atomic writes, versioning, clear corruption messages, backup-before-overwrite.
- **NFR-03** No hardcoded/real credentials in repository examples/tests or logs. Owner permits optional device passwords in private project JSON (ADR-024); current v1 has no fields for them. Keep credentials separate from URLs/launch arguments and preserve file-format compatibility.
- **NFR-04** All live network mutations gated behind explicit user intent; no live network in automated tests.
- **NFR-05** Backward compatibility with original user profile files.
- **NFR-06** Clear error propagation; do not claim connectivity based solely on Ping, ARP or port response.
- **NFR-07** Modular, maintainable C#/.NET 8/WPF code with tests; minimum UI changes.

## Phase 02 decisions approved (ADR-009)

Single-file project catalogue; independent project collection without five-slot cap; no legacy mapping/apply in Phase 02; explicit automatic/static/leave-unchanged DNS; stable IDs with duplicate/orphan rejection; strict unknown/future-version incompatibility; reserved link-local rejection; conservative secret-URL persistence blocking. Exact semantics: [project JSON specification](PROJECT_JSON_SPEC.md).

All new interface elements reuse the current design language, per owner instruction recorded in [DESIGN.md](../../DESIGN.md). Device UI/browser launch and Apply & Open remain later phases.

## Owner-approved requirement follow-up — ADR-024

- **FR-13** Permit optional device passwords in portable project JSON. Storage permission is approved; implementation and versioned format extension are pending. Proposed login/password editor, reveal/copy behavior, backup implications, compatibility and acceptance criteria are in [the product-fit review](../PRODUCT_FIT_REVIEW_RU.md). Automatic login, token storage and TLS bypass are not included.
- Original-outcome gaps IDEA-01 (project Apply Profile without device URL) and IDEA-02 (practical DHCP/no-lease/APIPA/static DNS transitions) are recorded for separate scope/design, not silently implemented in Phase 06. Existing 49 phase-task IDs, phase ordering and acceptance gates remain unchanged.
