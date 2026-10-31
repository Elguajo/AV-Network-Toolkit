# Apply & Open — explicit workflow (Phase 04)

**Must not be confused with a simple 'Open' button.** This operation changes a selected Windows adapter and can break connectivity.

## Proposed state machine

`Idle → Select project/profile/device/adapter → Validate → Show diff + confirm → Snapshot → Elevate/apply → Read actual adapter state → Verify → Launch browser → Complete`.

Failure paths: `UAC denied`, `selected NIC missing/changed`, `invalid config`, `netsh partial failure`, `post-apply mismatch`, `browser launch failure`. Any failure must show current observed config and available recovery steps; no false claim of atomic OS change.

## Required rules

- Never apply on merely opening a project, selecting a device or clicking **Open**.
- Show selected adapter, proposed IP/mask/gateway/DNS, previous current config and a warning if altering a connected interface.
- Preserve base application's least-privilege/UAC validation. Revalidate NIC identity and inputs at the elevated boundary.
- Snapshot enough Windows state to restore it (including current DHCP/static DNS policy and gateway; verify actual rollback ability in isolated tests).
- Running `netsh` multiple times is **not atomic**. On a failed stage, show actual result and offer recovery, rather than claiming an automatic rollback succeeded without verification.
- After apply, inspect actual OS adapter state. Network reachability/Ping is a **separate optional diagnostic** and does not determine whether IP assignment succeeded.
- Only then launch full `http(s)` URL. Browser/TLS issues must not trigger unrequested OS changes.
- If no adapter is associated or multiple adapter candidates exist, force manual choice; do not auto-change Wi-Fi or Hyper-V NIC.
- To minimize risk of losing a remote session, show a prominent local-host disclaimer before applying; no unattended remote apply.

## Tests

Unit tests must fake network interface, elevation, OS result and browser launcher. Cover: happy path, cancellation, UAC denied, validation rejected, changed adapter identity, partial address applied, DNS failed, verification mismatch, browser blocked, user chooses recovery. Windows NIC tests only on an isolated machine/test adapter with manual permission.

## Required contract after Phase 01 audit

Baseline limitations and their evidence are recorded as F01/F03/F04/F07/F08 in [the source map](../architecture/BASELINE_AND_SOURCE_MAP.md). This is the target contract for Phase 04, not a claim about existing apply/revert.

- **Snapshot:** capture stable NIC identity, all affected IPv4 addresses/prefixes, DHCP address state, gateway/route data affected by the operation, ordered DNS values and DNS automatic/static policy. Existing AdapterInfo exposes a reduced view; HistoryEntry has no DNS snapshot. Determine the exact additional state needed for the supported Windows configuration.
- **Support boundary — NEEDS_DECISION:** choose preservation/recovery support or explicit pre-apply blocking for multiple addresses/gateways, custom routes, policy-managed DNS and other configurations the snapshot cannot restore. Never silently reduce them to the first address and two DNS values. This adoption does not select the support boundary or promise full Windows-state recovery.
- **Identity/privilege:** carry a typed, validated request tied to the reviewed adapter identity; re-resolve and compare after UAC and immediately before mutation. Alias changes, removal or replacement must not redirect to another NIC. Keep the worker constrained; review temp request/result paths, tampering/replay and request lifetime in 04-T06 without exposing a general command runner.
- **Post-state:** distinguish command success from observed address/mask/gateway/DNS-policy success. Define time bounds and DHCP verification semantics without requiring a particular lease or successful ping. A successful refresh or active-profile highlight alone is not verification.
- **Partial failure:** record which stage failed, read actual state, and expose deliberate recovery. Separate network-apply failure from history-persistence failure so the UI never suggests no network change occurred when only history writing failed.
- **Recovery:** compare restored actual state with the snapshot and distinguish complete, partial, failed and unverifiable recovery. Never invent missing legacy values. Preserve compatibility with old history in a separately scoped 04-T07 review.
- **Cancellation:** define allowed cancellation points; after mutation starts, do not imply cancellation undoes completed commands. Read state and offer recovery as needed.

Fixtures must cover multiple addresses/gateways, DNS policy and secondary DNS, changed NIC identity, tampered worker requests, partial apply, failed history writes, failed recovery and no browser launch on failed verification. Actual UAC/NIC/TLS/browser integration remains separately authorized isolated testing.
