using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckNavigationFollowup(string output)
    {
        foreach (var theme in new[] { "Dark", "Light" })
        {
            ApplyRenderTheme(theme);
            var root = Path.Combine(output, "preferences-" + theme + "-" + Guid.NewGuid().ToString("N"));
            AppPaths.OverrideDataDirectory(root);
            try
            {
                var fake = new DiagnosticsSurface { Gate = null };
                var delay = new DiagnosticDelay { Pause = true };
                MainWindow Create(WindowLayoutStore? store = null) => new(new MainViewModel(() => new() { new() { Id = fake.Adapters[0].Id.ToString(), Name = "Ethernet fixture" } },
                    new AdapterDiagnosticsViewModel(fake, new(fake), delay: delay.WaitAsync)),
                    new ProjectViewModel(new FakeDialogs(), new FakeBrowser()), layoutStore: store);
                var statePath = Path.Combine(root, "window_layout.json");
                var window = Create(new WindowLayoutStore(statePath)); window.Show(); Pump();
                window.Width = 1060; window.Height = 720; window.Left = 90; window.Top = 70; Pump();
                var splitter = (GridSplitter)window.FindName("AdapterSplitter");
                DragNavigation(splitter, 80); Pump(); window.IsSidebarExpanded = false;
                window.Hide(); Pump();
                var saved = new WindowLayoutStore(statePath).Load() ?? throw new Exception("Layout did not save on tray hide.");
                if (saved.SidebarExpanded || saved.SidebarWidth != 320 || Math.Abs(saved.Width - 1060) > 2 || saved.Maximized)
                    throw new Exception("Normal layout/sidebar preference was not captured.");
                window.Show(); window.WindowState = WindowState.Maximized; Pump(); window.Close(); Pump();
                saved = new WindowLayoutStore(statePath).Load()!;
                if (!saved.Maximized || Math.Abs(saved.Width - 1060) > 2) throw new Exception("Maximize overwrote normal restore bounds.");

                var restored = Create(new WindowLayoutStore(statePath)); restored.Show(); Pump();
                if (restored.WindowState != WindowState.Maximized || restored.IsSidebarExpanded) throw new Exception("Restart did not restore maximize/collapse.");
                restored.WindowState = WindowState.Minimized; Pump(); restored.Hide(); Pump(); restored.ShowFromTray(); Pump();
                if (restored.WindowState != WindowState.Maximized) throw new Exception("Tray restore lost the last maximized state.");
                restored.WindowState = WindowState.Normal; Pump(); restored.IsSidebarExpanded = true; Pump();
                var sidebar = (FrameworkElement)restored.FindName("AdapterSidebar");
                if (Math.Abs(restored.Width - 1060) > 2 || Math.Abs(sidebar.ActualWidth - 320) > 1)
                    throw new Exception("Normal size or preferred sidebar width did not survive a new window.");
                var expectedPosition = saved.Place(WindowScreenPlacement.WorkArea(WindowScreenPlacement.CurrentMonitor(restored)), WindowScreenPlacement.WindowDpi(restored));
                var restoredPixels = WindowScreenPlacement.WindowPixels(restored);
                if (Math.Abs(restoredPixels.Left - expectedPosition.Left) > 2 || Math.Abs(restoredPixels.Top - expectedPosition.Top) > 2)
                    throw new Exception("Restart did not restore monitor-relative position.");
                Capture(restored, Path.Combine(output, $"layout-restored-{theme}.png"));
                restored.WindowState = WindowState.Minimized; Pump();
                restored.Close(); Pump();
                var minimized = new WindowLayoutStore(statePath).Load()!;
                if (minimized.Maximized || Math.Abs(minimized.Width - 1060) > 2) throw new Exception("Minimize overwrote usable restore bounds/state.");

                // A retired monitor/position uses a current work area and leaves a reachable caption.
                var writer = new WindowLayoutStore(statePath); var original = writer.Load()!;
                writer.TrySave(original with { Monitor = "disconnected-fixture", OffsetX = 99999, OffsetY = -99999, Maximized = false });
                var activity = Create(new WindowLayoutStore(statePath)); activity.Show(); Pump();
                var vm = ((MainViewModel)activity.DataContext).Diagnostics;
                var pixels = WindowScreenPlacement.WindowPixels(activity);
                var area = WindowScreenPlacement.WorkArea(WindowScreenPlacement.CurrentMonitor(activity));
                if (!area.IntersectsWith(pixels) || pixels.Top < area.Top || pixels.Left < area.Left)
                    throw new Exception("Disconnected monitor restored the window off screen.");
                var refresh = vm.RefreshAsync(); Until(() => refresh.IsCompleted); refresh.GetAwaiter().GetResult();
                vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0]; vm.Target = "192.0.2.229";
                vm.SelectedTest = vm.Presets.Single(p => p.Kind == NetworkProfileManager.Models.DiagnosticTestKind.ContinuousPing);
                var run = vm.RunSelectedTestAsync(); Until(() => delay.Entered);
                AssertActivity(activity, true, false);
                Capture(activity, Path.Combine(output, $"activity-inline-{theme}.png"));
                activity.IsSidebarExpanded = false; Pump(); AssertActivity(activity, true, false);
                vm.StopCommand.Execute(null); Until(() => run.IsCompleted); run.GetAwaiter().GetResult();
                AssertActivity(activity, false, false);

                var held = new DiagnosticsSurface();
                using var peer = new AdapterDiagnosticsViewModel(held, new(held));
                var testWindow = new PingWindow(peer) { Owner = activity };
                activity.TrackDiagnosticWindow(testWindow, peer); testWindow.Show(); Pump();
                var read = peer.RefreshAsync(); Until(() => read.IsCompleted); read.GetAwaiter().GetResult();
                peer.SelectedAdapter = peer.Adapters[0]; peer.Source = peer.Sources[0]; peer.Target = "192.0.2.229";
                var pending = peer.PingAsync(); Until(() => held.Sends == 1); AssertActivity(activity, true, false);
                delay.Entered = false;
                var overlap = vm.RunSelectedTestAsync(); Until(() => delay.Entered);
                testWindow.Close(); Pump(); AssertActivity(activity, true, false);
                held.Gate!.SetResult(new(0, "192.0.2.229", 2)); Until(() => pending.IsCompleted); pending.GetAwaiter().GetResult();
                AssertActivity(activity, true, false);
                vm.StopCommand.Execute(null); Until(() => overlap.IsCompleted); overlap.GetAwaiter().GetResult();
                AssertActivity(activity, false, false);

                var mdnsFake = new MdnsSurface();
                using var session = new MdnsDiscoverySession(mdnsFake, mdnsFake);
                using var mdnsVm = new MdnsDiscoveryViewModel(mdnsFake, session, mdnsFake);
                var discovery = new MdnsDiscoveryWindow(mdnsVm) { Owner = activity };
                activity.TrackMdnsWindow(discovery); discovery.Show(); Pump(); AssertActivity(activity, false, false);
                var list = mdnsVm.RefreshAsync(); Until(() => list.IsCompleted); list.GetAwaiter().GetResult();
                mdnsVm.SelectedInterface = mdnsVm.Interfaces[0];
                var start = mdnsVm.StartAsync(); Until(() => start.IsCompleted); start.GetAwaiter().GetResult();
                AssertActivity(activity, false, true);
                discovery.Hide(); Pump(); Capture(activity, Path.Combine(output, $"activity-mdns-{theme}.png"));
                AssertActivity(activity, false, true);
                var stop = mdnsVm.StopAsync(); Until(() => stop.IsCompleted); stop.GetAwaiter().GetResult();
                AssertActivity(activity, false, false);
                var restart = mdnsVm.StartAsync(); Until(() => restart.IsCompleted); restart.GetAwaiter().GetResult();
                discovery.Close(); Pump(); AssertActivity(activity, false, false);
                CheckAvailableDpi(activity, output, theme);
                activity.Close(); Pump();
            }
            finally { AppPaths.OverrideDataDirectory(null); }
        }
    }

    private static void AssertActivity(MainWindow window, bool tests, bool mdns)
    {
        Pump();
        if (window.AreTestsRunning != tests || window.IsMdnsListening != mdns
            || ((FrameworkElement)window.FindName("TestsActivityBadge")).IsVisible != tests
            || ((FrameworkElement)window.FindName("MdnsActivityBadge")).IsVisible != mdns)
            throw new Exception("Activity badges disagree with real operation lifecycle.");
        var testAction = (FrameworkElement)window.FindName("RailTests");
        if (AutomationProperties.GetHelpText(testAction) != Loc.Get(tests ? "Navigation.TestsRunning" : "NetworkTests.Title"))
            throw new Exception("Activity state lacks accessible text.");
    }

    private static void CheckAvailableDpi(MainWindow window, string output, string theme)
    {
        var records = new List<object>();
        var observedDpi = new HashSet<int>();
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            var dpi = WindowScreenPlacement.Dpi(screen);
            var state = new WindowLayoutState(1, screen.DeviceName, 20, 20, 1000, 700, false, 240, true);
            WindowScreenPlacement.Move(window, state.Place(WindowScreenPlacement.WorkArea(screen), dpi)); Pump();
            var actual = WindowScreenPlacement.WindowDpi(window);
            var visual = VisualTreeHelper.GetDpi(window).PixelsPerInchX;
            var context = FollowupGetWindowDpiAwarenessContext(new WindowInteropHelper(window).Handle);
            var awareness = FollowupGetAwarenessFromDpiAwarenessContext(context);
            if (awareness != 2 || Math.Abs(actual - dpi) > 1 || Math.Abs(visual - actual) > 1)
                throw new Exception($"Native HWND/WPF DPI mismatch: monitor={dpi}, HWND={actual}, WPF={visual}, awareness={awareness}. Run the manifest-bearing harness EXE.");
            window.IsSidebarExpanded = true; Pump();
            Capture(window, Path.Combine(output, $"native-dpi-{actual}-{theme}.png"));
            observedDpi.Add((int)actual);
            records.Add(new { screen.DeviceName, Dpi = dpi, WindowDpi = actual, WpfDpi = visual, Awareness = awareness });
        }
        File.WriteAllText(Path.Combine(output, $"native-monitors-{theme}.json"), System.Text.Json.JsonSerializer.Serialize(new
        { Monitors = records, Scale150 = observedDpi.Contains(144) ? "PASS" : "NOT RUN: no 144-DPI monitor",
            Scale200 = observedDpi.Contains(192) ? "PASS" : "NOT RUN: no 192-DPI monitor",
            MixedDpiTransfer = observedDpi.Count > 1 ? "Exercised available different-DPI monitors" : "NOT RUN: no different-DPI displays" }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowDpiAwarenessContext")] private static extern IntPtr FollowupGetWindowDpiAwarenessContext(IntPtr window);
    [DllImport("user32.dll", EntryPoint = "GetAwarenessFromDpiAwarenessContext")] private static extern int FollowupGetAwarenessFromDpiAwarenessContext(IntPtr context);
}
