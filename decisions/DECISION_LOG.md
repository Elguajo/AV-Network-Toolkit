# Decision log — immutable history by appended records

| ID | Date | Decision | Rationale | Status |
|---|---|---|---|---|
| ADR-001 | 2026-10-31 | Base fork = Network Profile Manager (MIT) | Complete IP/Mask/Gateway/DNS + JSON + WPF | User-approved |
| ADR-002 | 2026-10-31 | AV Network Toolbag = feature reference only | No declared license at inspection | Safety constraint |
| ADR-003 | 2026-10-31 | Nine review-gated development phases | Keep Codex context and regression risk under control | User-approved |
| ADR-004 | 2026-10-31 | Add project JSON and Device Web Interfaces | Project-scoped AV configuration and deep URLs | User-approved concept; data schema provisional |
| ADR-005 | 2026-10-31 | Proposed versioned `project.avnet.json` sidecar | Avoid replacing existing AppData JSON model | PROPOSED (Phase 02 review) |
| ADR-006 | 2026-10-31 | Adopt Phase 01 audit evidence into canonical documentation/plan and make a local documentation-only commit | User replied "Разрешаю" to the proposed documentation package/commit; avoid losing findings between sessions | User-approved adoption scope; phase acceptance and Phase 02 start NOT approved |
| ADR-007 | 2026-10-31 | Merge the completed documentation branch into main and push main to origin | User explicitly requested merge to main, then added "Можешь и Push делать" | User-approved integration/publication of this documentation change only; no phase advancement |
| ADR-008 | 2026-10-31 | Start Phase 02 in a new chat, including prerequisite 01-T06–01-T08 preparation | Owner explicitly requested Phase 02 start and transferred bounded SDK/test/tool preparation scope | User-authorized execution; schema choices still require 02-T01 review; no live tests, merge/push or Phase 03 authorization |

| ADR-009 | 2026-10-31 | Approve the Phase 02 format package in docs/spec/PHASE02_FORMAT_REVIEW.md | Owner answered «Утверждаю пакет решений» to the explicit 02-T01 review | User-approved schema/semantics; no OS apply or legacy mapping in Phase 02 |
| ADR-010 | 2026-10-31 | All new windows, buttons and other UI elements reuse the existing product design, documented in DESIGN.md | Owner explicitly requested existing style/design for every new interface element | User-approved standing constraint; localized additions only, no redesign or separate visual system |

| ADR-011 | 2026-10-31 | Retire Turkish UI support; keep English and map legacy tr preferences to English on read | Owner explicitly said «Турецкий язык не нужен»; execution assumption stated in chat: remove Turkish from app, retain English | User-authorized localization exception; no automatic settings-file rewrite, profile-format/network behavior unchanged |

| ADR-012 | 2026-10-31 | Commit the integration handoff, merge the verified Phase 02 branch into main and push main to origin | Owner explicitly requested «Коммит и пуш в Main можно сделать Merge» after the implementation report | User-approved integration/publication of this change; ordinary push, no force; no Phase 03 or live-test authorization |

| ADR-013 | 2026-10-31 | Simplify repository paths to app/ and test/, and rename solution to AVNetworkToolkit.sln | Owner selected lowercase «App и Test можно строчной» and retained the proposed solution name | User-approved structural change only; project filenames, namespaces, assembly identities and user-data paths stay unchanged |

| ADR-014 | 2026-10-31 | Commit the owner-confirmed obsolete entry-file deletion and merge/push the verified path-refactor branch to main | Owner confirmed deleting 00_READ_FIRST_RU.md, then explicitly requested «Сделай merge в main и сделай коммит и пуш» | User-approved integration of ADR-013 plus that deletion; ordinary push, no force; no later-phase/live-test/release authorization |

## ADR-006 scope and consequences

- Source: Phase 01 bootstrap report and subsequent user-approved documentation/plan proposal in this chat on 2026-10-31. Evidence and per-finding dispositions: [baseline/source map](../docs/architecture/BASELINE_AND_SOURCE_MAP.md).
- Preserve nine phases and all existing task IDs. Add preparation checkpoint 01-T06/01-T07/01-T08 before Phase 02 implementation, plus scoped reviews 02-T06/04-T06/04-T07 and APIPA acceptance task 05-T05. Mirror them in machine/plan.json.
- The original investigation stays read-only historically; this explicit exception authorizes recording evidence and committing documentation now. It authorizes no source/test/tool changes, dependency installation, live tests, push/PR/merge or phase advancement.
- Existing planning Markdown plus draft schema/example/machine JSON are first versioned by the documentation commit. Draft schema/example contents are not changed or approved. Application sources, tests, license and workflows remain unchanged.
- Material choices remain NEEDS_DECISION: project catalogue layout, project/legacy mapping, unknown-field/future-version policy, reserved link-local mode, suspected URL-secret policy, and support/rejection of complex network configurations. ADR-005 remains PROPOSED.
- Legacy storage/history fixes are recorded as separate reviews, not silently folded into feature work. SDK/test-isolation/tooling checkpoint execution requires separate authorization.
- Commit evidence: the commit containing this record on `codex/phase-01-baseline-audit` (resolve using `git log -1 --format=%H -- decisions/DECISION_LOG.md`). A commit cannot embed its own resulting SHA; exact pre-commit evidence anchor is recorded in STATUS.md and final commit ID is reported in the session handoff response.

## ADR-007 scope and consequences

- Supersedes only ADR-006's lack of merge/push authorization for the completed documentation change. Source documentation commit: `abb1430e4cf1fb02a0c201073ded3d81728476bf`; include this integration handoff follow-up.
- Fetch/reconcile remote main first. Prefer fast-forward; ordinary push only, no force/history rewrite. Preserve untracked original archive manifest/validator, exclude them from commit/push. No PR merge or branch deletion is required.
- No implicit phase acceptance, task completion, SDK install, source/test/tool changes, live tests or Phase 02 authorization.
- The existing main push workflow runs unfiltered tests that emit real probes (F06). Use `[skip ci]` on this documentation integration commit to avoid unauthorized live probes until 01-T07 is addressed. Do not call skipped CI passing and do not change workflows or publish tags/releases. [GitHub skip semantics](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/skip-workflow-runs) were checked before relying on the marker.
- Exact merge/push outcomes and final local/remote SHA are verified after execution and reported in the chat handoff; STATUS.md retains the known pre-integration anchor and reproducible commit lookup.

## How to log a change

Append `ADR-XXX | YYYY-MM-DD | accepted/rejected/proposed | scope and alternatives | consequences | who approved`. Never rewrite history to imply a proposal was implemented or approved. Link relevant commit/PR.

## ADR-011 compatibility

Settings remain the same flat string dictionary and file path. `en` and `System` remain valid preferences; System resolves to supported English. Saved `tr` normalizes in memory to `en`; startup uses persist=false and leaves existing bytes/other keys untouched, including on repeated reads. An older app can still read the original value. A later explicit user settings change uses the existing writer. Removed `Tr.xaml` and the Turkish menu option; retained resource-based labels and English dictionary. Unsupported culture inputs never load a removed dictionary. No customer settings were edited to exercise this transition; tests use temp fixtures.

## ADR-012 integration scope

Integrate source branch `codex/phase-02-project-json` at `da8a976409fd90fafbb8b19499660b557f6880b1` plus this authorization/handoff record. Fetched local main and origin/main both equal `5e6231f731a532fd0947903e4dbaf47115f0d2e3`; both are ancestors of the source, so use `git merge --ff-only`. No PR, force push, history rewrite, branch deletion, tag or release is required. Preserve untracked MANIFEST.sha256.

Existing code verification remains 286 isolated tests, Release build, four component renders and ten validator regression tests. No source changes in integration; no unnecessary rerun of unchanged checks. Unit OS/probe boundaries were re-inspected before allowing normal push CI; ADR-007's earlier skip-CI workaround concerned the pre-isolation documentation push and is not needed now. CI outcome must be reported separately from local PASS evidence.

The user authorized integrating these results. Phase 02's previously recorded manual acceptance limitations and Phase 03 review gate remain visible; this record does not invent native-dialog, live-network, deployment or full phase-acceptance results. Execution after this commit: switch main, fast-forward merge, ordinary `git push origin main:main`, compare local HEAD with `git ls-remote origin refs/heads/main`. Report actual outcomes and resulting SHA after execution.

## ADR-013 repository-path scope

Rename `NetworkProfileManager/` to `app/`, `NetworkProfileManager.Tests/` to `test/`, and `NetworkProfileManager.sln` to `AVNetworkToolkit.sln`. Update solution/project references, source-root discovery in tests, CI build/release command paths and current documentation links/commands. Historical execution commands remain recorded under their original names; this is a path refactor, not retroactive execution evidence. `MANIFEST.sha256` is the original archive inventory and is excluded from this refactor; it was absent at final verification.

No product, network behavior, JSON format, namespace, assembly/executable name or `%LOCALAPPDATA%/NetworkProfileManager/data` migration. SDK/build/test/smoke verification must pass on the new paths. Phase 02 review state and later-phase/live-test gates are unchanged. This authorization does not extend ADR-012's completed merge/push scope to the new change.

## ADR-014 integration scope

Source: `codex/simplify-project-paths` at `b119a84ffb7a8202a1fefeb24ea58cc0a882fde4`, plus this integration handoff and the owner's confirmed `00_READ_FIRST_RU.md` deletion. At preflight, local main and freshly fetched origin/main both equal `4cddfcd1dad4be867d499e74a3d5213929bda7f8`; both are ancestors of source. No production source/test changes in this step. Preserve namespaces, assembly/output/data identities and current Phase 02 review limits.

Record the deletion with explicit paths rather than staging unrelated working-tree changes. Run planning/link/schema and tooling regression/whitespace checks, commit on source, switch main and `git merge --ff-only codex/simplify-project-paths`, then ordinary `git push origin main:main` and independently compare local HEAD with `git ls-remote origin refs/heads/main`. Final command outcomes/SHA are verified after execution. No PR merge, tag/release, force push or branch deletion. Normal push CI may run the previously isolated unit suite; its result is separate from local PASS evidence.

## ADR-015 — Phase 03 execution authorization, 2026-10-31

Owner requested «Начинай 3 фазу на новом чате». Start Phase 03 (03-T01–03-T05) on `codex/phase-03-device-web-interfaces`, based on `17c7014ba585fe85680fcdede51ce967f238054d`. This explicitly supersedes stale Phase 03 NOT AUTHORIZED snapshots. Phase 02 integration evidence/manual limitations remain historical; no fabricated manual acceptance. No merge/push, live network/browser execution, release or Phase 04 authorization.

Reuse `ProjectDevice` and strict JSON v1 unchanged. Group by existing type or related profile inside the current project; no persisted tags/group field, migration or new schema. Extend the Project window using DESIGN.md. URL launch is a distinct deliberate command, passes the exact validated string through the default-browser association, and never applies a profile, probes a host or bypasses TLS. Show destination and HTTP/IP-host/unlinked warnings before confirmation. Imported orphan references stay rejected; an editor reference can be explicitly relinked or unlinked, and referenced profiles cannot be deleted. Draft guards cover device edits too.

## ADR-016 — Deferred adapter-specific Ping requirements, 2026-10-31

Owner requested «Запиши на будущее задачи» after discussing simultaneous Ethernet/Wi-Fi and Ping lacking adapter binding. Record explicit adapter/source IPv4 selection and routing visibility under 07-T02, plus isolated regression coverage under 07-T04. This is approved backlog adoption only; no Phase 07 implementation, reprioritization, general CMD shell, live tests or publication authorization. Existing task IDs, phase order and application/network behavior remain unchanged.

Current destination-only Ping calls cannot guarantee the selected adapter is used. A user diagnostic screenshot showed Wi-Fi routing and an unreachable result; it establishes neither Ethernet reachability nor the cause. The supplied static IPv4 has no verified mask/adapter assignment. Keep private addresses out of repository examples. Future UI must separate requested/OS-predicted/observed routing and fail visibly when an explicit adapter cannot be honored, without silent fallback or network-setting changes.

## ADR-017 — Publish current work and use main directly, 2026-10-31

Owner requested «Сделай merge в main и коммит и пуш» and «не создавать отдельные ветки и работать в main всегда». This authorizes the current integration/publication and replaces AGENTS.md's focused-branch rule with direct work on main for this repository. No new branches unless the owner explicitly changes that instruction. Future phase/review/live-test/release gates remain unchanged; working on main does not itself authorize a new phase or every future push.

Preflight: current source `codex/plan-adapter-ping` at `9dd249dda74d03e23856117d756a7d63f50309d0` includes Phase 03 `e861c3a426f24e0103e9cc26b7e82b6e2f1e503f`, handoff `dfd5a71839354a91ee1b2b520831a69a7b60ce13` and approved deferred Ping documentation (ADR-016), with clean working tree. Freshly fetched origin/main and local main both equal `17c7014ba585fe85680fcdede51ce967f238054d`; ancestry checks passed. Switched to main and fast-forwarded to source successfully, preserving the approved planning record. No conflict, force/history rewrite, PR merge or branch deletion.

Commit this documentation/rule update directly on main, run schema/planning/tooling/whitespace checks, ordinary push main to origin and independently compare local/remote SHA. Final outcomes are verified after execution and reported in chat; the containing commit is resolved via `git log -1 --format=%H -- STATUS.md`. Existing 308 unit tests/20 WPF renders/build evidence is retained; documentation-only integration does not invent new live/manual/CI acceptance or mark a phase complete. Normal push CI may execute the isolated unit suite; its outcome is separate from local evidence.
