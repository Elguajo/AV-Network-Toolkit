# Phase 04 — Apply & Open

- **Priority:** P0
- **Dependencies:** Phase 02, Phase 03
- **Current status:** AWAITING_REVIEW — 04-T01–04-T07 EVIDENCE_RECORDED; owner acceptance pending
- **Scope:** Provide explicit Apply & Open with selected NIC, UAC, post-apply verification and recovery paths.
- **Read first:** docs/spec/APPLY_OPEN_AND_ROLLBACK.md; docs/security/THREAT_MODEL.md
- **Implementation contract:** [support, typed boundary, recovery and verification](../docs/spec/PHASE04_IMPLEMENTATION_CONTRACT.md).
- **Review evidence:** [exact commands, results, risks and isolated integration plan](../docs/testing/PHASE04_REVIEW.md). No live tests or next-phase authorization implied.

## Tasks and acceptance criteria

- [ ] **04-T01** — Define explicit state machine and adapter-selection gate.
  - **Acceptance:** No implicit apply; user sees target adapter, requested old/new config.
- [ ] **04-T02** — Snapshot pre-change config, apply with UAC, verify actual post-state.
  - **Acceptance:** Settings re-read; failure clearly reported; best-effort recovery design.
- [ ] **04-T03** — Launch browser only after supported network-state verification.
  - **Acceptance:** Not tied to ping success; no browser launch on failed apply.
- [ ] **04-T04** — Add cancellation, error messaging, retry and manual recovery route.
  - **Acceptance:** All branches documented incl. UAC denied, adapter removed, partial netsh failure.
- [ ] **04-T05** — Unit tests for state machine and isolated Windows integration plan.
  - **Acceptance:** No real NIC changes in CI; manual NIC tests reported separately.

- [ ] **04-T06** — Review and harden the elevated request boundary for the new apply flow.
  - **Acceptance:** Bind typed validated request to stable NIC identity, recheck after UAC, review request/result paths and tampering, and cover rejection paths without elevation.
- [ ] **04-T07** — Review legacy history and revert limitations separately.
  - **Acceptance:** Define backward-compatible handling of missing DNS/policy/routes/additional-address snapshots; scope any legacy repair separately and never invent recoverable state.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.

## Audit-derived acceptance details

- 04-T01/04-T06: no alias-only identity, first-adapter fallback or changed-NIC substitution in the new flow; bind request to reviewed selection and revalidate at the elevated boundary.
- 04-T02: record every affected address/prefix, gateway/route and DNS policy/value needed for recovery. Choose and review preserve-or-block behavior for complex/unsupported configurations before mutation; existing AdapterInfo/HistoryEntry are insufficient.
- 04-T02/04-T04: compare actual post-state; on partial command failure re-read actual state, distinguish apply from history-write failure and offer deliberate verified recovery. Do not launch browser on failed/mismatched apply.
- 04-T05: fixtures for secondary DNS, automatic DNS with static IP, multiple addresses/gateways, cancellation at boundaries, renamed/replaced NIC, worker tampering and failed/partial recovery.
- 04-T07 retains old history compatibility; missing fields are unknown, not evidence that recovery is complete. Legacy behavior changes require their own scoped review.
