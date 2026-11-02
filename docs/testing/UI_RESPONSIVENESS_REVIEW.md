# UI responsiveness investigation — NT-PERF-01, 2026-11-02

## Scope and authorization

Owner reported slow Network tests opening, unreadable white preset cards and the main window disappearing on the tool's caption close. Owner clarified the process stays in the tray, rejected multi-second readiness delays and requested searching throughout the application for similar defects. Phase 07 remains AWAITING_REVIEW, work directly on main. No live probes, scans, listener, network mutation, UAC, installation, publication or phase advancement.

Initial source was 3085ec6; final isolated validation uses committed profile follow-up de02b22 plus only the six implementation/test paths belonging to this task. Concurrent device-modal/source changes are preserved and excluded. Resolve the containing implementation commit with `git log -1 --format=%H -- app/Services/WindowsQuickAdapterDiagnostics.cs`.

Exact initial baseline: **3085ec67728f4011bd48365fc251967097926d50**. Newer committed base: **de02b22a6db4ef2762fa687037213adc21f6d175**.

## Confirmed cause and correction

Every diagnostic opening awaited one Windows PowerShell process that sequentially enumerated adapters, addresses, IP interfaces, DNS, routes, hardware and bindings, then re-read adapter identities. This took about five seconds on this workstation. It was already asynchronous, but the view model disabled all selection until the entire inventory completed; merely moving an already asynchronous call to another thread would not remove the readiness wait.

`WindowsQuickAdapterDiagnostics` now supplies initial GUID/index/MAC, link, DHCP, DNS, IPv4 prefix/DAD/**SkipAsSource** and active route-only metrics using .NET local interface properties and the read-only IP Helper tables. It runs on a worker. Unsupported IPv4 interfaces are skipped only in this initial IPv4 catalogue and remain available through the complete background inventory. Hardware kind, physical state and Hyper-V binding start explicitly unknown; names/interface type never certify topology. No dependency, persistent cache or stored format is added.

The existing bounded 20-second PowerShell inventory remains for complete hidden/hardware/Hyper-V facts, as a separate background operation. It does not gate selecting an adapter/source or running a test. Fresh Refresh cancels/invalidate older detail work; disposal drops late results. Detail completion preserves only an explicitly selected GUID/index/MAC and still-eligible source, clears a removed/changed/filtered identity without auto-selecting another, and defers publication until an in-flight immutable test finishes. Actual WPF ComboBox rebinding is covered, including the reentrant SelectedItem-clearing path. Existing before/after ICMP/TCP binding and route checks remain unchanged.

The preset list used an explicit FrameworkElement visibility style without inheriting the themed ListBox style. During loading/testing its disabled native template painted an opaque white backing under light foreground text. The actual screenshot defect was reproduced, then corrected by inheriting the existing ListBox style. No palette, layout or visual redesign.

## Measurements and evidence

Explicit passive local inventory profiling, not part of the automated fake-boundary suites:

- Initial original-query timing: adapter 1624 ms, addresses 2421, IP interface 2449, DNS 3651, routes 3686, hardware 4015, final adapter read 4938 ms, measured from script start. Process startup is additional. Only timing/output-length metadata was printed; adapter values were withheld.
- Corrected native path: **63.5 / 49.7 / 76.4 ms** over three sequential calls, six initial IPv4 interfaces. Same-run PowerShell path: **5114.6 ms**, 17 complete inventory entries. These are local-provider timings, not a universal cold-launch or device-response guarantee.
- Sequential comparison matched five GUID/index/MAC identities; **0 address / 0 route / 0 DNS differing adapters** for those matches. Not an atomic snapshot, not certification of other hardware/Windows builds. Initial and full catalogue counts intentionally differ while optional hidden-interface facts load.
- First native profiling attempt exposed `GetIPv4Properties` on an IPv6-only interface (10043); fixed by checking `Supports(IPv4)` before reading IPv4 properties. No network configuration changed.
- Original product plus the new disabled-list assertion produced the expected failure: `Disabled preset list falls back to opaque white in Dark theme.` Prior screenshot and failing reproduction retained under ignored `out/NetworkTestsStartupScope/` and `TestResults/NetworkTestsStartup/`.
- Initial shared-tree build failed in concurrently introduced, unrelated `DeviceModalTests.Dialogs` interface implementations. No unrelated source/test was repaired for this task. Final checks use a git-archive export of committed de02b22 with owned paths only.

Commands use the existing private SDK: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`. Final working directory is `out/DiagnosticPerfFinal/source/`. Only the local profiling command reads real local interface facts; no profiling mode sends packets or starts App/tray services.

| Command | Outcome |
|---|---|
| `$sdk build AVNetworkToolkit.sln -c Release --no-restore` | PASS, 0 warnings/errors |
| `$sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-restore` | PASS, 0 warnings/errors |
| `$sdk test test/NetworkProfileManager.Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=full.trx' --results-directory TestResults/DiagnosticPerf` | **969 PASS / 0 failed / 0 skipped**, 28 seconds; 23 task-specific cases |
| `scripts/ProjectUiSmoke/bin/Release/net10.0-windows/ProjectUiSmoke.exe TestResults/DiagnosticPerf/Startup --network-tests-startup` | **16 captures PASS / 0 binding errors**; delayed initial/optional reads, background readiness/dispatcher heartbeat, source retention, Dark/Light normal/minimum, native caption close with production-equivalent close-to-tray handler |
| Same EXE `TestResults/DiagnosticPerf/Phase07 --phase07`, then repeat `out/DiagnosticPerfFinal/Phase07Repeat --phase07` | Initial run stopped at existing keyboard-order assertion in Light minimum after 51 captures. One justified full repeat **64 captures PASS / 0 binding errors**. No assertion removed/weakened; initial focus failure's cause remains UNVERIFIED on the shared interactive desktop |
| Same EXE `out/DiagnosticPerfScope/LocalTiming --measure-diagnostic-inventory` in the first scoped export | Passive timings/comparison above; explicit diagnostic only, never called by normal fixture modes |

17 early focused cases passed; six additional native cancellation/eligibility cases are included in the 969 final full count. Automated service/UI checks use fake interfaces/routes/probes/sockets/storage, except for the separately labelled read-only profiling command. Dark normal background-ready screenshot was visually inspected: readable themed cards, enabled explicit source controls and Run while optional inventory remains blocked.

Primary Windows contracts checked: [unicast address row/DAD/SkipAsSource](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/ns-netioapi-mib_unicastipaddress_row), [GetUnicastIpAddressTable](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getunicastipaddresstable), [GetIpForwardTable2](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getipforwardtable2), [route table padding](https://learn.microsoft.com/en-us/windows/win32/api/netioapi/ns-netioapi-mib_ipforward_table2). Native buffers are released in finally, table counts bounded, and ABI/first-entry padding covered without calling the host in unit tests.

## Application-wide search: remaining findings

These are source-confirmed synchronous paths and performance risks. **Their user-visible duration is UNVERIFIED**; do not attribute the measured five seconds to every window. No unrelated feature was silently rewritten.

| ID / priority | Location and trigger | Evidence / smallest follow-up |
|---|---|---|
| PERF-02 / high | MainViewModel constructor and Refresh; MainWindow post-apply callback | Constructor calls Refresh synchronously. It enumerates interfaces then reads/parses every adapter's profiles/history on the UI thread, including existing rows on every refresh. Defer initial enumeration until the window can paint; split worker-owned immutable reads from dispatcher publication, deduplicate refreshes, preserve drafts/selection/apply guards. |
| PERF-03 / high | IpScannerViewModel field setters and SaveSettings | Every StartIp/EndIp/timeout/options change synchronously saves settings. PreservedObjectFile performs locking, current-file hash/read validation, durable temporary write and File.Replace with a fresh retained backup. TextBox fields use PropertyChanged updates: typing can cause a full transaction per character. Coalesce writes with an orderly close flush, revision/conflict protection and error feedback. Never remove atomic storage/retention to gain speed. |
| PERF-04 / medium | ProjectApplyWindow.Create / WindowsProjectApplyObservation.EnumerateAsync | Apply and Link-Local opening await a PowerShell adapter enumeration before target selection. The window can paint and cancellation exists, but readiness still depends on process/module startup. Use a quick identity catalogue; retain strict fresh full preparation/worker/result verification before mutation. Do not cache away safety checks. |
| PERF-05 / medium | ProjectViewModel.Open/Save, MainViewModel import/export, MdnsDiscoveryWindow.CsvDialog, IpScannerViewModel.ExportCsv | File read/schema validation/hashing or serialization/durable writes occur in synchronous UI commands. Slow/large/removable/network-path files can block the interface. Move immutable file work to an owned asynchronous operation and keep dirty drafts, dialogs, overwrite consent and conflict revision checks on the dispatcher. No actual personal file or slow-share benchmark performed. |
| PERF-06 / medium | App.OnStartup → StorageStartup.Run → UserStorage.OpenLocked | Root selection, legacy inventory/hash/divergence and consent/migration happen before normal window creation. These checks are necessary for choosing the correct data root; asynchronously present an honest preparation/recovery surface while preserving first-use consent and fail-closed integrity. No personal data was copied or migration policy changed. |
| PERF-07 / lower | AppSettingsStore.Get and WindowLayoutStore / MainWindow.Layout.SaveLayout | Settings Get re-reads/parses/hashes the object per lookup; hiding/closing the main window durably writes changed layout. Usually small, but delayed storage can affect startup/close. Measure on synthetic slow storage before choosing a bounded coherent cache/coalescing strategy; preserve external-revision conflict handling. |
| PERF-08 / open | Owner-reported Network tests caption closes/hides main | Owner confirms tray icon remains; this is main-window hiding, not established process termination. Both caption automation and actual non-client messages in isolated real HWNDs keep main visible, including a production-equivalent main Closing→Hide handler, blocked reads, running/draining probes and maximized child. Real Portable UI attempt could not click a hidden window (`coordinate input geometry is unavailable`), then owner stopped Computer Use with Escape. No real-child click succeeded. **Cause/fix UNVERIFIED**, do not claim repaired or replace tray behavior. |

Other inspected surfaces: mDNS interface reading already uses Task.Run; update network/download/extraction work is asynchronous; scanner probes/reverse-DNS waits run inside the existing scan worker, not its initial window constructor. Tray popup rebuilding uses the in-memory adapter/profile list. The retired-worker/update-helper GetResult calls run before normal UI in dedicated process modes, so are not evidence of normal window freezes. Secondary close handlers contain no Owner.Close/Owner.Hide or generic app-shutdown calls. A search found no second instance of the same explicit unbased preset ListBox style; this does not certify every theme/state or native caption path.

## Review and limits

Implementing-agent code/concurrency review traced source eligibility, native ABI, UI publication ordering, stale refreshes, close cancellation, filter invalidation and immutable in-flight requests. No independent-review claim. Driver/API latency cannot be universally bounded by these samples; the native read remains off the dispatcher. Full topology enrichment can still take seconds **in the background**, and native network egress/UAC/recovery/high-DPI/owner acceptance remains separate.

Next safe steps: owner reviews the scoped result; PERF-02/03 are the next bounded fixes, followed by PERF-04/05. PERF-08 needs an observed real-window sequence. No numbered phase completion, release, install or live network approval is implied.

Final planning/schema/formats/references validation of the staged scoped documentation: **950 links PASS**, tooling **12 PASS**, whitespace PASS. An earlier isolated documentation copy accidentally included a concurrently appended device-contract link without that other task's file and failed 2 validator cases; corrected by exporting staged scoped blobs, without changing/weakening validation or importing the unrelated contract. The first commit attempt lacked configured author identity; no configuration was changed, and the existing repository author identity is used only for the scoped commit command.

## Exact local portable result

Source **dadb8bba5b1e1ad78c2b33c1fc6fd5756cbab380**, exported with git archive before packaging; concurrent uncommitted device UI is excluded. Command: private SDK publish of the exported app project, Release, win-x64, self-contained true, DebugType=none, DebugSymbols=false, PublishReadyToRun=false, SourceRevisionId=dadb8bba5b1e1ad78c2b33c1fc6fd5756cbab380, InformationalVersion=1.1.6-local.diagnostic-ready, ReleaseChannel=Preview, output out/DiagnosticReady-dadb8bb/Portable. Publish PASS without warnings/errors; MIT license and existing third-party notices included.

scripts/test_package_self_test.ps1 PASS: hidden WPF/JSON self-test, create-only report, malformed arguments and retired-worker rejections, no ordinary App startup or network action. Payload confirms 1.1.6.0, informational version 1.1.6-local.diagnostic-ready+dadb8bba5b1e1ad78c2b33c1fc6fd5756cbab380, .NET 10.0.14, Preview, no production updater key. This is a local candidate, not native feature/phase acceptance.

Ready EXE: out/DiagnosticReady-dadb8bb/Portable/NetworkProfileManager.exe (keep adjacent DLLs). Archive: out/DiagnosticReady-dadb8bb/AVNetworkToolkit-1.1.6-local-diagnostic-ready-dadb8bb-portable.zip; **8 extracted entries hash-identical**, ZIP SHA-256 **5a3579f3d8a74fbf70ff32eb63b05220dc5d298389da2c619181f2829dc8647e**. Old out/ApipaSwitchFix-e299418 and the running instance are unchanged; no host installation or public release. Owner can exit the old instance through tray Exit, then review the new local copy. Application-wide PERF-02–08 findings remain open.
