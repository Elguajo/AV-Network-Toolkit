# Phase 01 — Baseline audit

- **Priority:** P0
- **Dependencies:** fork available
- **Current status:** AUDIT_RECORDED_AWAITING_REVIEW; preparation checkpoint VERIFIED under ADR-008
- **Scope:** Read-only architecture and safety investigation. No repository edits (including status docs) during the audit itself; report findings in the Codex response or a separately approved report change afterward.
- **Read first:** docs/architecture/BASELINE_AND_SOURCE_MAP.md; docs/testing/TEST_MATRIX.md; docs/legal/LICENSE_NOTES.md

## Tasks and acceptance criteria

- [ ] **01-T01** — Record fork origin, exact commit, branch, license and build prerequisites.
  - **Acceptance:** Evidence with git commands, repo SHA and MIT notice recorded.
- [ ] **01-T02** — Map models, viewmodels, services, UAC/netsh, data paths, scanner and revert.
  - **Acceptance:** Source map with paths, entry points, call graph and risk notes.
- [ ] **01-T03** — Inventory all existing capabilities, supported data formats and compatibility.
  - **Acceptance:** Baseline preservation checklist signed off.
- [ ] **01-T04** — Run non-invasive baseline build/test/static checks where supported.
  - **Acceptance:** Exact commands/results/limitations; no device configuration.
- [ ] **01-T05** — Review risk surface and propose implementation boundaries for Phases 02–09.
  - **Acceptance:** Audited phase plan, security assumptions and open decisions documented.

- [x] **01-T06** — Prepare the SDK/build environment in a separately authorized checkpoint.
  - **Acceptance:** Record SDK 8 version, approved install/restore scope and baseline build command/result; no application launch, elevation or network probes.
- [x] **01-T07** — Isolate automated tests from network probes and real adapter enumeration.
  - **Acceptance:** Review exact safe test set; fake OS/probe boundaries or segregate integration cases; run the isolated baseline and report every exclusion as NOT RUN.
- [x] **01-T08** — Repair and verify the planning validator in a separate tooling task.
  - **Acceptance:** Support current plan states and task inventory; distinguish GitHub-relative links from filesystem links; retain meaningful checks and demonstrate expected valid/invalid cases.

## Phase completion gate

- [ ] Read-only report references concrete source paths and exact base commit.
- [ ] Baseline test logs or explicit `NOT RUN` recorded.
- [ ] Implementation proposals separated from verified findings.
- [ ] No code/files/OS network configuration changed during audit.
- [ ] User approved phase 02 start.

## Authorized adoption boundary

On 2026-10-31 the user authorized the proposed documentation/plan adoption and a local documentation-only commit (ADR-006). Read-only audit is preserved as historical evidence; this exception permits recording it, not implementing findings or starting Phase 02. Source map and test evidence are recorded in the referenced documents. 01-T01/01-T02/01-T04/01-T05 have EVIDENCE_RECORDED status; 01-T03 is AWAITING_REVIEW. No acceptance checkbox is closed by adoption.

Preparation 01-T06 through 01-T08 is planned and requires separate execution authorization. Do not install SDK/dependencies, fix tests/tooling or run OS/network integration tests merely because this plan was updated.

## 2026-10-31 execution update

ADR-008 authorizes preparation and Phase 02; ADR-009 approves the format proposal. Historical authorization statements above describe their original scope. Preparation: SDK 8.0.427, Release build PASS, isolated xUnit 189 PASS/0 skipped, planning validator and 10 regression tests PASS. Exact commands and limits: docs/testing/TEST_MATRIX.md. No live network or elevation tests.
