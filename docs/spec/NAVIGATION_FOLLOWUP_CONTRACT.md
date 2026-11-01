# Navigation follow-up — UI-PREF-01 / UI-ACTIVITY-01 / UI-DPI-01

2026-11-01 (Asia/Tashkent), ADR-050. Owner selected interface persistence, actual running-tool indicators and native DPI checks, then answered “Ок”. Search/filter additions are not included. Local main work only; Phase 07 AWAITING_REVIEW, existing network/phase/publication gates remain.

## Private window preferences

Add `%LOCALAPPDATA%\NetworkProfileManager\data\window_layout.json`, a version-1 array containing exactly one window-layout record. No existing app_settings/profile/history/project file, JSON v1 schema or storage root changes. Older application versions ignore this new file; it has no automatic adapter/profile/test restoration.

Persist normal window size in DIPs, normal position as DIP offsets from the saved monitor's work area, monitor identity, last normal/maximized state, preferred sidebar width and expanded state. Save changed state on tray Hide and actual Close; do not write for every mouse movement or rewrite on startup. Minimized geometry/state is never persisted as the restart target. Tray restore preserves the last maximized state. Store normal geometry separately while maximized/minimized.

Example, using a synthetic monitor name:

```json
[
  {
    "Version": 1,
    "Monitor": "Example display",
    "OffsetX": 50,
    "OffsetY": 30,
    "Width": 1060,
    "Height": 720,
    "Maximized": false,
    "SidebarWidth": 320,
    "SidebarExpanded": true
  }
]
```

Validate version/count/finite numbers and bounds: width 760–10000, height 520–10000, sidebar 200–420, bounded monitor name/offsets and a 64 KiB initial file-size limit. Missing file uses existing defaults without writing. Damaged/unknown-version/invalid preferences remain untouched and disable saving for that store instance. Reopening after manual repair can read again; no destructive migration or silent reset. Saving reuses LegacyJsonFile's exclusive create-only lease, revision check, flushed temporary file, atomic replacement and exact-byte unique backup. Conflicting instances cannot clobber each other. Identical state is a no-op. Noncritical write failure retains the original/backup and must not block closing or any network workflow; it can mean the latest layout is not remembered.

Choose a still-connected monitor by identity, otherwise primary; calculate screen pixels from current monitor DPI, clamp restored size/position to its current work area and retain the existing window minimum. Transient display lookup/placement failure falls back to existing centered placement. Displays whose logical work area is smaller than the existing 760x520 minimum can retain an oversized window with its top-left/caption reachable; changing the product minimum is outside scope. Capture failures retain the last good geometry. Layout preferences store no selected NIC, addresses, credentials or project paths.

## Tool activity

Small semantic accent badges on Network tests/mDNS, backed by accessible HelpText and tooltip state. A dot is a status mark, not another command or an indication of device reachability. No animation, palette or new navigation page.

Tests indicator aggregates the inline diagnostics VM and separately opened peers. Refresh/open/select does not count as a running test. Explicit Run starts the indicator; it stays while cancellation drains an already entered call, including a closed peer, and clears only when all tracked tests finish. Retire closed peers after draining and detach subscriptions when the main window closes. No persistence/restart of a test.

mDNS indicator means the existing session is actually Listening and active, not Starting/Refreshing/Failed/Stopped. Use existing UI-thread polling (up to one-second state refresh) and explicit Start/Stop/Close lifecycle; no new listener, background probe or polling service. Closing/disposal clears the state. Existing separate windows and independent operations remain.

## DPI and verification boundaries

Declare modern PerMonitorV2 awareness using the 2016 dpiAwareness manifest entry; both app and real-HWND component harness share it. Avoid the legacy dpiAware entry that triggers the observed WFO0003 warning in this WPF/WinForms-tray project. No warning suppression or dependency change. Launch the manifest-bearing harness EXE for DPI verification; running its DLL through dotnet uses the host executable's manifest and does not prove the app's awareness. Maximized bounds use the actual window monitor/DPI rather than primary-screen assumptions.

Read the actual connected monitor inventory and compare monitor DPI, GetDpiForWindow, WPF visual DPI and HWND awareness. Test current physical monitor(s), keyboard/native caption behavior, restoration and existing scenarios. Check 150%/200% layout and monitor-relative geometry separately with fake/simulated boundaries. Mark actual 150%/200% and mixed-monitor transfer NOT RUN if those displays are unavailable; do not change global display settings or claim simulated DPI as native acceptance.

Relevant primary sources: [process DPI awareness](https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process), [GetDpiForWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow), [WFO0003](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/compiler-messages/wfo0003), [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos).

Acceptance: isolated storage legacy/corrupt/version/conflict/retry/backup tests; repeated actual WPF close/new-window/normal/maximized/minimized/tray/disconnected-monitor restoration; overlapping inline/peer and cancelled late-echo badges, fake mDNS start/stop/close; no traffic on navigation; Light/Dark normal/minimum and existing workspace/diagnostic regressions. Exact native DPI coverage and unavailable hardware cases must remain visible in task/status evidence.
