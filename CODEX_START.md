# Codex entry point

- **First session:** copy prompt from `prompts/CODEX_BOOTSTRAP.md`.
- **Every later session:** copy prompt from `prompts/CODEX_RESUME.md`.
- **Always load:** `AGENTS.md`, `PROJECT_CONTEXT.md`, `STATUS.md`, `TASKS.md`, current `phases/NN-*.md`, relevant `docs/spec/`.
- **Phase 01 investigation:** read-only historically; separately authorized preparation was verified under ADR-008/009. Preserve recorded evidence/manual limits.
- **Every phase:** use its recorded authorization. Work directly on main, no separate branches (ADR-017). Live tests, publication, release and later phases retain separate gates; do not ask again for already-authorized scope.

This package does not claim Codex has persistent memory; continuity comes from repository files, commit history and evidence-backed status updates.

Current handoff: Phase 04 AWAITING_REVIEW under ADR-019; use STATUS.md and docs/testing/PHASE04_REVIEW.md, independently inspect git and run the required AGENTS.md bootstrap. 399 isolated tests/40 component renders recorded; actual NIC/UAC/browser integration NOT RUN. No Phase 05, push or live-test authorization.
