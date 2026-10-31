using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void RenderWorkspace(string output, string theme, int width, int height)
    {
        var isolatedRoot = Path.Combine(output, "workspace-fixture-" + theme + width);
        AppPaths.OverrideDataDirectory(isolatedRoot);
        try
        {
            var id = Guid.Parse("11111111-1111-4111-8111-111111111111");
            var main = new MainViewModel(() => new()
            {
                new() { Id = id.ToString(), Name = "Ethernet", Description = "Realtek PCIe GbE Family Controller", AdapterType = "Ethernet",
                    CurrentIp = "192.0.2.100", CurrentSubnet = "255.255.255.0", CurrentPrimaryDns = "198.51.100.53", IsConnected = true },
                new() { Id = "22222222-2222-4222-8222-222222222222", Name = "USB Ethernet", AdapterType = "Ethernet", IsConnected = false },
                new() { Id = "33333333-3333-4333-8333-333333333333", Name = "Wi-Fi", AdapterType = "Wireless80211", IsDhcp = true,
                    CurrentIp = "198.51.100.25", CurrentSubnet = "255.255.255.0", IsConnected = true }
            });
            foreach (var (name, address) in new[] { ("Woxcon Video Wall", "192.0.2.100"), ("Extron Control", "198.51.100.100"), ("DSP / Dante", "203.0.113.100") })
                main.SelectedAdapter!.Profiles.Add(new(new() { Name = name, IpAddress = address }));
            main.SelectedAdapter!.Profiles.Add(new(new() { Name = "Office / DHCP", IsDhcp = true }));
            var dialogs = new FakeDialogs(); var browser = new FakeBrowser();
            var project = new ProjectViewModel(dialogs, browser) { ProjectName = "Demo Hall" };
            foreach (var (name, address) in new[] { ("Woxcon Video Wall", "192.0.2.100"), ("Extron Control", "198.51.100.100"), ("DSP / Dante", "203.0.113.100") })
            {
                var draft = new ProjectProfileEditor { Name = name, Mode = "static", Address = address, DnsMode = "static", Dns1 = "198.51.100.53" };
                if (!project.TryStoreProfile(draft)) throw new Exception(project.Error);
            }
            if (!project.TryStoreProfile(new() { Name = "Office / DHCP" })) throw new Exception(project.Error);
            project.DeviceEditor.Name = "TouchPanel — Main";
            project.DeviceEditor.WebUrl = "https://192.0.2.20:8443/web/panel/index.html#!/main";
            project.DeviceEditor.ProfileId = project.Profiles[0].Id;
            project.KeepDeviceCommand.Execute(null);
            var requested = 0; Guid? target = null; ProjectDevice? requestedDevice = null;
            var window = new MainWindow(main, project, (_, device, adapter) => { requested++; target = adapter; requestedDevice = device; })
            { Width = width, Height = height };
            window.Show(); Pump();
            Capture(window, Path.Combine(output, $"workspace-adapter-{theme}-{width}.png"));
            var scopes = (TabControl)window.FindName("ProfileScopes"); scopes.SelectedIndex = 1; Pump();
            if (width < 1000) Descendants(window).OfType<ProjectProfilesPanel>().Single().BringIntoView();
            Pump();
            Capture(window, Path.Combine(output, $"workspace-project-{theme}-{width}.png"));
            var apply = Descendants(window).OfType<Wpf.Ui.Controls.Button>().First(b => b.Tag is ProjectProfile && b.Content?.ToString() == "Apply to Ethernet");
            apply.BringIntoView(); Pump();
            AssertWorkspaceBounds(window, apply);
            apply.Focus(); Pump();
            if (!apply.IsKeyboardFocusWithin || !apply.MoveFocus(new(System.Windows.Input.FocusNavigationDirection.Next)))
                throw new Exception("Workspace profile actions were not keyboard reachable.");
            apply.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            if (requested != 1 || target != id || requestedDevice != null || browser.Opened != 0)
                throw new Exception("Profile Apply did not retain its explicit target or launched a browser.");
            main.SelectedAdapter = main.Adapters[1]; Pump();
            if (apply.Content?.ToString() != "Apply to USB Ethernet") throw new Exception("Apply label retained the old target.");
            apply.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            if (requested != 2 || target?.ToString() != main.Adapters[1].Info.Id) throw new Exception("Apply target was guessed from a profile hint.");
            main.SelectedAdapter = main.Adapters[0]; Pump();
            // A long alias must wrap its target label instead of widening the window.
            var originalInfo = main.SelectedAdapter.Info;
            var longInfo = new AdapterInfo { Id = id.ToString(), Name = "USB Ethernet — Control room service connection", IsConnected = true, CurrentIp = "192.0.2.100" };
            main.SelectedAdapter.UpdateInfo(longInfo);
            // Refreshing the existing object preserves selection; labels must follow its name.
            Pump();
            if (!apply.Content!.ToString()!.Contains("Control room")) throw new Exception("Renamed NIC did not update the target label.");
            apply.BringIntoView(); Pump(); AssertWorkspaceBounds(window, apply);
            main.SelectedAdapter.UpdateInfo(originalInfo); Pump();
            var tabs = (TabControl)window.FindName("WorkspaceTabs"); tabs.SelectedIndex = 1; Pump();
            ((FrameworkElement)window.FindName("DeviceWorkspace")).BringIntoView(); Pump();
            Capture(window, Path.Combine(output, $"workspace-devices-{theme}-{width}.png"));
            project.ApplyAndOpenCommand.Execute(null);
            if (requested != 3 || requestedDevice?.Id != project.SelectedDevice?.Id || target != id)
                throw new Exception("Main device action bypassed selected target.");
            dialogs.Open = false; project.OpenDeviceCommand.Execute(null);
            if (browser.Opened != 0) throw new Exception("Declined Open launched a browser.");
            dialogs.Open = true; project.OpenDeviceCommand.Execute(null);
            if (browser.Opened != 1) throw new Exception("Explicit Open did not use the fake browser.");
            tabs.SelectedIndex = 0; scopes.SelectedIndex = 0; Pump();
            var manual = Descendants(window).OfType<Expander>().First(); manual.IsExpanded = true; Pump();
            Capture(window, Path.Combine(output, $"workspace-manual-{theme}-{width}.png"));
            // Modal fields are detached. Validation failure and Cancel retain the old record.
            var original = project.Profiles[0];
            var editor = new ProjectProfileDialog(project, original) { Owner = window, Height = height < 700 ? 480 : 700 };
            editor.Show(); Pump();
            var draftEditor = (ProjectProfileEditor)editor.DataContext; draftEditor.Name = "Unsaved modal edit";
            Capture(editor, Path.Combine(output, $"workspace-editor-{theme}-{width}.png"));
            draftEditor.Address = "invalid";
            var keep = Descendants(editor).OfType<Wpf.Ui.Controls.Button>().Single(b => b.Content?.ToString() == Loc.Get("Project.KeepProfile"));
            AssertWorkspaceBounds(editor, keep);
            keep.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); Pump();
            if (!ReferenceEquals(project.Profiles[0], original) || !editor.IsVisible || string.IsNullOrEmpty(((TextBlock)editor.FindName("EditorError")).Text))
                throw new Exception("Invalid modal edit replaced the retained profile.");
            Capture(editor, Path.Combine(output, $"workspace-editor-error-{theme}-{width}.png"));
            dialogs.Discard = false; var prompts = dialogs.DiscardPrompts;
            if (window.ConfirmApplicationExit() || dialogs.DiscardPrompts != prompts + 1 || editor.ConfirmApplicationExit())
                throw new Exception("Tray exit did not protect the detached modal draft.");
            editor.Close(); Pump();
            if (!editor.IsVisible) throw new Exception("Declined modal close discarded the draft.");
            dialogs.Discard = true; editor.Close(); Pump();
            dialogs.Discard = false;
            if (window.ConfirmApplicationExit()) throw new Exception("Main/tray exit lost the shared project draft.");
            var manage = new ProjectWindow(project) { Owner = window }; manage.Show(); Pump(); manage.Close(); Pump();
            if (manage.IsVisible || !ReferenceEquals(window.Project, project) || !project.HasUnsavedChanges)
                throw new Exception("Closing Manage project discarded the main workspace.");
            main.SelectedAdapter = null; Pump();
            Capture(window, Path.Combine(output, $"workspace-no-adapter-{theme}-{width}.png"));
            main.SelectedAdapter = main.Adapters[0]; Pump();
            foreach (var control in Descendants(window).OfType<Control>().Where(c => c.IsVisible))
            {
                if (control is not (System.Windows.Controls.TextBox or ComboBox or ListBox or TabControl) || control.TemplatedParent != null) continue;
                if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
                    throw new Exception("Unnamed workspace input: " + control.GetType().Name);
            }
            dialogs.Discard = true;
            if (!window.ConfirmApplicationExit()) throw new Exception("Approved workspace exit was blocked.");
            window.Close(); Pump();
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }

    private static void AssertWorkspaceBounds(Window window, FrameworkElement element)
    {
        var point = element.TranslatePoint(new Point(0, 0), window);
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0 || point.X < 0 || point.Y < 0
            || point.X + element.ActualWidth > window.ActualWidth + 1 || point.Y + element.ActualHeight > window.ActualHeight + 1)
            throw new Exception("Workspace action clipped at window size: " + element.GetType().Name);
    }
}
