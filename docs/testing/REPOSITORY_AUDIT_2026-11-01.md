# Repository audit and primary-source research — 2026-11-01

Fork: https://github.com/Elguajo/AV-Network-Toolkit. Inspected HEAD: `de7a97e`, main; application source unchanged from `ba44a525f51ff8a36e67f8d303178d65a29148b1`. Phase 07 remains AWAITING_REVIEW. Owner requested an audit/research, not implementation. All findings below remain OPEN; this report does not authorize compatibility/auth/storage changes, native network tests, publication or a new phase.

## Assessment

The project has useful separation between pure policy, native boundaries, MVVM and isolated tests. The typed project-apply path is substantially more defensive than the inherited local-profile path: immutable review, identity/state rechecks, authenticated pipe, pre-mutation recovery journal, observed-state verification and controlled browser handoff. New project persistence validates content and uses revision checks and atomic replacement. URL validation is repeated at the browser boundary; mDNS packet/cache work is bounded.

A26-01/03/04 consolidate still-present inherited/baseline risks rather than claiming they were introduced by the latest feature changes. This audit adds current reproduction evidence and severity/sequence; previously fixed JSON validation findings are not reopened without new evidence.

However, these safeguards are not uniform across the application. Data-preservation defects and recovery availability failures are reproducible despite a green 681-test suite. Do not promote the Preview to a fully accepted stable/hardware-tested release on this evidence. Prioritize the data/recovery defects and runtime lifecycle, then close the legacy apply safety gap under an explicit compatibility contract.

## Findings

P1 means address before stable acceptance or normal reliance on the affected recovery/security behavior. P2 means a reproducible functional/reliability defect to schedule next. Confirmed code defects, credible security risks and external lifecycle risks are explicitly distinguished.

| ID | Priority / evidence | Finding | Smallest useful correction |
|---|---|---|---|
| A26-01 | P1 / reproduced | Local profiles/history can delete their only original after backup fails | Preserve originals on all read/backup failures; unique backups and atomic writes |
| A26-02 | P1 / reproduced | One corrupt journal prevents reading healthy recovery snapshots | Isolate per-file errors, expose healthy validated entries and damaged-file diagnostics; bounded paging |
| A26-03 | P1 / code + pure command check | Inherited Apply/Revert lacks complete recovery and observed-state success verification | Review a compatibility-preserving safety upgrade of all mutation entry points |
| A26-04 | P1 / credible boundary risk | Inherited elevated worker consumes mutable temp-file command text without binding it to reviewed NIC/intent | Typed authenticated transport, validated intent/identity and fixed system executable path |
| A26-05 | P2 / reproduced | Local profile save failure escapes after the UI collection changes | Persist candidate data first or roll back the collection; handle/report IO failure |
| A26-06 | P2 / reproduced | Migration records completion after a transient copy failure, preventing retry | Track per-file failures and mark completion only after successful/explicitly resolved migration |
| A26-07 | P2 / reproduced | Tray marks a profile active when secondary DNS differs | Use a shared full matching rule while retaining deliberate reapply |
| A26-08 | P1 / current primary source | .NET 8 support ends 2026-11-10; shipped packages bundle that runtime | Review .NET 10 LTS migration and OS/packaging compatibility, then rebuild/reverify both formats |

### A26-01 — failed backup still deletes the source

Location: [ProfileRepository](../../app/Services/ProfileRepository.cs), lines 48–51; [HistoryRepository](../../app/Services/HistoryRepository.cs), lines 61–64. Both `Load` methods catch any read/parse failure, call `BackupAndReset`, then return an empty list. `BackupAndReset` suppresses a failed `File.Copy` and still deletes the source. Existing `.bak` files are also overwritten on later corruption.

Controlled reproduction: write invalid fixture JSON and create a directory at the `.bak` path. The copy fails deterministically; loading deletes the original and produces no backup file. Reproduced separately for profiles and history. No user data was read or deleted.

Related integrity exposure: `Save`/`Append` use direct `File.WriteAllText`, with no atomic replacement or cross-process revision/lease. Microsoft documents that existing files are truncated and overwritten by this API; interruption/concurrent writes remain credible additional risks, not power-loss tests performed here. [File.WriteAllText documentation](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.writealltext).

Keep legacy filenames/JSON/slot limits compatible. A failed backup must never authorize source deletion; surface corruption separately from an empty collection. Retain old data until independently usable recovery exists. This confirms and reproduces the earlier [legacy persistence review](../architecture/LEGACY_PERSISTENCE_REVIEW.md); that review did not repair it.

### A26-02 — recovery discovery has an all-or-nothing failure mode

Location: [ProjectApplyJournal.ReadSnapshots](../../app/Services/ProjectApplyJournal.cs), lines 56–74; [ProjectApplyViewModel.LoadSnapshots](../../app/ViewModels/ProjectApplyViewModel.cs), lines 199–209.

Controlled reproduction: a valid supported request can be read. Add a second `.request.json` containing only `{`: `ReadSnapshots` throws `JsonReaderException` instead of returning the valid entry. The UI clears its recovery collection before calling this method, so it can offer no healthy entry. The valid file remains intact and can be recovered manually; the finding is loss of in-app recovery availability, not deletion of all journals.

This matters after a torn journal write or crash: the recovery index must tolerate an independently invalid entry. A second fixture confirms that 1001 request files produce an immediate `InvalidDataException` before any entry is presented. Journals accumulate without automatic pruning; the cap is documented, but there is no bounded page of healthy entries when it is exceeded.

Do not weaken individual snapshot validation or auto-delete evidence. Return per-file diagnostics and only independently validated entries, keep damaged files for inspection, and page/filter bounded sets. Keep recovery a separate explicit review/consent action. Actual native snapshot restoration was NOT RUN.

### A26-03 — legacy mutation and recovery use a weaker contract

Location: [MainViewModel.ApplyCoreAsync](../../app/ViewModels/MainViewModel.cs), lines 162–200; `ApplyProfileAsync`, lines 251–283; `RevertAsync`, lines 454–472; [HistoryEntry](../../app/Models/HistoryEntry.cs); [NetworkService.BuildDnsCommands](../../app/Services/NetworkService.cs), lines 153–170.

Inherited local/manual/tray applies run alias-targeted netsh actions, append reduced history only after all commands finish, and display success before the subsequent refresh. They do not compare requested settings against independently observed state. A partially successful command sequence can therefore fail without a new pre-change recovery record. A history-write failure after a successful network mutation is also reported through the general apply-error path.

Legacy history has no original DNS/policy/routes/additional IPv4. Revert passes old IP/mask/gateway to `ApplyStaticIp` with empty DNS parameters. A pure reflection check confirms that empty DNS creates zero DNS commands. Thus if a local profile changes DNS, the legacy Revert code has no information or command to restore those original DNS values. The exact resulting OS state was NOT tested live; do not claim full restoration from this path.

This is distinct from the new typed project path, which journals before mutation and verifies after it. Harden the inherited path through a reviewed compatibility matrix: preserve local profiles, hotkeys/tray, empty-field semantics, DHCP and the approved supported transitions. Routing everything into the restrictive new path without that review would silently break existing supported usage. Full native fail/deny/partial/recovery acceptance remains required.

### A26-04 — unbound mutable input at the legacy privilege boundary

Location: [ElevationHelper](../../app/Helpers/ElevationHelper.cs), lines 39–54, 100–119 and 139–154; [NetworkService.RunNetsh](../../app/Services/NetworkService.cs), line 271.

Actor/scenario: another process running as the initiating account can watch the account's temp directory and alter the serialized command list while UAC is pending. The worker rereads that mutable file; its validation restricts command prefixes/control characters but does not bind adapter/address/policy to the action reviewed by the user. A pure validator fixture accepts a syntactically valid request for a different invented adapter. No worker, netsh or UAC operation was executed.

The random filename limits guessing but is not authentication against an account that can enumerate its own temp directory. This is a credible consent/intent substitution risk, not a demonstrated UAC bypass, remote exploit or privilege escalation. Full exploitability and elevated-path resolution remain UNVERIFIED. The legacy runner also launches bare `netsh`; a system-qualified executable should be used, as in the new boundary. [Microsoft CreateProcess search semantics](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw).

Reuse the typed boundary's authenticated peer/intent concepts after reviewing compatibility. Do not remove legacy functionality or silently broaden privileged commands. Native pipe/UAC/elevated executable integrity still require separate lab evidence.

### A26-05 — failed local save leaves UI state ahead of disk

Location: [MainViewModel.AddProfile/UpdateProfile](../../app/ViewModels/MainViewModel.cs), lines 493–524; [ProfilePanel event handlers](../../app/Views/ProfilePanel.xaml.cs), lines 12–46.

Controlled reproduction: construct the VM with a fake adapter reader and a private data folder; a directory at the target profile-file path forces `UnauthorizedAccessException`. `AddProfile` has already added the row when the exception escapes. The event handler does not catch that save failure. A real GUI process crash was not induced; the observed facts are the escaping exception and one in-memory profile with no saved profile file. Similar edit ordering is present in `UpdateProfile`.

Persist a candidate collection before committing UI state, or restore the old collection on failure. Return a clear error and keep the edit available for retry; test locked/read-only/disk-error cases without accessing real user files.

### A26-06 — transient migration failure becomes permanent skip

Location: [AppPaths.MigrateLegacyDataIfPresent](../../app/Helpers/AppPaths.cs), lines 53, 68–69, 81–86; [existing migration tests](../../test/LegacyMigrationTests.cs).

Controlled reproduction in the throwaway harness's executable directory: hold its invented legacy JSON open with `FileShare.None`, call migration, release the lock and call migration again. First copy count = 0, completion marker exists, second copy count = 0, target absent; original remains. The current tests check marker/idempotence/preservation but not failure followed by a successful retry.

Fix the completion contract without overwriting existing destination data. Distinguish copied/already-present/conflict/failed files, then retry unresolved files. Keep the future DATA-01 migration separately reviewed; moving to user-home storage alone would not fix this algorithm.

### A26-07 — inconsistent current-profile evidence in the tray

Location: [ProfileViewModel.IsActive](../../app/ViewModels/ProfileViewModel.cs), lines 68–84; [tray use](../../app/Views/TrayPopupWindow.xaml.cs), line 56; compare [ProfileCurrentConverter](../../app/Converters/ProfileCurrentConverter.cs), lines 11–30.

Controlled reproduction: same static IP/mask/gateway/primary DNS, different secondary DNS. Tray `IsActive` = true while the main converter = false. The tray ignores secondary DNS and also treats missing current primary DNS as sufficient to match; DHCP matches are less strict than the main presentation. This is a misleading checkmark, not evidence that any network command ran. Reuse one explicit evidence rule across surfaces while retaining reapply.

### A26-08 — near-term runtime servicing deadline

Location: [application target](../../app/NetworkProfileManager.csproj), `net8.0-windows`; [SDK policy](../../global.json); [build workflow](../../.github/workflows/build.yml), .NET 8 setup; existing self-contained installer/ZIP contract.

Microsoft's policy, checked 2026-11-01, lists .NET 8.0.33 as current and .NET 8 end of support as **2026-11-10**; .NET 10 LTS is supported through 2028-11-14. Therefore the installed/bundled runtime is currently patched, but staying on this major version is not a durable servicing plan. This is an external lifecycle risk, not a new CVE found in the shipped app. [Current Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy).

A self-contained app does not automatically use a machine-wide runtime security update; its runtime must be serviced through rebuilt/redeployed packages. Review .NET 10/WPF/WPF-UI compatibility and the exact supported Windows client versions, then rerun build/unit/WPF/native/clean-client/install/update/uninstall gates for both package formats. Do not silently retarget this audit or infer Windows 10 support from one VM. [Runtime patch selection](https://learn.microsoft.com/en-us/dotnet/core/deploying/runtime-patch-selection).

## Dependency research — calibrated result

`dotnet list AVNetworkToolkit.sln package --vulnerable --include-transitive --format json` queried nuget.org. Application project: no vulnerable package reported. Test project: **High** advisories on transitive `System.Net.Http 4.3.0` and `System.Text.RegularExpressions 4.3.0`. The resolved test `.deps.json` has dependency-only entries for these packages, no runtime assets; neither old DLL is copied to the test output. Therefore these alerts do not establish that the corresponding vulnerable implementations ship in the application or execute in the current net8 test process.

The Http advisory describes old .NET Core/runtime/package versions, not a new .NET 8.0.33 flaw. Keep the audit signal visible; modernize the test dependency chain after tracing its parents rather than suppressing alerts or forcing arbitrary direct runtime package overrides. [Microsoft CVE-2018-8292 advisory](https://github.com/dotnet/announcements/issues/88), [Microsoft NuGet transitive auditing guidance](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nugetaudit-transitive-packages). Raw tool result retained locally in `TestResults/RepositoryAudit20261101/nuget-vulnerabilities.json`.

## Evidence and actual checks

Specialist lenses used: project-audit, codebase-explorer, security-review, data-storage-review and reliability-review. No subagents, external private-code uploads or actual native network actions. Only public documentation/package identifiers were used for research.

- Release solution build: **PASS, 0 warnings / 0 errors**.
- Full existing suite: **681 PASS / 0 failed / 0 skipped**. Local TRX `TestResults/RepositoryAudit20261101/Unit/repository-audit.trx`, SHA-256 `59AD58365FDDA2ABB6EE9D26A1EF9E2A741A0CF27B7D0CBFBE69A43A6A1B2DB5`.
- **9 isolated audit scenarios** reproduced defects or confirmed limits/boundary behavior as explicitly labelled in the [retained results](evidence/repository-audit-2026-11-01.json). Passing reproduction means a defect was demonstrated, not repaired. Results SHA-256 `567BEC89EFE435CC046F7A9E29136A8D861B794254699CA8AD828E65035B7934`.
- [Reproducible harness source](evidence/repository-audit-2026-11-01-repro.txt), SHA-256 `AB2CBFECFF235E7520A2B16F599028422125110993D6865AAE2F2C05FECF1CFF`. Initial harness compile failed because WPF implicit usings omit System.IO; added that using only to the throwaway harness, then final 9-scenario run passed. No product code or existing test assertion changed.
- Final planning/schema/formats/49 stable tasks/**411 links PASS**, **12 tooling PASS**, whitespace PASS. Documentation/evidence-only local commit; no push.
- Native App startup, GUI crash induction, UAC/IPC, real IP/DNS/DHCP changes, restoration, DAD, cable/driver cases, route/ICMP/TCP/mDNS/packet egress and scanner behavior: **NOT RUN**. Existing lab provisioning is not native app acceptance.
- New project save/URL/MVVM, diagnostic source/route checks, mDNS parser/cache/socket boundaries and package/CI definitions inspected. No new confirmed defect is asserted for each inspected surface; this audit is not an exhaustive security, accessibility or performance certification.

Reproduction commands, from repository root with the existing private tools:

```powershell
$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"
& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore --nologo
& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'trx;LogFileName=repository-audit.trx' --results-directory TestResults/RepositoryAudit20261101/Unit --nologo
& $sdk list AVNetworkToolkit.sln package --vulnerable --include-transitive --format json
# Recreate the two files from the retained harness text at this ignored path first.
# Always choose a fresh fixture directory; no customer data belongs there.
& $sdk run --project TestResults/RepositoryAudit20261101/Repro/AuditRepro.csproj --configuration Release -- TestResults/RepositoryAudit20261101/FixturesFresh
```

## Recommended sequencing — proposal, not an adopted implementation plan

1. **Now:** A26-01 and A26-02 data/recovery preservation, plus the A26-08 supported-runtime decision before stable publication. Add regressions that fail on the current behavior; retain original data on every failure.
2. **Next:** review the legacy mutation/privilege compatibility contract for A26-03/04; address A26-05/06 in the related storage work and A26-07 as a bounded UI correction. Do not weaken typed support checks to make the old path appear compatible.
3. **Then:** execute approved isolated native cases against pinned binaries, including independent restoration, UAC denial, partial failure, changed/removed NIC, source/route/packet evidence and both package formats. Keep physical/USB/Wi-Fi/Hyper-V evidence separate from VMware observations.
4. **Later/product decisions:** APIPA/no-lease transitions remain deliberately blocked by the new flow, scanner uses OS routing rather than diagnostic source binding, IPv4-only passive mDNS does not cover full active discovery, and DHCP server/passwords/user-home migration are unimplemented/deferred. These are support/scope choices, not defects to silently fix during this audit.

No app fixes, phase completion, roadmap reprioritization, new dependencies or publication performed. Owner review and separately scoped implementation remain the next decision.
