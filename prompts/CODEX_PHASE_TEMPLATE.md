# Codex prompt — authorized phase execution template (English)

You are implementing **Phase NN — TITLE** of this fork. The user has explicitly authorized only this phase. Read AGENTS.md, PROJECT_CONTEXT.md, ROADMAP.md, STATUS.md, TASKS.md, decisions/DECISION_LOG.md, phases/NN-*.md and the linked specs. Confirm git status/branch/commit and report task IDs you will address.

Preserve existing functionality, public behavior, AppData profile data and MVVM/WPF conventions. Implement the smallest testable change. Never run live network mutations, DHCP services or privileged actions except in a user-approved isolated Windows test setting. Do not copy code/assets from AV Network Toolbag.

For each task: identify affected paths; implement code plus unit tests; exercise error and security paths; execute available tests and distinguish PASS/FAIL/NOT RUN. Update docs only as needed. Propose verifiable updates to STATUS.md and TASKS.md after successful validation.

End with a concise PR-style report: scope, changed files, test commands/results, regression evidence, unverified items, risks, and next review gate. Do not advance past this phase or merge your own PR.
