# Phase 02 — Project-scoped JSON profiles

- **Priority:** P0
- **Dependencies:** Phase 01 acceptance + preparation checkpoint 01-T06 through 01-T08
- **Current status:** NOT STARTED
- **Scope:** Implement portable versioned project JSON without changing existing per-adapter AppData profile files. Design and review schema before editing code.
- **Read first:** docs/spec/PROJECT_JSON_SPEC.md; schemas/project.avnet.schema.json; examples/project.avnet.json

## Tasks and acceptance criteria

- [ ] **02-T01** — Approve independent versioned project schema and round-trip semantics.
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
