using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void RenderAdapterVisibility(string output)
    {
        AppPaths.OverrideDataDirectory(Path.Combine(output, "adapter-fixture"));
        try
        {
            foreach (var theme in new[] { "Dark", "Light" })
            foreach (var minimum in new[] { false, true })
            {
                ApplyRenderTheme(theme);
                int reads = 0;
                var main = new MainViewModel(() =>
                {
                    reads++;
                    return new() {
                        new() { Id = "ethernet", Name = "Ethernet", AdapterType = "Ethernet", IsConnected = true, CurrentIp = "192.0.2.10" },
                        new() { Id = "usb", Name = "USB Ethernet (disconnected)", AdapterType = "Ethernet" },
                        new() { Id = "wifi", Name = "Wi-Fi", AdapterType = "Wireless80211" },
                        new() { Id = "vpn", Name = "Radmin VPN", AdapterType = "Ethernet" },
                        new() { Id = "hyperv", Name = "vEthernet (lab)", AdapterType = "Ethernet" },
                        new() { Id = "filter", Name = "Ethernet-WFP Native MAC Layer LightWeight Filter-0000", IsSystemInterface = true },
                        new() { Id = "wan", Name = "WAN miniport", IsSystemInterface = true }
                    };
                });
                var project = new ProjectViewModel(new FakeDialogs(), new FakeBrowser());
                var window = new MainWindow(main, project) { Width = minimum ? 760 : 1200, Height = minimum ? 520 : 900 };
                window.Show(); Pump();
                var panel = Descendants(window).OfType<AdapterListPanel>().Single();
                var list = Descendants(panel).OfType<ListBox>().Single();
                var options = (Wpf.Ui.Controls.Button)panel.FindName("AdapterListOptions");
                var menu = options.ContextMenu!;
                var toggle = (MenuItem)menu.Items[0];
                options.Focus(); Pump();
                if (!options.IsKeyboardFocusWithin || AutomationProperties.GetName(options) != Loc.Get("Adapters.ListOptions"))
                    throw new Exception("Adapter list options is unnamed or cannot receive keyboard focus.");
                AssertWorkspaceBounds(window, options);
                CaptureToolbar((FrameworkElement)options.Parent, window, Path.Combine(output, $"adapter-header-{theme}-{minimum}.png"));
                options.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                if (!menu.IsOpen || !ReferenceEquals(menu.DataContext, main)) throw new Exception("Adapter list options lost its view model or did not open.");
                if (list.Items.Count != 5 || toggle.IsChecked != false || main.SelectedAdapter?.Info.Id != "ethernet")
                    throw new Exception("Default list hid a working adapter or showed plumbing.");
                if (AutomationProperties.GetName(toggle) != Loc.Get("Adapters.ShowAll")) throw new Exception("Missing toggle accessible name.");
                toggle.Focus(); Pump();
                if (!toggle.IsKeyboardFocusWithin) throw new Exception("Adapter toggle cannot receive keyboard focus.");
                menu.IsOpen = false; Pump();
                var selected = main.SelectedAdapter;
                selected.EditIp = "192.0.2.42";
                Capture(window, Path.Combine(output, $"adapters-default-{theme}-{minimum}.png"));
                options.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                ((IInvokeProvider)new MenuItemAutomationPeer(toggle).GetPattern(PatternInterface.Invoke)).Invoke(); Pump();
                if (list.Items.Count != 7 || !main.ShowAllAdapters || !ReferenceEquals(main.SelectedAdapter, selected)
                    || selected.EditIp != "192.0.2.42" || reads != 1) throw new Exception("Show all reloaded/dropped drafts or changed target.");
                Capture(window, Path.Combine(output, $"adapters-all-{theme}-{minimum}.png"));
                list.SelectedItem = main.Adapters.Single(a => a.Info.Id == "filter"); Pump();
                options.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                if (!toggle.IsChecked) throw new Exception("Reopened menu lost the Show all checkmark.");
                // Let the existing popup opening animation reach its final layout before capturing.
                var menuAnimation = System.Diagnostics.Stopwatch.StartNew();
                while (menuAnimation.ElapsedMilliseconds < 300) { Pump(); System.Threading.Thread.Sleep(10); }
                Pump();
                var popupRoot = (FrameworkElement)PresentationSource.FromVisual(menu).RootVisual;
                var menuBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(popupRoot.ActualWidth), (int)Math.Ceiling(popupRoot.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                menuBitmap.Render(popupRoot);
                var menuPng = new System.Windows.Media.Imaging.PngBitmapEncoder();
                menuPng.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(menuBitmap));
                using (var stream = File.Create(Path.Combine(output, $"adapter-options-menu-{theme}-{minimum}.png"))) menuPng.Save(stream);
                ((IInvokeProvider)new MenuItemAutomationPeer(toggle).GetPattern(PatternInterface.Invoke)).Invoke(); Pump();
                if (main.SelectedAdapter != null || list.SelectedItem != null || list.Items.Count != 5 || reads != 1)
                    throw new Exception("Hiding a selected filter silently switched targets.");
                Capture(window, Path.Combine(output, $"adapters-cleared-{theme}-{minimum}.png"));
                window.Close(); main.Diagnostics.Dispose(); Pump();
            }
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }
}
