# AV Network Toolkit

<div align="center">
  <img src="app/Assets/logo.png" width="128" alt="AV Network Toolkit cyan and blue ribbon logo">
  <p><strong>Network profiles. Device interfaces. Field diagnostics.</strong></p>
  <p>A Windows workspace for the laptop you bring to an AV installation.</p>
  <p>
    <a href="https://github.com/Elguajo/AV-Network-Toolkit/actions/workflows/build.yml"><img src="https://github.com/Elguajo/AV-Network-Toolkit/actions/workflows/build.yml/badge.svg" alt="Build status"></a>
    <img src="https://img.shields.io/badge/platform-Windows%20x64-0078D4" alt="Platform: Windows x64">
    <a href="https://github.com/Elguajo/AV-Network-Toolkit/releases"><img src="https://img.shields.io/badge/status-Preview-E6A23C" alt="Release status: Preview"></a>
    <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-2EA44F" alt="License: MIT"></a>
  </p>
  <p>
    <a href="https://github.com/Elguajo/AV-Network-Toolkit/releases"><strong>Download Preview</strong></a> ·
    <a href="#quick-start">Quick start</a> ·
    <a href="#features">Features</a> ·
    <a href="#screenshots">Screenshots</a> ·
    <a href="#documentation">Documentation</a>
  </p>
</div>

AV work often means moving one laptop between a control processor, a DSP, a video wall and the office network. AV Network Toolkit keeps the computer's network presets and the equipment's web interfaces together: select an adapter, choose a profile and open the device you need.

![AV Network Toolkit dark workspace showing the selected Ethernet adapter, current settings and saved profiles](docs/screenshots/readme/main-dark.png)

> **Preview software.** Hardware and recovery acceptance is still in progress. Review the [known limitations](#status-and-limitations) before using it on a working network.

## Why AV Network Toolkit?

- **Take the project with you.** Store network profiles and device links in a project JSON file you can move between computers.
- **Keep computer settings and equipment addresses clear.** Profiles describe this laptop's adapter; device entries describe the equipment you want to reach.
- **Choose what changes.** Open a device's web interface, apply a network profile, or deliberately combine the two with Apply & Open.
- **Check the connection in the same workspace.** Use Ping, a TCP port check, IP scanning, passive mDNS discovery and adapter facts without opening a command shell.

## Download

Choose a published Preview from [GitHub Releases](https://github.com/Elguajo/AV-Network-Toolkit/releases). Each release includes its changes, known issues, checksums and source identity. `main` can contain changes newer than the downloads.

| Package | Best for | Start here |
|---|---|---|
| **Installer** — `*-win-x64-setup.exe` | Regular use on a service laptop | Install to Program Files, then launch from the Start menu. Setup requests administrator permission. |
| **Portable ZIP** — `*-win-x64-portable.zip` | Field work and quick launch | Extract the **entire** archive, then run `NetworkProfileManager.exe`. Keep its accompanying files together. |

Both packages include the required .NET runtime; no separate .NET or SDK installation is needed. They provide the same application features. The portable package still uses the computer's per-user data folder.

**Platform:** Windows x64. Current .NET 10 builds follow Microsoft's [supported Windows versions](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md#windows); ordinary Windows 10 22H2 is not listed. Bundling the runtime does not extend OS support. The interface is English, with Light and Dark themes.

Binaries are unsigned. Verify the download against the release's `SHA256SUMS.txt` before running it; see [installation and updates](docs/USER_GUIDE.md#installation-and-updates).

## Quick start

1. **Select the adapter connected to your equipment.** Check its name and current IP, subnet, gateway and DNS in the workspace.
2. **Create a network profile.** Use **This computer** for local per-adapter presets, or **Project profiles** for presets saved with an AV project. Enter the address the **computer** should use.
3. **Apply deliberately.** Use the adapter-named Apply action, review current and requested settings, then confirm. Windows requests elevation for the change; unsupported configurations are blocked with a reason.
4. **Add the equipment.** In **Device interfaces**, save its HTTP/HTTPS URL and optionally associate a project profile. Keep the entry, then save the project.
5. **Open or diagnose.** Use **Open Web Interface** to open the link. Choose **Apply & Open** when the associated profile should be reviewed and applied first, or open **Network tests** to check a specific destination.

Opening a project, selecting a device or expanding Network tests does not apply settings or send diagnostic probes. Scans, listeners and tests have their own explicit start actions.

Explore the [example project](examples/project.avnet.json) to see the file structure. Its addresses are documentation examples; replace them with your actual plan before applying a profile. See the [user guide](docs/USER_GUIDE.md) for profile scopes, diagnostics and recovery.

## Features

| Area | What you can do |
|---|---|
| **Network profiles** | Save static IPv4 or DHCP presets, subnet/mask, gateway and primary/secondary DNS. Keep up to five local profiles per adapter; import/export local profiles as JSON. |
| **AV projects** | Keep multiple profiles and device entries in a separate versioned JSON file. Open, Save and Save As without replacing the local profile store. |
| **Device interfaces** | Search and group equipment links. Preserve full HTTP/HTTPS URLs, including ports, paths, queries and fragments; optionally link a network profile. |
| **Apply and recovery** | Review a named adapter's current/requested state, confirm changes and verify the observed result. Supported original settings are journaled for separately confirmed recovery. |
| **Network tests** | Run four-request, continuous or single-request Ping, or a TCP check against one port. Choose the adapter/source IPv4 explicitly; use the inline panel or an independent window. |
| **Discovery** | Scan with ARP/ICMP/TCP options, or explicitly start a passive IPv4 mDNS listener and filter/export its advertised services. |
| **Adapter tools** | Inspect link, IPv4, DNS, routes and Hyper-V facts. Hide confirmed system-list clutter, reveal it with Show all, and optionally filter Hyper-V uplinks in diagnostic selectors. |
| **Desktop workflow** | Use tray profile shortcuts, history/revert and Light/Dark themes. Applying settings remains a deliberate operation with UAC. |

## Screenshots

Select an image for the full-size view. These WPF component captures use invented data and test doubles; they show the interface, not live-network results. They predate the later adapter-list and theme fixes. [Exact source and capture notes](docs/screenshots/readme/README.md).

| Project profiles · Light | Device interfaces · Dark |
|---|---|
| [![Light workspace with project profiles and explicit adapter Apply actions](docs/screenshots/readme/project-light.png)](docs/screenshots/readme/project-light.png) | [![Device directory with a touch-panel deep URL and project profile association](docs/screenshots/readme/devices-dark.png)](docs/screenshots/readme/devices-dark.png) |

<details>
<summary>Profile editor, Apply & Open, network tests and mDNS</summary>

| Profile editor | Apply & Open review |
|---|---|
| [![Static IPv4 project profile editor with gateway and DNS fields](docs/screenshots/readme/profile-editor-dark.png)](docs/screenshots/readme/profile-editor-dark.png) | [![Current and requested adapter configuration with a separate confirmation action](docs/screenshots/readme/apply-review-light.png)](docs/screenshots/readme/apply-review-light.png) |

| Ping and TCP tests | Passive mDNS discovery |
|---|---|
| [![Network test presets with explicit adapter, source IPv4, destination and TCP port](docs/screenshots/readme/network-tests-dark.png)](docs/screenshots/readme/network-tests-dark.png) | [![mDNS service results with addresses, ports, TTL and export and stop actions](docs/screenshots/readme/mdns-light.png)](docs/screenshots/readme/mdns-light.png) |

</details>

## Status and limitations

The current source targets **1.1.7 Preview**, including the completed profile/diagnostic/device follow-ups and themed unsaved-change confirmation. [Version notes](docs/releases/v1.1.7.md) describe compatibility; the [1.1.7 release review](docs/testing/RELEASE_1.1.7_REVIEW.md) records publication and exact-source verification. Read the version notes and [changelog](CHANGELOG.md) before upgrading.

- **Network safety:** complex or unknown adapter configurations are blocked. Recovery is best effort and can fail; older history does not contain a complete original snapshot. Keep an independent way to restore the adapter. [Supported states and field semantics](docs/USER_GUIDE.md#applying-settings-and-recovery).
- **Acceptance:** automated checks and hosted package tests do not certify personal-data migration, real NIC/UAC/DAD, physical packet egress, multicast, clean-client deployment or manual accessibility. Native DPI was checked at 100%; actual 150%/200% and mixed-monitor acceptance remain pending. [Known issues](docs/releases/v1.1.6.md#known-limitations).
- **Scope:** no temporary DHCP server, automatic login, TLS bypass or USB-local data. The update interface is included, but automatic checks/download/installation remain disabled until production signing trust and native acceptance are configured.

Home-data migration requires explicit consent, verified copies, retained originals and independent backups. Older builds keep using their separate LocalAppData dataset. [Behavior, compatibility and verification limits](docs/testing/DATA_STORAGE_MIGRATION_REVIEW.md).

Optional device passwords are plaintext in project JSON and backups. The first password save creates a new v2 file and preserves v1; older builds cannot open v2. [Behavior, compatibility and verification limits](docs/testing/DEVICE_PASSWORD_REVIEW.md).

## Documentation

| Start with | For |
|---|---|
| [User guide](docs/USER_GUIDE.md) | Installation, local versus project profiles, device links, diagnostics, data and recovery |
| [Release notes](docs/releases/v1.1.6.md) · [Changelog](CHANGELOG.md) | Version changes, compatibility and known issues |
| [Roadmap](ROADMAP.md) | Planned capabilities and acceptance status |
| [Test matrix](docs/testing/TEST_MATRIX.md) · [Packaging report](docs/testing/PACKAGING_REVIEW.md) | What was checked and the limits of that evidence |
| [Project schema](schemas/project.avnet.schema.json) · [Example](examples/project.avnet.json) | Project JSON format |
| [Contributing](CONTRIBUTING.md) · [Security reporting](SECURITY.md) | Development contributions and private vulnerability reports |

## Build from source

Use Windows and the .NET 10 SDK selected by [global.json](global.json). Published packages include the runtime; a source checkout requires the SDK.

```powershell
git clone https://github.com/Elguajo/AV-Network-Toolkit.git
cd AV-Network-Toolkit
dotnet build AVNetworkToolkit.sln -c Release
dotnet test test/NetworkProfileManager.Tests.csproj -c Release --no-build
```

To build the installer and portable ZIP, run `./scripts/build_packages.ps1` in PowerShell 7 from a clean checkout. It verifies the pinned Inno Setup compiler and writes both packages, checksums and a source manifest to a fresh `out/packages` directory. [Packaging contract](docs/spec/PACKAGING_CONTRACT.md).

The application is C# / WPF with MVVM. Source lives in `app/`, isolated tests in `test/`, project formats in `schemas/` and `examples/`, and build tools in `scripts/` and `packaging/`. The executable and internal namespace retain `NetworkProfileManager` for data compatibility.

## Contribute

[Report a bug](https://github.com/Elguajo/AV-Network-Toolkit/issues/new) with the app version, Windows version, reproduction steps and expected/observed behavior. Use invented addresses and remove private project data from attachments. Send vulnerabilities through [private security reporting](SECURITY.md), not public issues.

For code changes, start with [CONTRIBUTING.md](CONTRIBUTING.md). Tests must use fake networking; real adapter changes, probes and listeners require an explicitly approved isolated environment.

## License and credits

[MIT licensed](LICENSE). Based on [Network Profile Manager](https://github.com/rmzncakir/network-profile-changer), copyright 2026 rmzncakir; original attribution is retained. [Third-party notices](docs/legal/THIRD_PARTY_NOTICES.md) accompany the application. The logo was supplied by the project owner.

AV Network Toolbag informed desired behaviors only. Its unlicensed source, resources and artwork are not copied into this project.
