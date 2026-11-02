# A26 audit fixes — implementation contract

Current bounded amendment: [SWITCH-01 / ADR-055](../testing/PROFILE_SWITCHING_REVIEW.md), 2026-11-02, corrects ordinary Windows observation and extends empty-DNS/duplicate-source transitions after the owner's failed real-use report. Its explicit compatibility matrix supersedes the corresponding blocked rows below for current local source only; historical A26/release evidence stays attributed to its source.

[SWITCH-02 / ADR-056](../testing/APIPA_PROFILE_SWITCHING_REVIEW.md) further permits classified connected DHCP/APIPA/no-address sources for static/DHCP operations. Browser-free DHCP policy completion is separate from a usable lease and never enables browser opening. It supersedes the corresponding no-lease blocks below only; manual Link-Local rules, unknown/alternate/complex-state blocks and historical evidence remain.

2026-11-01, main, Phase 07 AWAITING_REVIEW. Owner requested “Исправь все что нашел” after the [eight-finding audit](../testing/REPOSITORY_AUDIT_2026-11-01.md). This authorizes the scoped local remediation, including the runtime and inherited privilege boundary. No push, release, native networking, VM mutation or phase completion. ADR-047 records the decision.

## Scope and compatibility

Preserve local named profiles, five slots, static/DHCP input, import/export, scanner, diagnostics, history, tray, UAC, English resources and project JSON v1. Preserve storage paths and existing JSON arrays; DATA-01 is separate. No visual redesign or new dependencies. The concurrent README screenshot commit is preserved.

The owner delegated the complex-state choice (“Не знаю как лучше … гибкий … адаптивнй и расширяемый”). Engineering choice: route local/manual/hotkey/tray Apply and Revert through the existing typed coordinator and policy. This intentionally narrows unsafe inherited mutation behavior; it is not evidence that every former netsh input remains supported. The alternative of exact restoration for arbitrary routes/multiple addresses/managed DNS would require a separately specified, observed and verified policy extension. There is no generic plugin/command framework or silent fallback to the retired worker.

| Selected operation/state | New local behavior |
|---|---|
| One preferred usable IPv4, classified static DNS, matching persistent static address/default route | Explicit current/requested review, confirmation, durable original snapshot, typed worker, observed verification |
| DHCP with a usable lease and fully classified policy → static with explicit DNS | Supported by the existing policy; recovery restores DHCP/DNS policy, not the previous lease |
| DHCP request from a supported source | Enables address/DNS automatic policy; verifies resulting supported state |
| Static empty gateway | Explicitly removes the supported default gateway, shown in review; original gateway retained for recovery |
| Static empty DNS | Leave unchanged; never implicitly erase DNS. DHCP→static with automatic DNS and no explicit replacement is blocked |
| Secondary DNS without primary | Block with specific feedback; no settings changed |
| No IPv4, APIPA/no usable DHCP lease, multiple addresses, /31–/32, noncontiguous mask | Block; retain editable profile/draft and give reason. These common boundaries are not silently labelled supported |
| Static automatic DNS, unknown/managed DNS, custom/complex routes, unknown persistent state | Block before journal/mutation; retain data and explain reason |
| New local history entry | Optional `RecoveryOperationId` identifies the exact validated original journal; explicit full supported-policy recovery including DNS |
| Old history without pointer | Retain Revert with explicit incomplete-history warning: recorded static address/gateway with unchanged DNS, or DHCP address/DNS policy. Never claim exact historical restoration |
| Missing/invalid journal or another NIC's journal | Block recovery; never fall back to an alias/another adapter or pretend legacy restoration is complete |

Policy extension belongs in `ProjectApplyPolicy`, typed request validation, worker, observation, verification, recovery and fixtures together. This is the deliberate extension seam; no arbitrary command execution. Preparation alone never applies changes. Missing confirmation fails closed. Coordinator alone owns the mutation lease. Exit requests cancel and wait for observation instead of abandoning an active operation. Final journal/history failure is a separate persistence warning after verified completion, with the original request retained.

## Persistence and recovery availability

- Local array reads preserve corrupt originals and existing backups and report an error. No reset-on-read. Strict UTF-8, non-null array/elements and 8 MiB limit.
- Cooperative exclusive file lease, expected-content hash for local profile saves, same-directory temporary write and disk flush, atomic replacement with a unique backup. UI collections change only after save. Add/Edit remains open with the draft on failure; refresh reconciles external writers before retry. History append is leased read/modify/replace.
- A non-cooperating external writer can still race the revision check; the exact file replaced is retained in the unique backup. No claim of arbitrary filesystem transaction isolation or bounded backup retention.
- Recovery enumeration handles each file independently, preserves invalid files, reports a count and exposes valid entries. Read at most 1000 files/page plus one lookahead. Next page clears stale selection; Read snapshots restarts. Concurrent folder changes require refresh; pages are not a frozen directory snapshot. Direct history recovery validates the exact operation file independently of paging.
- Journal creation is flushed, create-only and atomic. A failed legacy copy cannot create the completion marker; retries skip existing destinations and preserve sources. Existing markers are not silently invalidated; DATA-01 conflict/migration design is separate.

## Additive local history field

`RecoveryOperationId` is nullable GUID data in the existing local history array, not a project schema/version change. Old arrays load with null and get the explicit limited recovery flow. New arrays round-trip with the pointer. Older application versions ignore unknown JSON fields and cannot use the full recovery path; do not claim downgrade recovery equivalence. Journals remain in the private existing directory, not embedded in exports or URLs. No credentials added. Never delete journals when clearing the visible history list.

## Runtime and validation

Use supported .NET 10 LTS/WPF, SDK 10.0.403/latestPatch; CI, test/render projects, publish profile and framework-dependent package control target the same major. No package version churn or warnings suppressed. Historical v1.1.1 downloads remain .NET 8 and unchanged. Microsoft support depends on the OS edition/version; ordinary Windows 10 22H2 is not listed as a supported .NET 10 client. Windows 10 lab startup/compatibility and Windows 11 clean-client/native NIC checks remain unverified.

Acceptance evidence: preservation/retry/conflict/corrupt-file/paging tests; typed local consent/identity/recovery/partial failure/exit/history-write tests; tray/main DNS agreement; full isolated suite; Release build; Light/Dark minimum/normal WPF; self-contained installer/ZIP creation, packaged diagnostic and retired worker rejection. Record original red runs as well as green runs. Native UAC/IPC/NIC/route/DAD/clean-client/installer acceptance is separate. [Observed review](../testing/AUDIT_FIXES_REVIEW.md).
