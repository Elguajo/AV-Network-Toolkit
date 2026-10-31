# Phase 05 — Link-Local implementation review, awaiting owner acceptance

2026-10-31, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit; main; base `48913b21b7a4ddf62a9f58663fbfcde79d630a29`. Owner start authorization ADR-021 and explicit DNS decision ADR-022. Resolve implementation SHA with `git log -1 --format=%H -- app/Services/LinkLocalAddressing.cs`; the subsequent documentation handoff pins that SHA. No push, release or phase completion.

## Outcome and scope

05-T01–05-T05 have local implementation/unit/component evidence; acceptance remains open. 05-T04 isolated-adapter execution is **NOT RUN**. No real NIC assignment, enumeration acceptance, ARP/ICMP/TCP probes, elevation or browser launch was performed. [Exact support contract](../spec/PHASE05_IMPLEMENTATION_CONTRACT.md).

- Manual Link-Local is a separate title-bar action. Explicit NIC selection, local candidate suggestion or manual address, canonical `169.254.1.0–169.254.254.255`, /16 and no gateway. Suggestion/open/selection/review changes no network settings. Old/new review and final consent remain separate.
- Reuse Phase 04 coordinator, typed/elevated worker, authenticated pipe, short single-use confirmation, pre-change private journal, stable identity and per-stage rechecks, mutation lease, post-state comparison and cancellation/exit guards. No second command runner or new dependency.
- Conflict detection uses enabled Windows DAD **during assignment**, not ping or a speculative off-subnet availability check. Worker waits for preferred address before DNS commands. Duplicate → explicit conflict/partial-change result; tentative/missing/disabled/unknown DAD → failure/unverified. No automatic address retry, recovery or browser.
- Preserve known static DNS with empty fields, or explicitly replace DNS with 1–2 validated servers. Owner-approved automatic→explicit-static transition stores original automatic policy for separate recovery. Deliberate restore and return-to-DHCP are independent reviewed actions. DHCP success requires a usable lease; APIPA/no lease is not success.
- Handle the expected partial manual-Link-Local/automatic-DNS state only as a current state for explicit recovery **to DHCP**. Do not treat it as a restorable static source, admit custom/managed/unknown state or restore unproved static automatic DNS.
- Keep observed link-local addresses in enumeration, selected-adapter facts and disconnected adapter rows. Manual label requires manual origins and known disabled DHCP. DHCP-enabled link-local is labelled APIPA/alternate configuration; unknown origin/policy stays unknown. Reduced display is never recovery authority.
- Project JSON v1/schema/example and legacy profiles/history/storage remain unchanged. Manual action is transient. New optional local `Dad` snapshot evidence preserves reading of older journals without rewrites; older binaries may reject new fields. No downgrade recovery compatibility claim.

Changed areas: `app/Services/LinkLocalAddressing.cs`, `ProjectApplyPolicy/Coordinator/Worker/Wire/Elevation/Journal`, `WindowsProjectApplyObservation`; `Models/ProjectApply/AdapterInfo`; `ViewModels/ProjectApplyViewModel/AdapterViewModel`; `MainWindow`, `Views/ProjectApplyWindow/AdapterListPanel`, `Strings/En.xaml`; three `test/LinkLocal*.cs` files; isolated WPF/observer harnesses; canonical planning/spec/security/testing/handoff documentation.

Protected-path comparison against base passed for App startup/tray, MainViewModel, legacy elevation, profile/history repositories/model, project store/JSON/model, scanner/Ping code, schema/example, MIT license and workflow. NetworkService mutation/diagnostics text from `ApplyStaticIp` onward was independently compared with base and is identical. Only its adapter projection was changed. Branding, internal identities/paths, English support and existing UI primitives retained.

## Exact checks and results

Windows 10.0.26300.0 x64; local SDK 8.0.427; .NET 8/WPF target and dependencies unchanged. Variables:

```powershell
$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"
$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"
```

| Command/check | Actual outcome |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase05-final.trx'` | Final run PASS **463**, 0 failed/skipped; 64 added cases. Does not close intermittent P05-R01 below |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --filter 'FullyQualifiedName!~LinkLocal' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase05-existing-regression.trx'` | PASS **399** existing cases, 0 failed/skipped; diagnostic comparison only, not a reduced final gate |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --filter 'FullyQualifiedName~ProjectJsonTests' --logger 'console;verbosity=minimal'` | Diagnostic PASS **53**, 0 failed/skipped after P05-R01 |
| `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/Phase05UiSmoke` | PASS **64** isolated captures, 0 binding errors: prior 40 plus 20 Link-Local states and 4 adapter-list captures |
| `& ./scripts/validate_apply_observation.ps1` | PASS parse/command allowlist and **11** fully shadowed OS-query fixtures; no native queries/probes/processes |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py` | PASS schema Draft 2020-12/formats/example/references, 9 phases/49 stable IDs/local links; **10** validator regressions |
| `git -c core.safecrlf=false diff --check`; protected paths / legacy method-tail comparison | PASS; no protected behavior/source changes described above |

Ignored local artifacts: `test/TestResults/phase05-final.trx`, `phase05-existing-regression.trx`, `phase05.trx` (failed run retained), `TestResults/Phase05UiSmoke/*.png`. No cases skipped. Build/test and smoke builds must run sequentially because they write the same WPF intermediate output.

Functional coverage: range endpoints/reserved blocks/canonical input, explicit no-gateway command and ordered DNS; disabled/missing/changed DAD and disconnected/changed NIC; preferred/tentative/duplicate/unknown observation; cancellation after address but before DNS, journal rejection, partial automatic-DNS recovery, DHCP lease/APIPA distinctions; manual snapshot persistence/recovery, missing/new strict fields, old journal reading without byte changes, current-state display and no browser. Existing Phase 04 expiry/replay/UAC/IPC/reentry/journal/failure fixtures remain in the full run.

WPF checks instantiate a plain WPF Application with explicit existing resources, never actual App startup/MainWindow/network monitor. They exercise actual address/DNS bindings, manual NIC selection, declined/final confirmation, conflict and recovery results, DHCP review, named inputs, forward focus to confirmation, fixed footer, frozen inputs and busy close/application-exit refusal. Adapter fixtures render disconnected manual/DHCP/unknown-policy rows without hiding addresses. Light/Dark, 760×760/620×520 tool sizes; representative final minimum Light, normal Dark and adapter-list PNGs visually inspected against DESIGN.md. Native Mica is composited; full desktop/backdrop/manual accessibility is not certified.

## Failures found and review dispositions

- Early fixtures accidentally used different random NIC identities in one recovery assertion; corrected fixture identity reuse. Fake IPAddressCollection needed an explicit enumerator. A stage-text assertion without WPF resources incorrectly expected resolved localized text; the component harness verifies the actual conflict message instead.
- Adapter harness initially had an ambiguous TextBlock type and a read-only selected-item property in a two-way binding; corrected fixture types. Parallel test/smoke compilation caused an intermediate DLL sharing failure; reran sequentially. No production workaround or test suppression.
- Visual review removed unused project metadata space from the Link-Local surface and retained selected/disconnected address visibility. A manual label now requires known manual origins.
- Failure review identified the expected DAD-failed-before-DNS partial state; added the narrow DHCP recovery transition and real journal/fake-worker/VM coverage. Static/unknown/managed/custom-route states remain blocked. Missing optional connection evidence now yields unknown DAD rather than breaking ordinary observation.
- **P05-R01 — OPEN, separate scope:** one full 463-case run failed `ProjectJsonTests.RejectsInvalidStructureOrSemantics("emptyStaticDns", "Structure")`: no expected exception; 462 passed/1 failed/0 skipped, command as final suite above but `LogFileName=phase05.trx`. Standalone 53 JSON cases, original 399-case comparison and subsequent full 463-case run passed. `ProjectJson.cs`, schema and this test are unchanged from base. Root cause and baseline attribution are **UNVERIFIED**; shared schema evaluation/concurrency is a hypothesis only. Do not call a later green run proof of resolution or change/serialize/suppress that existing gate here. Under AGENTS.md, newly surfaced out-of-phase bugs are logged, not silently fixed. Preserve failure TRX and review a separately scoped deterministic reproduction/repair before release.

## Focused review and residual risks

Self-review; no independent reviewer/subagent used. Applies the code-review, security-review, concurrency-review, reliability-review, design-system, visual-qa and accessibility-review workflows within this diff. No license/API assets copied and no new dependency.

| id | kind | status | Source / result |
|---|---|---|---|
| P05-ARCH | architecture/source | passed | Existing Phase 04 boundaries and primary Microsoft/RFC sources in contract; native DAD, transient action, approved DNS semantics |
| P05-CODE | code review | passed | Scoped diff/callers and 64 added cases; corrected partial recovery/display/optional-evidence issues above |
| P05-SEC | security | passed | Strict typed validation at worker, canonical /16/no gateway/DNS constraints, no new command/URL/path transport, missing DAD fails closed; native behavior unverified |
| P05-CONC | concurrency/reliability | passed | Existing lease/IPC/worker serialization retained; DAD before DNS, cancellation/busy exit/partial recovery/journal/identity fixtures |
| P05-UI | design/visual | passed | Existing Fluent/Mica/Card/FooterBar/semantic resources; 64 component captures and representative visual inspection |
| P05-A11Y | accessibility | inferred | Named native controls, forward-focus/frozen/footer checks; manual keyboard/AT/high contrast/zoom NOT RUN |
| P05-REG | regression stability | failed | P05-R01 remains open despite final 463-case PASS; no altered thresholds, skips or suppression |
| P05-GATE | release check | unavailable | **NO-SHIP**: owner review, P05-R01 disposition and isolated/native/deployment evidence pending |

Residual limits: native netsh/DAD/PowerShell/UAC/pipe and actual driver/packet behavior UNVERIFIED; DAD checks during assignment and cannot prove silent/offline/future conflicts absent. Policy/identity check-to-command races, external legacy instances, conservative address/route/DNS coverage, private journal growth and filesystem/hardware limitations remain as in Phase 04. Automatic APIPA/no-lease source is displayed but blocked for mutation; return to DHCP with no usable lease remains unverified. Manual source and DHCP/APIPA must not be conflated. Static automatic DNS is unsupported except the explicit, narrow recovery **to DHCP** described above. P05-R01 root cause remains unresolved.

NOT RUN: real NIC enumeration acceptance/assignment/recovery, active ARP/ICMP/TCP/scan, actual Windows DAD/netsh/observation/pipe/UAC, real browser/TLS, DHCP server, full App/tray/native dialogs, manual keyboard/AT/high contrast/zoom, power loss/clean-machine/deployment, CI or publication. Earlier migration-marker attribution uncertainty and legacy history/URL/storage limits remain historical and unchanged.

## Isolated Windows test plan — separate authorization required

Use a disposable Windows VM/local console, isolated NIC/switch, no production bridge, out-of-band access and a saved independent full pre-state. Record exact binary/commit, OS/build/driver, selected GUID/index/MAC, active/persistent IPv4/routes/DNS/registration/DAD and IPv6/unrelated NIC controls. Owner approval plus proven isolation are prerequisites; this plan authorizes no execution.

1. One known static IPv4 → suggested/manual Link-Local, old gateway → none, unchanged static DNS; independently observe /16, active/persistent manual origins, DAD preferred and expected DNS. Check IPv6/other NICs unchanged.
2. DHCP with usable lease/automatic DNS → explicit static DNS/Link-Local → restore original DHCP/DNS policy; accept a different usable lease. Separate Return to DHCP review/confirmation. No lease/APIPA must remain unverified.
3. A deliberately conflicting lab peer, tentative delay, disabled/unknown DAD and disconnect: no false success, no automatic retry/undo/browser. Capture packets to establish actual OS DAD/interface behavior; do not infer it from command exit or Ping. Confirm conflict before DNS replacement can restore original DHCP policy.
4. Preferred manual Link-Local → another supported state → explicit original manual Link-Local/DNS recovery; conflict during recovery must fail visibly. Unknown/extra addresses, default/custom routes, alternate DHCP and managed DNS block before mutation.
5. UAC denial, NIC rename/removal/replacement between review/UAC/commands, cancellation, worker disconnect, command/observation timeouts, denied pre/final journal and close/tray exit. Preserve original journal and actual state; no implied atomic rollback. Test native main-window button, selected/disconnected display, keyboard and accessibility separately.

## Deviations and proposed next phase

Native Windows DAD during assignment is the conflict mechanism; no separate preflight ARP engine or full RFC 3927 allocator/defender. JSON v1 is deliberately unchanged; Link-Local is transient. Conservative APIPA/unknown-state blocking remains explicit. Isolated 05-T04 execution/manual acceptance is incomplete; no phase marked complete.

Safe next action: owner reviews this packet/diff and open P05-R01, decides its separate investigation/repair scope and whether to authorize isolated Windows validation. Proposed next development phase is 06 — mDNS, **only after a new owner decision**. No automatic Phase 06, push or release.
