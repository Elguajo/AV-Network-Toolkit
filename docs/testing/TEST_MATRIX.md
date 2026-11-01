# Testing strategy and acceptance matrix

## Tray menu placement — UI-TRAY-01, 2026-11-01

[Review and exact commands](TRAY_PLACEMENT_REVIEW.md): 9 focused placement tests PASS, 6 Light/Dark WPF captures PASS/0 binding errors with actual HWND position and all three action callbacks checked. Full combined working-tree unit suite 723 PASS/0 failed/skipped, including concurrent adapter filtering. Physical mixed-DPI/Explorer/native taskbar acceptance UNVERIFIED; no networking or installed-app changes.

## A26 remediation — ADR-047, 2026-11-01

[Review](AUDIT_FIXES_REVIEW.md): .NET 10 Release build 0 warnings/errors; final full xUnit 687 PASS/0 failed/0 skipped; focused second run 92 PASS; 8 focused save-error/recovery-page + 128 workspace + 64 diagnostics WPF captures, 0 binding errors. Storage conflict/failed-write/corruption, failed migration retry, local typed consent/recovery/exit/final-persistence and shared DNS matching tested with temporary directories/fake OS boundaries. Old executor allowance tests retired with the executor; rejection controls replace them. Package and final planning evidence in the review/status. Native UAC/IPC/NIC/recovery/clean-client and new hosted install/runtime checks NOT RUN. Earlier red runs and DOC-R01/UX-R01/P05-R01 remain recorded; later green does not resolve their root causes.

## Ready-made network tests — ADR-041 / NT-01, 2026-11-01

Main, bootstrap `2ccf8cd668ab0e27ef98f93b4db87afaf6ec4c67`. [Exact commands, failures/corrections, source/route/TCP limits and actual captures](NETWORK_TEST_PRESETS_REVIEW.md). Final Release solution/harness builds PASS/0 warnings/errors; **681 full unit PASS / 0 failed / 0 skipped**, **50 added cases**, focused 97 PASS. **64 final focused + 128 existing WPF captures / 0 binding errors**, Light/Dark normal/minimum. Fake all NIC/route/DNS/ICMP/TCP boundaries; no native socket or App startup. Counters cover attempts rather than captured packets; continuous results bounded to 100, statistics cover whole run. [Retained TRX hashes](evidence/network-test-presets.txt) include initial instrumentation/scheduling failures and final passing runs. Protected legacy/network/worker/schema/storage source comparison PASS. Planning/tooling final result is recorded in STATUS.md. Owner/native acceptance remains pending; ADR-037 safe-only, no push/release/Phase 08.

## Phase 05 current local evidence — 2026-10-31

Main, base `48913b21b7a4ddf62a9f58663fbfcde79d630a29`, ADR-021/022. [Complete review packet](PHASE05_REVIEW.md) records changed paths, exact commands, artifacts, failures/corrections, focused reviews and separate isolated Windows plan. Implementation commit is pinned by the subsequent STATUS handoff. Local SDK/Python paths remain those below.

- Release solution build `--configuration Release --no-restore`: PASS, 0 warnings/errors.
- Full xUnit `--configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase05-final.trx'`: final PASS **463/0 failed/0 skipped**; **P05-R01 OPEN** from earlier 463-case run: 462 passed/1 failed, empty static DNS not rejected. Failed `test/TestResults/phase05.trx` retained. Later green is not resolution.
- Diagnostic existing suite `--filter 'FullyQualifiedName!~LinkLocal'`: PASS **399**; JSON-only `--filter 'FullyQualifiedName~ProjectJsonTests'`: PASS **53**. Neither filter replaces the final full run. JSON/schema/test unchanged; cause and baseline attribution UNVERIFIED, separate repair scope required under AGENTS.md.
- `scripts/ProjectUiSmoke` to `TestResults/Phase05UiSmoke`: PASS **64** Light/Dark normal/minimum component captures, 0 binding errors, actual bindings/focus/consent/busy close/exit and disconnected Link-Local display; fake all OS/DAD/process/elevation/browser boundaries.
- `& ./scripts/validate_apply_observation.ps1`: PASS parse/allowlist and **11** fully shadowed OS-query fixtures, no native calls.
- `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py`: PASS schema/formats/example/planning/local links and **10** validator regressions. Whitespace/protected paths/unchanged legacy mutation-method comparison: PASS.

No real NIC/probe/UAC/browser/DHCP/native observation/manual integration performed. Native Windows DAD/assignment/recovery, full App/tray/dialogs/accessibility/deployment/CI remain NOT RUN; release NO-SHIP and phase acceptance pending. Project JSON v1 and legacy formats unchanged; existing historical limits retained.

## Phase 04 current local evidence — 2026-10-31

Main, base abdd1d1c61dd4f0d6b39adfc24ee69ff46367cbc. Exact resulting implementation SHA is in STATUS.md / resolved with `git log -1 --format=%H -- app/Services/ProjectApplyCoordinator.cs`. Full [review packet](PHASE04_REVIEW.md) contains commands, supported contract, failures/corrections, risks and isolated manual plan. Phase 04 AWAITING_REVIEW under ADR-019, not completed.

- `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore`: PASS 0 warnings/errors.
- `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase04.trx'`: PASS 399/0 failed/0 skipped.
- `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/Phase04UiSmoke`: PASS 40 Light/Dark normal/minimum component captures, 0 bindings, manual NIC/consent and cancel/observe/exit guards; all network/elevation/process/browser boundaries fake.
- `& ./scripts/validate_apply_observation.ps1`: PASS parse/allowlist + 6 shadowed query fixtures, no real OS queries.
- `& $py -B scripts/validate_plan.py`, `& $py -B scripts/test_validate_plan.py`: PASS schema/formats/example/planning/links and 10 tooling regressions. `$sdk`/`$py` local paths recorded below and in review packet.
- `git diff --check`, staged whitespace and protected-path check against abdd1d1: PASS.

NOT RUN: actual adapter/netsh/UAC/IPC/browser/TLS integration, live traffic/apply/recovery, full app/tray/native dialogs/manual AT/high contrast/zoom, CI, publish/clean-machine and hardware/adversarial filesystem tests. No unit exclusions. Historical Phase 02/03 limitations remain; owner review, live-test, publication and Phase 05 gates are separate.

Automated tests must use fakes/mocks for OS network changes, DHCP packet output, UAC and default-browser launch. Real NIC/network operations are **manual isolated Windows tests only**.

| ID | Feature | Test cases | Evidence required |
|---|---|---|---|
| QT-01 | Existing profiles | Static IP/mask/gateway/DNS import/apply preserved; document legacy revert lacks DNS; new recovery requires sufficient snapshot | Unit + isolated Windows |
| QT-02 | DHCP | IP/DNS DHCP transitions, no stale previous policy | Unit + isolated Windows |
| QT-03 | JSON backward compatibility | Existing `profiles_<adapter-id>.json` unchanged | Fixture + regression |
| QT-04 | Project JSON | Save/open, duplicates, invalid/future schema, orphan refs, path permissions, atomicity | Unit |
| QT-05 | Profile mapping | Missing/similar names/USB NIC switch require explicit selection | Unit + manual UI |
| QT-06 | Web links | Deep path, `#!/main`, query, custom port, FQDN, HTTP warning | Unit + UI |
| QT-07 | URL security | Reject file/javascript/credential-bearing links | Unit |
| QT-08 | Apply & Open | UAC refused, NIC unplugged, partial netsh, verify mismatch, no auto-launch | Unit |
| QT-09 | Link-Local | Address constraints, conflicts, restore/DHCP | Unit + isolated Windows |
| QT-10 | mDNS | Packet fixtures, TTL expiration, wrong interface, malformed input | Unit + isolated Windows |
| QT-11 | Hyper-V | Physical/virtual NIC identification, UI-only hiding | Unit + Windows UI |
| QT-12 | DHCP server | Existing DHCP, pool bounds, firewall/lease cleanup, restart recovery | Unit + isolated packet capture |
| QT-13 | Existing scan/tools | ARP/ICMP/TCP, ping, history and tray still operate | Unit + manual regression |
| QT-14 | Deployment | Signed/unsigned notices, packaged exe dependencies, clean machine launch | Windows build + manual |

## Required test evidence record

- Exact command, current commit, OS/SDK, environment, exit status, failures, skipped tests, log/artifact path.
- `dotnet test NetworkProfileManager.Tests/NetworkProfileManager.Tests.csproj` is an **upstream documented example**, not proof it ran here.
- Build and UI tests must be carried out in an appropriate Windows environment; if unavailable, mark `NOT RUN` and do not approve release.
- Manual testing on existing AV production networks is **not authorized by this plan**.

## Phase gates

- Phase 01: baseline results and known skips documented.
- Each feature phase: own unit tests + regression as relevant; no uncontrolled OS mutation.
- Phase 09: complete matrix, isolated Windows test evidence, release approval.

## Phase 01 execution evidence — 2026-10-31

Baseline commit: `603cf24fa0ba1819c0ec10b6a57ef5cf57c3852e`; audit on `main`, adoption on `codex/phase-01-baseline-audit`; Windows `10.0.26300.0`, x64. Results are from this session chain, not imported CI claims. Console evidence is retained in this table; no build artifact or on-disk execution log was produced.

| Exact command / check | Result and limit |
|---|---|
| `git rev-parse HEAD`, `git remote -v`, `git branch --show-current`, `git log -5 --oneline` | PASS: provenance recorded in baseline/source map; no fetch/checkout of remote source |
| `dotnet --info` | Exit 0: runtime 10.0.9, no SDK; reconfirmed during adoption |
| `dotnet build NetworkProfileManager.sln --configuration Release --no-restore` | Exit 1: `No .NET SDKs were found`; compilation did not start; no dependency install/restore |
| `python -B scripts/validate_plan.py` during original audit | FAIL, exit 1: `broken link: README.md -> ../../actions/workflows/build.yml`; GitHub-relative link misclassified as local |
| `python -B -`, original audit stdin check | PASS, exit 0: 172 unique matching En/Tr XML resource keys; two csproj + manifest XML parse; three planning JSON files parse. Not compilation or full schema validation |
| `python -B -c "import importlib.util; print('jsonschema available:', importlib.util.find_spec('jsonschema') is not None)"` | Exit 0: `jsonschema available: False`; full JSON Schema validation NOT RUN, no dependency installed |
| `git diff --check`, `git diff --exit-code`, `git diff --cached --exit-code` at audit end | PASS, exit 0: no tracked/staged changes during investigation; untracked planning overlay already present |
| xUnit / WPF UI / publish / real UAC / scanner / isolated NIC integration | NOT RUN. SDK unavailable; no authorization for live operations. README's 188 tests/coverage and Windows support remain UNVERIFIED |

The imported validator also hardcodes `plan.status == NOT_STARTED`. Re-running `python -B scripts/validate_plan.py` during adoption returned exit 1 on that line-13 assertion before reaching its link bug. 01-T08 must fix both in a separate tooling change. This remains a failing package validator.

Adoption check below was executed via `python -B -` with the fenced Python supplied on stdin: PASS, exit 0; 9 phases, 49 stable tasks, machine/backlog/phase agreement, 49 local links, JSON parse/example references, 172 matching language keys and project XML parse. This confirms documentation consistency only; full schema validation and application tests remain NOT RUN.

Staged-scope verification: `git diff --cached --check` PASS; a separate read-only `python -B -` stdin check compared `git diff --cached --name-only` against the Markdown/draft-JSON allowlist and resolved local Markdown targets against `git ls-files`. PASS, exit 0: 36 staged files, all local links in index, no staged or unstaged edits to original application/tests/workflows/README/LICENSE. This initial versioning includes existing supporting documents and unchanged draft schema/example; the imported validator and archive manifest remain untracked.

## Safe execution checkpoint — required before Phase 02 implementation

Current update (ADR-008/009): preparation is authorized and verified; earlier pending-authorization statements below are historical. Use the maintained `scripts/validate_plan.py` with `scripts/requirements-validation.txt`, rather than the historical adoption-only snippet after plan state changes.

- 01-T06: separately authorize SDK/dependency preparation; record SDK 8 version, restore/build commands and results. This documentation adoption installs nothing.
- 01-T07: audit/fake probe and adapter boundaries before selecting an isolated test command. Existing `IpScannerServiceTests` emits actual probes to `192.0.2.x`; documentation ranges do not prevent traffic. `MainViewModelTests` constructs a VM that enumerates real NICs. Enumeration is read-only but environment-dependent; distinguish it from active probing and mutation.
- Separate integration tests from unit tests, retain their cases, and list every excluded case and reason as NOT RUN. Do not silently disable tests or claim the full suite passed from a filtered run. Record the exact filter only after source review; no currently certified safe filter is supplied here.
- Apply/elevation/browser tests use fakes and temp data. Actual NIC changes, UAC, DHCP, discovery and browser integration require their own isolated/manual approval. Do not run the workflow's unfiltered test command merely because CI already contains it.
- 01-T08 owns validator repair. The following adoption-only stdin check verifies plan consistency; it is not a replacement for that repair, full schema validation or runtime acceptance.

## Documentation adoption consistency check

Run from repository root: `python -B -`, supplying the following Python on stdin. It only reads files; no application, probes or OS adapter APIs are used.

```python
# adoption-consistency-check
from pathlib import Path
import json, re, xml.etree.ElementTree as ET

root = Path.cwd()
plan = json.loads((root / 'machine/plan.json').read_text(encoding='utf-8'))
backlog = (root / 'TASKS.md').read_text(encoding='utf-8')
assert plan['status'] == 'AUDIT_RECORDED_AWAITING_REVIEW'
assert plan['activePhase'] == '01' and plan['phase02Authorized'] is False
assert len(plan['phases']) == 9
assert '[x]' not in backlog
all_ids = []
original_counts = [5, 5, 5, 5, 4, 4, 4, 5, 5]
for number, phase in enumerate(plan['phases'], 1):
    assert phase['id'] == f'{number:02d}'
    text = (root / phase['file']).read_text(encoding='utf-8')
    section = re.search(rf'## Phase {number:02d}:.*?(?=\n## Phase |\Z)', backlog, re.S).group()
    expected = re.findall(r'- \[ \] \*\*([^*]+)\*\* — (.*?)\. \*\*AC:\*\* (.*?)(?:\n|$)', section)
    assert len(expected) == len(phase['tasks'])
    assert len(re.findall(r'- \[ \] \*\*\d{2}-T\d{2}\*\*', text)) == len(expected)
    for task, (task_id, title, acceptance) in zip(phase['tasks'], expected):
        assert (task['id'], task['title'], task['acceptance']) == (task_id, title, acceptance.rstrip('.'))
        assert f'**{task_id}** — {title}.' in text
        assert f'**Acceptance:** {acceptance}' in text
        state = ('AWAITING_REVIEW' if task_id == '01-T03' else
                 'EVIDENCE_RECORDED' if task_id in ['01-T01', '01-T02', '01-T04', '01-T05'] else 'NOT_STARTED')
        assert task['status'] == state
        all_ids.append(task_id)
    assert phase['status'] == ('AUDIT_RECORDED_AWAITING_REVIEW' if number == 1 else 'NOT_STARTED')
    assert {f'{number:02d}-T{i:02d}' for i in range(1, original_counts[number-1]+1)} <= {t['id'] for t in phase['tasks']}
assert len(all_ids) == len(set(all_ids)) == 49
assert set(plan['preparationCheckpoint']) <= set(all_ids)
schema = json.loads((root / 'schemas/project.avnet.schema.json').read_text(encoding='utf-8'))
example = json.loads((root / 'examples/project.avnet.json').read_text(encoding='utf-8'))
assert schema['properties']['schemaVersion']['const'] == example['schemaVersion'] == 1
profile_ids = [p['id'] for p in example['profiles']]
assert len(profile_ids) == len(set(profile_ids))
assert all(d.get('profileId') is None or d['profileId'] in profile_ids for d in example['devices'])
assert all(d['webUrl'].startswith(('http://', 'https://')) for d in example['devices'])
files = [root/p for p in ['AGENTS.md', 'PROJECT_CONTEXT.md', 'ROADMAP.md', 'STATUS.md', 'TASKS.md', 'CODEX_START.md', 'RELEASE_CHECKLIST.md', '00_READ_FIRST_RU.md', 'README FOR AGENTS.md']]
for folder in ['decisions', 'docs/architecture', 'docs/legal', 'docs/security', 'docs/spec', 'docs/testing', 'phases', 'prompts', 'templates']:
    files.extend((root/folder).glob('*.md'))
links = 0
for path in files:
    text = re.sub(r'```.*?```', '', path.read_text(encoding='utf-8'), flags=re.S)
    for target in re.findall(r'\]\(([^)]+)\)', text):
        if target.startswith(('https://', 'http://', '#', 'mailto:')):
            continue
        assert (path.parent / target.split('#', 1)[0]).resolve().exists(), (path, target)
        links += 1
xkey = '{http://schemas.microsoft.com/winfx/2006/xaml}Key'
keys = []
for language in ['En', 'Tr']:
    values = [n.attrib[xkey] for n in ET.parse(root/f'NetworkProfileManager/Strings/{language}.xaml').iter() if xkey in n.attrib]
    assert len(values) == len(set(values))
    keys.append(set(values))
assert keys[0] == keys[1]
for path in ['NetworkProfileManager/NetworkProfileManager.csproj', 'NetworkProfileManager.Tests/NetworkProfileManager.Tests.csproj', 'NetworkProfileManager/app.manifest']:
    ET.parse(root/path)
print(f'PASS: 9 phases, {len(all_ids)} stable tasks, machine/backlog/phase agreement; {links} local links; JSON parse/example references; {len(keys[0])} matching language keys; project XML parse')
```

## Preparation checkpoint execution — 2026-10-31 (01-T06–01-T08)

Branch `codex/phase-02-project-json`, starting HEAD `5e6231f731a532fd0947903e4dbaf47115f0d2e3`. Resolve preparation commit with `git log --format=%H --grep="isolate baseline tests" -1`.
Windows 10.0.26300 x64; local SDK 8.0.427 / runtime 8.0.33. Installed without elevation using Microsoft's `dotnet-install.ps1 -Channel 8.0 -InstallDir "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet" -NoPath`. SDK first-run generated an untrusted development certificate; no trust command executed. No persistent PATH/global git configuration changed.

For commands below, PowerShell `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"` and `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`.

| Exact command | Outcome |
|---|---|
| `dotnet --info` before install | Runtime 10.0.9 only; no SDK |
| `& $sdk --info` | SDK 8.0.427, runtime/Desktop 8.0.33 |
| `& $sdk restore NetworkProfileManager.sln` | PASS, both projects restored |
| `& $sdk build NetworkProfileManager.sln --configuration Release --no-restore` | Initial preparation attempt FAIL: new scanner assertion selected obsolete async xUnit overload. Corrected to assert synchronous validation. Repeat PASS, 0 warnings/errors |
| `& $sdk test NetworkProfileManager.Tests/NetworkProfileManager.Tests.csproj --configuration Release --no-build --filter 'FullyQualifiedName~IpScannerServiceTests|FullyQualifiedName~MainViewModelTests' --logger 'console;verbosity=minimal'` | PASS: 5 tests, 0 failed/skipped |
| `& $sdk test NetworkProfileManager.Tests/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=preparation.trx'` | PASS: 189 tests, 0 failed/skipped, 2 s. Local ignored artifact `NetworkProfileManager.Tests/TestResults/preparation.trx` |
| `python -m venv "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python"`; `& $py -m pip install 'jsonschema[format]==4.26.0'` | PASS; tooling dependency local to venv |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py` | PASS: 9 phases, 49 IDs, full draft-2020-12 schema/example with FormatChecker; 10 regression tests. Initial link failures exposed Releases/Discussions/security-advisory GitHub routes; handled only for exact known source+route pairs. Ordinary broken links still fail |

Test boundary source review: all test files inspected; no tests invoke valid OS mutation commands. NetworkService tests exercise early validation/no-op only; worker tests contain rejected/empty commands before process launch. Scanner tests replace the complete host probe and bypass local-subnet enumeration, exercise actual scan ordering/progress/cancellation. MainViewModel tests inject a fixture adapter provider; the default production constructors retain real behavior. Clipboard writes use delegates; persistence tests use temp AppPaths fixtures (AppPaths default-path tests may create the data directory, but do not alter its contents). Language/theme tests do not launch an application. No test cases excluded from this isolated unit run.

NOT RUN: original real-probe scanner behavior, real-NIC enumeration acceptance, WPF UI, live ping/scans, browser, UAC, actual apply/revert, publish/clean-machine and isolated Windows integration. Unit success does not certify these. No CI execution or push in this checkpoint. Original archive MANIFEST.sha256 preserved untracked, not an integrity contract.

## Phase 02 final local evidence — 2026-10-31

Final implementation commit: `0bf1070216255c9e1411a4a96445372b6e6a45d1`. Complete [review packet](PHASE02_REVIEW.md) records exact commands, artifacts, failures/corrections, limitations and specialist review decisions. Phase 02 AWAITING_REVIEW; not a release approval.

- `& $sdk build NetworkProfileManager.sln --configuration Release --no-restore`: PASS, 0 warnings/errors.
- `& $sdk test NetworkProfileManager.Tests/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase02-english.trx'`: PASS 286/0 failed/0 skipped.
- `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/ProjectUiSmoke`: PASS 4 English component renders, bindings/control state and draft guards; no NIC/OS-network calls in corrected harness.
- `& $py -B scripts/validate_plan.py` and `& $py -B scripts/test_validate_plan.py`: PASS full draft-2020-12+format/example/planning validation and 10 tooling regression tests.
- Exact SDK/Python variable paths are in the preparation section and review packet. Current CI/publish/native-dialog/full-app/live-network certification NOT RUN.

Earlier 278-test/En+Tr results precede owner ADR-011; final UI is English-only. Legacy `tr` and System settings are interpreted as English without altering saved bytes on initialization. The early failed harness reached real startup/migration before resources failed; `.migrated_v1` was observed afterward (prior existence UNVERIFIED), preserved; no network mutation/probe was reached. See review packet for full incident and corrected proof boundary.

## Repository path refinement — ADR-013, 2026-10-31

Branch `codex/simplify-project-paths`; starting anchor `4cddfcd1dad4be867d499e74a3d5213929bda7f8`. Resolve the containing refactor commit via `git log -1 --format=%H -- AVNetworkToolkit.sln` after commit. SDK 8.0.427, Windows 10.0.26300 x64; `$sdk`/`$py` use the local tool paths recorded above.

| Exact command / check | Result |
|---|---|
| `& $sdk restore AVNetworkToolkit.sln` | PASS, application/test projects restored using new paths |
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=path-rename.trx'` | PASS, 286 tests, 0 failed/skipped; `test/TestResults/path-rename.trx` local ignored evidence |
| `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/ProjectUiSmoke` | PASS, 4 isolated English component renders, 0 binding errors, retained command/selection/draft guards |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py` | PASS: full schema+format/example/planning/local-link checks and 10 tooling regression tests |
| Read-only `python -B -` comparison of `git ls-tree -r`/`git show 4cddfcd:<old-path>` against moved files (normalized line endings) | PASS: 59 application files content-identical; 25 test files preserved except expected project reference and two source-root lookup edits; no old source directories/solution remain |
| Source inspection of solution/harness/project/CI references; `git diff --check` | PASS: new paths resolve; workflow command edits only; unchanged namespaces, assembly/project filenames and AppData paths |

Historical commands in earlier evidence sections intentionally retain the filenames used when they ran. Current navigation links point to moved files. Updated current README/CONTRIBUTING instructions and fixed their stale bilingual resource path to match the already-approved English-only UI. No tests were added/skipped solely for this mechanical rename. CI/release packaging and native/live-network tests NOT RUN in this follow-up; no push/tag/release or OS network operation performed.


## Phase 03 Device Web Interfaces — 2026-10-31

Branch `codex/phase-03-device-web-interfaces`, starting HEAD `17c7014ba585fe85680fcdede51ce967f238054d`. Implementation SHA: `e861c3a426f24e0103e9cc26b7e82b6e2f1e503f`. Complete [review packet](PHASE03_REVIEW.md) has changed paths, exact scope, source evidence, initial failures/corrections, risks and owner gate. Phase 03 AWAITING_REVIEW, not complete. `$sdk`/`$py` use the local tool paths recorded above.

| Exact command/check | Result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase03.trx'` | PASS, 308 tests, 0 failed/skipped; 22 new device cases |
| `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -- TestResults/Phase03UiSmoke` | PASS, 20 Light/Dark normal/minimum project/device captures, 0 binding errors; real selected-row identity, 24 linked devices, filtering/grouping/draft guards, fake launch/cancel, named controls, footer bounds and programmatic forward focus |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py` | PASS, full schema+formats/example/planning/107 links and 10 validator regressions |
| `git diff --check` / staged whitespace / protected-path diff against 17c7014 | PASS; schema/example/license/workflow and legacy network/UAC/history/storage/App/tray/MainWindow unchanged |

Ignored local artifacts: `test/TestResults/phase03.trx`, `TestResults/Phase03UiSmoke/*.png`. Existing DESIGN.md styling verified from representative empty/selected/error/lower-form renders in both themes and sizes. All 20 captures generated; actual native Mica/backdrop not certified. Programmatic traversal is not manual keyboard or screen-reader acceptance.

NOT RUN: actual browser handler/navigation/endpoint/TLS, network apply/revert/scan/DHCP/UAC, real adapter enumeration acceptance, native dialogs/full app/tray, manual keyboard/AT/high contrast/zoom, publish/clean machine/hardware failure and CI. No unit exclusions or live network settings/probes. Existing Phase 02/manual and baseline risks remain; no new release/support guarantee.

## Phase 06 mDNS — 2026-10-31

Main, clean starting `f160ac0986af59c7cc4357021b8ca7fe942b63e0`; implementation SHA resolved through `app/Services/MdnsDiscoverySession.cs` history and pinned in STATUS handoff. [Full review packet](PHASE06_REVIEW.md) records exact commands, changed paths, 79 added negative/lifecycle/native-boundary/cache/export fixtures, initial failures/corrections and remaining limits. AWAITING_REVIEW; no phase acceptance.

| Exact command/check | Result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/Phase06/` | PASS, 0 warnings/errors; separate output avoids an existing running EXE lock |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build -p:OutputPath=bin/Phase06/ --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=phase06-final-reviewed.trx'` | PASS 542/0 failed/0 skipped; 79 new mDNS cases, fake network/NIC/socket/clock |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/Phase06/` then `& $sdk scripts/ProjectUiSmoke/bin/Phase06/ProjectUiSmoke.dll TestResults/Phase06UiSmokeReviewed` | PASS, 0 warnings/errors; 84 Light/Dark normal/minimum captures/0 binding errors, 20 mDNS states, fake boundaries and actual WPF bindings/focus/footer/close |
| `& ./scripts/validate_apply_observation.ps1` | PASS 11 fake OS-query fixtures + parse/allowlist, no real OS calls |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py` | PASS full schema/formats/planning/local links and 10 tooling regressions |
| `git diff --check`; protected-source comparison against f160ac0 | PASS; JSON/schema/example/apply/legacy/license/workflow unchanged |

**P05-R01 remains OPEN**, root cause/baseline attribution UNVERIFIED; subsequent 542 PASS does not repair or close earlier empty-static-DNS rejection failure. Native IFLIST/membership/metadata/shared-port/IGMP and isolated network acceptance NOT RUN. No actual socket, probes, NIC mutation, UAC, browser, DHCP or full App/tray launch. Native dialogs/manual AT/high contrast/zoom/clean-machine/deployment/CI NOT RUN; no push. New ADR-024/IDEA-01–03 documentation is separate concurrent work, not mDNS functionality.

## Workflow UX follow-up — 2026-11-01

Main baseline ef2e4773a36fa2d7feb941655e07427fcf7ae108; [exact commands, findings, captures and limits](UX_WORKFLOW_REVIEW.md). Release solution and WPF harness builds PASS 0 warnings/errors. Final unit regression **FAIL: 564 passed / 1 failed / 0 skipped, total 565**; thirteen added save/validation cases pass. Failure: unchanged ProjectJsonTests dhcpIpv4 should reject Structure but did not throw; [excerpt/hash](evidence/ux-json-failure.txt), related-to-P05-R01 attribution UNVERIFIED. No green retry or gate suppression.

Focused safe WPF walkthrough PASS (12 captures); full Light/Dark/normal/minimum suite PASS (112 captures), 0 binding errors. Actual Add/Keep, invalid/corrected field focus, declined close, save picker/repeat path, no adapters/disconnected IP, fixed target, loading, fake UAC denial/partial failure and visible recovery are exercised. No live NIC/probe/socket/elevation/browser/native-dialog/App/tray operation. Owner tabs feedback positive; overall task success, manual AT/high contrast/scaling and native/isolated-network acceptance NOT RUN. Phase 06 AWAITING_REVIEW; release NO-SHIP.

## Compact marked-block correction — ADR-030, 2026-11-01

Base main `4af148dd47abc56962ac623e35a99d94fdae8d06`; [reference, actual captures and exact commands](COMPACT_WORKSPACE_REVIEW.md). Release harness/solution builds PASS 0 warnings/errors; 17 focused ProjectViewModel cases PASS/0 failed/0 skipped. Full safe WPF suite PASS 112 captures plus focused 12 captures, 0 binding errors: compact project bar, visible initial local list, named source-selector round trip preserving both stores, source→Import→Export→Add keyboard order, overflow/shared-project menu and previous workflow gates. Light/Dark normal/minimum inspected. Newest target supersedes second visible tab row; no storage/behavior migration.

Full unit suite NOT RUN again for this layout-only follow-up; latest full result **564 PASS/1 FAIL/0 skipped** and UX-R01 / OPEN P05-R01 stay release-blocking. Native App/tray/dialog/AT/high contrast/scaling/NIC/UAC/browser NOT RUN. Owner compact-layout/source/menu acceptance pending; no phase completion, push or release.

## Reference buttons and current-state Selected — ADR-031/032, 2026-11-01

Base main `f7346833c7f8b21af4a73741a257852287679cdf`; [commands/captures/display semantics](PROFILE_VISUAL_STATES_REVIEW.md). Release solution/harness builds PASS 0 warnings/errors; 45 targeted cases PASS, then final full **576 PASS/0 failed/0 skipped**, including 11 new display-match cases. Initial resource-key convention failures corrected in production names; existing StringResourceTests unchanged. **140 WPF captures/0 binding errors**: 128 full Light/Dark normal/minimum including toolbar/focus/disabled/current-row closeups, 12 focused safe workflow captures. Existing-window palette updates/system-brush mapping, five-slot limit/re-enable, keyboard order, observed mismatch/restore, editor-selection independence and explicit reapply verified through fake boundaries.

OPEN UX-R01/P05-R01 remains unresolved/release-blocking; this green run does not diagnose earlier unsafe JSON acceptance. Current display cannot confirm DHCP/automatic-DNS source, which stays Apply. Native hover/press/full App/tray/dialog/AT/high contrast/scaling/NIC/UAC/browser NOT RUN. No network mutation, phase acceptance/advance, push or release.

## Phase 07 diagnostics and explicit-source Ping — ADR-034, 2026-11-01

Base main `db143aba6e4405da57acf2a85f661f3b579c279d`; [exact commands, task mapping, captures, initial corrections and limits](PHASE07_REVIEW.md). Release solution/harness builds PASS 0 warnings/errors. Focused 51 PASS (48 new + 3 resources); final full **624 PASS/0 failed/0 skipped** (`TestResults/Phase07/phase07-final-reviewed.trx`). New query parser/command allowlist and 15 fake OS fixtures PASS under PowerShell 7 and Windows PowerShell 5.1; existing 11 fake apply-observer fixtures PASS. Planning/schema and 10 validator regressions, whitespace and protected-source comparisons PASS.

32 focused inline/window WPF captures (Light/Dark normal/minimum) and 128 existing project/device/apply/link-local/mDNS/main captures PASS, 0 binding errors; timing of the separate batches is documented in the review. Source/adapter input and command state, busy/Stop/close, immutable request ownership, route conflict/no probe, facts/routes, error/empty, scrolling, fixed Close and basic programmatic focus verified. Runtime facts, resolver, routing and ICMP are fake; no real adapters, OS network commands, packets, UAC, browser or DHCP used in tests.

Phase 07 AWAITING_REVIEW, checkboxes open. Native NIC/PowerShell/Hyper-V/USB/route/ICMP/TTL/actual-egress acceptance, real unplug/policy/privilege/driver behavior, full App/tray/native dialogs/manual AT/high contrast/scaling/deployment/CI NOT RUN. Source plus predicted route does not prove egress. OPEN UX-R01/P05-R01 remains unresolved/release-blocking despite green results; no related repair/suppression. Release **NO-SHIP**; no push or Phase 08 start.

## Bootstrap document contract and safe review — ADR-038, 2026-11-01

Owner confirmed retiring the redundant CODEX_START.md. AGENTS.md remains the bootstrap authority. The offline validator requires AGENTS.md, PROJECT_CONTEXT.md, ROADMAP.md, STATUS.md, TASKS.md, prompts/CODEX_BOOTSTRAP.md, prompts/CODEX_RESUME.md, templates/SESSION_HANDOFF.md, RELEASE_CHECKLIST.md, docs/security/THREAT_MODEL.md and this TEST_MATRIX.md. It also retains all existing 49 stable task/state/checkbox, schema+format/example/reference and link checks. Scripts mirror this project contract; no temporary exception/suppression or runtime gate relaxation. Contract owner: repository owner; approved permanent retirement ADR-038, no exception expiry needed. Future removal of required documents or weakening of assertions requires a recorded owner decision and meaningful negative coverage.

From repository root with existing `$py`/`$sdk` paths in earlier sections:

- Baseline `& $py -B scripts/validate_plan.py` FAIL missing CODEX_START.md; `& $py -B scripts/test_validate_plan.py` 9 PASS/1 FAIL for that same missing file.
- After approved retirement/successor enforcement, those exact commands PASS on the actual working tree; **12 tooling tests PASS**, including retired-file absence, missing mandatory bootstrap/handoff files, invalid state/authorization/task/completion, broken links and schema formats. No archive stand-in is needed now.
- `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/JsonInvestigation/ --filter FullyQualifiedName~ProjectJson --logger 'trx;LogFileName=json-safe-review.trx' --results-directory TestResults/JsonInvestigation`: **60 PASS/0 failed/0 skipped**, compile 0 warnings/errors. Scoped self-review of 8f9106c found no new actionable defect. Recomputed SHA-256 for all ten retained TRXs listed in docs/testing/evidence/json-race-investigation.txt: **10/10 match**.
- `git diff --check` PASS. Production app/test sources, schema/examples, LICENSE and workflows unchanged in this follow-up. Previous 631 full unit PASS and Release build evidence retained; not rerun for docs/tooling-only changes. Real NIC/UAC/browser/probes/mDNS/DHCP/native acceptance NOT RUN. Phase 07 AWAITING_REVIEW; no new push/release permission or deferred REL-03/04 start.

## Published branded v1.1.1 Preview — ADR-045, 2026-11-01

Source/tag: `3fd7e77e1f5b560172b7e23d33928bace9cdaa73` / v1.1.1. Successful release workflow: https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41273489475. First successful pre-branding run 41272961614 retained separately. Root global.json selects SDK 8.0.427/latestPatch; final Release build 0 warnings/0 errors, 681 unit PASS/0 failed/0 skipped and 12 Python tooling tests PASS. Initial newer-SDK CS9264 warnings were resolved through correct SDK selection, no suppression/source workaround.

Hosted Windows Server package checks actually PASS: framework-dependent control fails when global runtime discovery is unavailable; both self-contained packages load/render JSON+WPF and open their ordinary expected main window; installed payload hashes match ZIP; pinned compiler integrity/rejections pass; locked-target update/uninstall reject; same-version reinstall/uninstall preserve fixture data; runtime roots/environment restored. CI artifact runner-verification/result.json inspected and retained in docs/testing/evidence/published-v1.1.1.json. No Apply/Run/Open command invoked; native network tests remain NOT RUN.

Independent post-publication verification: `gh release download v1.1.1 --repo Elguajo/AV-Network-Toolkit --dir out/Published-v1.1.1`; all four GitHub asset API digests match downloads, installer/ZIP match manifest and SHA256SUMS, tag resolves to release source. ZIP 65071824 bytes SHA256 780B0C41E90EAECE141AB545505636974E117F15B542036BD959BB528EE89A6E; installer 64609647 bytes SHA256 905758C3988A22F3A62CEDD2A98EA47CB533F1814988DCA272E63F4287DB98CB. Published ZIP extracted; explicit test_package_self_test.ps1 positive/create-only/3 invalid-argument tests PASS, reports runtime .NET 8.0.33/source/version. Extracted EXE icon 32x32 pixel-equal to approved ICO; packaged MIT LICENSE SHA matches repository. Downloaded actual Actions artifact and compared both package hashes to published downloads: PASS.

Publication is owner-requested Preview; native/phase acceptance and a pristine Windows-client/cross-version upgrade remain unverified. No product installation/elevation/runtime-root moves or network changes occurred on the owner's PC. DATA-01 .av-network-toolkit migration not included. Exact workflow/package contracts and remaining limits are in PACKAGING_REVIEW and release notes; source tag is not moved for outcome-only metadata.
