# Phase 05 — manual Link-Local implementation contract

2026-10-31, Asia/Tashkent. Owner explicitly requested «Начинай фазу 5 на новом чате». Base: main `48913b21b7a4ddf62a9f58663fbfcde79d630a29`. Execution is authorized; live tests, push, release and Phase 06 are not.

## Minimal architecture and compatibility

Use the Phase 04 coordinator, typed worker, identity checks, mutation lease, local authenticated pipe, private recovery journal and bounded observation. Add a manual Link-Local action to the existing application and reuse its review/recovery window. No browser is part of this action. No new generic elevated runner, dependency, project format or legacy history migration.

Project JSON v1 remains byte/semantics compatible: only static/DHCP profiles; link-local intent is transient and is not saved into project or legacy slots. Schema, example and strict unknown/future/duplicate/orphan rejection remain unchanged. A future portable link-local profile requires a separately reviewed format decision.

## Address and conflict contract

User enters an address or deliberately requests a random candidate. Accept canonical `169.254.1.0` through `169.254.254.255`, inclusive, exactly /16, no gateway. Generation does not assign, probe or establish availability. Replacing an existing ordinary gateway is visible in old/new review and confirmation.

After confirmation/UAC and before assignment, re-read the full supported pre-state and selected identity. Conflict checking uses Windows duplicate address detection (DAD) during assignment on that exact indexed interface, followed by bounded observation. The confirmation explicitly says checking happens during assignment and may leave a changed/duplicate address. There is no separate availability promise. Ordinary ARP resolution is not used as an off-subnet availability test: its no-reply errors can also mean an unreachable destination. No ping, subnet scan, route insertion or fallback to another adapter.

Require a connected interface and positively observed enabled IPv4 DAD; never enable DAD implicitly. After assignment require preferred manual address in active/persistent state, /16, no gateway, expected DNS and DAD still enabled. Duplicate, tentative, missing or unknown observations are not success. No automatic address retry or rollback. DAD cannot guarantee absence of silent/offline hosts or future conflicts; actual driver/packet behavior remains UNVERIFIED until isolated testing. This is manual configuration, not an implementation of the RFC 3927 automatic allocation/defence protocol.

## DNS and supported source state — owner-approved ADR-022

Default: preserve a known static ordered DNS list and registration flags without DNS commands. Automatic DNS cannot silently become static or be erased. When the source uses automatic DNS, require an explicitly entered static list (1–2 ordinary IPv4 DNS servers), shown in the diff; retain the original automatic policy in the recovery journal. Empty DNS input means preserve, never reset. Static automatic DNS and unclassified/managed DNS remain blocked under ADR-019. No unproved “empty static DNS” representation is introduced.

Owner approved «Утверждаю явный ввод статических DNS при автоматическом DNS» on 2026-10-31 before dependent implementation.

Support a single fully classified ordinary IPv4 or persistent preferred manual link-local /16, no extra addresses/custom routes/alternate DHCP, and Phase 04 DNS/route/lifetime constraints. Automatic APIPA/no-lease state is displayed distinctly but mutation from it remains blocked until its origins/policy/recovery can be proved in isolated Windows evidence. This is an explicit support limitation, not an empty snapshot or synthetic static address.

## Recovery, display and acceptance

Deliberate restore uses the original private snapshot and a fresh current diff/confirmation; manual link-local snapshots restore as manual link-local and repeat conflict/DAD checks. Deliberate return to DHCP resets IPv4/DNS to automatic; verify a usable preferred DHCP lease under existing semantics. APIPA/no lease remains unverified, never internet/connectivity success. No browser or unsolicited recovery. Keep partial/cancel/UAC/timeout/journal errors and busy/close/tray guards consistent with Phase 04.

An expected partial state is manual link-local with the old automatic DNS policy when DAD fails before explicit DNS replacement. Only explicit recovery **to DHCP** may leave this fully classified single-address/no-gateway state. Static/Link-Local targets, unknown/managed policy, extra addresses/routes and using this partial state as a restorable original remain blocked. The original successful pre-change snapshot is retained; recovery never promises to recreate unsupported static/automatic DNS. The recovery attempt's own transient snapshot remains readable as evidence.

The local snapshot adds optional `Dad` evidence (transmissions/connected); older journals without it remain readable without rewriting. Unknown DAD blocks a new Link-Local assignment. Worker protocol remains local version 1, same-executable authenticated peers only; older binaries may reject newer journal fields and are not a downgrade recovery reader. No project schema or customer-data migration.

Remove APIPA hiding from the reduced adapter display, including disconnected rows and selected-adapter facts. Label DHCP-enabled link-local separately from manual link-local; unknown DHCP classification remains unknown. The reduced view cannot prove APIPA rather than alternate DHCP configuration, so it names both possibilities. The reduced display never authorizes mutation or replaces the full recovery snapshot. Preserve IPv6, unrelated NICs and all legacy mutation/storage semantics.

Automated acceptance uses fake OS/DAD, process, elevation and browser boundaries. Cover range endpoints/reserved blocks, unknown/conflict states, identity/configuration changes, DAD disabled/duplicate/tentative/delay, DNS preservation/explicit replacement/recovery, journal failure, replay, cancellation, partial mutation and display fixtures. Extend isolated WPF Light/Dark normal/minimum renders without actual App startup. Real Windows ARP/netsh/DAD/UAC/recovery and isolated adapter acceptance: NOT RUN until separately approved.

## Primary source evidence

- [RFC 3927](https://www.rfc-editor.org/rfc/rfc3927.html): /16 selection range/reserved portions, ARP probing and limitations; manual operation here does not claim full automatic protocol compliance.
- [SendARP](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-sendarp) and [ResolveIpNetEntry2](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-resolveipnetentry2): resolution errors can mean unreachable/off-subnet destinations; a hand-written preflight ARP path was rejected.
- [CreateUnicastIpAddressEntry](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-createunicastipaddressentry): Windows performs asynchronous DAD; command/API return alone does not establish a usable address.
- [Get-NetIPAddress](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-netipaddress), [Get-NetIPInterface](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-netipinterface), [New-NetIPAddress](https://learn.microsoft.com/en-us/powershell/module/nettcpip/new-netipaddress): address origins/states, DAD configuration and usability after enabled DAD completes.
- [netsh interface](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/netsh-interface): fixed IPv4 address/mask/gateway/persistence and DNS source commands; documentation is not local integration evidence.
