# Left navigation rail and resizable adapter sidebar

Date: 2026-11-01 (Asia/Tashkent). Follow-up: **UI-NAV-01**.

Read the [source-backed research and component contract](LEFT_NAVIGATION_RESEARCH.md) before implementation: it separates official Codex behavior, screenshot observations and proposed WPF geometry, and specifies interaction/accessibility quality checks. It supplements this brief without authorizing execution.

## Purpose and authorization

Give a future AI agent enough context to implement the navigation discussed with the owner without needing this chat. The owner requested a Codex-like sidebar toggle, adjustable sidebar width and an additional narrow icon bar at the far left, then requested this file. The immediate authorized deliverable is documentation only. No UI implementation has been performed or authorized by creating this file; implement when the owner explicitly delegates UI-NAV-01 or asks to implement this brief.

Follow [AGENTS.md](../../AGENTS.md) and its bootstrap before work. Read current [STATUS](../../STATUS.md), [TASKS](../../TASKS.md) and [DESIGN](../../DESIGN.md). Phase 07 remains AWAITING_REVIEW; this brief does not complete a phase, start Phase 08 or grant publication/live-network permission. Work on main under ADR-017 and preserve unrelated edits.

## Owner intent

- Add a narrow vertical icon rail at the far left, similar in interaction to the supplied Codex screenshots.
- Add a button with a sidebar icon to hide and reveal the adapter list.
- Let the user adjust the adapter sidebar width by dragging its right boundary.
- Keep the existing AV Network Toolkit visual language and functionality.

The screenshots illustrate interaction and layout, not permission to copy Codex assets or recreate unrelated Codex features. Temporary clipboard images are not durable dependencies: this written brief must suffice. Do not commit private adapter names, addresses or other user data from screenshots.

## Layout and behavior

The proposed structure is:

```text
Title bar, with a permanently reachable sidebar toggle
------------------------------------------------------------
Icon rail | Adapter sidebar | Main workspace
          |                 |
          | draggable edge  |
------------------------------------------------------------

Sidebar hidden:
Icon rail | Expanded main workspace
```

1. The narrow icon rail remains visible when the adapter sidebar is hidden. It has a fixed width; dragging changes the adapter sidebar only.
2. Place the toggle in the upper area, outside the content it hides. Use an existing WPF-UI SymbolIcon and SubtleButton, with localized Show/Hide adapter sidebar tooltip and accessible name. Preserve title-bar dragging and window controls.
3. Hide the entire adapter sidebar and its splitter, releasing that space to the workspace. Revealing restores its last width within the current window's limits. The initial expanded width remains the existing 240 WPF layout units.
4. Preserve the explicitly selected adapter, selected profile scope/tab, drafts, operation state and list position through hide/show. Do not recreate the MainViewModel or adapter list merely to toggle visibility.
5. Add a draggable boundary with a horizontal resize cursor and a usable hit target. Use minimum/maximum limits so neither navigation nor workspace becomes unusable. Clamp to available space when the window shrinks; inspect the actual content before choosing limits.
6. Keep long adapter names trimmed and tooltips available. Retain vertical scrolling and the current navigation selection/focus treatment.
7. Sidebar toggle and resize must support keyboard operation, have visible focus and preserve sensible focus when hiding a panel that contains it. Ctrl+Shift+S was visible in the Codex reference; it is not an approved required shortcut. Check existing bindings before adding any shortcut.
8. Remember the last expanded width during the current window lifetime. Persistence across app restarts is an optional follow-up, not a required storage change in this increment.

## Proposed icon rail destinations

This mapping was suggested in the chat, not individually approved as a new screen architecture. Use it as the default proposal and preserve the existing tool/window behavior in the first increment.

| Icon / accessible label | Proposed action |
|---|---|
| Network adapters | Reveal the adapter sidebar; retain the selected adapter and workspace. |
| Projects | Open or activate the existing project manager. |
| IP Scanner | Open the existing scanner using its current behavior. |
| mDNS | Open or activate the existing discovery window. |
| Network tests | Use the existing diagnostics/test surface and separate-window entry point. Preserve explicit adapter/source selection. |
| Settings, anchored at bottom | Open the existing settings dialog. |

Do not convert existing tools into embedded pages, create a generic navigation framework or introduce blank placeholder views. Determine the Network tests action from current code, including its no-adapter state; preserve existing availability conditions and explain a disabled action through a tooltip where needed. An action opening a separate window is not automatically the selected main page. Show a persistent active marker only for a real displayed navigation state, with a non-color cue. Avoid duplicate launch controls where safe, while retaining access to every existing title-bar action, including Link-Local.

## Source map and implementation guidance

Inspected source base: `8daf4d7eecc0610eabbc82092d2e7646bf33a6a9`, main, fork https://github.com/Elguajo/AV-Network-Toolkit. Reinspect before implementation; these are starting points, not a guarantee about future code.

- [MainWindow.xaml](../../app/MainWindow.xaml): title-bar tools and the two-column workspace; adapter column currently has fixed Width=240. Main window is initially 1200x850, minimum 760x520. The request concerns sidebar width; overall window resizing already exists.
- [MainWindow.xaml.cs](../../app/MainWindow.xaml.cs): existing tool launch handlers, project/diagnostics wiring and Ctrl+1..5 profile hotkeys. Reuse entry points and lifecycle guards.
- [AdapterListPanel.xaml](../../app/Views/AdapterListPanel.xaml): adapter navigation, refresh, selected-item binding, ellipsis and scrolling.
- [Styles.xaml](../../app/Themes/Styles.xaml): SubtleButton, NavigationListItem and shared visual/focus resources.
- [ProjectUiSmoke](../../scripts/ProjectUiSmoke/WorkspaceSmoke.cs): fake adapter/project boundaries and existing main-window rendering checks. Extend this safe harness rather than launching live networking.

Stay with the repository's WPF/MVVM and WPF-UI stack, existing semantic brushes and English localization resources. A standard WPF [GridSplitter](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-resize-columns-with-a-gridsplitter) can resize adjacent Grid columns without a new package. Exact dimensions/icon choices are implementation details to derive from DESIGN.md and rendered evidence.

## Preserved boundaries

No network mutation, automatic adapter switching, probes, scans, listener startup or profile application from navigation, toggle, resize or opening a tool. Preserve existing deliberate Start/Run/Apply controls, UAC, diagnostics cancellation and ownership behavior. Preserve profiles, DHCP, IP/mask/gateway/DNS, JSON import/export, project/device links, history/revert, tray and localization. No schema/storage migration, dependency upgrade, full redesign, telemetry, account rail, downloads menu, release or deployment work.

Do not opportunistically fix existing diagnostic cancellation risk DOC-R01 or unrelated defects. Record their effect on verification separately and do not suppress failing checks. Existing owner/native acceptance gates remain.

## Acceptance and verification for future implementation

- [ ] Icon rail remains visible with sidebar both expanded and hidden; all required existing actions remain reachable.
- [ ] Toggle is always reachable; repeated hide/show restores width, selection, scroll and drafts without triggering network or apply actions.
- [ ] Dragging adjusts only sidebar width, respects limits and does not cover or push essential workspace controls out of reach.
- [ ] Shrinking/restoring the main window, including 760x520, handles a previously wide sidebar safely; normal and maximized modes work.
- [ ] Rail actions retain existing window/command and availability behavior, with meaningful active/disabled states and no fake page selection.
- [ ] Keyboard toggle, resize, focus recovery, tooltip and accessible-name checks pass; existing Ctrl+1..5 bindings remain intact.
- [ ] Actual WPF captures cover Light/Dark at normal/minimum size, hidden/default/wide sidebar, long adapter names, many adapters and empty/no-selected-adapter states. No binding errors.
- [ ] Run the current Release build, relevant automated regressions, existing workspace checks and new focused interaction assertions through fake/isolated boundaries. Record exact commands and observed results; distinguish pre-existing failures and NOT RUN native/manual checks.
- [ ] Update coupled design/task/status evidence and append the standard session handoff. Do not mark phase acceptance or publish without the applicable owner gate.

Success means the user can reclaim workspace space, reveal adapters and make long adapter names more readable without losing context or access to existing tools. Verify through the checks above; no analytics is required.

## Suggested future invocation

> Implement UI-NAV-01 from docs/spec/LEFT_NAVIGATION_BRIEF.md. Follow AGENTS.md and DESIGN.md, preserve existing tool windows and all network safety boundaries, validate with the isolated WPF harness, and report evidence. Do not publish or advance phases.
