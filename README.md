# AV Network Toolkit

<div align="center">
  <img src="app/Assets/logo.png" width="160" alt="AV Network Toolkit cyan and blue ribbon logo">
  <br><br>
  <a href="https://github.com/Elguajo/AV-Network-Toolkit/actions/workflows/build.yml">
    <img src="https://github.com/Elguajo/AV-Network-Toolkit/actions/workflows/build.yml/badge.svg" alt="Build">
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT">
  </a>
</div>

<br>

<div>
Windows network profiles and project-scoped device web interfaces for AV field work. Select an adapter, review the computer's current settings, deliberately apply a saved profile, then open the equipment interface when needed.

**1.1.1 Preview:** includes the fork's project/device workflows, diagnostics, new logo and self-contained Windows packages. Automated checks do not replace isolated hardware acceptance; read the [release limitations](docs/releases/v1.1.1.md#limitations) before changing a working network.
</div>

![AV Network Toolkit in dark theme: selected Ethernet adapter, current settings and saved network profiles](docs/screenshots/readme/main-dark.png)

*Select an adapter, review its current configuration and choose a saved profile. [Explore the screenshots below](#screenshots).*

## Download and start

Get both formats from [Releases](https://github.com/Elguajo/AV-Network-Toolkit/releases):

| Package | Use |
|---|---|
| `AVNetworkToolkit-1.1.1-win-x64-setup.exe` | Primary installer: Program Files, Start menu shortcut and uninstaller. Installation requests administrator permission. |
| `AVNetworkToolkit-1.1.1-win-x64-portable.zip` | Additional option: extract the entire ZIP into a dedicated folder, then run `NetworkProfileManager.exe`. Keep all five WPF DLLs beside it. |

Both include .NET: users do not need to install .NET or the SDK separately. Target platform is x64 Windows 10/11; hosted package verification uses Windows Server 2022 with global .NET discovery disabled and a negative control, not a complete Windows-client/hardware certification. Binaries are currently unsigned; Windows may show an unknown-publisher warning. Download only artifacts whose source and SHA-256 you trust. Check `SHA256SUMS.txt` with PowerShell `Get-FileHash`.

The application runs unelevated by default. Applying network changes is a separate deliberate operation that requests administrator rights through UAC. Opening a project or selecting a device does not apply settings.

## Main workflows

- **Computer profiles:** retain named per-adapter static/DHCP profiles, IP/mask/gateway/primary+secondary DNS, five local slots, JSON import/export and existing scanner/history/tray tools.
- **Project profiles:** independent versioned project JSON, explicit Open/Save/Save As, multiple profiles and adapter hints. Project and local stores remain separate; hints do not silently select a NIC on another computer.
- **Device interfaces:** save/search/group device entries and exact HTTP/HTTPS deep links, associate a profile, then choose Open or Apply & Open. Unsafe schemes and URL credentials are rejected. Password fields are not implemented in v1.
- **Apply & Open / standalone project Apply:** target the selected adapter, review old/requested values, confirm, verify observed configuration and optionally open the device. Complex or unknown affected state is blocked. Recovery uses an explicit new review/confirmation; it is best effort, not atomic rollback.
- **Network tests:** choose four-request Ping, Continuous Ping, single Ping or one-port TCP check. Adapter/source and destination are explicit. Run/Stop controls and a separate test window are available. Route prediction/source binding do not prove actual physical egress; a TCP connection does not validate HTTP/TLS.
- **AV tools:** manual Link-Local with DAD-aware verification/recovery, passive selected-interface IPv4 mDNS listener, adapter/Hyper-V facts and optional diagnostic uplink filtering. Every live test/listener is explicit.

## Screenshots

These are actual WPF views rendered from the current source with the new logo and invented demonstration data. Network, browser and elevation operations are replaced by test doubles; the images show the interface, not live-network acceptance. Click an image to view it at full size. [Capture source and verification](docs/screenshots/readme/README.md).

### Project profiles · light theme

Keep project profiles separate from this computer's local profiles. Each Apply action names the selected adapter; the matching current configuration is marked Selected.

![Light-theme workspace with project profiles, static and DHCP presets, and explicit Apply to Ethernet actions](docs/screenshots/readme/project-light.png)

### Device web interfaces

Keep equipment links in the project, search the directory and preserve full URLs, including ports, paths and fragments. Open Web Interface and Apply & Open are separate actions.

![Device directory with a saved touch-panel deep URL and its associated network profile](docs/screenshots/readme/devices-dark.png)

### Network tests

Choose four-request Ping, Continuous Ping, single Ping or a TCP port check, then explicitly select the adapter, source IPv4 and destination. The separate window includes descriptions and Run/Stop controls.

![Network tests window with descriptive Ping and TCP presets, explicit source address and TCP port](docs/screenshots/readme/network-tests-dark.png)

<details>
<summary>More screenshots: profile editor, Apply & Open and mDNS discovery</summary>

### Profile editor

Edit the computer's static IP, prefix, gateway and DNS policy in a separate dialog. Keeping a profile in the project does not apply it to Windows.

![Project network profile editor with static IPv4 and explicit DNS settings](docs/screenshots/readme/profile-editor-dark.png)

### Apply & Open review

Review the selected adapter's observed and requested settings before deliberately confirming a change.

![Apply and Open review window with current and requested adapter settings and a separate Confirm changes button](docs/screenshots/readme/apply-review-light.png)

### Passive mDNS discovery

Select an interface and start listening explicitly. Inspect advertised services, addresses, ports and TTL, then optionally export filtered CSV results.

![Light-theme mDNS discovery window showing synthetic advertised services and explicit Stop listening and CSV export actions](docs/screenshots/readme/mdns-light.png)

</details>

## Data and updates

Profiles, history, scanner and theme/language settings currently remain under:

```text
%LOCALAPPDATA%\NetworkProfileManager\data\
```

Project JSON stays in the folder you explicitly choose. New-flow private recovery journals are under `%LOCALAPPDATA%\NetworkProfileManager\apply-recovery` and may contain private network configuration. Back up these folders and your projects before updating.

Exit the application through its tray Exit action before replacing/installing files. The installer does not force-close a busy app or remove user data on uninstall. Portable updates replace application files only; the ZIP does not make settings USB-local. There is no automatic updater. A future move to `%USERPROFILE%\.av-network-toolkit` is approved but **not implemented in this release**.

Legacy history does not snapshot every DNS/route/additional-address setting. Do not treat its Revert action as a complete restore of arbitrary adapter configurations. The new project recovery path also blocks unclassified state instead of inventing missing settings.

## Verification and limits

The [packaging report](docs/testing/PACKAGING_REVIEW.md), [test matrix](docs/testing/TEST_MATRIX.md), [JSON regression evidence](docs/testing/JSON_VALIDATION_REVIEW.md) and [release notes](docs/releases/v1.1.1.md) distinguish unit/fake, WPF, packaged-host and native evidence. GitHub CI runs isolated unit tests, schema/planning checks and package installation/runtime-discovery checks on disposable Windows runners.

Real NIC/DAD/UAC/IPC/multicast/route/ICMP/TCP behavior, actual packet egress, clean Windows-client deployment, cross-version installer upgrades and manual accessibility remain separate acceptance work. No temporary DHCP server or device-password storage is shipped. Static automatic DNS, /31–/32, initial DHCP without a usable lease/APIPA and complex/managed settings remain conservatively blocked for the new apply flow. Do not bypass these checks to configure a production network.

## Development

Use the .NET 8 SDK selected by `global.json`; runtime is bundled only in published packages, not ordinary build output.

```powershell
git clone https://github.com/Elguajo/AV-Network-Toolkit.git
cd AV-Network-Toolkit
dotnet build AVNetworkToolkit.sln -c Release
dotnet test test/NetworkProfileManager.Tests.csproj -c Release --no-build
```

Build both packages from a clean checkout with PowerShell 7:

```powershell
./scripts/build_packages.ps1
```

The script downloads and verifies pinned Inno Setup 6.7.4 build tools, publishes a self-contained win-x64 payload, executes the non-network package self-test and creates installer/ZIP plus a source/hash manifest under a fresh `out/packages` directory. A repeat build needs a fresh output directory. Native DLLs stay beside the executable instead of being extracted to a user-writable temporary location. See [packaging contract](docs/spec/PACKAGING_CONTRACT.md).

`app/` contains the .NET 8 WPF/MVVM application, `test/` its isolated xUnit suite, `schemas/` and `examples/` the project format, `scripts/` build/verification tools and `packaging/` installer configuration. Internal namespace/executable identities remain `NetworkProfileManager` for compatibility. [Contributing](CONTRIBUTING.md), [security reporting](SECURITY.md), [roadmap](ROADMAP.md).

## License and provenance

Forked from [Network Profile Manager](https://github.com/rmzncakir/network-profile-changer). [MIT license](LICENSE), copyright 2026 rmzncakir, retained unchanged; [third-party notices](docs/legal/THIRD_PARTY_NOTICES.md). The new logo was supplied by the owner. AV Network Toolbag is a behavior reference only; no source code, artwork or UX assets from that unlicensed repository are copied into this product.
