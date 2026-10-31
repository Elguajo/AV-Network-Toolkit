# Phase 06 — mDNS Discovery

- **Priority:** P1
- **Dependencies:** Phase 01, Phase 03
- **Current status:** AWAITING_REVIEW; 06-T01–06-T04 EVIDENCE_RECORDED under ADR-023 (2026-10-31), not phase acceptance
- **Scope:** Implement original mDNS discovery bound to selected NIC and safe exports.
- **Read first:** docs/spec/NETWORK_DISCOVERY_AND_DHCP.md; docs/security/THREAT_MODEL.md
- **Authorization:** owner explicitly requested the next phase in a new session after Phase 05. No repeated start permission. Work directly on main, preserve JSON v1/legacy behavior/DESIGN.md and OPEN P05-R01. No live socket/multicast/NIC tests, release, later phases or automatic push of future Phase 06 changes. Existing manual acceptance limits remain.

Implementation/support contract: [passive IPv4 scope, mandatory binding, lifecycle and limits](../docs/spec/PHASE06_IMPLEMENTATION_CONTRACT.md). Review/evidence: [Phase 06 packet](../docs/testing/PHASE06_REVIEW.md). Local implementation has 79 added fixture cases, 542 total PASS, 84 WPF captures/0 binding errors; live NIC/multicast acceptance NOT RUN. Optional Add-to-device omitted; no format changes or query sender. Checkboxes remain open for owner acceptance. OPEN P05-R01 and ADR-024/IDEA-01–03 are preserved separate work, not silently implemented here.

## Tasks and acceptance criteria

- [ ] **06-T01** — Define service-discovery scope, interface binding and listener lifecycle.
  - **Acceptance:** Explicit start/stop; does not leak onto unintended interface.
- [ ] **06-T02** — Implement mDNS listener/discovery without copying AV Toolbag source.
  - **Acceptance:** Source provenance clean; bounded CPU/memory and packet parsing.
- [ ] **06-T03** — Display service type, hostname, IP, port, TTL; optional add-to-device.
  - **Acceptance:** Entries may be incomplete; no automatic config or trust of unverified hostnames.
- [ ] **06-T04** — Add safe export/filter, timeouts and negative tests.
  - **Acceptance:** CSV injection mitigated, invalid packets handled; tests via fixtures.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.
