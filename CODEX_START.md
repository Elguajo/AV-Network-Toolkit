# Codex entry point

- **First session:** copy prompt from `prompts/CODEX_BOOTSTRAP.md`.
- **Every later session:** copy prompt from `prompts/CODEX_RESUME.md`.
- **Always load:** `AGENTS.md`, `PROJECT_CONTEXT.md`, `STATUS.md`, `TASKS.md`, current `phases/NN-*.md`, relevant `docs/spec/`.
- **Phase 01 investigation:** read-only; no live network actions. ADR-006 separately authorizes recording audit findings and a local documentation-only commit. Preparation 01-T06 through 01-T08 is planned, not authorized to execute.
- **Start of phase 02 onward:** request/confirm explicit authorization for that phase and create a focused branch.

This package does not claim Codex has persistent memory; continuity comes from repository files, commit history and evidence-backed status updates.

Current handoff: Phase 01 AUDIT_RECORDED_AWAITING_REVIEW; use STATUS.md rather than re-running the first-session bootstrap. Documentation adoption does not authorize Phase 02, SDK installs, test/tool fixes or unfiltered suite execution. Check source-map dispositions and test limits before proposing the next scoped preparation task.
