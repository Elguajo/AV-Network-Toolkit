using System;
using System.Windows;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;

namespace NetworkProfileManager;

public partial class MainWindow
{
    private WindowLayoutStore? _layoutStore;
    private WindowLayoutState? _normalLayout;
    private bool _layoutReady, _lastMaximized;
    internal bool LayoutSaveFailed { get; private set; }

    internal void ShowFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = _lastMaximized ? WindowState.Maximized : WindowState.Normal;
        Activate();
    }

    private void InitializeLayout(WindowLayoutStore? store)
    {
        _layoutStore = store;
        if (store == null) return; // Existing component checks opt in explicitly to isolated persistence.
        var saved = store.Load();
        if (saved != null)
        {
            _preferredSidebarWidth = saved.SidebarWidth;
            IsSidebarExpanded = saved.SidebarExpanded;
            WindowStartupLocation = WindowStartupLocation.Manual;
            SourceInitialized += (_, _) =>
            {
                try
                {
                    var monitor = WindowScreenPlacement.SelectMonitor(saved.Monitor);
                    if (!WindowScreenPlacement.Move(this, saved.Place(WindowScreenPlacement.WorkArea(monitor), WindowScreenPlacement.Dpi(monitor))))
                        WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                catch { WindowStartupLocation = WindowStartupLocation.CenterScreen; }
            };
        }
        Loaded += (_, _) =>
        {
            _layoutReady = true;
            CaptureNormalLayout();
            if (saved?.Maximized == true) WindowState = WindowState.Maximized;
        };
        LocationChanged += (_, _) => CaptureNormalLayout();
        SizeChanged += (_, _) => CaptureNormalLayout();
        StateChanged += (_, _) =>
        {
            if (WindowState != WindowState.Minimized) _lastMaximized = WindowState == WindowState.Maximized;
            CaptureNormalLayout();
        };
        IsVisibleChanged += (_, _) => { if (!IsVisible) SaveLayout(); };
        Closed += (_, _) => SaveLayout();
    }

    private void CaptureNormalLayout()
    {
        if (!_layoutReady || WindowState != WindowState.Normal || !IsVisible) return;
        try { CaptureNormalLayoutCore(); }
        catch { /* A transient display-topology failure must not interrupt closing or replace the last good geometry. */ }
    }

    private void CaptureNormalLayoutCore()
    {
        var pixels = WindowScreenPlacement.WindowPixels(this);
        var dpi = WindowScreenPlacement.WindowDpi(this);
        if (pixels.IsEmpty || dpi <= 0) return;
        var screen = WindowScreenPlacement.CurrentMonitor(this);
        var work = WindowScreenPlacement.WorkArea(screen);
        var scale = dpi / 96;
        _normalLayout = new(1, screen.DeviceName, (pixels.Left - work.Left) / scale, (pixels.Top - work.Top) / scale,
            Math.Max(MinWidth, pixels.Width / scale), Math.Max(MinHeight, pixels.Height / scale), false, _preferredSidebarWidth, IsSidebarExpanded);
    }

    private void SaveLayout()
    {
        if (!_layoutReady || _layoutStore == null) return;
        if (IsVisible) CaptureNormalLayout();
        if (_normalLayout == null) return;
        LayoutSaveFailed = !_layoutStore.TrySave(_normalLayout with
        { Maximized = _lastMaximized, SidebarWidth = _preferredSidebarWidth, SidebarExpanded = IsSidebarExpanded });
    }
}
