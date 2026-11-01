# README screenshot provenance

Captured on 2026-11-01 from https://github.com/Elguajo/AV-Network-Toolkit, branch `main`, source `bfc978c6305a2bd5d2d09424d29d7fad4e0a4b1c`. Final captures use an isolated `git archive` export of that exact commit: concurrent application edits appeared during the initial working-tree capture and were preserved/excluded. Application source is unchanged by this documentation task. Images are unchanged PNG copies from the existing isolated WPF component harness, not generated concepts or full-desktop captures. The harness composites the application's background behind WPF controls; native Mica is not certified.

All addresses, adapter identities, project names and device URLs are synthetic fixtures. IPv4 examples use documentation ranges; the mDNS IPv6 address is also a fixture. No real adapter enumeration/mutation, probes, multicast receiver, UAC prompt or browser launch was performed. The current owner-supplied icon is included in main-window captures.

## Reproduce

From the repository root in PowerShell, using the established private SDK:

```powershell
$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"
git archive --format=zip --output=TestResults/ReadmeScreenshots/source-bfc978c.zip bfc978c6305a2bd5d2d09424d29d7fad4e0a4b1c
Expand-Archive -LiteralPath TestResults/ReadmeScreenshots/source-bfc978c.zip -DestinationPath TestResults/ReadmeScreenshots/PinnedSource
& $sdk build TestResults/ReadmeScreenshots/PinnedSource/scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release -p:OutputPath=bin/ReadmeScreenshots/
& $sdk TestResults/ReadmeScreenshots/PinnedSource/scripts/ProjectUiSmoke/bin/ReadmeScreenshots/ProjectUiSmoke.dll TestResults/ReadmeScreenshots/PinnedWorkspace
& $sdk TestResults/ReadmeScreenshots/PinnedSource/scripts/ProjectUiSmoke/bin/ReadmeScreenshots/ProjectUiSmoke.dll TestResults/ReadmeScreenshots/PinnedDiagnostics --phase07
```

Build: **PASS, 0 warnings / 0 errors**. Workspace run: **PASS, 128 captures / 0 WPF binding errors**. Diagnostics run: **FAIL** at `DiagnosticsSmoke.cs:201`, `Hidden diagnostic window continued sending`, after 9 captures, both in the initial working-tree run and the independently pinned source run. The selected TCP-ready image was captured before that assertion; no successful full diagnostics run is claimed. Cause and product impact are **UNVERIFIED**; tracked as DOC-R01 in STATUS.md for a separate investigation. No harness or application change was made to suppress the failure. Reproduction needs fresh output directories; the commands assume the parent output directory already exists.

| README image | Harness PNG under `TestResults/ReadmeScreenshots/` |
|---|---|
| [main-dark.png](main-dark.png) | `PinnedWorkspace/workspace-adapter-Dark-1200.png` |
| [project-light.png](project-light.png) | `PinnedWorkspace/workspace-project-Light-1200.png` |
| [devices-dark.png](devices-dark.png) | `PinnedWorkspace/workspace-devices-Dark-1200.png` |
| [profile-editor-dark.png](profile-editor-dark.png) | `PinnedWorkspace/workspace-editor-Dark-1200.png` |
| [apply-review-light.png](apply-review-light.png) | `PinnedWorkspace/apply-review-Light-760.png` |
| [mdns-light.png](mdns-light.png) | `PinnedWorkspace/mdns-listening-Light-820.png` |
| [network-tests-dark.png](network-tests-dark.png) | `PinnedDiagnostics/network-tests-tcp-ready-Dark-normal.png` |

All seven copies were hash-compared with their source captures and visually inspected. These images document controls and fixture state only; they do not establish real NIC, routing, HTTP/TLS, multicast, cancellation or recovery acceptance.
