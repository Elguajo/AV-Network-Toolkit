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

## ADR-018 — Match application display name to the fork, 2026-10-31

Owner reported «Названия приложение не соотвествует названию нашего форк репо». This authorizes the bounded correction to **AV Network Toolkit** for the existing App.Title resource, tray tooltip and AssemblyTitle/Product metadata. Tray reads the existing localized title resource. Preserve internal assembly/executable/namespace names, startup registry identity, data paths, JSON, MIT attribution, layout and network behavior. This supersedes the earlier identity-preservation constraint for display/product wording only; no executable/data migration, later phase, live test, push or release authorization. Work directly on main under ADR-017 and commit the verified change locally.

## ADR-019 — Phase 04 start and conservative support boundary, 2026-10-31

Owner explicitly requested «Начинай фазу 4 на новом чате». This authorizes 04-T01–04-T07 directly on main, based on abdd1d1c61dd4f0d6b39adfc24ee69ff46367cbc, preserving the unpublished branding commit. Supersedes historical Phase 04 NOT AUTHORIZED snapshots only; no fabricated prior manual acceptance. No push, live network/UAC/browser tests, release or Phase 05 authorization.

After reviewing the explicit preserve-or-block proposal, owner answered «Утверждаю: блокировать сложное/неподтверждённое». Support only fully classified ordinary IPv4, one address, at most one gateway and ordinary known DNS. Block extra addresses/gateways, custom routes, policy-managed/unknown state before mutation. Block static-IP/automatic-DNS and /31–/32 until semantics are proved. JSON v1 storage remains unchanged. See [concrete implementation/recovery contract](../docs/spec/PHASE04_IMPLEMENTATION_CONTRACT.md). Keep legacy behavior compatible; do not repurpose its incomplete history for new recovery.

## ADR-020 — Publish current main including Phase 04, 2026-10-31

Owner explicitly requested «Коммит и пуш в main» after the implementation report. Authorizes the current main publication, including branding abdd1d1c61dd4f0d6b39adfc24ee69ff46367cbc, Phase 04 implementation 3e7a0ae3f0f3ef87729fa94e2873d53e23750991, handoff 571977947adcb07015f97d81714da9252cfb9d68 and this documentation-only authorization record. Supersedes earlier lack of push authorization for these changes only. Direct main workflow remains; no new branches, force push, tag/release, live tests or Phase 05 authorization. Phase/manual acceptance is not fabricated.

Preflight: clean main at 571977947adcb07015f97d81714da9252cfb9d68; fetched origin/main bb506194723fb8f5c60a7e845ef69b3bc853cb41 is an ancestor, 0 remote-only/3 local-only commits. Application/test/workflow code unchanged since recorded 399-test/40-render evidence; no unnecessary runtime rerun. Run planning/link/schema/tooling/whitespace checks, commit this bounded documentation update with owner identity, ordinary `git push origin main:main`, independently verify local/remote SHA. Normal CI may run the isolated suite; report CI separately from local evidence. Final push result is verified after execution and reported to the owner.

## ADR-021 — Phase 05 execution authorization, 2026-10-31

Owner explicitly requested «Начинай фазу 5 на новом чате». Start 05-T01–05-T05 directly on main at `48913b21b7a4ddf62a9f58663fbfcde79d630a29`; clean tree verified. Supersedes historical no-Phase-05 authorization snapshots only. Prior phase/manual acceptance limits remain; no push, release, live NIC/probe/UAC tests or Phase 06 authorization.

Smallest implementation uses Phase 04 typed apply/recovery and a transient manual action, preserving approved project JSON v1 and legacy formats. No persisted link-local project mode or migration is authorized implicitly. Windows DAD is checked during assignment, with explicit warning and enabled-DAD precondition; no speculative off-subnet ping/ARP availability test. Full [contract and proposed DNS refinement](../docs/spec/PHASE05_IMPLEMENTATION_CONTRACT.md). ADR-019 conservative blocking remains in force; any owner reply approving the proposed automatic-to-static DNS refinement must be recorded separately before implementing it.

## ADR-022 — Explicit DNS replacement for manual Link-Local, 2026-10-31

Owner answered «Утверждаю явный ввод статических DNS при автоматическом DNS» to the concrete Phase 05 contract. Approves an explicit 1–2 ordinary IPv4 static DNS list when entering manual Link-Local from automatic DNS, with old policy/values retained for separate recovery. Empty input preserves known static DNS only; it never clears DNS, converts automatic policy silently, or bypasses unknown/managed/complex-state blocking. The transient action leaves project JSON v1 and legacy data unchanged. Automatic APIPA/no-lease mutation remains blocked; live tests, publication, phase acceptance and later phases remain separate gates.

## ADR-023 — Publish Phase 05 and start Phase 06 in a new chat, 2026-10-31

Owner requested «Коммит и пуш и начи новую фазу в новой сесси» after the Phase 05 report. Authorizes ordinary publication of current main (implementation `d054a61ccd2f27ebd03cc73b9acb5e03e6ecd52d`, evidence handoff `e5858635759b22a7cd0d5a277b9eafebab67e2f4`, plus this bounded authorization record), then creation/start of a new project chat for Phase 06, 06-T01–06-T04. Supersedes previous no-push/no-Phase-06-start snapshots for these actions only; do not ask again for the same start permission.

Preflight: clean main at `e5858635759b22a7cd0d5a277b9eafebab67e2f4`; freshly fetched origin/main `48913b21b7a4ddf62a9f58663fbfcde79d630a29` is an ancestor, 0 remote-only/2 local-only commits. This chat changes authorization/planning/handoff metadata only, validates it, commits with owner identity, pushes main ordinarily and verifies local/remote SHA before dispatching the new chat. Final push/new-chat outcomes are reported after execution, not preclaimed here. No branches/worktrees, force push, tag or release.

Phase 06 develops original selected-interface mDNS listener/discovery, explicit start/stop, bounded untrusted packet/cache processing, service/host/address/port/TTL display, filtering/safe exports and optional confirmed device drafts. Reuse DESIGN.md, current MVVM/project/browser boundaries and strict JSON v1; no automatic NIC mutation/browser launch or schema migration. Fake socket/clock/interface/OS boundaries in tests; do not join multicast/open live listeners/probe production networks during CI or analysis. Live isolated execution still requires separate approval. No Phase 07/08 or automatic publication of future Phase 06 changes.

Phase 05 manual acceptance/05-T04 and earlier native limits are not retroactively passed or marked complete. **P05-R01 remains OPEN**; root cause/baseline attribution UNVERIFIED, final 463-case PASS does not close it. Carry it as a release blocker into Phase 06, without silently repairing/weakening/suppressing the unrelated JSON gate. Normal isolated push CI may run; its result is distinct from local evidence and may expose the recorded instability.

## ADR-024 — Permit device passwords in project JSON, 2026-10-31

Owner requested a comparison against the original problem and explicitly said «даю разрешения хранить еще пароли в JSON». This supersedes the blanket password-storage prohibition in AGENTS.md, the original product brief and the historical idea summary. Optional portable device-password storage is now permitted product scope (FR-13 / IDEA-03). This is not a claim of implementation, automatic login permission, token storage permission, TLS bypass or permission to commit real credentials.

Current HEAD at review start: main `648f207` (full anchor from `git rev-parse HEAD` in the handoff); pre-existing Phase 06 work is uncommitted and excluded. Existing v1 remains strict and has no password fields. Future implementation must settle version/compatibility before schema changes and cover explicit credential UI, exact round-trip, redaction, backups and no silent data loss. Do not weaken URL validation or use notes as a password-storage workaround. Anyone with a plaintext project file or its backups can read stored passwords; no encryption guarantee is made.

The [product-fit review](../docs/PRODUCT_FIT_REVIEW_RU.md) records IDEA-01 (standalone project profile apply) and IDEA-02 (practical isolated-network transitions) as gaps, without fixing them during mDNS work. This request authorizes the comparison and recording of the new permission; it does not advance/accept a phase, select a new schema version, change network behavior, authorize live tests or publish changes. Existing task IDs and phase order remain; implementation scheduling/design are separate follow-ups, and password permission itself need not be asked again.

## ADR-025 — Adopt the main-screen adapter/profile workflow, 2026-10-31

Owner endorsed the selected main-screen proposal with «С учетом этого нужно дальше идти», following the screenshot and explanation of choosing a connected port, creating/selecting profiles and deliberately applying settings. Adopt FR-14 / IDEA-04: selected adapter → observed state → saved/new profile or manual settings → explicit target-named Apply → observed outcome → optional device interaction. Distinguish laptop IP settings from device IP/URL; project files carry profiles/devices within that same understandable workflow. Open versus Apply & Open and ADR-024 password scope are preserved. Primary source and dispositions: [product-fit review](../docs/PRODUCT_FIT_REVIEW_RU.md#согласованное-направление-главного-экрана--adr-025).

This accepts the product direction and localized refinement within DESIGN.md, not a sweeping redesign or implicit migration of the legacy/project stores. IDEA-01/02 are reused, IDEA-03 remains password follow-up, and 07-T01–07-T04 retain adapter/Ping diagnostic ownership. No implemented behavior/phase acceptance is claimed. Exact layout, main/project mapping and phase placement require a bounded design handoff; current Phase 06 AWAITING_REVIEW, 49 phase task IDs/order, conservative apply gates, OPEN P05-R01 and live-test/publication gates are unchanged. This adoption does not itself start Phase 07, modify OS settings, approve a schema version or authorize push/release.

## ADR-026 — Select the second main-window concept as the visual target, 2026-10-31

Owner explicitly selected «Второй тогда зафиксируем как цель» after seeing two raster concepts. ACCEPTED visual target for FR-14 / IDEA-04: [second concept](../docs/design/main-window-target-v2.png), contract owned by [DESIGN.md](../DESIGN.md#approved-main-window-target--adr-026--fr-14). Adapter navigation stays left, current adapter information and wide profile rows stay right, every profile has an explicit Apply action, and Add/Edit opens a separate editor. Equipment uses the Device interfaces tab. The first permanently expanded-editor concept is not the selected goal.

The image is copied unchanged from the approved built-in imagegen output, with SHA-256/provenance retained in DESIGN.md; no dependency on an ephemeral chat image path. Selection approves layout/hierarchy within the existing visual language. It does not approve generated text/pixels as behavior, merge/migrate stores, relax network support/URL validation, implement credentials, accept Phase 06 or authorize Phase 07/live tests/push/release. Existing 49 task IDs/order and OPEN P05-R01 remain. Remaining design work covers store mapping and unpictured responsive/error/busy/credential/editor states; the main-window layout choice itself needs no repeated approval.

## ADR-027 — Publish all current main changes, 2026-10-31

Owner explicitly requested «Коммит и пуш в main все изменения». Authorizes ordinary publication of current main, including Phase 06 implementation/evidence, original-idea/product-fit documentation, ADR-024 password permission and ADR-025/026 workflow/visual target, plus this authorization/handoff. Supersedes earlier no-push statements for this current publication only. No phase acceptance, new implementation, Phase 07 start, live tests, tag/release, force push or history rewrite is implied.

Preflight: clean main `6f10792a43c316f3cc0961650c7638e8dc1a79b8`; freshly fetched origin/main `f160ac0986af59c7cc4357021b8ca7fe942b63e0` is an ancestor, 0 remote-only / 7 local-only commits. No merge/new branch needed. Validate documentation, commit this bounded publication record, ordinary push `main:main`, then independently compare local HEAD and remote main. Prior 542-test/84-render evidence and OPEN P05-R01/native/manual limits remain; CI is separate from local verification. Actual push outcome is reported after execution.

## ADR-028 — Implement the selected main workspace, 2026-10-31

Owner answered «Да делаем» to proceeding with the approved second-concept implementation. Authorizes the bounded IDEA-01/04 follow-up directly on main. [Contract](../docs/spec/MAIN_WORKSPACE_CONTRACT.md): explicit adapter/project scopes, one shared project VM, detached modal profile editor, per-row standalone project apply through Phase 04 with explicit GUID target, device tab and expandable existing tools. Reuses stores without migration. This records routine integration choices within the approved scope; password/schema work and support expansion remain separate. Phase 06 stays AWAITING_REVIEW; no implicit Phase 07/live-test/release/push permission or phase completion.

## ADR-029 — Audit and refine the main workflow UX, 2026-10-31

Owner explicitly requested real-control scenario exploration and minimal implemented usability improvements. [Baseline findings/evidence](../docs/testing/UX_WORKFLOW_REVIEW.md). Authorizes local IDEA-04 refinements: clearer source/save/apply language, normal Save through existing IO with conflict/overwrite/backup guarantees, actionable field validation/focus, modal-close draft protection, empty-state actions and visible status. No new screens, schema/store migration, network command/policy change, credential implementation or Phase 07 advancement; Phase 06/manual/P05-R01 gates remain. Work directly on main; no live tests/new push/release implied. Actual usability still requires owner walkthrough.

2026-11-01 owner feedback: the circled Network profiles / Devices and On this computer / Project profiles tabs are convenient. Keep that hierarchy; this is approval of tabs only, not full-flow/native acceptance or phase completion. Final regression observed unsafe dhcpIpv4 acceptance; retain UX-R01 / OPEN P05-R01 investigation and release blocker independently of this layout feedback.

## ADR-030 — Match the compact project/profile toolbar, 2026-11-01

Owner corrected the marked block with «Мы же хотели так сделать здесь» and supplied the compact target crop. This newer placement direction supersedes retaining the second visible tab row: project identity/file and Open/Save in one bar, Network profiles / Device interfaces below, saved-profile heading with actions at right. [Target and implemented evidence](../docs/testing/COMPACT_WORKSPACE_REVIEW.md). Retain the existing product resources and a local underlined tab treatment derived from its accent/hover/focus vocabulary.

Implementation choice within this localized correction: keep the two independent profile stores selectable through a compact named source ComboBox beside the heading; move Save as/Manage to the existing menu idiom, not remove them. No data merge/migration or new project-profile import format. Owner's earlier positive tab feedback is historical, not acceptance of this selector or the full journey. IDEA-04 only, directly on main; Phase 06/manual/JSON blocker and live-test/publication/release gates remain. No password, supported-state, worker/schema or phase-order change.

## ADR-031 — Reference appearance for profile toolbar actions, 2026-11-01

Owner explicitly requested Import, Export and Add Profile visually identical to the circled reference. This authorizes the narrowly scoped muted-blue component palette, contour glyphs, sizing, spacing and divider as a refinement of the existing WPF-UI controls. It supersedes system-accent-only styling for these actions; other product components keep their current appearance. Light/system-high-contrast mapping and narrow wrapping preserve readability/access. [Implementation/evidence](../docs/testing/PROFILE_VISUAL_STATES_REVIEW.md); no command/data/network change or phase advancement.

## ADR-032 — Selected is reported current state, 2026-11-01

Owner requested the blue current-profile row and Selected button, then explicitly chose «Когда профиль совпадает с текущими настройками адаптера; Selected показывает состояние». Adopt read-only Selected based on available adapter facts, not edit selection/click or a remembered last-applied ID. Preserve explicit reapply through the profile menu. Static IP/prefix/gateway and requested DNS semantics must match; unknown automatic-DNS source leaves Apply available, including DHCP profiles. Share the display rule across independent local/project lists without changing legacy tray matching, worker verification or stored data. This is current-state presentation only, not a reachability/support guarantee. Phase 06/native/JSON/publication/release gates remain.

## ADR-033 — Publish the current workspace refinements, 2026-11-01

Owner explicitly requested «коммит и пуш». Authorizes ordinary publication of current main, including seven existing local commits ef2e477 through 6ef8fe7 and this publication handoff. Clean preflight HEAD `6ef8fe7230d96ce4c7a53a1b5a7677aae3d644f6`; freshly fetched origin/main `103b50d34c6f63d69271a03e8caf4a15494e3e43`, 0 remote-only / 7 local-only. No merge, force push or new branch needed. Supersedes prior no-new-push snapshots for this publication only. Phase 06 acceptance, OPEN UX-R01/P05-R01, native/live-test, later-phase and release gates remain. No code/runtime change; validate planning/whitespace, commit handoff, push and verify remote SHA. Record actual outcome after execution.

Executed: ordinary push through authorization commit `15ec8f520d71ffec841eee17eaace75cdc33f23e` succeeded and independent remote-main lookup matched exactly. This outcome record is part of the same authorized publication. No release/tag, phase acceptance or CI success claimed.

## ADR-034 — Phase 07 execution, 2026-11-01

Owner explicitly requested «Делаем 7 фазу». Authorizes 07-T01–07-T04 directly on main, bootstrap `db143aba6e4405da57acf2a85f661f3b579c279d`, including ADR-016 explicit adapter/source Ping in the inline tool and separate window. Supersedes previous pending-start statements for Phase 07 only. [Contract](../docs/spec/PHASE07_IMPLEMENTATION_CONTRACT.md). Preserve legacy profile/scanner/history/revert/UAC/tray, strict JSON v1 and DESIGN.md. No migration, network-support expansion, DHCP server, live probes/NIC changes/elevation, push/release or Phase 08 permission. Earlier phases' owner/native acceptance is not fabricated. UX-R01/P05-R01 stays OPEN and release-blocking; no unrelated JSON repair or gate weakening here.

Use existing read-only Windows query style for diagnostic snapshots, native OS route prediction and source-address ICMP with explicit request/result association. Confirmed `vms_pp` binding on a physical adapter is the uplink evidence; names alone never classify or hide. Saved filter applies only to diagnostic selectors; legacy main/tray enumeration remains available. Source remains explicit after refresh. Hostname lookup uses OS DNS (may use another interface); literal IPv4 avoids lookup. Source-specific ICMP plus pre/post route checks does not prove physical egress or eliminate route/identity races. Native behavior requires isolated acceptance; these limits must remain visible.
