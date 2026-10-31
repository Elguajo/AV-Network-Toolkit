# Phase 05 — Link-Local addressing

- **Priority:** P1
- **Dependencies:** Phase 01, Phase 04
- **Current status:** NOT STARTED
- **Scope:** Manage IPv4 Link-Local on a selected adapter with conflict checks and restoration.
- **Read first:** docs/spec/NETWORK_DISCOVERY_AND_DHCP.md

## Tasks and acceptance criteria

- [ ] **05-T01** — Specify explicit 169.254/16 mode, selection and address constraints.
  - **Acceptance:** No gateway; previous config captured; DHCP mode not confused with manual link-local.
- [ ] **05-T02** — Add conflict check and controlled address choice/assignment.
  - **Acceptance:** Avoid claimed conflict-free guarantee from ping alone; use Windows APIs where justified.
- [ ] **05-T03** — Add restore/return-to-DHCP action and consistent UI states.
  - **Acceptance:** Existing profile semantics and IPv6 preserved.
- [ ] **05-T04** — Test on isolated adapter and cover failures in unit tests.
  - **Acceptance:** Test evidence with no live production network scanning.

- [ ] **05-T05** — Represent link-local state without hiding the selected address.
  - **Acceptance:** Account for current APIPA filtering in GetAdapters; distinguish automatic APIPA from manual link-local and verify display/snapshot/restore with fixtures.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.
