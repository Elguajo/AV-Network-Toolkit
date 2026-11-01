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

## Local corrected build after continuing notifications

2026-11-01, NOTIFY-02. Owner supplied repeated **Connection lost / Teredo Tunneling Pseudo-Interface** screenshots. Existing c580604 excludes Tunnel before either up or down notification; its repeated-transition regression already covers both directions. Published 1.1.5 is source 2b78f67 and lacks the fix. No NetworkProfileManager.exe or other matching product process was found through read-only Win32_Process inspection, so the screenshot's actual binary and host transition cause remain UNVERIFIED. No further runtime code change was justified.

Prepared a local self-contained Windows x64 candidate from clean main **c580604b74b63c5f2b6e6ea5c676edc896dfae9d**, without modifying app version, installed files, release/tag/assets or OS/network settings:

- EXE: `D:\01_DEV\AV-Network-Toolkit\out\TeredoFix-c580604\payload\NetworkProfileManager.exe`.
- ZIP: `out/TeredoFix-c580604/AVNetworkToolkit-1.1.5-local-TeredoFix-c580604-win-x64-portable.zip`.
- Identity: **1.1.5.0**, informational **1.1.5+c580604b74b63c5f2b6e6ea5c676edc896dfae9d**, bundled **.NET 10.0.14**. The local folder/ZIP name and informational SHA distinguish it from published 1.1.5; this is not a new release.
- EXE SHA-256: `79DB966F7AEE6D93218D3D7934C8DAD40C732D248B50C5A70359CB8A9E804606`.
- ZIP SHA-256: `E9322962D5A9080F6F2F87E86D031D0D861F090B4AE905F023E184B3A6228055`.
- Local machine-readable evidence: `out/TeredoFix-c580604/local-build-manifest.json`, `SHA256SUMS.txt`, `TestResults/TeredoFix-c580604`.

PowerShell, same private `$sdk` as above:

| Command / check | Observed result |
|---|---|
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/MonitorFix/ -p:IntermediateOutputPath=obj/MonitorFix/ --filter 'FullyQualifiedName~NetworkMonitorServiceTests' --logger 'trx;LogFileName=monitor-rebuilt.trx' --results-directory TestResults/TeredoFix-c580604 --nologo` | Current source rebuilt; 8 PASS / 0 failed / 0 skipped. Earlier no-build focused invocation also 8 PASS. |
| `& $sdk publish app/NetworkProfileManager.csproj -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -p:PublishReadyToRun=false -p:SourceRevisionId=c580604b74b63c5f2b6e6ea5c676edc896dfae9d -o out/TeredoFix-c580604/payload` | PASS; all eight expected files present after LICENSE copied, no compiler warning/error reported. |
| `scripts/test_package_self_test.ps1 -Executable out/TeredoFix-c580604/payload/NetworkProfileManager.exe -ResultsDirectory TestResults/TeredoFix-c580604/PayloadSelfTest` | Script PASS: WPF/JSON, report preservation, three malformed invocation cases and four retired-worker rejections; no normal app/network startup. An outer ad hoc `$LASTEXITCODE` assertion then falsely reported failure because the script intentionally tests exit 1. Script assertions and success report confirmed PASS; no check suppressed or product repair needed. |
| Compress/expand ZIP; SHA-256 compare every original/extracted file and file counts | PASS, all 8 files identical; original and extracted reports agree on exact source/version/runtime. |
| `scripts/test_package_self_test.ps1 -Executable TestResults/TeredoFix-c580604/Extracted/NetworkProfileManager.exe -ResultsDirectory TestResults/TeredoFix-c580604/ZipSelfTest` | PASS, same complete diagnostics; process exit 0. |

Full unit suite NOT RUN again for this packaging-only work; previous 744 result remains attributed to NOTIFY-01 above. No installer generated/run, normal app launched, host network enumerated/scanned/changed, real tray delivery tested or GitHub publication performed. Native confirmation requires the owner to exit any older copy through its tray menu and run this exact EXE, keeping all payload files together. Existing saved profiles/settings retain their established locations. Phase 07/owner/native gates remain unchanged.
