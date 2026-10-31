# Decision log — immutable history by appended records

| ID | Date | Decision | Rationale | Status |
|---|---|---|---|---|
| ADR-001 | 2026-10-31 | Base fork = Network Profile Manager (MIT) | Complete IP/Mask/Gateway/DNS + JSON + WPF | User-approved |
| ADR-002 | 2026-10-31 | AV Network Toolbag = feature reference only | No declared license at inspection | Safety constraint |
| ADR-003 | 2026-10-31 | Nine review-gated development phases | Keep Codex context and regression risk under control | User-approved |
| ADR-004 | 2026-10-31 | Add project JSON and Device Web Interfaces | Project-scoped AV configuration and deep URLs | User-approved concept; data schema provisional |
| ADR-005 | 2026-10-31 | Proposed versioned `project.avnet.json` sidecar | Avoid replacing existing AppData JSON model | PROPOSED (Phase 02 review) |
| ADR-006 | 2026-10-31 | Adopt Phase 01 audit evidence into canonical documentation/plan and make a local documentation-only commit | User replied "Разрешаю" to the proposed documentation package/commit; avoid losing findings between sessions | User-approved adoption scope; phase acceptance and Phase 02 start NOT approved |

## ADR-006 scope and consequences

- Source: Phase 01 bootstrap report and subsequent user-approved documentation/plan proposal in this chat on 2026-10-31. Evidence and per-finding dispositions: [baseline/source map](../docs/architecture/BASELINE_AND_SOURCE_MAP.md).
- Preserve nine phases and all existing task IDs. Add preparation checkpoint 01-T06/01-T07/01-T08 before Phase 02 implementation, plus scoped reviews 02-T06/04-T06/04-T07 and APIPA acceptance task 05-T05. Mirror them in machine/plan.json.
- The original investigation stays read-only historically; this explicit exception authorizes recording evidence and committing documentation now. It authorizes no source/test/tool changes, dependency installation, live tests, push/PR/merge or phase advancement.
- Existing planning Markdown plus draft schema/example/machine JSON are first versioned by the documentation commit. Draft schema/example contents are not changed or approved. Application sources, tests, license and workflows remain unchanged.
- Material choices remain NEEDS_DECISION: project catalogue layout, project/legacy mapping, unknown-field/future-version policy, reserved link-local mode, suspected URL-secret policy, and support/rejection of complex network configurations. ADR-005 remains PROPOSED.
- Legacy storage/history fixes are recorded as separate reviews, not silently folded into feature work. SDK/test-isolation/tooling checkpoint execution requires separate authorization.
- Commit evidence: the commit containing this record on `codex/phase-01-baseline-audit` (resolve using `git log -1 --format=%H -- decisions/DECISION_LOG.md`). A commit cannot embed its own resulting SHA; exact pre-commit evidence anchor is recorded in STATUS.md and final commit ID is reported in the session handoff response.

## How to log a change

Append `ADR-XXX | YYYY-MM-DD | accepted/rejected/proposed | scope and alternatives | consequences | who approved`. Never rewrite history to imply a proposal was implemented or approved. Link relevant commit/PR.
