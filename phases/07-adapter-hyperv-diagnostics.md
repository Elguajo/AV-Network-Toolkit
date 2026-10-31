# Phase 07 — Ethernet & Hyper-V diagnostics

- **Priority:** P1
- **Dependencies:** Phase 01, Phase 04
- **Current status:** NOT STARTED
- **Scope:** Extend diagnostics and clear Hyper-V adapter enumeration without implicit adapter changes.
- **Read first:** docs/spec/NETWORK_DISCOVERY_AND_DHCP.md

## Tasks and acceptance criteria

- [ ] **07-T01** — Map physical, USB, Wi-Fi, virtual and Hyper-V adapters without false binding.
  - **Acceptance:** Unambiguous selection and stable hints; never choose adapter silently.
- [ ] **07-T02** — Add diagnostics: IPv4/DHCP/DNS/gateway/route/link status.
  - **Acceptance:** Clearly distinguish interface-derived vs probe-derived status.
- [ ] **07-T03** — Add opt-in Hyper-V uplink filtering and persistence.
  - **Acceptance:** Adapter hiding affects UI only, never silently disables interfaces.
- [ ] **07-T04** — Extend tests for enumeration/failures/performance.
  - **Acceptance:** Virtual and unplugged adapters included in fixtures; existing behavior preserved.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.
