# Codex-inspired left navigation: research and component contract

Research date: 2026-11-01 (Asia/Tashkent). Task: UI-NAV-01. Read with the [implementation brief](LEFT_NAVIGATION_BRIEF.md) and [DESIGN.md](../../DESIGN.md).

This research was originally documentation and a proposed design contract. The owner subsequently authorized UI-NAV-01 implementation with “Внедряем” (ADR-049); the brief records the later hover-reveal refinement. Research claims below retain their original evidence boundaries. No new dependency, tool-page architecture or phase is approved here.

## Evidence and confidence

| Basis | What is established | Limit |
|---|---|---|
| Official OpenAI [Commands](https://learn.chatgpt.com/docs/reference/commands) | Windows sidebar toggle defaults include Ctrl+B and Ctrl+Shift+S; back/forward are separate commands. | These are product commands, not a reusable UI component API or a design-token specification. |
| Official OpenAI [Projects and chats](https://learn.chatgpt.com/docs/projects) | Sidebar organization includes pinning projects/chats; archiving is separate from deleting content. | This does not specify the geometry or implementation of the icon rail. |
| Owner's screenshots in this chat | A narrow vertical icon strip beside a wider sidebar, a filled rounded current-item surface, muted inactive glyphs, separators, large flexible vertical space and bottom-aligned utility controls are visible. | A still image cannot establish hover, keyboard, accessibility, persistence, animation or exact logical dimensions. Icon meanings and badge semantics cannot be inferred reliably. |
| Microsoft [NavigationView](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/navigationview) | Compact left navigation, grouped items and footer settings are established Windows patterns. | This documentation describes WinUI. It is design guidance here, not an instruction to use WinUI APIs in this WPF app or to copy its overlay/automatic-breakpoint behavior. |
| Microsoft [Tooltips](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/tooltips) | Icon-only toolbar commands need concise explanatory labels through tooltips. | Tooltips do not replace accessible names or essential in-place error guidance. |
| Microsoft [Keyboard accessibility](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/keyboard-accessibility) | Controls need keyboard reachability and tested visual/tab order. | WPF implementation and assistive-technology behavior still need direct verification. |
| Microsoft [GridSplitter.KeyboardIncrement](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.gridsplitter.keyboardincrement?view=windowsdesktop-10.0) | WPF supports arrow-key splitter resizing; the documented default increment is 10 layout units. | Focusability, visible focus and usable bounds must still be implemented and tested. |

The inspected OpenAI app/features/commands/project pages did not provide a public specification for rail width, icon sizes, corner radii, color tokens, splitter limits or its internal source/component implementation. The old developers.openai.com/codex/app routes currently redirect to learn.chatgpt.com. Do not describe the following proposals as official Codex measurements or claim pixel fidelity from these sources. No Codex application binary/source reverse engineering was performed.

## Component anatomy

1. **Global title bar:** existing product title and window controls, plus a reachable sidebar toggle. Keep native drag, maximize and system-menu behavior. Do not add Back/Forward buttons just to imitate the screenshot: this application has no approved navigation-history flow for them.
2. **Persistent icon rail:** fixed-width column below the title bar. Main actions are aligned at the top; Settings is anchored at the bottom, separated by flexible space. Icons stay stationary during adapter-sidebar resize and workspace scrolling.
3. **Adapter sidebar:** existing list/header/refresh control, with adjustable width and independent vertical scrolling. It is completely hidden when collapsed.
4. **Resize boundary:** a subtle visual divider with a wider mouse hit area; not a conspicuous permanent handle. Hide both its hit target and reserved width with the adapter sidebar.
5. **Workspace:** retains its current hierarchy and tool behavior; receives the space released by collapse. The rail is not a replacement for explicit adapter selection.

Keep the rail and adapter sidebar as separate concepts. The standard WinUI compact pane is useful precedent, but mechanically substituting a single expanding NavigationView would not automatically reproduce this two-part layout. Inspect the installed WPF-UI primitives before choosing the smallest implementation; do not introduce WinUI, web UI, a docking package or a generic page router.

## Proposed geometry for WPF validation

These are initial design candidates derived from the reference proportions and existing 20-unit icons/8-unit spacing, not measured Codex tokens or approved final constants. Express them through a small shared style/resource in the existing system.

| Part | Starting proposal | Validation condition |
|---|---|---|
| Rail | 52 device-independent units | 8-unit side insets around 36-unit action targets; fixed width in both sidebar states. |
| Action target | 36 x 36 | Center a 20-unit glyph; the whole target is clickable, not only the glyph. |
| Vertical spacing | 8 units between targets | Stable rhythm; related actions grouped without unnecessary separators. |
| Selection surface | Existing ControlCornerRadius | Use semantic selection/hover resources; no copied Codex color values. |
| Sidebar | Existing 240-unit expanded default | Remember preferred width in memory; clamp effective width to available client space. |
| Resize hit region | 6 units with a subtle divider | Easy to acquire without stealing clicks from list items or workspace controls. |

Minimum sidebar width and the workspace's actual usable minimum must be established from rendered content, not guessed from the screenshots. Compute the budget from the client area: rail + sidebar + resize region + workspace. Test this at the existing 760x520 window minimum before accepting sizes. If the candidates cannot fit, revise spacing/limits or use existing compact content behavior; do not silently raise the app's minimum size, hide required commands or invent a new automatic collapse/overlay mode.

Use device-independent units, WPF layout rounding as appropriate and semantic divider brushes. Test 100%, 150% and 200% Windows scaling for centered, uncut icons, clean boundaries and reachable controls. Screenshot pixels are not automatically WPF units.

## Interaction states and semantics

| State | Required behavior |
|---|---|
| Default | Muted readable glyph, quiet background; no decorative accent fill. |
| Hover | Existing subtle fill, tooltip with the action name; no layout shift or automatic pane opening. |
| Pressed | Existing pressed treatment; invoke once on the standard button activation path. |
| Keyboard focus | Visible focus independent of hover/selection; focus alone invokes no action. |
| Current location | Persistent fill plus a non-color cue and appropriate UI Automation selection/toggle semantics. Use only for content actually displayed in the main workspace. |
| Open-window action | Momentary button behavior. Launching Scanner, mDNS, Projects or Settings does not falsely replace the current main location. Reuse existing window-instance policy. |
| Disabled | Retain a predictable position, explain why unavailable and expose enabled state correctly. No dispatch through an alternate unsafe path. |
| Sidebar hidden | Rail and toggle remain available; hidden list/splitter are excluded from focus and hit testing. Toggle exposes the correct current state. |
| Loading/error | Keep navigation geometry stable; actual tool progress/errors stay on the tool's existing surface. Do not invent rail spinners/badges or replace icons with transient status. |

Use existing SubtleButton hover/pressed conventions and NavigationListItem selection/focus conventions. A scoped selected-state style may be needed; SubtleButton alone does not provide a navigation selection model. Use existing semantic text, surface, divider and accent resources and ControlCornerRadius; no new palette, shadows or theme-independent dark fills.

The existing application uses a mixture of workspace navigation and separate tool windows. Preserve that distinction in UI Automation: actual commands are buttons; the collapse control should expose its state through a suitable standard control/peer; selection semantics apply only to genuine navigation items. Do not create inaccessible clickable Borders or web ARIA attributes in WPF.

## Keyboard, focus and accessible labels

- Every action has a stable English resource label, tooltip and UI Automation name; glyph text is not an accessible label. Include a shortcut in a tooltip only when that shortcut is actually registered.
- Use Tab/Shift+Tab in visual order and Enter/Space to activate standard buttons. Keep a small button rail simple; do not add a custom arrow-key navigation scheme unless choosing a control whose standard semantics require it.
- When collapse hides the focused adapter list/splitter, move focus to the sidebar toggle. When focus is already in the workspace, keep it there. Expanding must not apply a profile or force a different adapter.
- The focused splitter supports arrow resizing and exposes its purpose. Changing width must not change the selected adapter or focus the workspace unexpectedly.
- Ctrl+B is a reasonable proposed toggle shortcut because it matches a documented Codex default. Inspect current bindings and text-editor contexts first. Ctrl+Shift+S is also a documented Codex default, but do not consume a Save as gesture without checking the repository's file workflows. Neither shortcut is approved merely by this research.
- Verify automation name, control role, enabled state, current/expanded state and keyboard reachability through the actual WPF automation tree. Manual Narrator behavior is separate evidence; source attributes alone do not prove it works.

## Overflow, themes and excluded decoration

Keep Settings visible at the bottom while the top action group can scroll if vertical space is constrained. Do not shrink targets until they are difficult to use. For the small proposed action set, a More menu is not required; add one only if actual content cannot remain reachable and every overflow item has a real destination.

The screenshot's avatar, update/download control, blue dot and other product-specific symbols are not features of AV Network Toolkit. Do not recreate them as empty controls, fake status or a new account system. Existing Link-Local/project/file actions must remain reachable even if not included in the first rail mapping.

Check Light, Dark, runtime theme switching and high contrast with semantic resources. A current marker must remain distinct from keyboard focus and disabled state. New animation is not required; preserve existing motion policy. No auto-hide on pointer leave, hover-triggered expansion or sliding transition is implied by the screenshots.

## Required future evidence

Extend the brief's acceptance checks with: mouse hover/pressed/current/focus/disabled states; top-group overflow and fixed Settings at minimum height; native title-bar drag/double-click behavior; a wide-sidebar then window-shrink/reveal sequence; scaling checks; theme changes on an existing window; UI Automation state checks; and no fake operation calls from navigation/toggle/resize.

Use synthetic adapters, long names and fake services. Compare actual WPF renders against the owner reference for structure/rhythm, and against DESIGN.md for styling. Treat geometry above as provisional until those checks pass. No implementation, rendering, native accessibility or interaction validation was performed at the research checkpoint; subsequent implementation evidence must be recorded separately.
