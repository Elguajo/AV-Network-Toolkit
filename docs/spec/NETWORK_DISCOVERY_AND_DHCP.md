# AV networking features — Phases 05–08

## Link-Local

- Explicit action for IPv4 Link-Local `169.254.0.0/16` local-interface connectivity; cannot be routed through a gateway.
- Generated/selected address must satisfy Windows accepted-address rules, account for reserved portions of the block and check for conflicts using suitable methods. ICMP ping alone is not proof of availability.
- Preserve previous configuration before assignment. Return to DHCP or restore saved state with deliberate action.
- Do not change IPv6 or unrelated NICs.

Phase 05 implementation: [exact support/DNS/DAD/recovery contract](PHASE05_IMPLEMENTATION_CONTRACT.md), authorized by ADR-021/022. Transient manual action, project JSON v1 unchanged. Conflict detection uses enabled Windows DAD **during assignment**; no pre-assignment availability guarantee or full RFC 3927 automatic-allocation claim. An address suggestion is local only. Automatic APIPA/no-lease source and complex/unknown state remain blocked, while their observed addresses remain visible. Actual Windows/lab acceptance remains NOT RUN until separately approved.

## mDNS

- Explicit bind to the selected interface, not to all interfaces by accident; explicit start/stop.
- mDNS normally uses UDP 5353 multicast; show service type, host, resolved address(es), port and TTL when available. Data is untrusted.
- Bound memory/packet processing; handle malformed packets and service expiry; sanitize export fields to avoid spreadsheet formula injection.
- 'Add discovered device' should create a **draft** requiring user confirmation, not automatically open/route/apply.

## Diagnostics

- Distinguish physical Ethernet vs Wi-Fi/USB/virtual/Hyper-V adapters and expose friendly name plus stable OS identity.
- The selected adapter for editing must remain explicit; hidden adapters stay configurable only by intentional re-selection.
- Report current link, IPv4, prefix, gateway, DNS, DHCP status and routing with clear evidence provenance.
- Avoid an inaccurate green 'online' from one successful probe or a failed ping interpreted as 'offline'.

## Temporary DHCP server — separate high-risk gate

- Intended only for controlled, isolated AV lab networks, **never** normal production/corporate LANs.
- Bind to one verified selected interface; explicit confirmation and active-service UI indicator; no background auto-start.
- Validate network/pool, reservations/excluded addresses, lease range, router and DNS options. Avoid serving to the host's own address or a live foreign DHCP server.
- Detection of another DHCP server can be incomplete: lack of a detected server is **not** proof of absence. Confirm physical isolation with the user.
- Restrict scope: no DHCP relay/router, no changes to other interfaces, no unauthorized firewall changes. On stop/crash, cleanup or recover firewall rules and sockets safely.
- QA: dedicated switch / isolated clients, packet capture, conflict test, full teardown. Block release until reviewed.
- Independent implementation only; no AV Toolbag source reuse without license/permission.
