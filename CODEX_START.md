# Codex entry point

- **First session:** copy prompt from `prompts/CODEX_BOOTSTRAP.md`.
- **Every later session:** copy prompt from `prompts/CODEX_RESUME.md`.
- **Always load:** `AGENTS.md`, `PROJECT_CONTEXT.md`, `STATUS.md`, `TASKS.md`, current `phases/NN-*.md`, relevant `docs/spec/`.
- **Phase 01 investigation:** read-only historically; separately authorized preparation was verified under ADR-008/009. Preserve recorded evidence/manual limits.
- **Every phase:** use its recorded authorization. Work directly on main, no separate branches (ADR-017). Live tests, publication, release and later phases retain separate gates; do not ask again for already-authorized scope.

This package does not claim Codex has persistent memory; continuity comes from repository files, commit history and evidence-backed status updates.

Current handoff: Phase 07 AWAITING_REVIEW under ADR-034; use STATUS.md, docs/spec/PHASE07_IMPLEMENTATION_CONTRACT.md and docs/testing/PHASE07_REVIEW.md, independently inspect git and run the required AGENTS.md bootstrap. 624 isolated tests, 15 fake Windows-query fixtures and 160 WPF captures across focused/existing checks recorded. Native NIC/ICMP/route/Hyper-V and actual egress NOT RUN / UNVERIFIED. OPEN UX-R01/P05-R01 and earlier owner/native limits remain. No Phase 08, push/release or live-test authorization.
