using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckNavigation(string output)
    {
        foreach (var theme in new[] { "Dark", "Light" })
        {
            ApplyRenderTheme(theme);
            AppPaths.OverrideDataDirectory(Path.Combine(output, "navigation-" + theme));
            try
            {
                var reads = 0;
                var fake = new DiagnosticsSurface();
                var delay = new DiagnosticDelay();
                using var diagnostics = new AdapterDiagnosticsViewModel(fake, new(fake), tcp: new(fake, fake), delay: delay.WaitAsync);
                var main = new MainViewModel(() =>
                {
                    reads++;
                    return Enumerable.Range(1, 40).Select(i => new AdapterInfo
                    {
                        Id = i == 1 ? fake.Adapters[0].Id.ToString() : Guid.NewGuid().ToString(),
                        Name = i == 1 ? "Ethernet" : $"USB Ethernet {i} — a deliberately long adapter name",
                        Description = "Synthetic navigation fixture", AdapterType = "Ethernet", IsConnected = true,
                        CurrentIp = "192.0.2.10", CurrentSubnet = "255.255.255.0"
                    }).ToList();
                }, diagnostics);
                var project = new ProjectViewModel(new FakeDialogs(), new FakeBrowser()) { ProjectName = "Navigation fixture" };
                var selected = main.SelectedAdapter!;
                selected.EditIp = "192.0.2.42";
                var window = new MainWindow(main, project) { Width = 1200, Height = 850 };
                window.Show(); Pump();
                var layout = (Grid)window.FindName("NavigationLayout");
                var sidebar = (AdapterListPanel)window.FindName("AdapterSidebar");
                var splitter = (GridSplitter)window.FindName("AdapterSplitter");
                var toggle = (ToggleButton)window.FindName("SidebarToggle");
                var location = (RadioButton)window.FindName("RailAdapters");
                var rail = (FrameworkElement)window.FindName("NavigationRail");
                var settings = (FrameworkElement)window.FindName("RailSettings");
                var list = Descendants(sidebar).OfType<ListBox>().Single();
                var listScroll = Descendants(list).OfType<ScrollViewer>().First();
                var tabs = (TabControl)window.FindName("WorkspaceTabs");
                var scopes = (TabControl)window.FindName("ProfileScopes");
                var togglePeer = UIElementAutomationPeer.CreatePeerForElement(toggle)!;
                var toggleProvider = (IToggleProvider)togglePeer.GetPattern(PatternInterface.Toggle);
                var locationProvider = (ISelectionItemProvider)UIElementAutomationPeer.CreatePeerForElement(location)!.GetPattern(PatternInterface.SelectionItem);
                if (toggleProvider.ToggleState != ToggleState.On || !locationProvider.IsSelected || sidebar.ActualWidth != 240)
                    throw new Exception("Navigation initial state/automation/default width is incorrect.");
                foreach (var item in Descendants(rail).OfType<ButtonBase>().Append(toggle))
                {
                    if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(item)) || item.ToolTip == null)
                        throw new Exception("An icon action has no accessible name or tooltip.");
                    AssertWorkspaceBounds(window, item);
                }
                location.Focus(); Pump();
                foreach (var name in new[] { "RailProjects", "RailLinkLocal", "RailScanner", "RailMdns", "RailTests", "RailSettings" })
                {
                    ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)); Pump();
                    if (!((UIElement)window.FindName(name)).IsKeyboardFocusWithin)
                        throw new Exception("Rail keyboard order differs from visual order at " + name + "; focused " + Keyboard.FocusedElement);
                }
                CheckNavigationTitleBar(window, toggle);
                CheckNavigationHover(window, splitter, toggle, list, output, theme);
                Capture(window, Path.Combine(output, $"navigation-default-{theme}.png"));
                toggleProvider.Toggle(); Pump();
                Capture(window, Path.Combine(output, $"navigation-compact-{theme}.png"));
                toggleProvider.Toggle(); Pump();

                // Exercise the actual GridSplitter event handlers, not an assignment to column width.
                splitter.Focus(); Pump();
                if (!splitter.IsKeyboardFocused) throw new Exception("Splitter is not keyboard reachable.");
                ResizeNavigationKey(splitter, Key.Right); Pump();
                if (Math.Abs(sidebar.ActualWidth - 250) > 1) throw new Exception("Arrow-key resize did not move by 10.");
                DragNavigation(splitter, 90); Pump();
                if (Math.Abs(sidebar.ActualWidth - 340) > 1 || Math.Abs(rail.ActualWidth - 52) > 1)
                    throw new Exception("Drag did not resize only the adapter sidebar.");
                DragNavigation(splitter, 1000); Pump();
                if (sidebar.ActualWidth > 421) throw new Exception("Drag exceeded sidebar maximum.");
                DragNavigation(splitter, -1000); Pump();
                if (Math.Abs(sidebar.ActualWidth - 200) > 1) throw new Exception("Drag exceeded sidebar minimum.");
                DragNavigation(splitter, 140); Pump();
                var preferred = sidebar.ActualWidth;
                splitter.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
                splitter.RaiseEvent(new DragDeltaEventArgs(40, 0) { RoutedEvent = Thumb.DragDeltaEvent });
                ResizeNavigationKey(splitter, Key.Escape); Pump();
                splitter.RaiseEvent(new DragCompletedEventArgs(40, 0, true) { RoutedEvent = Thumb.DragCompletedEvent });
                if (Math.Abs(sidebar.ActualWidth - preferred) > 1) throw new Exception("Escape did not cancel sidebar resize.");
                Capture(window, Path.Combine(output, $"navigation-wide-{theme}.png"));

                scopes.SelectedIndex = 1; tabs.SelectedIndex = 1;
                project.DeviceEditor.Name = "Unstored device draft";
                listScroll.ScrollToVerticalOffset(8); Pump();
                var offset = listScroll.VerticalOffset;
                var before = sidebar.ActualWidth;
                list.Focus(); Pump();
                toggleProvider.Toggle(); Pump();
                if (window.IsSidebarExpanded || sidebar.IsVisible || splitter.IsVisible || layout.ColumnDefinitions[1].ActualWidth > 0
                    || layout.ColumnDefinitions[2].ActualWidth > 0 || !rail.IsVisible || !toggle.IsKeyboardFocused
                    || toggleProvider.ToggleState != ToggleState.Off || togglePeer.GetName() != Loc.Get("Navigation.ShowSidebar"))
                    throw new Exception("Collapse did not release both columns or recover keyboard focus/state.");
                Capture(window, Path.Combine(output, $"navigation-hidden-{theme}.png"));
                toggleProvider.Toggle(); Pump();
                if (Math.Abs(sidebar.ActualWidth - before) > 1 || Math.Abs(listScroll.VerticalOffset - offset) > 0.1
                    || !ReferenceEquals(main.SelectedAdapter, selected) || selected.EditIp != "192.0.2.42" || reads != 1
                    || tabs.SelectedIndex != 1 || scopes.SelectedIndex != 1 || project.DeviceEditor.Name != "Unstored device draft")
                    throw new Exception("Hide/show lost width, scroll, target, draft or workspace context.");

                // The registered shortcut and mouse toggle share the same DP/binding path.
                var shortcut = window.InputBindings.OfType<KeyBinding>().Single(k => k.Key == Key.B && k.Modifiers == ModifierKeys.Control);
                var save = (UIElement)window.FindName("SaveProject"); save.Focus(); Pump();
                shortcut.Command.Execute(null); Pump();
                if (!save.IsKeyboardFocusWithin) throw new Exception("Sidebar shortcut stole workspace focus.");
                location.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                if (!window.IsSidebarExpanded || !locationProvider.IsSelected) throw new Exception("Adapters action did not reveal the list.");
                if (window.InputBindings.OfType<KeyBinding>().Count(k => k.Modifiers == ModifierKeys.Control && k.Key >= Key.D1 && k.Key <= Key.D5) != 5)
                    throw new Exception("Existing profile hotkeys were lost.");

                window.Width = 760; window.Height = 520; Pump();
                AssertNavigationBudget(window, layout, sidebar, rail, settings);
                Capture(window, Path.Combine(output, $"navigation-minimum-{theme}.png"));
                window.Width = 1200; window.Height = 850; Pump();
                if (Math.Abs(sidebar.ActualWidth - preferred) > 1) throw new Exception("Window resize overwrote preferred sidebar width.");
                window.IsSidebarExpanded = false; window.Width = 760; window.Height = 520; Pump();
                window.IsSidebarExpanded = true; Pump(); AssertNavigationBudget(window, layout, sidebar, rail, settings);
                window.Width = 1200; window.Height = 850; Pump();
                if (Math.Abs(sidebar.ActualWidth - preferred) > 1) throw new Exception("Resize while hidden lost preferred width.");

                // Open the existing project manager using injected project IO, then a fake diagnostics peer.
                ((ButtonBase)window.FindName("RailProjects")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                var manager = window.OwnedWindows.OfType<ProjectWindow>().Single();
                ((ButtonBase)window.FindName("RailProjects")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                if (window.OwnedWindows.OfType<ProjectWindow>().Count() != 1 || !locationProvider.IsSelected)
                    throw new Exception("Tool action duplicated the project window or changed main location.");
                manager.Close(); Pump();
                ((ButtonBase)window.FindName("RailTests")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                var testWindow = window.OwnedWindows.OfType<PingWindow>().Single();
                if (fake.Sends != 0 || fake.Connects != 0 || !locationProvider.IsSelected) throw new Exception("Opening tests started a probe or changed main location.");
                testWindow.Close(); Pump();

                main.SelectedAdapter = null; Pump();
                var tests = (ButtonBase)window.FindName("RailTests");
                if (tests.IsEnabled || tests.ToolTip?.ToString() != Loc.Get("Navigation.TestsUnavailable"))
                    throw new Exception("No-adapter test action is not disabled with a reason.");
                window.IsSidebarExpanded = false; Pump();
                Capture(window, Path.Combine(output, $"navigation-empty-{theme}.png"));
                location.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                if (main.SelectedAdapter != null || reads != 1) throw new Exception("Revealing navigation silently chose/refreshed an adapter.");
                main.SelectedAdapter = selected; tabs.SelectedIndex = 0; scopes.SelectedIndex = 0;

                // Layout scaling does not alter the desktop DPI setting. Native mixed-DPI remains manual.
                foreach (var scale in new[] { 1.5, 2.0 })
                {
                    ((FrameworkElement)window.Content).LayoutTransform = new ScaleTransform(scale, scale);
                    window.Width = 760 * scale; window.Height = 520 * scale; Pump();
                    AssertNavigationBudget(window, layout, sidebar, rail, settings);
                    Capture(window, Path.Combine(output, $"navigation-scaled-{scale}-{theme}.png"));
                }
                ((FrameworkElement)window.Content).LayoutTransform = Transform.Identity;
                window.Width = 1200; window.Height = 850; Pump();
                ApplyRenderTheme(theme == "Dark" ? "Light" : "Dark"); Pump();
                Capture(window, Path.Combine(output, $"navigation-theme-switch-{theme}.png"));
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.HighContrast,
                    Wpf.Ui.Controls.WindowBackdropType.None, updateAccent: false);
                NetworkProfileManager.App.RefreshWindowThemes(); Pump();
                Capture(window, Path.Combine(output, $"navigation-high-contrast-{theme}.png"));
                ApplyRenderTheme(theme); Pump();
                if (fake.Sends != 0 || fake.Connects != 0 || reads != 1) throw new Exception("Navigation caused network work.");

                // Hiding the navigation list is not hiding its owner window: keep an existing test running.
                var refresh = diagnostics.RefreshAsync(); Until(() => refresh.IsCompleted); refresh.GetAwaiter().GetResult();
                diagnostics.SelectedAdapter = diagnostics.Adapters[0]; diagnostics.Source = diagnostics.Sources[0];
                diagnostics.Target = "192.0.2.229";
                diagnostics.SelectedTest = diagnostics.Presets.Single(p => p.Kind == DiagnosticTestKind.ContinuousPing);
                fake.Gate = null; delay.Pause = true;
                var running = diagnostics.RunSelectedTestAsync(); Until(() => delay.Entered);
                window.IsSidebarExpanded = false; Pump(); window.IsSidebarExpanded = true; Pump();
                if (delay.Token.IsCancellationRequested || !diagnostics.CanStop || fake.Sends != 1)
                    throw new Exception("Sidebar visibility cancelled or restarted an existing diagnostic operation.");
                diagnostics.StopCommand.Execute(null); Until(() => running.IsCompleted); running.GetAwaiter().GetResult();
                window.Close(); Pump();
            }
            finally { AppPaths.OverrideDataDirectory(null); }
        }
    }

    private static void ResizeNavigationKey(GridSplitter splitter, Key key)
    {
        var source = PresentationSource.FromVisual(splitter);
        splitter.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.KeyDownEvent });
        splitter.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.KeyUpEvent });
    }

    private static void DragNavigation(GridSplitter splitter, double delta)
    {
        splitter.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        splitter.RaiseEvent(new DragDeltaEventArgs(delta, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        splitter.RaiseEvent(new DragCompletedEventArgs(delta, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
    }

    private static void AssertNavigationBudget(Window window, Grid layout, FrameworkElement sidebar, FrameworkElement rail, FrameworkElement settings)
    {
        var workspace = (FrameworkElement)window.FindName("WorkspaceSurface");
        if (sidebar.ActualWidth < 199 || workspace.ActualWidth < 499 || rail.ActualWidth != 52
            || layout.ColumnDefinitions.Sum(c => c.ActualWidth) > layout.ActualWidth + 1)
            throw new Exception($"Navigation width budget failed: {layout.ActualWidth}, sidebar {sidebar.ActualWidth}, workspace {workspace.ActualWidth}.");
        foreach (var element in Descendants(rail).OfType<ButtonBase>())
        {
            var bounds = element.TransformToAncestor(window).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > window.ActualWidth + 1 || bounds.Bottom > window.ActualHeight + 1)
                throw new Exception("Rail command clipped.");
        }
        if (settings.TranslatePoint(new Point(0, settings.ActualHeight), rail).Y < rail.ActualHeight - 12)
            throw new Exception("Settings is not anchored at the rail footer.");
    }

    private static void CheckNavigationTitleBar(MainWindow window, FrameworkElement toggle)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var caption = window.PointToScreen(new Point(window.ActualWidth / 2, 20));
        var togglePoint = toggle.PointToScreen(new Point(toggle.ActualWidth / 2, toggle.ActualHeight / 2));
        static IntPtr Coordinates(Point point) => new(unchecked(((int)point.Y << 16) | ((int)point.X & 0xffff)));
        if (NavigationSendMessage(handle, 0x0084, IntPtr.Zero, Coordinates(caption)).ToInt32() != 2
            || NavigationSendMessage(handle, 0x0084, IntPtr.Zero, Coordinates(togglePoint)).ToInt32() != 1)
            throw new Exception("Title bar lost native caption hit testing or swallowed the toggle.");
        NavigationSendMessage(handle, 0x00A3, new IntPtr(2), Coordinates(caption)); Pump();
        if (window.WindowState != WindowState.Maximized) throw new Exception("Native caption double-click did not maximize.");
        NavigationSendMessage(handle, 0x00A3, new IntPtr(2), Coordinates(caption)); Pump();
        if (window.WindowState != WindowState.Normal) throw new Exception("Native caption double-click did not restore.");
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr NavigationSendMessage(IntPtr window, uint message, IntPtr parameter, IntPtr data);

    private static void CheckNavigationHover(MainWindow window, GridSplitter splitter, ToggleButton toggle, ListBox list, string output, string theme)
    {
        if (!NavigationGetCursorPos(out var original)) throw new Exception("Cannot save cursor position for hover verification.");
        try
        {
            var marker = (Border)splitter.Template.FindName("Divider", splitter);
            var scrollbar = Descendants(list).OfType<ScrollBar>().Single(s => s.Orientation == Orientation.Vertical);
            var workspace = (FrameworkElement)window.FindName("WorkspaceSurface");
            void MoveTo(FrameworkElement control, Point offset)
            {
                var point = control.PointToScreen(offset);
                if (!NavigationSetCursorPos((int)point.X, (int)point.Y)) throw new Exception("Cannot move pointer within fixture.");
                Pump(); Mouse.Synchronize(); Pump();
            }
            toggle.Focus();
            MoveTo(workspace, new Point(100, 100));
            Until(() => !splitter.IsMouseOver && !list.IsMouseOver);
            if (marker.Opacity != 0 || scrollbar.Opacity != 0) throw new Exception("Idle divider or scrollbar remains visible.");
            Capture(window, Path.Combine(output, $"navigation-idle-{theme}.png"));
            MoveTo(list, new Point(60, 40)); Until(() => list.IsMouseOver);
            if (scrollbar.Opacity != 1) throw new Exception("Adapter scrollbar did not reveal on pointer hover.");
            MoveTo(splitter, new Point(3, 120)); Until(() => splitter.IsMouseOver);
            if (marker.Opacity != 1 || splitter.Cursor != Cursors.SizeWE) throw new Exception("Resize target did not reveal/highlight with a resize cursor.");
            Capture(window, Path.Combine(output, $"navigation-hover-{theme}.png"));
            MoveTo(workspace, new Point(100, 100)); Until(() => !splitter.IsMouseOver);
            if (marker.Opacity != 0 || scrollbar.Opacity != 0) throw new Exception("Idle reveal controls did not disappear after pointer exit.");
            splitter.Focus(); Pump();
            if (marker.Opacity != 1) throw new Exception("Keyboard focus did not reveal the resize target.");
            Capture(window, Path.Combine(output, $"navigation-resize-focus-{theme}.png"));
            toggle.Focus(); Pump();
        }
        finally { NavigationSetCursorPos(original.X, original.Y); Pump(); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NavigationPoint { public int X, Y; }
    [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool NavigationGetCursorPos(out NavigationPoint point);
    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool NavigationSetCursorPos(int x, int y);
}
