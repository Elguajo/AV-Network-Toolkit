# Testing strategy and acceptance matrix

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
