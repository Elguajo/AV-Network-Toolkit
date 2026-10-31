# Phase 04 — implementation review, awaiting owner acceptance

2026-10-31, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit; branch main; starting anchor abdd1d1c61dd4f0d6b39adfc24ee69ff46367cbc (unpublished branding preserved). Resolve the implementation commit with `git log -1 --format=%H -- app/Services/ProjectApplyCoordinator.cs`; the documentation handoff records its exact resulting SHA. No PR/push/release and no phase marked complete.

Implementation commit: **3e7a0ae3f0f3ef87729fa94e2873d53e23750991**. Review full diff with `git diff abdd1d1..3e7a0ae` (41 paths, 2396 insertions/22 deletions); later documentation-only handoff pins that anchor. Local main only; branding preserved, no publication.

## Outcome and changed surfaces

04-T01–04-T07 have implementation/review evidence, pending owner acceptance. ADR-019 records both the explicit Phase 04 start and owner approval to block complex/unknown state. [Implementation contract](../spec/PHASE04_IMPLEMENTATION_CONTRACT.md) and [legacy history review](../architecture/PHASE04_LEGACY_HISTORY_REVIEW.md) define the boundaries.

- `app/Models/ProjectApply.cs`, `app/Services/ProjectApplyCoordinator.cs`: immutable single-use two-minute confirmation, explicit state machine, requested-versus-observed verification, bounded DAD/DHCP polling, cancellation and differentiated network/browser/journal outcomes.
- `app/Services/ProjectApplyPolicy.cs`, `ProjectApplyWire.cs`, `ProjectApplyWorker.cs`, `ProjectApplyElevation.cs`, `WindowsProjectApplyObservation.cs`: strict typed protocol, fixed IPv4 commands only, stable GUID/index/alias/MAC binding, fresh pre-state after UAC and identity before every command, explicit gateway/DNS/registration semantics, active/persistent snapshots, authenticated local pipe and conservative classification. No URL/executable/path/argument list in worker payloads.
- `app/Services/ProjectApplyJournal.cs`: independent private local pre-change request and final outcome files, create-new + durable flush, no project/device text or URL, no automatic restore/pruning. Original snapshot remains after final record failure. Explicit read-only recovery inspection only.
- `app/ViewModels/ProjectApplyViewModel.cs`, `app/Views/ProjectApplyWindow.xaml(.cs)`, Project/device additions and English resources: explicit NIC selection, scrollable old/new review, confirmation, actual results, separate recovery and manual route; HTTP/host mismatch warnings. Related profile drafts must be retained/discarded explicitly. Simple Open remains separate.
- `NetworkMutationLease.cs`, localized MainViewModel guard, App worker dispatch and tray exit guard, ProjectWindow/ApplyWindow exit guards: prevent concurrent legacy/project mutations within this instance and prevent orderly shutdown while cancel/observation is pending. New workers serialize per NIC across instances. No legacy apply command or history-format rewrite.
- `test/ProjectApply*.cs`, `test/DeviceDirectoryTests.cs`, `scripts/ProjectUiSmoke/Program.cs`, `scripts/validate_apply_observation.ps1`: fake OS/process/worker/browser boundaries, local temp journal fixtures and real WPF components without App startup.
- Canonical status/tasks/phase/machine plan/context/roadmap/ADR/spec/security/testing records updated consistently; stable task IDs and JSON v1 retained.

Protected paths compared with abdd1d1: NetworkService, legacy ElevationHelper, ProfileRepository, HistoryRepository, HistoryEntry, ProjectStore/ProjectJson, MainWindow, schema/example, license and workflow unchanged. MainViewModel change is the shared mutation guard; App changes are new worker dispatch and active-apply exit protection. Existing serial profile/revert/UAC behavior is retained; live compatibility remains UNVERIFIED.

## Exact local checks

Windows 10.0.26300.0 x64; SDK 8.0.427, .NET 8/WPF target unchanged. No new dependency/install. Variables:

```powershell
$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"
$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"
```

| Command/check | Outcome |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase04.trx'` | PASS, 399 tests, 0 failed/skipped; local ignored `test/TestResults/phase04.trx` |
| `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/Phase04UiSmoke` | PASS, 40 captures: existing 20 + Apply empty/review/error/recovery/recovered × Light/Dark × 760×760/620×520; 0 binding errors |
| `& ./scripts/validate_apply_observation.ps1` | PASS, embedded script parse/command allowlist and 6 fully shadowed OS-query fixtures: ordinary, policy, finite lifetime, registration, global DNS, suffix origin. No actual OS queries/processes/probes |
| `& $py -B scripts/validate_plan.py` | PASS, full draft-2020-12/formats/example/references, 9 phases/49 stable tasks and local links |
| `& $py -B scripts/test_validate_plan.py` | PASS, 10 regressions |
| `git diff --check` / staged whitespace | PASS |
| `git diff --quiet abdd1d1 -- app/Services/NetworkService.cs app/Helpers/ElevationHelper.cs app/Services/ProfileRepository.cs app/Services/HistoryRepository.cs app/Models/HistoryEntry.cs app/Services/ProjectStore.cs app/Services/ProjectJson.cs app/MainWindow.xaml app/MainWindow.xaml.cs LICENSE schemas/project.avnet.schema.json examples/project.avnet.json .github/workflows/build.yml` | PASS, exit 0 |

Behavior evidence: exact deep URL only after supported verification; no launch on partial primary/secondary DNS/address failure, mismatch, UAC denial, changed/missing NIC, cancellation or failed recovery; static gateway null emits explicit none, secondary DNS ordered, DNS unchanged emits no DNS command, DHCP explicitly resets DNS, registration preserved; DHCP needs usable preferred lease and automatic policy rather than ping/old lease. Pre-change journal failure aborts; final write failure retains verified network result. Expiry/replay/reentry/concurrent coordinators, stale selection, invalid/missing/duplicate/unknown/oversize/truncated/invalid-UTF8 messages and invalid responses are rejected. Unsupported addresses/gateways/routes/policy/persistence and non-restorable existing configurations block before mutation.

WPF evidence includes actual manual selection, no mutation on review/declined consent, readable old/new/actual state, enabled fixed footer, named selectors and forward focus; close/application-exit refusal while a fake worker is pending, then cancel → observe → exit. Existing directory selection/draft/simple-Open behavior remains exercised. Representative current Light/minimum and Dark/normal captures visually inspected against DESIGN.md. Right inset corrected scrollbar/text overlap; minimum dimensions require scrolling. Native Mica is composited for PNG inspection. This is not manual keyboard/AT/desktop-backdrop certification.

## Initial failures and corrections

- Focused runs passed 39 then 67 cases; compiler/analyzer warnings exposed an unused fake field and blocking test assertion, corrected before final evidence.
- First full run: 383 passed/1 failed in resource-key validation because literal dynamic prefixes were interpreted as keys. Replaced terminal-state lookup with explicit resource mapping and retained a real unknown-stage fallback; final complete suite passes without changing/skipping that assertion.
- Review found shutdown could bypass owned-window cancellation via tray; added explicit busy-apply guard and fake WPF close/exit regression. Added shared legacy/project mutation guard, per-NIC new-worker serialization and permanent inhibition if worker exit cannot be established.
- Source/fixture review corrected string CIM InterfaceGuid normalization, finite/nonstandard address origins, DNS registration preservation, missing boolean defaults, late expiry after final identity read, and non-restorable network/broadcast/out-of-subnet existing state. No live reproduction claimed.

## Review decision and remaining limits

Self-review (no independent reviewer/subagent): code, security, concurrency and failure paths inspected with the tests above. Corrected issues are recorded above. Limited review is not security certification. Design-system/visual evidence passed; manual accessibility is inferred from native controls/names/focus and remains NOT RUN. Publication/release decision is NO-SHIP pending owner review and isolated integration evidence.

| id | kind | status | source / decision |
|---|---|---|---|
| P04-ARCH | solution-architecture / codebase-explorer | passed | Source map, existing call graph and approved contract; reuse v1/browser/style, independent typed project boundary |
| P04-CODE | code-review | passed | Final changed source, protected-path diff and 399-test regression; scoped fixes above, no independent reviewer |
| P04-SEC | security-review | passed | Strict DTO/framing, worker rejection/ACL/command fixtures and redacted failure paths; native IPC/UAC unverified |
| P04-CONC | concurrency-review / reliability-review | passed | Replay/reentry/two coordinators/cancellation/partial command/journal/failed recovery fixtures; WPF cancel-observe-exit guard |
| P04-UI | design-system / visual-qa | passed | DESIGN.md, 40 component renders and actual footer/selection/focus assertions; representative captures inspected |
| P04-A11Y | accessibility | inferred | Named native controls, focus traversal, wrapping and scrolling; manual AT/high contrast/zoom NOT RUN |
| P04-SOURCE | source-driven-implementation | passed | Microsoft contracts linked in implementation spec, .NET 8/local SDK and 6 fake-query fixtures; actual Windows integration NOT RUN |
| P04-GATE | release-check | unavailable | NO-SHIP: owner phase review/manual isolated integration/deployment evidence pending. Local implementation available for review; no release/push authorized |

Supported gate is intentionally narrow: one preferred ordinary IPv4, <=1 matching active/persistent default gateway, known ordinary IPv4 DNS (<=2 observed servers), recognized generated local routes only, known registration flags and ordinary infinite manual lifetimes. Static automatic DNS (including leave-unchanged from automatic), /31–/32, APIPA, extra addresses/gateways/routes, alternate DHCP, managed/unknown state and unrestorable settings block. An initial DHCP adapter without a usable lease is blocked too. Recovery may accept a classified transient state but will block unknown/complex partial results; the original snapshot remains available for manual work. DHCP recovery verifies policy plus a usable current lease, not historical IP/DNS leases.

Windows observation uses fixed local NetTCPIP/DnsClient queries and registry cross-checks. Unsupported/missing cmdlets, denied registry/policy queries or unrecognized values fail closed. Actual Windows 10/11 cmdlet/netsh/UAC/pipe behavior is **UNVERIFIED** in this run. Registry/policy classification is bounded, not a claim that every third-party network manager is detectable. No real apply/recovery support certification follows from fake fixtures.

The pipe uses first-instance creation, initiating-user ACL with NETWORK denial and peer PID/executable checks, one request per worker and a short lifetime. Same-account UAC consent is intended; alternate administrator credentials can be blocked by the ACL. CurrentUserOnly is intentionally not used across differing elevation levels. Same-user process injection, a replaced app binary, malicious local filesystem races and arbitrary storage/hardware guarantees are outside the tested boundary. NIC recheck and index-targeted netsh cannot form an atomic OS transaction; external Windows/legacy-other-instance edits or removal in the final check-to-command interval remain a race. Post-state comparison cannot establish absence of every unrelated setting change.

Journal folder: `%LOCALAPPDATA%/NetworkProfileManager/apply-recovery`. Files contain private NIC configuration, not credentials/URLs. No retention pruning; read caps at 1000 and reports an error instead of silently omitting older snapshots. Preserve/manage the folder manually when needed. Forced process/OS termination can leave only the original snapshot and uncertain actual state; nothing reapplies automatically on restart. A worker whose exit is unconfirmed inhibits further mutations in the initiating app instance; manual inspection must wait for worker exit.

NOT RUN: real NIC enumeration acceptance, network assignment/recovery, netsh/PowerShell observation integration, actual UAC/IPC peer checks, actual browser/TLS/navigation, DNS/HTTP/ping/scans, DHCP server, full App/tray/native dialogs, manual keyboard/AT/high contrast/zoom, power loss/hardware/filesystem adversary, CI, publish/clean machine. No unit cases excluded. Prior Phase 02/03 manual and storage/URL limitations remain historical.

## Separately authorized isolated integration plan

Use only a disposable isolated Windows VM/test NIC with local console, an out-of-band recovery route and no production bridge. Owner approval plus isolation are prerequisites; this packet authorizes no execution.

Record exact binary/commit, OS/build, NIC GUID/index/MAC and full pre-state. Test static→static/DHCP, old gateway→none/new, two DNS, unchanged DNS, registration flags, DHCP no lease/DAD delay, cancel/UAC deny, NIC rename/removal/replacement, partial failure/timeout, denied journal, browser failure and complete/failed/blocked recovery. Observe IPv4 active/persistent state, IPv6/unrelated settings and routes before/after independently; do not use reachability as assignment proof. Verify complex cases block before any command. Test same-user/alternate-credential UAC and worker disconnect/peer rejection separately. Preserve original journal and actual results for every failure. No TLS bypass or automatic recovery.

## Deviations and next gate

Approved deviation: conservative block policy under ADR-019 rather than speculative complex-state recovery. New local pipe instead of legacy temp transport; no generic elevated runner or legacy history migration. No reprioritization into Ping/link-local/mDNS/diagnostics/DHCP server.

Safe next action: owner reviews this diff and component evidence; decide Phase 04 acceptance and whether to authorize isolated manual validation. Proposed next development phase is 05 — Link-Local, only after explicit owner decision. No automatic push, release or Phase 05 start.
