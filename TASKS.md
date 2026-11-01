# Traceable task backlog

## Network-test presets — ADR-041, 2026-11-01

Separate follow-up ID; preserve the existing 49 phase task IDs/order.

- [ ] **NT-01** — Implement the approved expanding Network tests section and ready-made diagnostic presets. **AC:** [contract](docs/spec/NETWORK_TEST_PRESETS_CONTRACT.md): explicit source/run, Ping four/continuous/single, one TCP port, descriptions, Stop/close, bounded results and independent peers; fake-boundary tests and actual Light/Dark normal/minimum WPF evidence. **State:** EVIDENCE_RECORDED — 681 full unit PASS, 50 added cases, Release builds and 64 focused + 128 existing WPF captures/0 binding errors; owner/native acceptance remains separate. [Review](docs/testing/NETWORK_TEST_PRESETS_REVIEW.md).

This diagnostic addition does not change independently authorized REL-03 (ADR-040) or start Phase 08. Phase 07 remains AWAITING_REVIEW; ADR-037 safe-only execution persists.

## Release preparation follow-ups — ADR-036, 2026-11-01

Separate from phase acceptance and the 49 phase tasks; execute in this order:

| ID | Follow-up | State |
|---|---|---|
| REL-01 | Investigate/fix UX-R01/P05-R01 with causal reproduction and meaningful regression coverage | FIXED_LOCALLY / EVIDENCE_RECORDED; [reproduction, cause, correction, 631 PASS](docs/testing/JSON_VALIDATION_REVIEW.md); owner review pending |
| REL-02 | Prepare isolated Windows profile → observed IP/prefix/gateway/DNS → device open → restore, UAC denial, NIC disconnect and partial failure | PREPARATION_EVIDENCE_RECORDED; [156 fake tests + 11 observer fixtures in two shells](docs/testing/ISOLATED_WINDOWS_PREPARATION.md). Owner chose safe checks only; native NOT RUN/deferred pending lab |
| REL-03 | Confirm GitHub CI and same-version installer + portable ZIP on clean Windows without preinstalled .NET (ADR-039) | IN_PROGRESS: [local self-contained candidates verified](docs/testing/PACKAGING_REVIEW.md), source e4bd0cb; 681 unit PASS, packaged WPF/JSON + ZIP hashes/tool integrity PASS. Hosted install/ordinary startup/no-global-runtime/reinstall/uninstall NOT RUN, pending authorized main push/actual CI. REL-02 native restriction remains |
| REL-04 | Update first-release README/changelog/version/description/limits/checksum | DEFERRED until REL-01/02; reduced release scope not approved |

Phase 07 remains AWAITING_REVIEW. No push, tag/release, Phase 08 or DHCP server authorization.

ADR-038 maintenance follow-up: owner confirmed CODEX_START.md deletion; retired redundant entry and enforced existing AGENTS/bootstrap/resume/handoff documents instead. Real-tree planning/schema/links PASS, 12 tooling regressions PASS; JSON scoped self-review found no new actionable defect, 60 focused PASS and 10 retained TRX hashes verified. Phase/task acceptance and native/deferred release gates unchanged.

ADR-039: owner accepted primary installer + additional portable ZIP with bundled .NET, same application version/features/UAC boundary. Requirements recorded in PROJECT_CONTEXT and RELEASE_CHECKLIST; neither package is claimed implemented/verified. No USB-local storage mode, automatic updates, new native permission or release publication implied.

**DATA-01 — ADR-042, after packaging:** owner selected `%USERPROFILE%\.av-network-toolkit` as future shared per-user storage. Migration NOT STARTED; review all existing data/settings/history/recovery locations, backup/copy/conflicts/repeated-run/failure/recovery compatibility before changing paths. Current packages retain LocalAppData; this is not a USB-local mode.

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
| IDEA-03 | FR-13: optional device-password storage in project JSON; version/compatibility, explicit UI, redaction and backups | OWNER PERMISSION RECORDED (ADR-024); format/UI implementation not started |
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
