# AGENTS.md — standing instructions for Codex

This file is the persistent contract for every agent session, regardless of chat history or context compaction.

## Session bootstrap — required, in this order

1. Read `PROJECT_CONTEXT.md` and `ROADMAP.md`.
2. Read `STATUS.md`, `TASKS.md`, and `decisions/DECISION_LOG.md`.
3. Read the active `phases/NN-*.md` and any specifications it references.
4. Inspect the **current repository**, `git status`, current branch, recent commits, applicable code and test files. Do not assume prior chat statements are current.
5. Restate current phase, explicit in-scope items, preserved behavior, acceptance checks, risks and branch before editing.
6. If Phase 01 is active, remain **read-only**. No code/docs/metadata changes until the phase itself is authorized to finish.

## Non-negotiable rules

- Preserve **all existing Network Profile Manager functionality** unless a specific, reviewed task explicitly changes it: IP/mask/gateway/DNS, DHCP, JSON import/export, named per-adapter profiles, network scanner, ping, history and revert, tray, UAC behavior and localization.
- Add features incrementally, minimize diffs, preserve MVVM and testing conventions unless Phase 01 documents a justified alternative.
- Do **not** make sweeping rewrites or visual redesigns. Retain the UI language and layout except where a feature requires a localized addition.
- Do **not** modify real OS network settings, run a DHCP server, scan a live network, or require administrator elevation during unit tests, CI or read-only analysis. Explicit approval + an isolated Windows test environment are prerequisites for live tests.
- Do **not** change network configuration merely because a project file or device link was opened. Applying network settings must be a separate deliberate action.
- Never silently remove existing gateway, DNS, routes, or network adapter settings. Define and test exact semantics for empty/automatic fields and recovery paths.
- Do **not** save passwords, tokens, or device credentials to project JSON. Reject dangerous URL schemes (file, javascript, shell, etc.). Never auto-bypass TLS warnings.
- Respect base repository's MIT license/notice. AV Network Toolbag is **reference for desired behaviors only**: its repository lacks a declared license in the inspected version, so never port/copy its code, resources, images or UX assets.
- Do not fabricate test results, support claims or implementation status. Mark unknowns `UNVERIFIED`, test skips `NOT RUN`, and record specific tool/environment limits.
- No agent must merge its own PR or mark a phase complete without acceptance evidence and user review.
- Work in a focused branch, commit scoped changes, produce a diff summary, and give an explicit safe next step.
- A newly surfaced bug outside the active phase is logged as a risk or issue; it is not silently fixed in the same PR.

## Definition of Done for a task

- Task acceptance criteria and security controls are met.
- Relevant automated tests were run and results recorded (or limitations explicitly reported).
- No regression in existing profile, scanner, history/revert and UAC logic.
- Documentation/JSON schema examples updated consistently if behavior changed.
- `TASKS.md` checkboxes and `STATUS.md` updated only for verified completions; evidence includes commit/PR IDs and test commands/results.
- Session handoff appended using `templates/SESSION_HANDOFF.md`.

## Context retention protocol

- `STATUS.md` must always state: exact fork URL, commit SHA, active phase, completed tasks, in-progress tasks, blockers, next action, tests last executed.
- `decisions/DECISION_LOG.md` records any approved change in requirements, data-format version, or architecture with date and rationale.
- `TASKS.md` stores stable IDs; never delete an unfinished task or renumber tasks without recording a decision.
- Prefer on-disk evidence over chat history. When resuming, inspect status and git independently.
- Do not update `STATUS.md` just to make progress look better. Unknown is acceptable.

## Review gate

At the end of **each** phase report: (1) outcome, (2) changed paths, (3) exact tests and results, (4) risks, (5) deviations, (6) a proposed next phase. Await user decision before advancing to the next phase.
