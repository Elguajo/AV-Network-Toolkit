# A26 remediation review — local evidence

2026-11-01, https://github.com/Elguajo/AV-Network-Toolkit, main. Implementation started at `bfc978c6305a2bd5d2d09424d29d7fad4e0a4b1c`; concurrent documentation-only `684a0fe` screenshot gallery preserved. Resolve the implementation commit with `git log -1 --format=%H -- app/Services/LocalProfileApplyService.cs`. Phase 07 remains AWAITING_REVIEW. [Contract and compatibility matrix](../spec/AUDIT_FIXES_CONTRACT.md); ADR-047. Historical [audit](REPOSITORY_AUDIT_2026-11-01.md) and its red evidence remain unchanged except a follow-up link.

## Findings addressed

| Finding | Implemented correction | Verification |
|---|---|---|
| A26-01 | Preserve unreadable originals/backups, strict bounded reads, flushed atomic array replacement/unique backup, conflict detection | Corrupt input, backup-path directory, injected replace failure, stale writer and exact backup tests |
| A26-02 | Independent strict journal parsing, per-page unreadable count, bounded next-page UI and exact operation lookup | Healthy + corrupt journal; 1001 entries across pages; safe WPF stale-selection/next-page exercise |
| A26-03 | Local/manual/hotkey/tray and Revert use typed review, durable original snapshot, identity checks and observed verification; original DNS recovered through journal pointer | Decline, unsupported sources, partial failure, exact DNS recovery, old-history warning, wrong NIC, no confirmation, cancel/exit, final-journal/history failure tests |
| A26-04 | All product mutation callers use authenticated typed worker and absolute system netsh path; generic file worker removed and old switch rejects before normal startup | Caller search, old-worker unit tests, typed boundary suite; packaged switch rejection added to reusable self-test |
| A26-05 | Persist candidate before changing UI; retain Add/Edit draft on failure; import/delete also use save-before-collection order | Failed Add/Edit, external-refresh retry tests, actual WPF dialog remains open with error and reachable Save |
| A26-06 | Flush/create-only copied destination and marker only after successful copies; retry transient failure | Locked source then successful retry, source retained and idempotence |
| A26-07 | Shared main/tray matching including secondary DNS and unknown evidence | Secondary mismatch/match/missing evidence and existing visual-state tests |
| A26-08 | .NET 10 LTS across app/tests/render/runtime control/CI/publish, SDK pinned | SDK 10.0.403 / runtime 10.0.14 observed, build/full tests/WPF and dependency audit; package evidence follows below |

The full count differs from historical 681 because tests for the removed generic executor were replaced by rejection/typed-contract regressions. No filters/skips in the final full run. The project JSON schema and examples, native typed policy/worker/observation/verification implementations and all prior scanner/diagnostic tests are retained.

## Observed checks

Private tools (not global PATH): `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`; `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. Host Windows 11 build 26100 x64. No product install or OS networking performed.

| Command/check | Observed result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln -c Release --no-restore --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj -c Release --filter 'FullyQualifiedName~AuditStorageRegressionTests\|FullyQualifiedName~LocalProfileApplyTests\|FullyQualifiedName~ProfileRepositoryTests\|FullyQualifiedName~HistoryRepositoryTests\|FullyQualifiedName~ProfileViewModelTests\|FullyQualifiedName~ElevationHelperTests\|FullyQualifiedName~NetworkServiceValidationTests\|FullyQualifiedName~LegacyMigrationTests'` | Focused second run PASS 92/0 failed/0 skipped; 2 further MainViewModel history cases included in final full run |
| `& $sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=audit-fixes-full-final.trx' --results-directory TestResults/AuditFixes --nologo` | PASS 687/0 failed/0 skipped |
| `& $sdk run --project scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release -- TestResults/AuditFixes/Ui --audit-fixes` | PASS 8 Light/Dark normal/minimum captures; draft retained/retry footer reachable; recovery warning/paging/no mutation; 0 WPF binding errors |
| Same render project, `--no-build -- TestResults/AuditFixes/Workspace` | PASS 128 existing captures/0 binding errors, fake NIC/elevation/browser/socket; existing focus/footer/exit guards |
| Same render project, `--no-build -- TestResults/AuditFixes/Diagnostics --phase07` | PASS 64 captures/0 binding errors, fake diagnostics/probes, including hide/stop assertion on this run |
| `& $sdk list app/NetworkProfileManager.csproj package --vulnerable --include-transitive`; same for test | PASS, neither project reports vulnerable packages against current NuGet feed; not a complete security certification |

TRX and PNG output remain under ignored `TestResults/AuditFixes`; retained hashes/results are in [evidence](evidence/audit-fixes-2026-11-01.json). Four added UI captures were visually inspected: readable wrapped errors/warnings, persistent footer, scrollable form at minimum size, existing Light/Dark styles. Native AT/high-contrast/manual walkthrough NOT RUN.

### Failures observed and corrected

- First .NET 10 build produced C# 14 `field` keyword warnings in `ProjectProfileEditor.Validation.cs`; renamed the pattern variable `invalidField`, without suppressing diagnostics. Final build has 0 warnings.
- First focused run: 89 passed/3 failed; notification icons were constructed off the WPF dispatcher before the error handler. Construct icons inside the dispatcher; focused second run passed all 92.
- First full run: 684 passed/1 failed, resource scanner found constructed `Apply.State.` key. Reused the explicit shared state-to-resource switch. Final full run passed 687, including two additional persistence-failure regressions.

Historical DOC-R01 records an earlier diagnostic hide/cancel assertion failure from the screenshot task. This run's green diagnostic smoke does not establish its root cause or close that separate intermittent risk. UX-R01/P05-R01 are likewise not closed by a later green suite.

## Review and decisions

One bounded fresh-context adversarial review was used before the legacy safety decision. Findings reconciled: explicit unsupported-state matrix; no false all-compatibility claim; null consent fails closed; close/exit cancels and waits; only coordinator owns mutation lease; DHCP restoration is policy, not old lease; history uses the actual observed original; late persistence errors do not erase verified outcomes. Old entries remain accessible with an explicit limited-recovery warning. A nullable exact-journal pointer avoids duplicating recovery representations.

Self-review of final diff traced storage failures, UI commit order, all apply callers, typed worker system executable/identity/consent, additive old/new history and runtime migration. No further blocking code defect identified in this scoped diff. Cooperative revision/lease guarantees do not exclude non-cooperating filesystem races; replaced bytes remain in a unique backup. Backup/journal retention is manual and may grow. Static empty gateway means explicit removal; empty DNS means unchanged. Extending supported states requires extending observation, policy, privileged execution and verification together.

### Primary-source research

Microsoft's [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) (checked 2026-11-01) places .NET 8 end of support at 2026-11-10 and .NET 10 LTS at 2028-11-14; servicing requires current patches. SDK 10.0.403 installed privately, bundled/runtime host 10.0.14 observed. [Breaking changes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10) and Context7 `/dotnet/docs` informed the migration; build/tests are the local compatibility evidence.

The [.NET 10 OS matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) lists supported Windows 11 and specific Windows 10 LTSC/IoT/Enterprise editions, not ordinary Windows 10 22H2. Do not infer official support or actual startup from the existing build-19045 lab. Target stays `net10.0-windows`/win-x64; runtime OS support and application certification are distinct. Existing published v1.1.1 assets remain on .NET 8 and unchanged.

## Remaining acceptance / release decision

**NO-SHIP for stable/hardware-certified release.** Source remediation is locally verified; no new release or phase acceptance claimed. NOT RUN: new-source hosted CI, disposable runtime-negative-control/install/reinstall/uninstall, pristine Windows 10/11 deployment, native NIC/DAD/UAC/authenticated IPC and actual DNS/route recovery, live scanner/ICMP/TCP/mDNS, physical/USB/Wi-Fi/Hyper-V coverage, manual AT. These require the exact isolated baseline/recovery plan and existing owner gates. Current task does not start Phase 08 or DATA-01. Published release/lab's previous payload remains unchanged. Safe next step: review this diff and execute the separately approved isolated native acceptance plan; no production use inferred from unit PASS.
