# AV Network Toolkit user guide

This guide describes the current .NET 10 source and Preview workflow. A published package may be older than `main`; check its release notes before relying on a later fix. Start with the [README](../README.md) for an overview and screenshots.

## Installation and updates

Download a published Preview from [GitHub Releases](https://github.com/Elguajo/AV-Network-Toolkit/releases).

- **Installer:** use the `*-win-x64-setup.exe` asset. Setup requests administrator permission, installs to Program Files and adds a Start menu shortcut and uninstaller.
- **Portable ZIP:** extract the entire archive into a dedicated folder, then run `NetworkProfileManager.exe`. Keep all accompanying files, including the five WPF native DLLs, beside it.
- Both formats include the required runtime and have the same application features. You do not need a separate .NET/SDK installation to run them.

Current .NET 10 builds follow Microsoft's [supported Windows versions](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md#windows). Ordinary Windows 10 22H2 is not listed. An included runtime does not make an unsupported Windows edition supported. The earlier v1.1.1 packages remain on .NET 8; their original artifacts are unchanged.

Binaries are unsigned. Download the package and `SHA256SUMS.txt` from the same release, then compute the package's hash:

```powershell
Get-FileHash -LiteralPath .\AVNetworkToolkit-1.1.3-win-x64-setup.exe -Algorithm SHA256
```

Compare the complete hash with the matching filename in `SHA256SUMS.txt`. Use the filename for the version and format you downloaded. `package-manifest.json` also identifies the source commit and package hashes; it is not a code-signing certificate.

Before updating, back up the [data folders and projects](#data-and-backups), then exit using the tray's Exit action. The installer rejects a busy application instead of force-closing it. Uninstall retains user data. Updates remain manual in 1.1.6 because production signing trust is not configured; ZIP distribution does not make settings USB-local.

Current source adds **Settings → Application updates**. A build needs a configured trusted publisher key before it can offer automatic updates; otherwise use GitHub Releases manually. Checking once a day is opt-in, Preview inclusion is explicit, and download and installation are separate actions. The installer flow exits the app after draft/active-operation checks, then opens the interactive installer; start the app again after completion or canceled UAC. Portable preparation creates a new version folder and shows its EXE path, retaining the old copy and shared data. Stop scanner monitoring and finish network changes before updating. The first updater-enabled version must be installed manually. [Local verification and remaining signing/native gates](testing/APPLICATION_UPDATE_REVIEW.md).

## Optional device passwords in current source

Available in 1.1.6 source/packages, not earlier 1.1.5 downloads. In Device interfaces, select a device, enable **Save password in project file**, enter the password and use **Keep device in project** or **Save project file**. The first password-bearing save requires a new, unused filename for project format v2; the original v1 remains unchanged. Older application versions cannot open v2.

Passwords are hidden on opening. **Show / Hide** reveals the current draft temporarily; **Copy** copies only after that separate action. **Remove** stops saving the password after Keep/Save. Clearing the input alone produces a validation error and preserves the saved password. A saved v2 file stays v2 after removal. Passwords may contain spaces and Unicode, up to 1024 characters; empty/control-bearing values are rejected without trimming or silent truncation.

Anyone with the JSON file, its temporary save payload, backup or another copy can read stored passwords. Save asks for confirmation; removal does not erase old backups. Copy attempts conditional cleanup after 30 seconds and on exit, preserving later clipboard content. Windows clipboard history and other apps may retain copies. No automatic login or password-bearing URL is created. [Contract, local evidence and native clipboard limits](testing/DEVICE_PASSWORD_REVIEW.md).

## Local and project profiles

Both profile scopes describe the **computer's adapter configuration**, not the device's own address.

| Scope | Stored where | Use it for |
|---|---|---|
| **This computer** | Per-user app data, per adapter; up to five local slots | Frequent laptop configurations and tray shortcuts |
| **Project profiles** | The project JSON file you explicitly open/save | Profiles and equipment links that travel with an AV project |

Select the connected adapter before applying either kind of profile. A project adapter hint does not silently choose a NIC on another computer. The highlighted **Selected** profile matches the adapter's reported static fields; it is not an edit selection or a reachability test.

Local JSON import/export and project Open/Save are separate workflows. Saving a project does not replace local profiles. Keep a profile in the editor, then save the project file to persist it. Use Save As in the project menu for another destination.

The [example project](../examples/project.avnet.json) illustrates the [v1 format](../schemas/project.avnet.schema.json). Its addresses are invented documentation examples, not a ready-to-apply network plan.

## Device web interfaces

In Device interfaces, create or edit an entry, keep it in the project, then save the project. Entries can retain HTTP/HTTPS URLs with ports, paths, queries and fragments and can reference a project profile. Device IP and other descriptive fields are optional where the form allows them.

| Action | Effect |
|---|---|
| **Open Web Interface** | Opens the saved URL in the default browser; does not apply adapter settings |
| **Apply & Open** | Reviews and applies the linked profile to the deliberately selected adapter, verifies supported configuration, then opens the link |
| **Apply on a project profile** | Applies the profile through review/confirmation without requiring a device or opening a browser |

Opening a project or selecting an entry does not change the network. A failed or unverified Apply & Open does not launch the browser. Configuration verification does not prove the device is reachable.

Only HTTP/HTTPS links are accepted. Unsafe URL schemes and embedded URL credentials are rejected. Device-password fields, automatic login and certificate-validation bypass are not implemented. Avoid putting credentials or tokens in URL paths/query strings or shared project files.

## Applying settings and recovery

The application normally runs as the signed-in user. A deliberate network change requests administrator permission through Windows UAC. Review the named adapter and current/requested values before confirming, and keep local access plus an independent recovery method.

The current local/manual/tray and project flows share a conservative support contract:

| State or field | Behavior |
|---|---|
| Supported single usable IPv4 and classified configuration | Review, confirmation, original snapshot, apply and observed verification |
| DHCP with usable lease → static | Requires a supported source and explicit replacement DNS when necessary |
| Empty static gateway | Explicitly removes the supported default gateway; review shows the change |
| Empty DNS in the local/manual static flow | Leaves DNS unchanged; does not erase it. Project static-DNS profiles require valid explicit DNS |
| Secondary DNS without primary | Rejected |
| No usable IPv4, APIPA/no lease, multiple addresses, /31–/32, noncontiguous mask | Blocked; profile/draft remains editable |
| Static automatic DNS, managed/unknown DNS, complex routes or unknown persistent state | Blocked before mutation |

See the [exact compatibility contract](spec/AUDIT_FIXES_CONTRACT.md) for supported transitions. Do not bypass a refusal to apply to a production adapter.

New local history identifies the exact private recovery journal, including the supported original DNS policy. Recovery requires a new deliberate review and confirmation. A missing, invalid or wrong-adapter journal blocks full recovery. Older history explicitly offers only its limited recorded state or DHCP policy, not complete historical restoration.

Recovery is **best effort, non-atomic and can fail**. Restoring DHCP restores policy, not the previous lease. Preserve journals and backups until you no longer need them. Never treat the old Revert history as a complete backup of arbitrary DNS, routes or additional addresses.

## Diagnostics and discovery

Opening or expanding a diagnostic tool does not start a test. Choose the interface/source and destination, then use its explicit start action.

| Tool | Purpose and boundary |
|---|---|
| **Ping** | Four-request, continuous or single-request ICMP; explicit adapter/source IPv4, target, Run and Stop |
| **TCP port check** | One source-bound IPv4 connection to one specified port; no port-range scan or application payload |
| **IP Scanner** | Deliberately started ARP/ICMP/TCP-assisted discovery; choose its scan range/options |
| **mDNS** | Passive selected-interface IPv4 listening, advertised A/AAAA details, filtering and CSV export; no query sender |
| **Adapter facts** | Link, addresses, DHCP/DNS, routes and Hyper-V information; optional diagnostic uplink filtering |
| **Link-Local** | Deliberate manual IPv4 Link-Local assignment with review, Windows DAD-aware verification and recovery |

Source binding and Windows route prediction do not prove physical packet egress. A Ping failure does not prove a device is offline; a successful TCP connection does not validate HTTP/TLS. Hostnames use Windows DNS and may query another interface; literal IPv4 avoids that lookup. mDNS advertisements are untrusted observations, and an empty list does not prove there are no devices.

Confirmed system filters and idle built-in WAN/debug entries are hidden from the main/tray adapter list by default. Open **… (Adapter list options)** beside Refresh in the Network Adapters header and enable **Show all adapters** to reveal the existing underlying list; it does not disable, remove or configure a Windows interface. Working virtual, disconnected and unknown adapters remain available. If a selected adapter disappears or is hidden, choose its replacement explicitly.

DOC-R01 was traced to timing in the automated smoke check: it sampled the send count before WPF finished hiding the window. The synchronized check now verifies cancellation, completion and no restart on Show for both windows; application code is unchanged. [Investigation and limits](testing/DIAGNOSTIC_HIDE_REVIEW.md). Real-network/manual acceptance remains pending; see the [release limitations](releases/v1.1.3.md#known-limitations).

## Data and backups

Published 1.1.5 uses these locations:

| Data | Location |
|---|---|
| Local profiles, history, scanner and theme/language settings | `%LOCALAPPDATA%\NetworkProfileManager\data` |
| Main-window/sidebar layout (1.1.5+) | `%LOCALAPPDATA%\NetworkProfileManager\data\window_layout.json` |
| Private apply/recovery journals | `%LOCALAPPDATA%\NetworkProfileManager\apply-recovery` |
| Project JSON | The file/folder you explicitly choose |

Back up all relevant locations before updating. Journals contain private network configuration; projects contain equipment links and addresses. Remove sensitive data before sharing examples or bug-report attachments.

Current local profile/history saves use atomic replacement and unique backups. Corrupt files/backups are preserved; repair or restore them before retrying. Refresh after a save conflict; Add/Edit keeps a draft open if saving fails. Recovery lists preserve damaged files while showing healthy entries and provide additional pages beyond 1000 journal files.

Version 1.1.6 adds an explicit first-use copy to `%USERPROFILE%\.av-network-toolkit\data` and `apply-recovery`, with a separate verified snapshot in `%USERPROFILE%\.av-network-toolkit-backups\<transaction-id>`. Originals remain intact. Exit all other copies before migration and run the first migration as an ordinary Windows user. An unsupported/conflicting/unreadable home root stops startup with Retry/Exit; it is never replaced with an empty folder. A changed old folder requires explicit acknowledgement, without merging either copy. [Exact contract and verified limits](testing/DATA_STORAGE_MIGRATION_REVIEW.md).

Older builds continue using LocalAppData and cannot see later home edits. Explicit project files/exports and update-download cache are not moved. Migration requires an NTFS target; USB-local data is not implemented. Earlier 1.1.5 packages retain the old storage behavior. Backups, partial copies and journals can grow; manage retention manually and securely. Installer and ZIP share the per-user root rather than storing settings beside the executable.

## Verification and support

Published Previews are not completed native/phase acceptance. Unit/fake checks, real WPF component rendering and disposable Windows Server package/runtime-discovery checks provide different kinds of evidence. They do not certify pristine Windows-client installation, cross-version upgrades, every NIC/driver, real UAC/IPC/DAD/recovery, multicast/packet egress or manual accessibility/scaling.

Use the [test matrix](testing/TEST_MATRIX.md), [packaging report](testing/PACKAGING_REVIEW.md) and [release notes](releases/v1.1.3.md) for exact results and remaining limits. A temporary DHCP server and device-password storage are not shipped.

For reproducible bugs, open an [issue](https://github.com/Elguajo/AV-Network-Toolkit/issues/new) with the app/Windows versions, steps and expected/observed results using sanitized data. Use [private reporting](../SECURITY.md) for vulnerabilities.
