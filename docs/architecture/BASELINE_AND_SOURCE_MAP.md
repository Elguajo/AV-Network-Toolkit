# Phase 01 baseline and source map

Audit date: 2026-10-31 (Asia/Tashkent). Code baseline: `603cf24fa0ba1819c0ec10b6a57ef5cf57c3852e`.
Repository path note (ADR-013): source links now use `app/` and `test/`; baseline facts and historical command evidence still refer to the original inspected commit.

The investigation was read-only. The user subsequently authorized documentation adoption and a local documentation-only commit. This does not accept the phase or authorize Phase 02.

## Provenance and environment — FACT

- Fork: https://github.com/Elguajo/AV-Network-Toolkit; local `origin` fetch/push points here. GitHub metadata confirmed `fork: true`, parent `rmzncakir/network-profile-changer`.
- Audit branch: `main`; adoption branch: `codex/phase-01-baseline-audit`.
- HEAD: `603cf24fa0ba1819c0ec10b6a57ef5cf57c3852e`, subject `Publish as one exe plus WPF's native DLLs, Turkish/English resources only`. Preceding commits: `c0cae2c` (Release 1.1.1), `d771b23` (tray/preset adjustment).
- [LICENSE](../../LICENSE): MIT, copyright 2026 rmzncakir, retained unchanged.
- [Application project](../../app/NetworkProfileManager.csproj): version 1.1.1, `net8.0-windows`, WPF/Windows Forms; Newtonsoft.Json 13.0.3, WPF-UI 4.2.0.
- [Test project](../../test/NetworkProfileManager.Tests.csproj): xUnit 2.5.3, Microsoft.NET.Test.Sdk 17.8.0, coverlet.collector 6.0.0; Windows/WPF target.
- Host: Windows `10.0.26300.0`, x64. `dotnet --info`: runtime 10.0.9, no SDK. A .NET 8 SDK and dependency restore are prerequisites; neither was installed/run during this work.
- Tracked/staged diffs were empty at audit start. The planning overlay was untracked; adoption first versions its related documentation and draft JSON artifacts.
- Windows 10/11 support, README's 188 tests and coverage are upstream claims, **UNVERIFIED** by execution here.
- AV Toolbag license check: `main` resolved to `0c171e296bfe819b79ec40f0caf34e41cd5751f2`, tree `6545200e345433c2e568562cf0c4df46f0af4c8d`; recursive tree returned 77 entries, not truncated, no LICENSE/COPYING/NOTICE filenames. No source/assets copied. See [license policy](../legal/LICENSE_NOTES.md).

## Source map — FACT from code, not runtime certification

| Area | Source / entry point | Observed responsibility |
|---|---|---|
| Startup | [App.xaml.cs](../../app/App.xaml.cs), `OnStartup` | Dispatch `--run-netsh` before normal UI; otherwise migration, language/theme, tray and monitor |
| UI orchestration | [MainViewModel.cs](../../app/ViewModels/MainViewModel.cs), constructor / `Refresh` | Concrete services, enumeration, per-adapter profiles/history; restore selection by name with first-adapter fallback |
| Profile model/state | [IpProfile.cs](../../app/Models/IpProfile.cs), [AdapterViewModel.cs](../../app/ViewModels/AdapterViewModel.cs), [ProfileViewModel.cs](../../app/ViewModels/ProfileViewModel.cs) | ID/name/IP/mask/gateway/DHCP/two DNS/creation time; edit validation, slots and active display |
| Profile editor | [AddProfileDialog.xaml.cs](../../app/Views/AddProfileDialog.xaml.cs), `Save_Click`; [ProfilePanel.xaml.cs](../../app/Views/ProfilePanel.xaml.cs) | Create/edit dialog returns a new `IpProfile`, including on edit |
| Persistence | [ProfileRepository.cs](../../app/Services/ProfileRepository.cs), `Load` / `Save` | JSON list, sanitized filename, save limit five; direct writes, best-effort corruption backup/reset |
| Paths | [AppPaths.cs](../../app/Helpers/AppPaths.cs), `DataDirectory` / `MigrateLegacyDataIfPresent` | LocalAppData, test override; one-shot legacy copy without deleting old files |
| Import/export | `MainViewModel.ImportProfiles` / `ExportProfiles` | User-picked JSON array; duplicate-name/validation/count filtering on import; no project schema/version |
| Apply | `MainViewModel.ApplyAsync` / `ApplyProfileAsync` / `ApplyCoreAsync` | Capture target alias/settings; batch, append history, success notification, refresh |
| OS boundary | [NetworkService.cs](../../app/Services/NetworkService.cs), `GetAdapters`, `ApplyStaticIp`, `ApplyDhcp`, `BuildDnsCommands`, `RunNetshBatch`, `RunNetsh` | IPv4 enumeration/registry fallback; validation; sequential `netsh` using `ArgumentList` |
| UAC | [ElevationHelper.cs](../../app/Helpers/ElevationHelper.cs), `RunElevated` / `RunWorker` / `ValidateCommand`; [manifest](../../app/app.manifest) | `asInvoker`, one `runas` worker per batch, temp request/result files, verb/character checks |
| History/revert | [HistoryEntry.cs](../../app/Models/HistoryEntry.cs), [HistoryRepository.cs](../../app/Services/HistoryRepository.cs), `MainViewModel.RevertAsync` | 100 entries; old IP/mask/gateway and DHCP state, no DNS snapshot |
| Scanner | [IpScannerService.cs](../../app/Services/IpScannerService.cs), `ScanRangeAsync` / `RunScan` / `ProbeHost` | ARP/ICMP/TCP/reverse DNS; probes follow OS routing, no selected-NIC binding |
| Scanner UI | [IpScannerViewModel.cs](../../app/ViewModels/IpScannerViewModel.cs) | Start/stop, settings, CSV escaping, opt-in monitoring; enabled state not persisted |
| Ping | `NetworkService.PingAsync`; [PingWindow.xaml.cs](../../app/Views/PingWindow.xaml.cs), `RunPingAsync` | Single ping or four requests per window run with retry; README's continuous wording exceeds code |
| Tray/theme/startup | `App.InitTrayIcon` / `ShowTrayPopup` / `ApplyTheme`; [StartupHelper.cs](../../app/Helpers/StartupHelper.cs) | Close-to-tray, menu, System/Light/Dark, optional HKCU startup |
| Notifications/language | [NetworkMonitorService.cs](../../app/Services/NetworkMonitorService.cs); [LanguageManager.cs](../../app/Helpers/LanguageManager.cs) | Adapter notifications, Turkish/English/System, runtime switching and persistence |

Apply call graph: UI/hotkey/tray -> `ApplyCoreAsync` -> `NetworkService` -> direct `RunNetsh` if elevated, otherwise `RunElevated` -> `App.OnStartup --run-netsh` -> `RunWorker` -> `ValidateCommand` -> sequential `RunNetsh`. Success then records history and refreshes. No requested-versus-observed comparison is present.

Reuse MVVM, localization, structured process arguments and temp-directory test fixtures. Do not reuse active-profile highlighting as apply verification, or reduced `AdapterInfo`/`HistoryEntry` as a complete recovery snapshot.

## Preservation inventory — runtime UNVERIFIED, sign-off pending (01-T03)

| Capability observed in code | Preservation contract / limit |
|---|---|
| Static/DHCP | Preserve IP/mask/optional gateway/two DNS; DHCP commands reset address and DNS; empty static primary leaves DNS unchanged |
| Profiles | Preserve create/edit/delete, per-adapter storage, five-profile limit, Ctrl+1..5 and fill-only vendor presets |
| Legacy JSON | Preserve unversioned arrays and `%LOCALAPPDATA%/NetworkProfileManager/data/profiles_<sanitized-id>.json`; do not repurpose for projects |
| History/revert | Preserve 100-entry history and existing revert; do not claim DNS/routes/additional-address recovery |
| Scanner/ping | Preserve explicit ARP/ICMP/TCP scan, MAC/ports/hints, monitoring, CSV and existing ping actions |
| UI/tray | Preserve layout, themes, notifications, close-to-tray, startup and localization |
| Privilege/IPv6 | Preserve least privilege/constrained worker. Apply emits IPv4 commands; real IPv6/unrelated-NIC preservation requires isolated tests |

## Finding disposition and work owners

Accepted findings are planning evidence, not permission to fix code. Deferred fixes require separate scoped review.

| ID | Finding / evidence | Disposition | Owner / consequence |
|---|---|---|---|
| F01 | `HistoryEntry` lacks DNS/policy/routes/additional addresses; `RevertAsync` restores DHCP or IP/mask/gateway | MERGED | [04-T02, 04-T07](../../phases/04-apply-and-open.md): snapshot for new flow; legacy history reviewed separately |
| F02 | `BuildDnsCommands` does nothing for empty primary; secondary alone ignored | MERGED | [02-T01, 02-T03](../../phases/02-project-json.md): explicit semantics; no silent translation to legacy defaults |
| F03 | Command completion treated as success; batch can partly apply before exception/history append | ALREADY_COVERED | [04-T02 through 04-T05](../../phases/04-apply-and-open.md): post-state, failure/recovery tests and browser gate |
| F04 | Alias-based apply, selection fallback, no post-UAC NIC identity check | MERGED | [04-T01, 04-T06](../../phases/04-apply-and-open.md): stable identity; no fallback in new flow |
| F05 | Direct writes; backup exceptions swallowed before delete attempt | MERGED | [02-T04, 02-T06](../../phases/02-project-json.md): safe project store; legacy remediation DEFERRED pending scoped review |
| F06 | Scanner tests call real probes; `MainViewModelTests` constructor enumerates real NICs | ACCEPTED | [01-T07](../../phases/01-baseline-audit.md): fake/segregate OS operations; establish exact safe test set |
| F07 | Editor replaces ID; enumeration takes first IPv4/gateway/two DNS and hides APIPA | MERGED | [02-T01](../../phases/02-project-json.md) owns IDs; [05-T05](../../phases/05-link-local.md) APIPA; [07-T01/07-T02](../../phases/07-adapter-hyperv-diagnostics.md) diagnostics |
| F08 | Temp-file exchange and verb/character checks do not bind request to selected NIC | ACCEPTED | [04-T06](../../phases/04-apply-and-open.md): boundary review/hardening; exploitability UNVERIFIED, no exploit attempted |
| F09 | No SDK; test count/coverage unverified; package validator mistakes GitHub link for local path | ACCEPTED | [01-T06, 01-T08](../../phases/01-baseline-audit.md), [test evidence](../testing/TEST_MATRIX.md); no install/tool fix in adoption scope |
| F10 | Catalogue location, mapping, unknown fields, reserved modes, secret URL policy, complex-config support require choices | NEEDS_DECISION | [02-T01](../../phases/02-project-json.md), [03-T03/03-T04](../../phases/03-device-web-interfaces.md), [04-T02](../../phases/04-apply-and-open.md); proposals remain unapproved |

No application launch, live traffic, network changes, UAC or DHCP server was used to reproduce findings. Exact checks and limits are in [test evidence](../testing/TEST_MATRIX.md).

## Recommended boundaries — NOT IMPLEMENTED

- Phase 01: retain audit evidence; separately authorize preparation checkpoint 01-T06 through 01-T08. Test/tool changes are not part of this adoption.
- Phase 02: separate project model/store; settle 02-T01 before schema edits. Safe new IO does not authorize legacy migration or changing the five-profile workflow.
- Phase 03: catalogue and safe URL launcher independent of network mutation.
- Phase 04: typed request, sufficient snapshot, NIC revalidation, partial-failure observation and verified recovery. Decide preserve-or-block behavior for unsupported complex configurations before mutation. Legacy repairs need separate review.
- Phase 05: account for hidden APIPA, conflict checks and restore; distinguish automatic APIPA from manual link-local.
- Phases 06/07: interface-bound discovery lifecycle and richer diagnostics, preserving existing scanner behavior unless separately reviewed.
- Phase 08: separate safety approval and isolated lab; no detected foreign DHCP is not proof of isolation.
- Phase 09: full regression/deployment evidence; dependencies on 02 through 08 retained. Phase order/release scope unchanged.

## Review state

01-T01/01-T02/01-T04/01-T05: evidence recorded; 01-T03: inventory awaiting sign-off. No phase task is marked accepted/completed by adoption. Runtime acceptance remains outstanding; Phase 02 start is NOT AUTHORIZED. See ADR-006 in [decision log](../../decisions/DECISION_LOG.md).
