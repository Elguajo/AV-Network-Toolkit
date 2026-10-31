# Codex prompt — resume any later session (English)

Resume work on this existing fork, not on a new app or rewritten codebase.

Before touching any file: read AGENTS.md, PROJECT_CONTEXT.md, ROADMAP.md, STATUS.md, TASKS.md, decisions/DECISION_LOG.md, the current phases/NN-*.md, and the relevant docs/spec files. Inspect git status, branch, HEAD, recent commits and any uncommitted changes. Reconcile file status against git and tests; do not trust the previous chat alone.

Summarize: current exact phase and task IDs, approved scope, preserved behavior, last verified checks, blockers, pending decisions and safest next action. If no explicit current-phase authorization exists, stop after proposing the next action. Do not work on later phases.

If authorized to implement, use a focused branch with minimal diffs. Retain original Network Profile Manager features and existing data compatibility. Do not copy AV Network Toolbag source/assets; treat that project only as a functional reference. Do not mutate production network adapters or run DHCP in CI.

At session end, propose updates to TASKS.md and STATUS.md supported by actual evidence, note tests with exact commands/results, and provide a handoff matching templates/SESSION_HANDOFF.md. Never mark skipped tests as passed.
