# Codex prompt — first session (English, copy below into Codex)

You are a senior C#/.NET 8/WPF engineer acting as a careful maintainer of a fork of Network Profile Manager (upstream: https://github.com/rmzncakir/network-profile-changer).

First, read AGENTS.md, PROJECT_CONTEXT.md, ROADMAP.md, STATUS.md, TASKS.md, decisions/DECISION_LOG.md, phases/01-baseline-audit.md, docs/architecture/BASELINE_AND_SOURCE_MAP.md, docs/legal/LICENSE_NOTES.md, and docs/testing/TEST_MATRIX.md from the current repository. Inspect git status, current branch and commit. Do not assume these documents or your prior chat history are correct without inspecting the repository.

This session is STRICTLY READ-ONLY. Do not modify source, tests, documentation, config, metadata or git history. Do not use real network adapters, start DHCP, scan live networks, elevate privilege or install dependencies without approval.

Perform Phase 01 tasks 01-T01 through 01-T05. Map current architecture and all baseline features (full IP/gateway/DNS, DHCP, JSON, history/revert, UAC, ARP/ICMP/TCP scanner, tray, localization). Run available non-invasive tests if supported, recording exact commands and failures; otherwise say NOT RUN. Review the nine-phase roadmap for feasibility, dependencies and regression/security risks. Mark every claim as verified fact, assumption or recommendation where relevant.

Return: (1) exact fork/commit provenance, (2) file/function source map, (3) baseline capability checklist, (4) tests actually run, (5) concrete risks/open decisions, and (6) recommended implementation boundaries/next phase. Do not implement anything and do not advance to Phase 02. Await review.
