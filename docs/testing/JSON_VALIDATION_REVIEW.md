# JSON validation follow-up — REL-01 / ADR-036

2026-11-01 Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main. Bootstrap `1282fbcea2b3e88977b43373cc6ece53698e996c`; separate authorization `fc2589f`. Phase 07 remains AWAITING_REVIEW; this is a bounded repair, no phase acceptance or release authorization.

## Reproduction and cause

UX-R01 recorded invalid `dhcpIpv4` acceptance; P05-R01 recorded `emptyStaticDns`. Original evidence is retained. A standalone run of the original 53 JSON tests passed again, demonstrating why a later green full run alone was insufficient.

New tests build fixtures with System.Text.Json independently of ProjectJson's validation. Five sequential controls pass. Eight dedicated workers start together and perform 100 evaluations each, rotating valid input, DHCP-with-IPv4, empty static DNS and automatic/leave-unchanged DNS with nonempty servers. One case uses Parse only; another mixes Parse and Serialize (which calls Parse). No xUnit parallelism setting, collection or old test is changed.

Before the fix, three fresh test processes each yielded **5 PASS / 2 FAIL / 0 skipped**. Mixed/Parse-only failure counts were **155/153**, **153/139**, **159/173** out of 800 calls per case; messages include both original variants. These are repeatable stress reproductions, not a deterministic scheduler proof of every instruction interleaving. Exact summaries and TRX hashes: [evidence](evidence/json-race-investigation.txt).

The shared schema's evaluation cache is mutable. The installed NuGet manifest pins JsonSchema.Net 7.4.0 to upstream commit `a3c4e63e075f595e4039d6424a77765a6761f7d0`. In that [JsonSchema source](https://github.com/json-everything/json-everything/blob/a3c4e63e075f595e4039d6424a77765a6761f7d0/src/JsonSchema/JsonSchema.cs), Evaluate clears/rebuilds constraints for changed options. BuildConstraint publishes a cache entry before PopulateConstraint fills it; GetConstraint can reuse the cached child through Source. [SchemaConstraint.BuildEvaluation](https://github.com/json-everything/json-everything/blob/a3c4e63e075f595e4039d6424a77765a6761f7d0/src/JsonSchema/SchemaConstraint.cs) reads Source.Constraints. Overlap can therefore consume an incompletely built child and omit assertions. Lazy protects construction only. The exact instruction schedule of the historic failures was not captured, but both symptoms are reproduced through this shared evaluation boundary.

## Minimal correction and review

One private gate protects the whole Schema.Value.Evaluate call, including first initialization. JSON parsing, duplicate/version checks, model deserialization and semantic checks remain outside it. No IO, async wait, callbacks or network action is introduced while holding it; exceptions release the monitor. Evaluation options/format checks, schema bytes, JSON v1, URL rules, examples and legacy stores are unchanged. The tests still invoke validation concurrently; invalid documents must throw Structure and valid documents must succeed.

This serializes access to proven mutable shared state; it does not serialize the test runner or suppress a failure. Per-call schema parsing would avoid shared project-schema state at repeated construction cost; a dependency upgrade would require wider compatibility review. Neither is needed for this bounded repair. Large concurrent documents can queue behind evaluation; no throughput claim is made. Scoped code/concurrency self-review found no remaining defect in the change. No independent reviewer used.

## Commands and results

From repository root: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`; `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. All results under ignored `TestResults/JsonInvestigation`.

| Command | Result |
|---|---|
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/JsonInvestigation/ --filter FullyQualifiedName~ProjectJsonTests --logger 'trx;LogFileName=json-baseline.trx' --results-directory TestResults/JsonInvestigation` | Original baseline 53 PASS |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/JsonInvestigation/ --filter FullyQualifiedName~ProjectJsonConcurrencyTests --logger 'trx;LogFileName=json-repro-before-1.trx' --results-directory TestResults/JsonInvestigation` | Before correction 5 PASS/2 FAIL. Repeated with `--no-build` and before-2/before-3 filenames, same 5/2 result |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/JsonInvestigation/ --filter FullyQualifiedName~ProjectJson --logger 'trx;LogFileName=json-focused-after.trx' --results-directory TestResults/JsonInvestigation` | After correction 60 PASS/0 failed/0 skipped |
| Same new-test command with `--no-build` and `json-repro-after-1.trx`, `json-repro-after-2.trx`, `json-repro-after-3.trx` | Three fresh processes, each 7 PASS/0 failed/0 skipped |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build -p:OutputPath=bin/JsonInvestigation/ --logger 'trx;LogFileName=json-full-after.trx' --results-directory TestResults/JsonInvestigation` | 631 PASS/0 failed/0 skipped (624 existing + 7 added) |
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/JsonInvestigation/` | PASS, 0 warnings/errors |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py` | Working-tree FAIL missing pre-existing deleted CODEX_START.md; 9 PASS/1 FAIL tooling tests. File deletion preserved, no validator weakening |
| `git diff --check` | PASS |

Planning isolation control: exported tracked HEAD using `git archive --format=zip HEAD` into ignored `TestResults/JsonInvestigation/plan-snapshot`, overlaid current modified/new task files, retained tracked CODEX_START.md only in that snapshot. Ran `& $py -B TestResults/JsonInvestigation/plan-snapshot/scripts/validate_plan.py` and `& $py -B TestResults/JsonInvestigation/plan-snapshot/scripts/test_validate_plan.py`: PASS (9 phases/49 tasks/schema/formats/309 links and 10 tooling regressions). This verifies the task package independently of the pre-existing deletion; the actual working-tree gate still FAILS and is not reported as passing. No branch/worktree or validator change.

The first draft test used blocking Task.WaitAll and emitted xUnit1031; changed to async/await before the three retained pre-fix reproductions. Its earlier 149-failure exploratory result is retained separately in the evidence. No production/test disablement or failure threshold adjustment.

## Disposition and limits

REL-01 / UX-R01 / P05-R01: **FIXED_LOCALLY, regression evidence recorded, awaiting owner review**. This status relies on reproduced failure plus the narrow ownership correction, not subsequent green runs alone. Historical phase reports remain unchanged except for resolution links. No schema/format/support expansion or UI change. Native evidence is **NOT RUN**: no real NIC settings/enumeration acceptance, UAC, browser, probes, mDNS, DHCP or actual App startup. Full WPF captures were not repeated for this non-UI repair. CI and portable clean-machine evidence are separate deferred REL-03 work.

Next: REL-02 preparation/fake-boundary checks and confirmed lab/NIC/console/recovery facts before live-dependent execution. Phase 07 is still AWAITING_REVIEW; no push, release, Phase 08 or DHCP server.
