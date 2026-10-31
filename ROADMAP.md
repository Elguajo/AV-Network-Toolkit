# Roadmap — fixed phase order

| Phase | Title | Priority | Depends on | Primary deliverable |
|---|---|---|---|---|
| 01 | Baseline audit | P0 | fork available | Read-only audit + separately authorized documentation adoption; preparation checkpoint before Phase 02 |
| 02 | Project-scoped JSON profiles | P0 | 01 approved + preparation checkpoint | Versioned project import/export, folder selection |
| 03 | Device Web Interfaces | P0 | 02 | Device directory + safe browser launch |
| 04 | Apply & Open | P0 | 02,03 | Explicit apply → verify → open flow |
| 05 | Link-Local | P1 | 01,04 | Safe 169.254/16 address workflow |
| 06 | mDNS discovery | P1 | 01,03 | Bound listener, searchable results, safe exports |
| 07 | Ethernet/Hyper-V diagnostics | P1 | 01,04 | Adapter diagnostics without breaking switching |
| 08 | Temporary DHCP server | P2 | 05,07 | Explicit, isolated DHCP workflow + cleanup |
| 09 | Integration tests and release | P0 | 02–08 | Full regression report, release checklist, Windows build |

**Rule:** each phase has acceptance gates; tests run *throughout*, not first introduced in Phase 09. User can reprioritize only through a logged decision. Phase 08 is not enabled for live networks until safety requirements pass.

## Stable deliverables

- A versioned project JSON schema and migrator/reader in addition to current profile stores.
- A project-scoped Device Web Interfaces catalogue linked by stable `profileId`; same-file versus separate-file layout remains a Phase 02 decision, independent of legacy AppData.
- Explicit network adapter selection and re-verification after Windows UAC.
- Safe link opening (full URL preserved; browser default; allowed `http(s)` only).
- A dedicated AV discovery/diagnostics toolkit with no unapproved side effects.
- Documentation showing what was tested, on which Windows machine, and against which commit.

## Audit adoption and preparation checkpoint (ADR-006)

Phase order and priorities remain unchanged. Audit evidence is recorded, not phase acceptance. Phase 02 is NOT AUTHORIZED by documentation adoption.

Before Phase 02 implementation: close 01-T06 (SDK/build evidence), 01-T07 (isolated test boundary and safe baseline run), and 01-T08 (planning validator compatibility) through separately authorized work. These preparation tasks may change tests/tooling only in their own reviewed scope, never during read-only audit. Excluded integration cases must be listed as NOT RUN; unit green does not replace isolated Windows acceptance.

Phase 02 must settle 02-T01 decisions before changing the draft schema. Phase 04 must establish snapshot/identity/verification/recovery guarantees before enabling Apply & Open. Legacy persistence and history fixes remain separately scoped reviews (02-T06, 04-T07), not silent feature-phase repairs. Findings and dispositions live in docs/architecture/BASELINE_AND_SOURCE_MAP.md; runtime evidence lives in docs/testing/TEST_MATRIX.md.

## Current authorization — 2026-10-31

ADR-008 supersedes the earlier pending-start snapshot: the owner explicitly authorized Phase 02 and its prerequisite preparation in a new chat. ADR-009 approves the concrete format package in docs/spec/PHASE02_FORMAT_REVIEW.md. 01-T06–01-T08 have verified preparation evidence; historical Phase 01 acceptance is not fabricated. No authorization for live tests, future phases, merge or push.

## Phase 02 integration authorization — ADR-012

On 2026-10-31 the owner explicitly authorized committing the integration handoff, merging the verified Phase 02 branch into main and pushing main to origin. This supersedes earlier lack of publication authorization for this change only. No Phase 03, live-test, tag/release or force-push authorization is implied. Exact anchors and execution plan: STATUS.md / ADR-012.

## Naming-refactor integration authorization — ADR-014

On 2026-10-31 the owner requested commit, merge to main and push of the verified lowercase-path refactor, after confirming the obsolete 00_READ_FIRST_RU.md deletion. ADR-014 authorizes that integration/deletion and supersedes the earlier pending-publication snapshot for this follow-up. Phase 03/live-test/release gates remain unchanged.

## Phase 03 authorization — ADR-015

On 2026-10-31 the owner explicitly requested Phase 03 in a new chat. 03-T01–03-T05 execution is authorized, preserving JSON v1 and DESIGN.md. Supersedes stale no-Phase-03-authorization snapshots only. No live tests, merge/push, release or Phase 04 authorization.

## Current integration and branch rule — ADR-017

Owner authorized merging/committing/pushing the current Phase 03 work to main on 2026-10-31. Approved deferred Ping planning (ADR-016) is preserved. Future work in this repository runs directly on main without creating separate branches, as now required by AGENTS.md. Phase order, manual/live-test/release gates and Phase 04 start authorization remain unchanged.

## Phase 04 authorization and review — ADR-019

Owner explicitly started Phase 04 and approved conservative blocking of complex/unknown/unrestorable state before mutation. 04-T01–04-T07 have implementation evidence in [Phase 04 review](docs/testing/PHASE04_REVIEW.md), awaiting owner acceptance. Prior manual limitations remain historical. Phase order is unchanged; proposed Phase 05 needs a new owner decision. No new publication, release or live tests authorized.
