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
