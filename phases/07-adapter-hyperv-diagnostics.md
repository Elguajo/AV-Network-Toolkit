# Phase 07 — Ethernet & Hyper-V diagnostics

- **Priority:** P1
- **Dependencies:** Phase 01, Phase 04
- **Current status:** AWAITING_REVIEW; 07-T01–07-T04 EVIDENCE_RECORDED under ADR-034 (2026-11-01)
- **Scope:** Extend diagnostics and clear Hyper-V adapter enumeration without implicit adapter changes.
- **Read first:** docs/spec/NETWORK_DISCOVERY_AND_DHCP.md

Owner explicitly started 07-T01–07-T04; no repeated start permission. [Contract](../docs/spec/PHASE07_IMPLEMENTATION_CONTRACT.md). Earlier manual acceptance and OPEN UX-R01/P05-R01 remain; no live tests, new push/release or Phase 08.

[Review/evidence](../docs/testing/PHASE07_REVIEW.md): 624 full unit PASS, 15 fake OS-query fixtures, 32 focused and 128 existing WPF captures/0 binding errors. Native/owner acceptance pending; checkboxes remain open.

## Tasks and acceptance criteria

- [ ] **07-T01** — Map physical, USB, Wi-Fi, virtual and Hyper-V adapters without false binding.
  - **Acceptance:** Unambiguous selection and stable hints; never choose adapter silently.
- [ ] **07-T02** — Add diagnostics: IPv4/DHCP/DNS/gateway/route/link status.
  - **Acceptance:** Clearly distinguish interface-derived vs probe-derived status.
- [ ] **07-T03** — Add opt-in Hyper-V uplink filtering and persistence.
  - **Acceptance:** Adapter hiding affects UI only, never silently disables interfaces.
- [ ] **07-T04** — Extend tests for enumeration/failures/performance.
  - **Acceptance:** Virtual and unplugged adapters included in fixtures; existing behavior preserved.

## Deferred Ping requirements — ADR-016

Owner requested recording these tasks for future implementation, not starting Phase 07. Keep stable IDs 07-T02 and 07-T04 and the existing nine-phase order.

- [ ] **07-T02 / Ping selection:** Let the user explicitly select Ethernet or Wi-Fi and an available source IPv4 address. Cover the main-window Ping and separate Ping window. Validate the selected adapter/address at execution; if unavailable or the requested path cannot be honored, report it without silently switching interfaces. Preserve the existing deliberate Ping action and product style; do not add a general-purpose CMD shell.
- [ ] **07-T02 / Routing visibility:** Show adapter identity, source IPv4 and route/next hop. Clearly label requested, OS-predicted and actually observed values; do not claim a source-address choice alone proves egress through the selected interface. Never disable Wi-Fi, change metrics/routes, apply profiles or rewrite IP/gateway/DNS as a side effect of Ping.
- [ ] **07-T04 / Regression evidence:** Use fake probes/routing/adapter enumeration for simultaneous Ethernet and Wi-Fi, overlapping subnets, no matching route, multiple source IPv4 addresses, unplugged/removed adapter and selection changes while Ping is running. Associate each result with its original target/adapter. Any real traffic validation requires separate owner approval and an isolated Windows environment.

Observed limitation: current `NetworkService.PingAsync(host)` and `PingWindow(host)` receive a destination only, without adapter/source binding. Owner supplied a static IPv4 address; its adapter assignment and subnet mask remain UNVERIFIED. A supplied diagnostic screenshot showed Wi-Fi selected through its gateway and `DestinationHostUnreachable`; this does not prove Ethernet or the device is unavailable. Retain only this generalized scenario, not private addresses, in repository evidence. An illustrative fixture may use Ethernet `192.0.2.7/24`, target `192.0.2.229` and Wi-Fi `198.51.100.7/24`; do not assume the owner's mask is /24.

## Phase completion gate

- [ ] Only specified scope changed; all existing functionality preserved.
- [ ] Relevant unit tests and negative tests passed; `NOT RUN` recorded where unsupported.
- [ ] Exact commit, branch, PR/diff, tests and risks recorded in `STATUS.md`.
- [ ] User reviewed the results before the next phase.
