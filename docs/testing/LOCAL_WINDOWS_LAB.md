# Local Windows lab preparation — REL-02 / ADR-046

2026-11-01 Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, application source `ba44a525f51ff8a36e67f8d303178d65a29148b1`. Phase 07 remains AWAITING_REVIEW. This is environment preparation, not native application acceptance.

## Authorization and scope

Owner requested installing everything needed for live tests on this computer, then explicitly chose a full independent copy of the existing Windows 10 VMware VM. Preparation, cloning, VM isolation, snapshots and guest boot are authorized. Actual profile mutation, probes, browser/device actions, multicast, DHCP, host firewall/network changes and phase acceptance remain separate gates. ADR-037's historical lack of a lab is superseded only by the preparation evidence below.

The initial checkout had only an unrelated untracked `.serena/` directory. It was preserved. No application, dependency declaration, schema, lockfile or production network configuration changed.

## Installed and reused tools

- Host: Windows 11 Pro x64, build 26100, approximately 32 GB RAM. Existing hypervisor active; full Hyper-V role disabled. No Hyper-V feature, host virtual switch, NAT, DHCP service, adapter, metric, route or firewall setting was changed.
- Existing VMware Workstation **17.0.2 build 21581411**, vmrun and VMware services were reused. No host virtualization upgrade or reboot performed.
- Installed **.NET SDK 8.0.427**, including WindowsDesktop runtime **8.0.33**, in `%LOCALAPPDATA%\AVNetworkToolkit.BuildTools\dotnet`, matching `global.json`. Used Microsoft's `https://dot.net/v1/dotnet-install.ps1` with `-Version 8.0.427 -Architecture x64 -InstallDir <tool-directory> -NoPath`. Installer script SHA-256 `E8B873E18A81E5C4CD8AB69D84DAC8FEAD291D50B3C44633CD7FDDAD709A13D6`; installed dotnet.exe Authenticode signature Valid, Microsoft .NET publisher. Global .NET/PATH were not replaced.
- Existing Python **3.13.13** reused; created `%LOCALAPPDATA%\AVNetworkToolkit.BuildTools\python` venv and installed exact `scripts/requirements-validation.txt` requirement `jsonschema[format]==4.26.0` and its dependencies. `pip check` PASS.
- Existing Windows PowerShell 5.1, PowerShell 7, NetAdapter/NetTCPIP/DnsClient cmdlets, netsh and pktmon found. Capture, drivers and guest availability are not inferred from host command discovery. No capture/scanner/DHCP process started by this task.

## Private local artifacts

Lab root: `D:\00_TOOLS\AVNetworkToolkit-Lab` (outside Git and outside the source VM's OneDrive directory).

- `Windows10-REL02\Windows10-REL02.vmx`: full independent clone, not a linked clone; 2 vCPU, 4096 MB RAM, original 60 GB sparse disk capacity. Original VM remains suspended and was never started. Original VMX/VMSD SHA-256 checks matched after cloning and guest boot. Raw source identifiers, VM files, logs and screenshots are local only, not repository evidence.
- Ethernet 0 uses a dedicated private VMware LAN segment (`connectionType = "pvn"`), with `startConnected = "FALSE"`. No bridged/NAT/host-only test connection. Segment packet behavior and guest adapter baseline remain UNVERIFIED until approved native preparation/observation.
- USB passthrough, shared clipboard and drag/drop disabled in the clone. Removed inherited writable whole-disk shares. Only `ToolkitPayload` remains, read-only, mapped to the lab's Payload folder. Guest access still requires verification; do not claim all guest/host communication is disabled.
- Powered-off checkpoints `REL02-Isolated-BeforeBoot` and final `REL02-Prepared-Offline` created and listed successfully. Final clone shut down gracefully; `vmrun list` reports zero running VMs. VM files occupy approximately 17.12 GB. Actual checkpoint recovery has NOT RUN; existence is not a recovery proof.
- `Payload`: self-contained win-x64 app at the source above, complete native dependencies/runtime, MIT/third-party notices, existing example project and the existing read-only adapter diagnostic query extracted unchanged. EXE SHA-256 `A4EDAD5E2C2EA0EF84251F564CA0CDF673D1736A4F1435F7B764981AE1F066F0`. Not the published release asset and not independently advertised as a new release.
- `Open Test Lab.lnk`, `Enter-ToolkitEnvironment.ps1`, local README and private hashes. Shortcut opens the console without running app/network tests. Environment helper exposes the private SDK/venv only to its process; observed SDK/Python versions match above.

## Observed verification

Commands below used `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"` and `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`.

| Command / check | Observed result |
|---|---|
| `& $sdk --info`; dotnet.exe Authenticode | SDK 8.0.427, .NET/WindowsDesktop 8.0.33; Valid Microsoft signature |
| `& $py -m pip check` | PASS, no broken requirements |
| `& $sdk build AVNetworkToolkit.sln --configuration Release --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --logger 'trx;LogFileName=local-lab-preparation.trx' --results-directory TestResults/LocalLabPreparation --nologo` | **681 PASS / 0 failed / 0 skipped** |
| `& $sdk publish app/NetworkProfileManager.csproj --configuration Release --runtime win-x64 --self-contained true --output D:/00_TOOLS/AVNetworkToolkit-Lab/Payload --nologo` | PASS; complete guest payload staged |
| `& ./scripts/test_package_self_test.ps1 -Executable D:/00_TOOLS/AVNetworkToolkit-Lab/Payload/NetworkProfileManager.exe -ResultsDirectory TestResults/LocalLabPreparation/PackageSelfTest` | PASS WPF/JSON/runtime load, create-only report and 3 malformed invocation checks; no ordinary startup/network |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py`; `git diff --check` | Final real-tree PASS, 9 phases / 49 stable tasks / **380 links**, **12 tooling PASS** and whitespace PASS |
| `scripts/validate_apply_observation.ps1` and `scripts/validate_adapter_diagnostics.ps1`, each in PowerShell 7 and Windows PowerShell 5.1 | PASS: **11 + 15 fake-query fixtures in each shell**, parsing/allowlists; no actual OS network query/probe/mutation |
| `vmrun -T ws clone <source> <clone> full -cloneName=...` | PASS full independent clone |
| `vmrun -T ws snapshot <clone> REL02-Isolated-BeforeBoot`; `listSnapshots <clone>` | PASS, checkpoint listed |
| `vmrun -T ws start <clone> nogui`; `checkToolsState <clone>`; console inspection | Guest starts; VMware Tools reports **running**; actual Windows desktop visible after restoring original 3D setting. Guest driver log identifies Windows build **19045** |

Unit TRX SHA-256: `CC679F4BCB1CA1F9F0CAF6897E2D4E7A3C90785BBC9C714179856F59E7217F27`. Final source VMX/VMSD hashes again matched after graceful clone shutdown and both checkpoint creations.

A wrapper incorrectly inspected stale `$LASTEXITCODE` after the successful PowerShell package self-test, reporting a wrapper failure; the script's assertions and retained `success.json` passed. Corrected the wrapper continuation without suppressing or changing the self-test. First console capture showed a black guest screen. Gracefully stopped the clone and restored its original `mks.enable3d = "TRUE"` setting before restarting; subsequent console inspection confirmed the actual Windows desktop. This sequence does not prove the exact cause of the first black screen. Guest input through the computer-use helper did not produce an observable Explorer/focus change; payload access/copy is therefore UNVERIFIED. No production application repair or authentication bypass inferred.

## Remaining gates

Guest payload access, independent checkpoint recovery and supported complete guest NIC baseline must be verified before mutations. Guest desktop was observed without entering credentials. No guest credentials collected/stored or authentication bypass attempted. A separate isolated peer/device is needed for meaningful remote ICMP/TCP/mDNS/browser cases. This clone is not clean-Windows/no-.NET, Windows 11 guest, physical/USB/Wi-Fi, Hyper-V, hardware unplug or packet-egress acceptance. Normal App startup, UAC/IPC, Apply & Open, restore, Link-Local/DAD, all real probes and multicast tests remain **NOT RUN**. No DHCP server, Phase 08, publication or phase completion authorized.
