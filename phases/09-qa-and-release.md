# Phase 09 — Integration, QA & release

- **Priority:** P0
- **Dependencies:** Phase 02, Phase 03, Phase 04, Phase 05, Phase 06, Phase 07, Phase 08
- **Current status:** NOT STARTED
- **Scope:** Full Windows build/QA across all requirements, a clear release decision and preserved MIT notices.
- **Read first:** docs/testing/TEST_MATRIX.md; RELEASE_CHECKLIST.md

## Tasks and acceptance criteria

- [ ] **09-T01** — Run full unit, static-analysis and Windows build regression suite.
  - **Acceptance:** Commands, OS/build, exact commit and results documented.
- [ ] **09-T02** — Run isolated adapter tests across DHCP/static/gateway/DNS/revert/link-local.
  - **Acceptance:** Pass/fail matrix; never claim unperformed live tests passed.
- [ ] **09-T03** — Verify JSON compatibility, web deep-links, URL security, profiles + history.
  - **Acceptance:** Import/export round-trip and previous version fixtures covered.
- [ ] **09-T04** — Verify mDNS, Hyper-V diagnostics and DHCP isolated-network safety.
  - **Acceptance:** DHCP release blocked if service safety evidence absent.
- [ ] **09-T05** — Prepare changelog, MIT notices, tagged release and reproducible artifacts.
  - **Acceptance:** Release checklist approved; unsigned-binary risks documented.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.
