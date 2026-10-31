# Compact project and profile toolbar — 2026-11-01

Base: main `4af148dd47abc56962ac623e35a99d94fdae8d06`, clean bootstrap. Owner pointed to the marked block and the approved compact raster: project identity/file + Open/Save, Network profiles / Device interfaces, then saved-profile heading and right-aligned actions. This corrects the prior tall banner and second visible navigation row; it is a localized IDEA-04 follow-up under ADR-030, not a new phase or storage design.

Implementation/evidence commit: `9b1823b203e7591b7bb8d2316530170922e85abd` (21 paths), followed by this documentation-only SHA pin. No push, phase completion or release.

## Result and boundaries

- One-row project bar: folder, project name, file basename and unsaved marker; full path remains in the tooltip. Open project and Save project stay visible. Save as and Manage project remain in a named, keyboard-focusable “…” menu connected to the same shared ProjectViewModel and existing commands.
- One visible tab row, with the active tab underlined using existing accent/hover/focus resources. Device interfaces shows the actual project-device count. No new colors, assets, library or unrelated screen redesign.
- Saved profiles for the selected adapter, a compact source selector and right-aligned Add Profile. Local profiles retain their labelled Import/Export commands; portable project files retain Open/Save/Save as rather than presenting legacy import as a project operation.
- The source selector replaces only the placement of On this computer / Project profiles. It uses existing separate collections and selection state; no profiles are copied, migrated or silently matched by adapter alias. This placement is the implementation choice that reconciles the owner's latest one-row reference with storage compatibility. Earlier positive feedback on tabs is not claimed as acceptance of this new selector.
- Apply, detached editors, draft protection, save/revision/backup rules, device actions, manual settings, Ping/history/recovery and all NIC/security policies retain their previous paths. JSON, schema, worker/coordinator, NetworkService and legacy persistence source files are unchanged.

## Rendered comparison

| Reference / actual | Meaning |
|---|---|
| [Owner's target crop](../design/compact/toolbar-target.png) | Supplied in this chat on 2026-11-01; existing generated concept, illustrative text/spacing, no external assets |
| [Actual local profiles, dark 1200×850](../design/compact/adapter-dark.png) | Compact project bar, underlined tabs, source selector, Import/Export/Add and per-profile Apply |
| [Actual project profiles, dark 1200×850](../design/compact/project-dark.png) | Same structure with portable project collection and modal Add/Edit |
| [Actual project profiles, light 760×520](../design/compact/project-minimum-light.png) | Scrolled to profiles; toolbar/actions reachable, names wrap/trim with existing tooltips |

The actual images are RenderTargetBitmap captures of live WPF controls with safe synthetic fixtures, composited over existing theme surfaces. They do not certify native Mica, full application startup, live network behavior or manual accessibility. Normal/minimum Light/Dark captures were visually inspected against the target; layout/hierarchy is materially closer, not a claim of pixel identity or measured user task success.

## Verification

PowerShell / SDK 8.0.427. `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`; `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. Commands from repository root:

| Exact command | Result |
|---|---|
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/Compact/` | PASS, 0 warnings/errors |
| `& $sdk scripts/ProjectUiSmoke/bin/Compact/ProjectUiSmoke.dll TestResults/CompactReviewed` | PASS, 112 Light/Dark normal/minimum captures, 0 binding errors |
| `& $sdk scripts/ProjectUiSmoke/bin/Compact/ProjectUiSmoke.dll TestResults/CompactAfter --ux-after` | PASS, 12 focused captures, 0 binding errors, guarded first/repeat Save and actual Add/Keep |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/CompactUnit/ --filter "FullyQualifiedName~ProjectViewModelTests" --logger "trx;LogFileName=compact-project.trx" --results-directory TestResults/CompactUnit` | PASS, 17/17, 0 failed/skipped; existing save/path/draft/conflict/IO cases |
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/CompactUnit/` | PASS, 0 warnings/errors |

The existing real-control harness now checks initial local-list visibility, source-selector round trip with both four-profile collections intact, one-row project-bar bounds, source → Import → Export → Add forward focus order, the actual overflow menu's command/context, shared Manage window, per-row target routing, named inputs and keyboard/footer/close behavior. No additional unit tests for visual values. Filtered unit evidence is proportionate to the FileName display/toolbar change; it is not a replacement for the full regression gate.

Observed and corrected while implementing: content-only scope host initially omitted automatic first selection (explicit index 0 + initial-visibility check); Run.Text attempted two-way binding to a read-only basename (explicit OneWay); selected-tab font weight inherited into the list (targeted the header presenter instead). Source caption shortened to This computer to avoid clipping. Final captures/binding checks were repeated after these corrections; no gate weakened.

Code/design/accessibility self-review of the bounded diff: existing WPF-UI controls and semantic resources reused; menu/source controls have accessible names and preserve focus behavior; minimum-size toolbar/menu and source switching exercised. Screen reader, high contrast, scaling/zoom, physical keyboard walkthrough, full App/tray/native dialogs and isolated NIC/UAC/browser acceptance remain NOT RUN. Owner review of the compact layout and source-selector discoverability is pending.

## Remaining blockers and next action

Latest **full** unit gate remains the earlier **564 PASS / 1 FAIL / 0 skipped** from [UX review](UX_WORKFLOW_REVIEW.md), with [preserved JSON rejection failure](evidence/ux-json-failure.txt). Full suite NOT RUN again for this layout-only follow-up; UX-R01 / OPEN P05-R01 remains unresolved and release-blocking. Seventeen focused passes do not clear it. No JSON fix/suppression/serialization, native test, network mutation, password feature, support expansion, push, release or Phase 07 advancement here.

Next safe step: owner compares these actual captures to the target and checks finding the profile source and More project actions. Separately scope the recorded JSON-validation investigation. Phase 06 remains AWAITING_REVIEW; no acceptance checkbox is closed by this correction.
