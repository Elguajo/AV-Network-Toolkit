# Phase 03 — Device Web Interfaces

- **Priority:** P0
- **Dependencies:** Phase 02
- **Current status:** AWAITING_REVIEW (ADR-015; 03-T01–03-T05 EVIDENCE_RECORDED)
- **Scope:** Add a device catalog and safe browser launch independent of network mutation. Preserve deep paths and fragments.
- **Read first:** docs/spec/DEVICE_WEB_INTERFACES.md; docs/security/THREAT_MODEL.md

## Tasks and acceptance criteria

- [ ] **03-T01** — Design device model with name/type/IP/URL/profile reference and group.
  - **Acceptance:** Explicit identity, profile association and full URL fields.
- [ ] **03-T02** — Add per-project Device Web Interfaces UI: create/edit/delete/search/group.
  - **Acceptance:** Manage multiple device links for one profile without UI redesign.
- [ ] **03-T03** — Launch full http/https deep URL with browser default and safe validation.
  - **Acceptance:** Path, query, fragment preserved; unsafe schemes and URL credentials rejected.
- [ ] **03-T04** — Handle HTTP warning, unreachable endpoints and stale profile refs.
  - **Acceptance:** No hidden credential handling or unsafe TLS suppression.
- [ ] **03-T05** — Automated tests for URL parsing/launch abstraction and JSON round-trip.
  - **Acceptance:** Tests include deep paths/fragments and malformed URLs.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.

## Execution evidence

Implementation and isolated local verification are recorded in [Phase 03 review](../docs/testing/PHASE03_REVIEW.md). 308 unit tests, Release build without warnings/errors and 20 WPF renders with no binding errors passed. Browser/live/full-app/manual acceptance remains NOT RUN. No checkbox/phase completion claimed before owner review. No Phase 04, merge/push or release authorization.
