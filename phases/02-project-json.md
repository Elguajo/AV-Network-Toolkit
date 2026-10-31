# Phase 02 — Project-scoped JSON profiles

- **Priority:** P0
- **Dependencies:** ADR-008 explicitly authorizes continuation with historical audit evidence; preparation checkpoint 01-T06 through 01-T08 verified
- **Current status:** AWAITING_REVIEW — implementation/test evidence recorded; phase not complete
- **Scope:** Implement portable versioned project JSON without changing existing per-adapter AppData profile files. Design and review schema before editing code.
- **Read first:** docs/spec/PROJECT_JSON_SPEC.md; schemas/project.avnet.schema.json; examples/project.avnet.json

## Tasks and acceptance criteria

- [x] **02-T01** — Approve independent versioned project schema and round-trip semantics.
  - **Acceptance:** Schema reviewed; includes project, profiles, adapter hints, device refs; no secrets.
- [ ] **02-T02** — Implement Open Project / Choose Folder / Save As with explicit path.
  - **Acceptance:** Open/save project JSON from arbitrary user-approved folders; never overwrite silently.
- [ ] **02-T03** — Import/export static + DHCP settings including gateway and DNS.
  - **Acceptance:** Round-trip preserves all specified fields and legacy profile compatibility.
- [ ] **02-T04** — Handle schema versions, corrupt files, atomic writes and backups.
  - **Acceptance:** Clear errors, original file preserved, missing/unknown fields handled per spec.
- [ ] **02-T05** — Add unit tests for parse/validate/export, path security and migration.
  - **Acceptance:** Automated test evidence; existing ProfileRepository unaffected.

- [ ] **02-T06** — Review legacy persistence remediation as a separate compatibility change.
  - **Acceptance:** Document direct-write and backup-failure/delete risks, recovery fixtures and proposed scoped PR; do not change legacy storage without reviewed authorization.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.

## Audit-derived acceptance details

- 02-T01: decide catalogue file layout, stable IDs across edits, duplicate/conflict handling, unknown fields/future versions, project-to-legacy mapping with the existing five-profile limit, and reserved link-local handling before Phase 05. No option is approved by ADR-006.
- 02-T03: distinguish leave-unchanged legacy DNS from explicit automatic/static project DNS. Reject unsupported semantics before OS work; opening/importing a project remains parse-only. Prove empty/absent/null gateway behavior rather than assuming omitted netsh arguments implement it.
- 02-T04: original survives failed temp write, backup, flush or replace; define restore behavior, locks and permissions. Do not copy the legacy backup-and-delete behavior into the project store.
- 02-T05: full draft-2020-12 schema validation (format enforcement explicit) plus semantic/round-trip tests; parsing JSON alone is not validation. Include stable IDs, orphan refs, DNS modes, gateway semantics and failed-write recovery.
- 02-T06 is a scoped remediation review, not authorization to migrate or repair legacy files in this feature.

## 2026-10-31 execution update

ADR-008 authorizes preparation and Phase 02; ADR-009 approves the format proposal. Historical authorization statements above describe their original scope. Preparation: SDK 8.0.427, Release build PASS, isolated xUnit 189 PASS/0 skipped, planning validator and 10 regression tests PASS. Exact commands and limits: docs/testing/TEST_MATRIX.md. No live network or elevation tests.

## Implementation review packet — 2026-10-31

02-T02–02-T06: EVIDENCE_RECORDED, acceptance review pending. Implementation commit `eeb589cf81d8871f4243218d2e1a668829b79a2e`; owner English-only refinement `0bf1070216255c9e1411a4a96445372b6e6a45d1`. [Exact commands, results, risks and deviations](../docs/testing/PHASE02_REVIEW.md). Final local evidence: Release build 0 warnings/errors; 286 isolated tests PASS/0 skipped; four English Light/Dark component renders PASS; full schema+format/planning validation and 10 validator tests PASS. Native file dialogs/full app/live-network integration NOT RUN. Early failed harness startup/marker uncertainty is explicitly recorded in the review packet.

Owner refinements: ADR-010 applies existing UI design to every new element, with DESIGN.md as the contract; ADR-011 retires Turkish support with read-compatible English fallback. Existing network/profile/legacy functionality otherwise preserved. No Phase 03 work or publication authorized. Proposed next phase after owner acceptance: Device Web Interfaces.

## Repository naming refinement — ADR-013

Owner requested lowercase `app/` and `test/`, retaining proposed `AVNetworkToolkit.sln`. References, CI command paths, source-root test discovery and current docs/links updated; application contents/internal identities/user-data paths unchanged. Release build and 286 isolated tests PASS; 4 UI-smoke renders PASS; planning/schema validator and 10 tooling tests PASS. Exact commands: docs/testing/TEST_MATRIX.md, ADR-013 section. This is a local focused follow-up, not phase completion or Phase 03 execution.
