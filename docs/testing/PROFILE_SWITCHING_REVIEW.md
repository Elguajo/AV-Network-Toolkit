# SWITCH-01 — ordinary Windows profile switching

2026-11-02, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, clean inspected base `4f2f870b455fad8adfd06314f90ca92f9dfd40b2`. Owner reported a generic preparation failure and explicitly requested a correction after entering IPv4 manually. Separate defect follow-up, Phase 07 AWAITING_REVIEW unchanged. Implementation commit resolves via `git log -1 --format=%H -- app/Services/WindowsProjectApplyObservation.cs`; later evidence commits do not change that source.

## Cause and evidence

Read-only execution of the embedded observer on the deliberately named Ethernet reproduced a null method call in route enumeration: Windows reports no Protocol for a persistent ordinary default route. A later read, after the owner changed configuration independently, reproduced `CmdletizationQuery_NotFound_AddressFamily` when the persistent IPv4 store was empty. Such a store is normal under DHCP. No network setting was changed by the agent.

The local DNS policy container existed with zero values/children; the old existence-only test misclassified it as active policy. After the owner's manual assignment, Windows reported a duplicate configured manual address plus its automatic WellKnown/Link APIPA fallback. The screenshots alone did not establish any of these facts. Passive reads did; no personal addresses, route tables, credentials or screenshots are committed here.

The corrected observer later read the current DHCP adapter successfully: classification complete, one Preferred address, two DNS servers, zero persistent addresses and six routes. This is observation evidence only, not native apply/UAC/recovery acceptance.

## Current compatibility amendment — ADR-055

| Case | Corrected behavior |
|---|---|
| Empty persistent address/route store | Enumerate the store without a CIM AddressFamily filter, then filter in memory; real query failures still fail closed. IPv6 is read only to filter the result, never modified |
| Persistent default route with no Protocol | Preserve the distinct `persistent` origin; require a unique active NetMgmt default with identical next hop and route metric, even during recovery. Never invent an observed protocol |
| Empty DNS-policy registry container | Not an active policy; populated values/children, effective NRPT/global DNS or unreadable state still block |
| Static profile with empty DNS, current static DNS | Preserve current servers/policy, including the known-empty `none` state; no DNS command in ordinary leave-unchanged apply |
| DHCP/automatic DNS → static, profile DNS empty | Freeze currently reported servers as an explicit static list in the reviewed request. No invented/public DNS. For an already empty list, use internal `none`; reject ordinary `none` intent against existing servers |
| Restore original DHCP | Restore automatic policy, not historical lease/server addresses |
| Restore original known-empty static DNS | Reviewed `none` intent generates fixed `source=static address=none`; no server indexing into an empty list |
| One configured manual Duplicate address, optionally one Preferred WellKnown APIPA /16 fallback | Allow switching away. Keep full source in journal and pre-state checks; history/recovery use the configured manual address. Only generated local fallback routes are permitted. Extra manual addresses, changed persistent address and custom routes stay blocked |
| Duplicate requested/resulting address | Unverified, explicit conflict feedback, no browser. Recovery restores configuration intent, not the conflict condition or a guaranteed automatic fallback address |
| Settings change after review | Reject before journal/mutation, including changed DHCP DNS |

Project v1/v2 and local-profile JSON formats remain unchanged. Internal typed requests/private journals may now contain DNS `none` and route origin `persistent`. Older application builds may reject recovery of those new journals; no migration, downgrade or silent fallback is provided. Retain original snapshots and use the corrected build for recovery. Ordinary unknown/multi-address/custom-route/managed-policy, DHCP APIPA/no lease and /31–/32 boundaries otherwise remain; this is not universal network-state support.

## Validation and retained failures

Private SDK `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` 10.0.403 restored locally because only SDK 8 was present; no global PATH/product installation. Validation Python `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. All automated OS/process/worker/browser commands below use fakes except the explicitly identified passive reads above.

| Command | Result |
|---|---|
| `powershell.exe -NoProfile -NonInteractive -File scripts/validate_apply_observation.ps1 -SourcePath out/ApplySwitchFix/OriginalObserver.cs` using `git show 4f2f870:app/Services/WindowsProjectApplyObservation.cs` | Expected FAIL: empty-policy misclassification; initial direct passive reproduction also proved null Protocol and filtered-empty-CIM failures |
| `powershell.exe -NoProfile -NonInteractive -File scripts/validate_apply_observation.ps1` and `& ./scripts/validate_apply_observation.ps1` | 18 fake-query fixtures PASS in Windows PowerShell 5.1 and PowerShell 7; no real NIC queries. Active-null protocol/policy values/policy children stay unknown; empty persistent/DNS/policy and fallback are covered |
| `& $sdk build AVNetworkToolkit.sln -c Release --no-restore -p:OutputPath=bin/ApplySwitchFix/` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-build --no-restore -p:OutputPath=bin/ApplySwitchFix/ --filter 'FullyQualifiedName~ProfileSwitchingRegressionTests\|FullyQualifiedName~LocalProfileApplyTests\|FullyQualifiedName~ProjectApply\|FullyQualifiedName~LinkLocal' ...` | Initial focused 193 PASS; one additional recovery-provenance test added afterward. Literal filter pipes shown escaped for Markdown |
| Full suite, same test command without filter, `--logger 'trx;LogFileName=final-verified.trx' --results-directory TestResults/ApplySwitchFix` | Final 902 PASS / 0 failed/skipped (24 s), including 15 new regression cases |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-restore -p:OutputPath=bin/ApplySwitchFixUi/` | PASS, 0 warnings/errors |
| Manifest-bearing `scripts/ProjectUiSmoke/bin/ApplySwitchFixUi/ProjectUiSmoke.exe TestResults/ApplySwitchFix/Ui` with process-local DOTNET_ROOT set to the private runtime | 128 existing WPF captures PASS / 0 binding errors |
| Same harness, `TestResults/ApplySwitchFix/SwitchUi --profile-switching` | 12 Light/Dark normal/minimum review captures PASS / 0 binding errors. DHCP DNS list, known-empty DNS, conflict warning and reachable confirmation asserted. Dark minimum conflict and Light normal DHCP screenshots visually inspected |

Initial full run: 900 PASS / 1 FAIL (`StringResourceTests`), retained `full.trx`. A duplicate `Apply.Stage.AddressConflict` resource introduced during this correction also caused the first UI run to fail at resource load; removed the duplicate and reused the existing key. Subsequent full suite and renders passed. Failures are not suppressed or counted as success.

Focused self-review using the code-review skill traced coordinator normalization, authenticated typed worker revalidation, DNS command derivation, actual-state verification, source-only duplicate handling, exact journal/history selection and both UI entry points. Added guards for unmatched persistent gateways during recovery and ordinary empty intent clearing existing servers. No unresolved actionable defect found within this scope. This is the implementing agent's review, not independent certification.

## Limits and next action

No real apply, elevated worker/UAC, DNS or gateway mutation, probes, scanner, DHCP server, device/browser launch, ordinary application startup, installation or personal-data migration. Native netsh behavior, conflict recovery and owner acceptance remain NOT RUN/UNVERIFIED. No push, public release or phase advancement. A local self-contained portable correction is prepared separately below; installed and published 1.1.6 binaries remain unchanged.

Out-of-scope documentation issue observed: an older Device web interfaces paragraph in USER_GUIDE still says passwords are unimplemented despite the current v2 section. Logged here, not silently repaired as part of networking work.

Next safe action: owner exits the old app through tray Exit, extracts the complete corrected portable folder and checks the named-adapter confirmation. Any native network test by the agent still needs the separately approved isolated environment. No new phase proposed until current review gate is resolved.

## Local artifact — exact source e3315a2

Implementation commit **`e3315a22b38d048266109bc24350ac29c13a2c33`**, clean tree before publish. Self-contained publish command:

```powershell
& $sdk publish app/NetworkProfileManager.csproj -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -p:PublishReadyToRun=false -p:SourceRevisionId=e3315a22b38d048266109bc24350ac29c13a2c33 -p:InformationalVersion=1.1.6-local.profile-switch -p:ReleaseChannel=Preview -o out/ProfileSwitchFix-e3315a2/Portable
```

PASS, no build warnings/errors. MIT LICENSE copied into the payload alongside existing third-party notices and five native WPF dependencies. Hidden `NetworkProfileManager.exe --package-self-test <absolute ignored report>` exits 0 before ordinary startup: Success/JSON/WPF true, version 1.1.6.0, informational version `1.1.6-local.profile-switch+e3315a22b38d048266109bc24350ac29c13a2c33`, .NET 10.0.14, Preview, no production update key. Public version/tag/assets remain unchanged; this is clearly a local correction, not a new published version.

Portable folder: `out/ProfileSwitchFix-e3315a2/Portable/`; archive: `out/ProfileSwitchFix-e3315a2/AVNetworkToolkit-1.1.6-local-profile-switch-e3315a2-portable.zip`. Expanded to a separate fresh ignored directory and all **8 files** compared with source payload by SHA-256, PASS. ZIP SHA-256 **`cbe0ef8697640da0d465158bbeff722c11e400ecab6c102c5b51259f43d23c0b`**. Exact ignored per-file identities/diagnostic: `out/ProfileSwitchFix-e3315a2/package-manifest.json` and `package-self-test.json`. Full final unit TRX SHA-256 `0e56ef1341bf77fd32733379504339d773d6c241590a188342a16fa3eabddf3c`.

Final documentation validation before handoff: private Python `-B scripts/validate_plan.py` PASS (9 phases/49 stable tasks/schema/formats/920 links), `-B scripts/test_validate_plan.py` PASS (12), `git diff --check` PASS. Final metadata/handoff validation is recorded in STATUS. Git identity was absent; the scoped commit used per-command metadata matching the existing repository author, without changing global or local Git configuration. No push or installer was performed.
