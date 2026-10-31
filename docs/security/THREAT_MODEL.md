# Threat model & safety gates

| Risk | Failure mode | Mitigation/evidence |
|---|---|---|
| Wrong adapter | Laptop disconnects office or AV management | Require selected NIC identity; show diff; recheck identity at apply boundary |
| Loss of network settings | Gateway/DNS/routes wiped by sequential commands | Capture previous settings; explicit semantics; verify post-state; documented recovery |
| Elevated command injection | Malicious project JSON controls `netsh` args | Validate IPv4/adapter ID, retain restricted elevated worker and structured arguments |
| Worker request substitution | Temp-file exchange or insufficient request binding targets a different NIC/configuration | 04-T06: review request/result paths, lifetime and tampering; typed payload, stable NIC identity recheck after UAC; current exploitability UNVERIFIED |
| Incomplete recovery | Reduced history/snapshot loses DNS policy, extra addresses or affected routes | 04-T02/04-T07: capture sufficient state or block unsupported cases before mutation; verify recovery and retain unknown legacy fields as unknown |
| Test-induced network traffic | Documentation-range scanner tests still emit real probes; VM tests enumerate host NICs | 01-T07: fake probes/enumeration or segregate integration tests; approved safe unit set before execution |
| Malicious URL | Opens local file/shell or leaks secrets | Restrict to `http(s)`, disallow embedded credentials; review query tokens; safe launcher |
| Untrusted discovery data | CSV formula injection, spoofed hostname/services | Treat mDNS/ARP responses as untrusted; escape CSV; no auto-trust or auto-apply |
| DHCP collision | Rogue server disrupts production LAN | Explicit isolated NIC, never auto-start, reject unsafe conditions, user confirms isolation |
| Firewall/socket leakage | DHCP service leaves persistent privileged state | Stop/cleanup journal, crash recovery and isolated tests |
| Config corruption | Incomplete JSON write destroys all project profiles | Validate before modification, atomic replace, backup strategy, reject unknown versions |
| Unsafe distribution | Unsigned binaries and unreviewed auto-update | Check integrity, document SmartScreen warning and release provenance |
| Rights violation | Copy of code lacking declared license | Base MIT maintained, feature reference only for AV Toolbag |
| Sensitive project data | Private device addresses committed publicly | Documentation-range samples, secrets excluded, private project configs ignored |

**Stop-the-line conditions:** unexpected NIC switch, unverified network mutation, lost config, suspected DHCP leak, missing license permission, failing regression tests. Log issue and stop release until investigated.

## Evidence and unresolved boundaries

See [Phase 01 findings and dispositions](../architecture/BASELINE_AND_SOURCE_MAP.md). The existing worker has verb/character checks; no exploit was attempted, and documentation does not establish a privilege-escalation exploit. The contract and request transport require review before the new apply flow (04-T06).

Existing repositories write directly and may attempt original deletion after a failed backup. New project IO must preserve the original on failure (02-T04); legacy changes are deferred to a separately scoped 02-T06 review. Decisions about complex network-state support and secret-bearing URL detection remain open; do not resolve them implicitly in code or schema.

## Phase 02 implemented boundary — review evidence

Assets: project JSON and exact prior-version backups. Untrusted input: local JSON and explicitly selected filesystem paths. Embedded schema only; no user-supplied schema/ref fetching, network resolution, browser process, netsh/UAC or legacy persistence dependency in project IO. Parse/save share structural+semantic validation with explicit formats, duplicate/ref checks, bounded UTF-8/depth/size and redacted code/field errors. HTTP(S) storage rejects credential-like query/fragment keys and embedded userinfo without visiting endpoints. Detection cannot establish that arbitrary free text contains no secrets.

IO: explicit overwrite confirmation plus observed revision, cooperative exclusive lease, same-directory durable temp+backup, atomic replace and preserved conflicting-version backup. Tests cover denied overwrite, locks, injected failures and external edits. Existing reparse paths/UNC/device/ADS paths are rejected; this is not a hardened boundary against a malicious same-user process changing filesystem links between checks. Shares, power-loss hardware testing, forced process/OS shutdown draft recovery and arbitrary filesystems remain UNVERIFIED/NOT RUN. Backups contain user data, are never auto-pruned and need user retention care.

Code review found and corrected schema lifetime, permissive abbreviated-IPv4 formatting, profile-list selection mutation and tray-exit draft protection issues before final evidence collection. No claim of complete security certification. Legacy persistence/history findings remain separately scoped and unchanged; see [persistence review](../architecture/LEGACY_PERSISTENCE_REVIEW.md).
