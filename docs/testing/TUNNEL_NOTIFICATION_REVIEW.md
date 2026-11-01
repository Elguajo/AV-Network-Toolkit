# Tunnel notification correction — NOTIFY-01

2026-11-01, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, inspected `0507357`; owner explicitly requested fixing repeated Connection established / Teredo Tunneling Pseudo-Interface notifications. Scoped commit SHA: `git log -1 --format=%H -- docs/testing/TUNNEL_NOTIFICATION_REVIEW.md`.

## Cause and correction

NetworkService.GetAdapters excludes Loopback and Tunnel. NetworkMonitorService.TakeSnapshot excluded only Loopback, so tunnel Up/Down transitions produced ordinary connection balloons even though those interfaces were absent from the profile list. Add the same Tunnel exclusion before reading IP properties or storing state. Filtering uses the OS interface type, not display name. No Windows Teredo disable/removal, arbitrary throttling, new setting, adapter-list visibility change or IPv6/network mutation.

Keep public construction, NetworkChange subscription/unsubscription, snapshot comparison and real tray notification behavior. An internal constructor injects the interface source and notification sink; CheckForChanges shares the exact event-handler path. Tests never instantiate NotifyIcon, subscribe to real network events or enumerate host interfaces. Ethernet, Wi-Fi and ordinary virtual Ethernet retain initial-silent/up/down/IP-change behavior; an Ethernet adapter with Teredo in its name remains monitored. System-filter/other adapter notification policy is unchanged.

## Observed verification

PowerShell; `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"` (10.0.403).

| Command | Result |
|---|---|
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/MonitorFix/ -p:IntermediateOutputPath=obj/MonitorFix/ --filter 'FullyQualifiedName~NetworkMonitorServiceTests' --logger 'trx;LogFileName=monitor-focused.trx' --results-directory TestResults/MonitorFix --nologo` | 8 PASS / 0 failed / 0 skipped |
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/MonitorFix/ -p:IntermediateOutputPath=obj/MonitorFix/ --nologo` | PASS, 0 warnings/errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --no-restore -p:OutputPath=bin/MonitorFix/ -p:IntermediateOutputPath=obj/MonitorFix/ --logger 'trx;LogFileName=monitor-full.trx' --results-directory TestResults/MonitorFix --nologo` | 744 PASS / 0 failed / 0 skipped |
| `& $sdk test TestResults/MonitorNegative/test/NetworkProfileManager.Tests.csproj --configuration Release --filter 'FullyQualifiedName~NetworkMonitorServiceTests' --logger 'trx;LogFileName=monitor-negative.trx' --results-directory TestResults/MonitorFix/Negative --nologo` | Expected exit 1: 3 FAIL / 5 PASS; repeated Teredo/renamed-tunnel notifications and unwanted tunnel message in mixed Ethernet snapshot reproduced |

Negative control is an independent local git-archive export of app/test/schema/legal/global.json plus current monitor/test sources, with exactly the Tunnel continue line removed. Working tree was unchanged. The three failures assert actual notification output, not merely a helper predicate. Tests cover five repeated tunnel status/address cycles, loopback, ordinary interfaces, unchanged-state deduplication and simultaneous tunnel/Ethernet transitions. Initial test compilation failed because fake IP property/address classes omitted abstract members; completed them using the existing fixture convention, then all focused/full checks passed. No test/assertion suppressed.

Self-review: the changed runtime rule is limited to the existing excluded interface type, independent of names or IPv4 presence; existing event/diff policy is retained. No native type/API signature or production dependency change. Full UI rendering NOT RUN for this service-only change. Host Teredo behavior, native balloon delivery and real network events remain UNVERIFIED; no traffic, OS settings/UAC, App startup, install, release or push performed. Existing concurrency/stack-failure/lifecycle behavior is outside this bounded filter fix; no broader monitor hardening claimed.

Published 1.1.5/installed or running applications are unchanged. Phase 07 remains AWAITING_REVIEW; DATA-01/Phase 08 and owner/native acceptance gates remain.
