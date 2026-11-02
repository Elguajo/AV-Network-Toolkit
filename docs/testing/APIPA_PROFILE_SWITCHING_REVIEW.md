# SWITCH-02 — DHCP/APIPA to static profile

2026-11-02, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, clean base `941e0cc`. Continuation of the owner's request to fix real profile switching: the first local correction explicitly left DHCP APIPA/no-lease blocked. The owner reproduced that remaining restriction while applying a saved static equipment profile, then clarified the ordinary manual workflow: static IPv4/mask/gateway and empty DNS. Personal screenshot addresses and saved-profile names/values are not committed. No real profiles were changed.

Phase 07 remains AWAITING_REVIEW. SWITCH-02 / ADR-056 is a separate bounded compatibility correction, not a phase advance, publication or native test authorization.

## Cause and bounded contract

The screenshot's refusal is the `LocalApply.AddressState` branch, not a Windows/UAC failure. `ValidateSnapshot` required a usable lease even before changing the adapter. That excluded the normal isolated-equipment case where DHCP is enabled but no server supplies an address. The new regression cases reproduced six failures on the prior product source before the correction.

Microsoft describes [APIPA when no DHCP server responds](https://learn.microsoft.com/windows-server/troubleshoot/how-to-use-automatic-tcpip-addressing-without-a-dh) and documents [prefix/suffix origins](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-netipaddress). These sources identify policy/address distinctions; they do not certify native apply or recovery here.

The corrected source permits static/DHCP preparation from a fully classified connected DHCP interface with enabled DAD, no persistent IPv4, no default gateway and either zero active IPv4 addresses or one Preferred `WellKnown` APIPA candidate with /16. Only ordinary generated local routes are accepted. Manual/alternate or additional addresses, wrong prefix/state, managed/unknown DNS, custom/default routes and unknown/disconnected/disabled-DAD state remain blocked. Manual Link-Local assignment does not gain this source-state support; its existing explicit-DNS/DAD tests remain intact.

| Operation | Result |
|---|---|
| DHCP/APIPA or connected DHCP awaiting its first address → saved static IPv4/mask/gateway, empty DNS | Review and confirm immutable requested fields. Freeze currently reported automatic DNS servers, or explicitly retain a known-empty list, using SWITCH-01 semantics |
| Same source with known static DNS | Leave current DNS servers untouched; full original DNS policy retained for recovery |
| Source changes or a DHCP lease arrives after review | Reject before journal/mutation; no adapter substitution |
| Static apply | Retain exact original APIPA/no-address snapshot; worker checks the request and pre-state as before. Strict result requires requested Preferred manual IPv4, gateway/persistence and DNS before success/browser |
| History from no-lease source | `WasDhcp=true`, exact journal pointer. APIPA old address/mask retained as observed; with no address, old address/mask are empty rather than invented. Recovery uses original policy, never manually reassigns the APIPA address |
| DHCP apply/recovery, no destination, no server | After Finished worker execution and observation, verify fully classified DHCP/DNS policy separately. Stage `DhcpPolicyVerified` with explicit waiting-for-lease/no-connectivity-verification copy |
| DHCP Apply & Open without a usable lease | Unverified; never open browser. `Matches` remains strict; the policy-only helper is available only on the coordinator's destination-free branch |
| Cancellation, worker failure/UAC denial or journal failure | Existing failure/consent/journal ordering remains; observing a DHCP policy does not turn a failed or cancelled operation into success |

No public model/project schema, profile file, command syntax, storage root, IPv6 or other NIC change. The private journal already supports the observed address origin and empty arrays; older builds can reject recovering these newly supported snapshots, so retain journals and use the corrected build. DHCP recovery restores policy and may produce a different APIPA address or no address. It is not a lease, reachability or exact automatic-address guarantee.

## Verification

Private SDK `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` (10.0.403); process-local DOTNET_ROOT for the manifest-bearing render harness. No real NIC/process/browser/elevation boundary in automated tests. Existing code-review workflow applied by the implementing agent: traced source validation, strict result versus policy-only result, browser exclusion, worker-failure/cancellation ordering, no-address history, original DNS-policy recovery and old Link-Local behavior. Not independent reviewer certification.

| Exact command/check | Result |
|---|---|
| `$sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-restore -p:OutputPath=bin/ApipaSwitchFix/ --filter 'FullyQualifiedName~NoLease\|FullyQualifiedName~DhcpWithoutLease\|FullyQualifiedName~LeaseArriving' --logger 'trx;LogFileName=before.trx' --results-directory TestResults/ApipaSwitchFix` on prior product plus new tests | Expected red: 6 FAIL / 12 PASS / 0 skipped. `before.trx` retained |
| Focused test filter `ProfileSwitchingRegressionTests\|LocalProfileApply\|ProjectApply\|LinkLocal`, same output, `focused.trx` | 212 PASS / 0 failed/skipped; two additional DNS/UI cases added afterward |
| `$sdk build AVNetworkToolkit.sln -c Release --no-restore -p:OutputPath=bin/ApipaSwitchFix/` | PASS / 0 warnings/errors |
| `$sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-build --no-restore -p:OutputPath=bin/ApipaSwitchFix/ --logger 'trx;LogFileName=full.trx' --results-directory TestResults/ApipaSwitchFix --logger 'console;verbosity=minimal'` | **922 PASS / 0 failed/skipped**, 29 seconds; 20 additional cases beyond SWITCH-01 |
| `powershell.exe -NoProfile -NonInteractive -File scripts/validate_apply_observation.ps1` and `& ./scripts/validate_apply_observation.ps1` | 20 fake-query fixtures PASS in each shell, including empty-DNS DHCP/APIPA and DHCP without an address; no native queries |
| `$sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-restore -p:OutputPath=bin/ApipaSwitchFixUi/` | PASS / 0 warnings/errors |
| `scripts/ProjectUiSmoke/bin/ApipaSwitchFixUi/ProjectUiSmoke.exe TestResults/ApipaSwitchFix/SwitchUi --profile-switching` | 20 Light/Dark normal/minimum captures PASS / 0 binding errors; APIPA/no-address→static/gateway/empty-DNS review and reachable confirmation. Dark minimum APIPA render visually inspected |
| Same harness `TestResults/ApipaSwitchFix/ExistingUi`, no flag | 128 existing captures PASS / 0 binding errors; project/device/apply/Link-Local/adapter/mDNS/workspace/consent/focus/exit flows retained |

Filter pipes above are escaped for Markdown, passed as literal pipes to dotnet. Remaining documentation/package commands and exact hashes are recorded in the outcome/handoff after completion.

## Limits and next step

Real application/network apply, netsh/UAC/IPC, DHCP renewal timing, DAD/hardware, connectivity, conflict checks and recovery remain **NOT RUN / UNVERIFIED** by the agent. No live scans, server, probes, real data mutation, ordinary App startup, installation, push/release or phase acceptance. This correction is verified through the real source policy/coordinator/worker with fake OS execution and rendered UI; no claim that unit success certifies native behavior.

Next safe action is the owner's review of the corrected local portable's explicit adapter/current/requested values. Keep the prior folder and original snapshots. The agent still needs approval plus an isolated environment for live network acceptance; no new phase is proposed here.
