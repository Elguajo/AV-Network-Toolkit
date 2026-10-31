# Phase 02 review packet — awaiting owner review

Date: 2026-10-31, Asia/Tashkent. Branch `codex/phase-02-project-json`.

## Outcome and exact artifacts

- Preparation: `11a6e5cc12f0d752dcbda9160b55369233bb6c98` — SDK/checkpoint, isolated scanner/adapter tests, repaired validator and approved schema proposal.
- Project implementation: `eeb589cf81d8871f4243218d2e1a668829b79a2e` — independent JSON v1, models, structural/semantic validation, atomic local IO, stable editor IDs/refs, New/Open/Choose Folder/Save As and component verification harness.
- Owner localization refinement: `0bf1070216255c9e1411a4a96445372b6e6a45d1` — retire Turkish, retain English, read-compatible settings fallback, English native dialog labels and updated design contract.
- Status/handoff commit is documentation only; resolve with `git log -1 --format=%H -- STATUS.md`. No push/PR/merge/release performed. Phase 02 is not marked complete; Phase 03 has not started.

Diff review: `git diff 5e6231f731a532fd0947903e4dbaf47115f0d2e3..0bf1070216255c9e1411a4a96445372b6e6a45d1 --stat` and the full diff at those anchors.

Changed surfaces: project model/validator/store/viewmodel/window; localized main-window Project entry and new-project tray-exit draft guard; shared navigation style reused without changing its existing adapter template; English resources/settings language normalization; project schema/example/spec; DESIGN/AGENTS/decisions; preparation/feature tests and scripts; legacy persistence review and dependency notices.

## Verification

Windows 10.0.26300 x64, local SDK 8.0.427 / runtime 8.0.33. PowerShell variables: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`, `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`.

| Exact command / check | Verified outcome |
|---|---|
| `& $sdk restore NetworkProfileManager.sln` | PASS, including pinned JsonSchema.Net 7.4.0 dependencies |
| `& $sdk build NetworkProfileManager.sln --configuration Release --no-restore` | PASS, 0 warnings/errors |
| `& $sdk test NetworkProfileManager.Tests/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase02-english.trx'` | PASS: 286 tests, 0 failed/skipped, 2 s; local ignored artifact `NetworkProfileManager.Tests/TestResults/phase02-english.trx` |
| `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/ProjectUiSmoke` | PASS: 4 isolated English renders (Dark/Light × 900×780/700×540), bound edits/stable selected ID, no discard prompt during commit, enabled delete command, rejected selection discard, close/application-exit guards, named form controls, legacy `tr` → English dictionary swap, 0 WPF binding errors |
| Visual inspection of current `TestResults/ProjectUiSmoke/project-En-*.png` | Existing Fluent styling/theme surfaces, navigation selection, command hierarchy and fixed footer inspected; native Mica composited over existing ApplicationBackgroundBrush for PNG capture. No claim of desktop/AT certification |
| `& $py -B scripts/validate_plan.py` | PASS: 9 phases, 49 stable task IDs, machine/backlog/phase agreement, valid states/checkboxes, schema draft-2020-12 with FormatChecker, example references and Markdown links |
| `& $py -B scripts/test_validate_plan.py` | PASS: 10 tests; malformed states/tasks/checkboxes/links and invalid IPv4 format are rejected |
| `& $sdk list NetworkProfileManager/NetworkProfileManager.csproj package --vulnerable --include-transitive` | No vulnerable packages reported by configured NuGet source at this date; not a supply-chain guarantee |
| Read-only Python XML comparison of original adapter ListBoxItem style at 11a6e5c against shared NavigationListItem | PASS: attributes, templates and triggers identical; only resource key/placement changed |
| `git diff 603cf24 -- NetworkProfileManager/Services/ProfileRepository.cs NetworkProfileManager/Services/HistoryRepository.cs NetworkProfileManager/Services/NetworkService.cs NetworkProfileManager/Helpers/ElevationHelper.cs LICENSE .github/workflows/build.yml` | Empty diff: legacy persistence/netsh/UAC/license/workflow unchanged |
| `git diff --check`, staged `git diff --cached --check` | PASS |

Meaningful coverage: round-trip IP/prefix/gateway/three DNS policies/DHCP/adapter hints/deep URLs; required/missing/empty/null fields; stable and duplicate IDs, orphan refs, unknown/future formats, ambiguous/corrupt JSON/UTF-8, bounded size; unsafe schemes/userinfo/secret query+fragment encodings and redacted errors; create-only/confirmed overwrite; locks/read-only target; serialization/write/flush/backup/backup-flush/replace injection; actual replace permission failure; cooperative lease and external-write race recovery. VM tests prove no legacy writes, retained drafts, valid pending editor export and referenced-delete rejection. Language fixtures prove repeated old `tr`/System interpretation leaves settings bytes/theme untouched.

No isolated unit cases were excluded. NOT RUN: real scanner/ICMP/ARP/TCP, real-NIC enumeration acceptance, actual apply/DHCP/revert/UAC, browser launching, production or isolated network tests, full app/tray integration, native file-dialog interaction, manual keyboard/screen-reader/high-contrast/zoom acceptance, actual power-loss/disk-full/hostile-link races, publish/clean-machine and CI. Failure injection is not a hardware test. No release/support/coverage claim follows from unit success.

## Initial failures and side effects retained as evidence

- New tests initially failed to compile (missing System.IO and obsolete async assertion overload). Corrected; successful reruns recorded.
- Initial JsonSchema.Net 9.5 experiment failed due to disposed schema backing JsonDocument; corrected, then that dependency was excluded after local nuspec/OSMFEULA inspection. Final 7.4.0 plus transitive package licenses are MIT; notices ship in build/publish output. Final API is FromText/EvaluateAs/RequireFormatValidation, verified by tests and installed package XML.
- Final 7.4.0 IPv4 format accepts abbreviated forms; an initial negative test caught this. Public schema now adds canonical dotted-quad patterns; semantic validation still runs separately. No weakened assertion.
- Resource test caught the dynamic error-key prefix; added a real generic English error fallback. Path test caught GetFullPath normalizing trailing dots; validate raw segments before normalization.
- **Failed first UI harness:** instantiated the actual `NetworkProfileManager.App`, whose startup callback ran when dispatcher work was pumped. It reached `AppPaths.MigrateLegacyDataIfPresent`, then failed in language-resource resolution before startup refresh, tray/network-monitor creation or MainWindow/NIC construction. No network mutation/probe/UAC occurred through this path. Afterward `.migrated_v1` was observed in LocalAppData (creation/write UTC 06:13:34); pre-run existence was not captured, so creation attribution is UNVERIFIED. No marker or user data was removed/rolled back. Harness source/output had no legacy data/settings payload to copy. Corrected harness uses plain WPF Application and explicitly loaded component resources; final PASS applies to that isolated harness only.
- Earlier successful 278-test / En+Tr component evidence predates ADR-011; final acceptance evidence is 286 tests plus English-only renders. Turkish removal is an explicit owner-requested exception, not accidental regression.

## Specialist review evidence

| ID | Kind | Status | Source / decision |
|---|---|---|---|
| P02-CODE | code-review | passed | Changed source/diff and regression tests inspected; corrected schema lifetime, strict IPv4 and selection mutation before final checks. No independent reviewer/subagent used; owner review remains required |
| P02-DATA | data-storage-review | passed | ProjectStore failure/backup/conflict tests and schema/spec. Unique backups, no destructive reset; [legacy review](../architecture/LEGACY_PERSISTENCE_REVIEW.md) proposes separate remediation only |
| P02-SEC | security-review | passed | Embedded-schema/no-network boundary, strict input/path checks, redacted URL failures and dependency notices inspected. Bounded review, not security certification |
| P02-UI | design-system / visual-qa | passed | [DESIGN.md](../../DESIGN.md), existing styles/templates and English Light/Dark PNGs; existing adapter style XML equality verified |
| P02-A11Y | accessibility-review | inferred | Named native controls, source tab order/focus styles, wrapping/scrolling/fixed footer inspected; manual assistive-technology/keyboard/zoom/high-contrast acceptance NOT RUN |
| P02-LANG | data-migration | passed | ADR-011 and temp settings tests: no format/path/version change or automatic rewrite, fallback is idempotent, old bytes remain readable by old app |
| P02-GATE | release-check | unavailable | NO-SHIP for publication/merge: owner phase review is pending, full integration/deployment certification is outside this phase. Local Phase 02 implementation is ready for review |

## Risks, deviations and next phase

No remaining known failed automated checks. Residual risks: manual UI/native-dialog/AT acceptance, filesystem/hardware guarantees, conservative/incomplete secret-key detection, unbounded backup retention, baseline legacy persistence/history/apply limitations and the early harness marker uncertainty above. Forced OS/process termination does not recover unsaved in-memory project drafts.

Scoped deviations: local files only (no UNC/network shares/reparse paths); pinned MIT dependency rather than newer binary with extra terms; explicit owner refinements ADR-010/011 (existing design for all new UI, English-only support). No device UI/browser launcher, Apply & Open, link-local execution or later-phase features were added.

Next safe action: owner reviews this diff and the Project window. Phase 02 remains AWAITING_REVIEW until that decision. Proposed next phase after acceptance: Phase 03 — Device Web Interfaces. Do not begin it, push/merge or run live-network tests merely because this packet exists.
