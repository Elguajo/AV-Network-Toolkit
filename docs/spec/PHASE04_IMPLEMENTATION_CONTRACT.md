# Phase 04 implementation contract — conservative support approved

Date: 2026-10-31, Asia/Tashkent. Start authorized by owner («Начинай фазу 4 на новом чате»). Base: main, abdd1d1c61dd4f0d6b39adfc24ee69ff46367cbc. No live execution or publication authorized.

## Architecture and smallest implementation

Keep legacy NetworkService, ElevationHelper, profile/history files and their callers compatible. Add a separate project apply coordinator with injectable observation, elevated execution, recovery journal and browser boundaries. Reuse ProjectJson validation, ProjectDevice/Profile and DefaultBrowserLauncher. A localized Apply & Open addition in Project uses the retained related profile, explicit NIC selection and an immutable confirmation plan; pending editors are never silently saved or discarded. An unlinked device cannot apply. Open Web Interface stays independent.

State flow: Idle → Preparing → AwaitingConfirmation → Applying → Observing → Verified → Opening → Complete. Terminal alternatives distinguish rejected/cancelled-before-change, UAC denial, uncertain/partial apply, mismatch, browser failure and journal failure. Only Verified can enter Opening. No probe participates in verification. Reentry is rejected while any operation owns the coordinator; a plan is single-use and expires. Revalidation compares stable identity and complete relevant pre-state after confirmation and after UAC, immediately before mutation. Every command uses the re-resolved interface index and rechecks identity; no alias fallback.

## Approved support boundary — owner response, 2026-10-31

Support only fully classified ordinary IPv4: a single address, at most one ordinary default gateway, known automatic/static IPv4 DNS policy and ordered server list. Block extra addresses/gateways, custom active/persistent routes, policy-managed DNS, alternate static DHCP configuration or any unknown affected state before mutation. Preserve IPv6 and unrelated settings. Static /31–/32 and static-IP/automatic-DNS requests remain storable but blocked pending evidence. DHCP enables automatic address and DNS; verification requires observed DHCP and automatic DNS policy plus a usable preferred DHCP address, without pinning a particular lease. A timeout is unverified, not success. Disconnected/no-lease DHCP cannot open a browser.

Owner answered «Утверждаю: блокировать сложное/неподтверждённое». This approves the conservative boundary above. Unsupported/unknown cases stay blocked; no schema narrowing or legacy apply change is implied.

## Request, observation and recovery

An elevated request contains protocol version, random operation ID, short expiry, selected stable NIC identity, the reviewed snapshot and typed desired configuration. It contains no executable, command text, paths, URL or credentials. Validate structurally and semantically again in the worker. The worker derives fixed IPv4 operations itself. Replace new-flow temp-file requests/results with a single-use bounded authenticated local pipe; authenticate peer process IDs and executable identity, restrict its ACL to the initiating user, reject replay/unknown fields/duplicate properties/oversize/stale messages. Do not use CurrentUserOnly across UAC: Microsoft documents that Windows also compares elevation level. Do not route new requests through the legacy generic list-of-arguments worker. Retain that old entry point for compatibility and record its limits separately.

The snapshot must distinguish observed active state from persistent intent: all relevant addresses/prefixes/origins/DAD state, DHCP policy, ordered IPv4 DNS values and source, default routes/metrics and persistent routes, and evidence that policy/alternate configuration has been classified. Unknown is blocking, never equivalent to an empty list. Store a private local recovery journal before mutation, independently of project JSON and lossy legacy history. If this write fails, abort. Never store device URL/project text in the journal. Record final observation and outcome separately; final-journal failure does not imply network apply failed or did nothing.

Sequential execution is non-atomic. A command failure/timeout stops remaining commands; always attempt bounded reread. Cancellation before execution changes nothing; after execution begins it stops at a safe command boundary, rereads and reports possible changes. Do not kill an elevated worker and assume no mutation. Explicit recovery requires another diff/confirmation and identity/current-state recheck, then verifies restored policy/configuration. DHCP recovery restores policy, not a historical lease. Browser failure/TLS never invokes recovery. Crash recovery starts as read-only journal display; it never applies on startup.

## Verification and review sequence

Implementation details: DNS registration is captured and supplied explicitly on DNS changes; ambiguous registration, finite manual lifetimes/nonstandard origins and non-restorable address/gateway state block. Initial snapshots require one preferred usable address. Static leave-unchanged from automatic DNS also blocks. Verification polls up to 10 times at 500 ms intervals within 30 seconds; native reads time out at 20 seconds, commands at 10 seconds. Cancellation stops at worker permission checks; it does not undo commands or immediately stop an approved in-flight stage.

Journal: `%LOCALAPPDATA%/NetworkProfileManager/apply-recovery`, private create-new/flushed files, separate final outcome, explicit read cap 1000 with an error and no pruning. Same-instance mutation lease and per-NIC new-worker serialization bound reentry. Unconfirmed worker exit inhibits further mutation in the initiating instance. External edits/other legacy instances and final NIC check-to-index-command races remain non-atomic. Full evidence: [review](../testing/PHASE04_REVIEW.md).

1. Contract/coordinator fixtures independent of Windows. Check no browser on failures, actual-state mismatch, partial mutation, UAC denial, cancellation, concurrent requests and persistence failure.
2. Owner support-boundary decision before dependent production mutation logic.
3. Windows observation/typed worker and transport fixtures with fake processes, tampering and unsupported-state rejection. Consult authoritative Microsoft sources for exact policy/routes/DNS behavior; ambiguous classification blocks.
4. Localized UI, immutable diff and recovery surface in DESIGN.md style. Isolated WPF harness only; never instantiate actual App startup.
5. Focused security/concurrency/code review, full safe unit regression/build/planning validation; record exact results and NOT RUN limits. Local scoped commits on main. No push, live tests or Phase 05.

## Source evidence consulted

- [.NET/Windows netsh interface reference](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/netsh-interface): explicit address/DNS sources, gateway and persistence parameters. Documentation alone does not prove recovery on this machine.
- [Get-NetIPAddress](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-netipaddress): address state and policy store are distinct query dimensions.
- [Set-DnsClientServerAddress](https://learn.microsoft.com/en-us/powershell/module/dnsclient/set-dnsclientserveraddress): resetting server addresses uses DHCP defaults; static-IP/automatic-DNS behavior is not certified by this source.

Runtime target remains .NET 8/WPF; no dependency upgrade, schema migration or new network probe.

Additional primary sources consulted: [route policy stores](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-netroute), [PipeOptions elevation checks](https://learn.microsoft.com/dotnet/api/system.io.pipes.pipeoptions), [pipe security](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipe-security-and-access-rights), [NETWORK denial for local pipes](https://learn.microsoft.com/windows/win32/ipc/named-pipes), [client PID](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getnamedpipeclientprocessid), [server PID](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getnamedpipeserverprocessid), [string CIM InterfaceGuid](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/legacy/hh968170%28v%3Dvs.85%29), and [manual lifetime representation](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-netipaddress). Native integration remains NOT RUN; documentation does not replace isolated Windows evidence.
