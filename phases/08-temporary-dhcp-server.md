# Phase 08 — Temporary DHCP server

- **Priority:** P2
- **Dependencies:** Phase 05, Phase 07
- **Current status:** NOT STARTED
- **Scope:** High-risk temporary DHCP server: separate explicit safety approval, isolated network only.
- **Read first:** docs/spec/NETWORK_DISCOVERY_AND_DHCP.md; docs/security/THREAT_MODEL.md

## Tasks and acceptance criteria

- [ ] **08-T01** — Threat-model and specify isolated service boundaries BEFORE implementation.
  - **Acceptance:** Written approval checkpoint for binding, relay, conflicts, firewall, cleanup.
- [ ] **08-T02** — Design pool, lease, router/DNS/NTP options with safe validation.
  - **Acceptance:** Pool excludes host/router/reserved and validates subnet/leases.
- [ ] **08-T03** — Implement explicit start/stop, adapter binding, visibility and audit logs.
  - **Acceptance:** No auto-start; active interface prominent; service cannot silently move NICs.
- [ ] **08-T04** — Detect existing DHCP where feasible; reliable cleanup of sockets/firewall.
  - **Acceptance:** Abort or warn for suspicious network; crash recovery and stop behavior tested.
- [ ] **08-T05** — Prove behavior only on isolated lab network with explicit permission.
  - **Acceptance:** Documented packet captures/leasing and negative tests; never use production LAN.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.

**Release blocker:** do not start DHCP on an unisolated network; cannot approve phase without an isolated-lab test report.
