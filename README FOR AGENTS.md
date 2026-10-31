# AV Network Manager — development documentation package

**Purpose:** document the approved development plan for a fork of [Network Profile Manager](https://github.com/rmzncakir/network-profile-changer), preserving continuity across Codex sessions and PRs.

**This is a planning overlay, not a modified application.** It contains Markdown instructions, specs, acceptance tests and a draft JSON Schema; no application source has been changed.

## How to use

1. Fork `rmzncakir/network-profile-changer` on GitHub and clone **your fork**.
2. Copy the **contents** of this package into the root of your fork, keeping its original code, existing `LICENSE`, `.git` and workflows intact. Review any filename conflicts before copying.
3. Open the fork in Codex and send the prompt in `prompts/CODEX_BOOTSTRAP.md` (the contents are English for the agent).
4. Start at Phase 01 (read-only audit). Do not authorize implementation until its report is reviewed.
5. On every future session use `prompts/CODEX_RESUME.md`. The agent must re-read `AGENTS.md`, `PROJECT_CONTEXT.md`, `STATUS.md`, `TASKS.md` and the current phase file.
6. Require one branch/PR per phase or independently reviewable subtask. Tests on Windows are mandatory before a release.

## Source of truth hierarchy

1. Direct user request made later (record scope changes in `decisions/DECISION_LOG.md`).
2. `AGENTS.md` — mandatory agent operating rules.
3. `PROJECT_CONTEXT.md` — scope, baseline, constraints and user intent.
4. `docs/spec/` — functional/security requirements.
5. `TASKS.md` and `phases/` — traceable work and acceptance criteria.
6. `STATUS.md` — **verified** progress only; not a substitute for specs.

## Contents

- `AGENTS.md`: persistent rules for Codex.
- `PROJECT_CONTEXT.md`: requirements, original product and constraints.
- `ROADMAP.md` + `TASKS.md`: nine phases and stable task IDs.
- `phases/01...09`: implementation tasks, DoD and risk mitigations.
- `docs/spec/`: proposed behavior and data model.
- `docs/security/`, `docs/testing/`, `docs/legal/`: critical operating constraints.
- `schemas/`, `examples/`: proposed project-file format, **not** an existing app format.
- `prompts/`, `templates/`: session prompts and handoff/PR templates.
- `machine/plan.json`: machine-readable task inventory.
- `scripts/validate_plan.py`: offline consistency checker.

## Known source facts (verified 2026-10-31)

| Topic | Network Profile Manager (base) | AV Network Toolbag (feature reference only) |
|---|---|---|
| Repository | `rmzncakir/network-profile-changer` | `mefranklin6/AV-Network-Toolbag` |
| Stack | C# / .NET 8 / WPF / MVVM | Dart / Flutter |
| License | MIT | No LICENSE found in current tree; permission is **not** presumed |
| Existing relevant functions | Complete static IP/mask/gateway/DNS, DHCP, JSON export/import, scan, history/revert | Link-local, temporary DHCP server, mDNS listener, diagnostics |

All source facts can change upstream; Phase 01 must verify them against the **fork's exact commit**. Never copy source code from AV Network Toolbag without appropriate permission.

## Adopted repository state (2026-10-31)

This package is attached to fork `Elguajo/AV-Network-Toolkit` at code baseline `603cf24fa0ba1819c0ec10b6a57ef5cf57c3852e`. Follow STATUS.md and the resume prompt. ADR-006 authorizes documentation adoption/local commit only; Phase 01 acceptance and Phase 02 start remain pending. Setup steps above are historical onboarding, not instructions to create another fork.

The imported scripts/validate_plan.py and MANIFEST.sha256 remain untracked and unchanged in this documentation-only commit. The validator has known status/link failures (01-T08); the archive manifest is not an integrity claim for this repository. Draft schema/example are versioned unchanged and remain unapproved. See docs/architecture/BASELINE_AND_SOURCE_MAP.md and docs/testing/TEST_MATRIX.md for verified code facts and NOT RUN limits.
