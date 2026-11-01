# Roadmap — fixed phase order

## Bounded diagnostics addition — ADR-041, 2026-11-01

Owner approved the expanding Network tests concept and explicitly requested implementation. Separate follow-up NT-01 adds described Ping four/continuous/single and one-port TCP presets inline and in the existing separate window; [contract](docs/spec/NETWORK_TEST_PRESETS_CONTRACT.md), [local review/evidence](docs/testing/NETWORK_TEST_PRESETS_REVIEW.md). This is a Phase 07 diagnostic follow-up, not a new numbered phase. Independent REL-03 authorization belongs to ADR-040; this addition does not change it. Phase 07 AWAITING_REVIEW, native/owner acceptance, ADR-037 safe-only execution and no automatic push/release/Phase 08 remain.

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

- ADR-039: primary installer and additional portable ZIP of the same version, both including .NET runtime. Clean-Windows/no-preinstalled-.NET verification required for each; packaging and release gates remain separate.

ADR-045 publication milestone: [v1.1.1 Preview](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.1) ships current implemented fork features/new logo with both same-source self-contained packages. [Hosted verification](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41273489475) passed at 3fd7e77; this does not complete the nine-phase roadmap or real NIC/native/client acceptance. DATA-01 user-home migration and Phase 08 DHCP remain unimplemented/separately gated.

Original-outcome review: [product fit and pending IDEA-01–03](docs/PRODUCT_FIT_REVIEW_RU.md). ADR-024 permits optional device passwords in project JSON; FR-13 implementation/versioning is pending. Standalone project Apply Profile and practical isolated-network transitions need separate follow-up scope. This records gaps/permission without changing the nine-phase order or expanding the concurrent Phase 06 implementation.

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

## Current publication follow-up — ADR-020

Owner authorized committing/pushing current main including branding and Phase 04. Ordinary push only; phase order and manual/live-test/release gates remain. This supersedes stale no-publication statements for the current changes, without marking Phase 04 complete or starting Phase 05.

## Phase 05 start — ADR-021/022

Owner explicitly authorized 05-T01–05-T05 in a new chat and approved explicit static DNS replacement for automatic DNS. Current work: manual Link-Local using Phase 04 infrastructure, strict JSON v1 unchanged. [Contract](docs/spec/PHASE05_IMPLEMENTATION_CONTRACT.md) defines supported states and DAD/recovery limits. Prior acceptance gaps remain historical; Phase 06, live tests and publication of new work require separate owner decisions.

## Publication and next phase authorized — ADR-023

Owner requested current commit/push and next-phase execution in a new chat. Publish Phase 05 ordinarily and start Phase 06 mDNS (06-T01–06-T04) in a new local project chat, directly on main. No fabricated Phase 05/manual completion, no live tests/release/Phase 07/08 or automatic push of new Phase 06 work. P05-R01 remains OPEN and release-blocking; phase order unchanged.

## Phase 06 evidence — review gate

Bounded owner-authorized follow-up ADR-028 implements IDEA-01/04 using the selected second-concept workspace; [review packet](docs/testing/MAIN_WORKSPACE_REVIEW.md). This is a scoped follow-up to existing profile/device/apply work, not Phase 07 or a reordering of the nine phases. Project/legacy storage stays separate; IDEA-02/03 remain pending. Phase 06 owner/native review and later-phase/live-test/release gates remain.

06-T01–06-T04 now have [local implementation evidence](docs/testing/PHASE06_REVIEW.md), AWAITING_REVIEW. Passive IPv4 only; IPv6 advertisements displayed when received over IPv4, optional Add-to-device omitted. No schema, password or legacy mutation changes. Native isolated acceptance and owner review remain pending. Proposed next phase is 07 (including ADR-016 Ping), requiring a new owner decision; IDEA-01–03 scheduling remains separate and the fixed order is unchanged.

## Phase 07 execution — ADR-034, 2026-11-01

Owner explicitly started 07-T01–07-T04 directly on main, including ADR-016 Ping. [Contract](docs/spec/PHASE07_IMPLEMENTATION_CONTRACT.md). This supersedes prior Phase 07 pending-start snapshots only; earlier owner/native acceptance and OPEN UX-R01/P05-R01 remain. No live tests, new publication/release or Phase 08 implied.

Phase 07 now has [implementation/evidence](docs/testing/PHASE07_REVIEW.md), AWAITING_REVIEW: 624 full unit PASS, 15 fake query fixtures and 160 WPF captures across focused/existing checks. Native/owner acceptance remains pending; no phase completed, live test, publication/release or Phase 08 implied.
