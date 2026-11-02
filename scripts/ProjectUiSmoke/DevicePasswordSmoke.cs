using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckDevicePasswords(string output)
    {
        foreach (var theme in new[] { "Dark", "Light" })
        foreach (var minimum in new[] { false, true })
        {
            ApplyRenderTheme(theme);
            CheckDeviceDirectoryFlow(output, theme, minimum);
            CheckPasswordModal(output, theme, minimum);
        }
        Console.WriteLine("PASS: actual New/Edit modal clicks, detached cancel/exit/validation, multiple devices, linked apply, main/project surfaces, password masking/automation/reveal/copy/removal; Light/Dark normal/minimum; fake boundaries.");
    }
    private static void DeviceClick(System.Windows.Controls.Button button)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(button);
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
        Pump();
    }
    private static void WithDeviceModal(Window owner, System.Windows.Controls.Button button, FakeDialogs dialogs, Action<ProjectDeviceDialog> action)
    {
        Exception? failure = null;
        owner.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            var modal = owner.OwnedWindows.OfType<ProjectDeviceDialog>().Single(w => w.IsVisible);
            try { action(modal); }
            catch (Exception ex) { failure = ex; }
            finally { if (modal.IsVisible) { dialogs.Discard = true; modal.Close(); } }
        }));
        DeviceClick(button); Pump();
        if (failure != null) throw new Exception("Device dialog interaction failed.", failure);
    }
    private static void CheckDeviceDirectoryFlow(string output, string theme, bool minimum)
    {
        var dialogs = new FakeDialogs(); var browser = new FakeBrowser();
        var vm = new ProjectViewModel(dialogs, new ProjectStore(), browser, _ => true) { Section = 1 };
        var window = new ProjectWindow(vm) { Width = minimum ? 700 : 1000, Height = minimum ? 540 : 850 };
        var tag = $"{theme}-{window.Width}";
        try
        {
            window.Show(); Pump();
            var view = Descendants(window).OfType<DeviceDirectoryView>().Single(v => v.IsVisible);
            var add = (System.Windows.Controls.Button)view.FindName("NewDeviceButton");
            WithDeviceModal(window, add, dialogs, modal =>
            {
                modal.Width = minimum ? 440 : 620; modal.Height = minimum ? 480 : 760; Pump();
                var name = (System.Windows.Controls.TextBox)modal.FindName("DeviceNameInput");
                var url = (System.Windows.Controls.TextBox)modal.FindName("DeviceUrlInput");
                foreach (var section in new[] { "NetworkSection", "PasswordSection", "DetailsSection" })
                    if (((Expander)modal.FindName(section)).IsExpanded) throw new Exception("New device optional section starts expanded.");
                if (!name.IsVisible || !url.IsVisible || vm.HasUnsavedChanges) throw new Exception("Essential fields missing or opening dirtied the project.");
                name.Focus(); name.MoveFocus(new(System.Windows.Input.FocusNavigationDirection.Next)); Pump();
                if (!url.IsKeyboardFocusWithin) throw new Exception("Device name to URL keyboard order is broken.");
                AssertDeviceInViewport(url, (ScrollViewer)modal.FindName("EditorViewport"));
                Capture(modal, Path.Combine(output, $"device-add-{tag}.png"));
                name.Text = "Control panel"; url.Text = "file:///C:/NEVER_ECHO";
                DeviceClick((System.Windows.Controls.Button)modal.FindName("KeepButton")); Pump();
                if (vm.Devices.Count != 0 || !modal.IsVisible || !url.IsKeyboardFocusWithin || vm.Error.Contains("NEVER_ECHO"))
                    throw new Exception("Invalid URL was retained, echoed or not focused.");
                Capture(modal, Path.Combine(output, $"device-add-error-{tag}.png"));
                url.Text = "http://192.0.2.20/panel";
                DeviceClick((System.Windows.Controls.Button)modal.FindName("KeepButton")); Pump();
                if (modal.IsVisible || vm.Devices.Count != 1 || !vm.HasUnsavedChanges || vm.DeviceNotice.Length == 0)
                    throw new Exception("Actual Add did not retain/select the project draft.");
            });
            var first = vm.SelectedDevice!;
            if (browser.Opened != 0 || !vm.HasSelectedDevice || vm.HasLinkedDeviceProfile) throw new Exception("Addition ran an action or invented a profile.");
            Capture(window, Path.Combine(output, $"device-added-{tag}.png"));
            vm.TryStoreDevice(new ProjectDeviceEditor(new ProjectDevice { Name = "DSP", WebUrl = "https://198.51.100.30/control", Type = "Audio" })); Pump();
            var list = Descendants(view).OfType<ListBox>().Single();
            list.SelectedItem = vm.DeviceView.Cast<DeviceDirectoryEntry>().Single(r => r.Device.Id == first.Id); Pump();
            if (vm.SelectedDevice?.Id != first.Id) throw new Exception("Selecting a second device lost identity.");
            Capture(window, Path.Combine(output, $"device-multiple-{tag}.png"));
            var edit = (System.Windows.Controls.Button)view.FindName("EditDeviceButton");
            WithDeviceModal(window, edit, dialogs, modal =>
            {
                modal.Width = minimum ? 440 : 620; modal.Height = minimum ? 480 : 760; Pump();
                modal.Draft.Name = "Uncommitted rename";
                dialogs.Discard = false;
                modal.Close(); Pump();
                if (!modal.IsVisible || window.ConfirmApplicationExit() || vm.SelectedDevice!.Name != first.Name)
                    throw new Exception("Cancel/owner exit lost a detached draft.");
                Capture(modal, Path.Combine(output, $"device-edit-{tag}.png"));
                dialogs.Discard = true; modal.Close(); Pump();
                if (vm.SelectedDevice!.Name != first.Name) throw new Exception("Cancel changed the project.");
            });
            WithDeviceModal(window, edit, dialogs, modal =>
            {
                modal.Draft.Name = "Control panel renamed";
                ((Expander)modal.FindName("DetailsSection")).IsExpanded = true;
                modal.Draft.Ip = "invalid";
                DeviceClick((System.Windows.Controls.Button)modal.FindName("KeepButton")); Pump();
                if (!modal.IsVisible || vm.SelectedDevice!.Name != first.Name) throw new Exception("Invalid IP replaced the original.");
                modal.Draft.Ip = "192.0.2.20";
                DeviceClick((System.Windows.Controls.Button)modal.FindName("KeepButton")); Pump();
                if (modal.IsVisible || vm.SelectedDevice!.Id != first.Id || vm.SelectedDevice.Name != "Control panel renamed")
                    throw new Exception("Edit did not replace the same retained ID.");
            });
            if (!vm.TryStoreProfile(new ProjectProfileEditor { Name = "Equipment" })) throw new Exception(vm.Error);
            var linked = new ProjectDeviceEditor(vm.SelectedDevice!) { ProfileId = vm.Profiles[0].Id };
            if (!vm.TryStoreDevice(linked)) throw new Exception(vm.Error);
            int apply = 0; vm.ApplyAndOpenRequested += (_, _) => apply++;
            Pump();
            var applyButton = Descendants(view).OfType<System.Windows.Controls.Button>().Single(b => b.Command == vm.ApplyAndOpenCommand);
            if (!applyButton.IsVisible) throw new Exception("Linked Apply & Open unavailable.");
            vm.ApplyAndOpenCommand.Execute(null);
            if (apply != 1 || browser.Opened != 0) throw new Exception("Linked action did not preserve the explicit handoff.");
            applyButton.BringIntoView(); Pump();
            Capture(window, Path.Combine(output, $"device-linked-{tag}.png"));
        }
        finally { dialogs.Discard = true; window.Close(); Pump(); }
        AppPaths.OverrideDataDirectory(Path.Combine(output, "device-main-" + tag));
        try
        {
            var main = new MainViewModel(() => new()
            {
                new() { Id = "11111111-1111-4111-8111-111111111111", Name = "Ethernet", AdapterType = "Ethernet", IsConnected = true }
            });
            var host = new MainWindow(main, vm) { Width = minimum ? 760 : 1200, Height = minimum ? 520 : 850 };
            try
            {
                host.Show(); Pump(); ((TabControl)host.FindName("WorkspaceTabs")).SelectedIndex = 1; Pump();
                var view = Descendants(host).OfType<DeviceDirectoryView>().Single(v => v.IsVisible);
                view.BringIntoView(); Pump();
                var open = Descendants(view).OfType<System.Windows.Controls.Button>().Single(b => b.Command == vm.OpenDeviceCommand);
                open.BringIntoView(); Pump();
                if (!open.IsEnabled) throw new Exception("Main device Open is unavailable.");
                Capture(host, Path.Combine(output, $"device-main-{tag}.png"));
                // Actual edit entry works from the main surface too.
                WithDeviceModal(host, (System.Windows.Controls.Button)view.FindName("EditDeviceButton"), dialogs, modal =>
                {
                    modal.Draft.Name = "Main detached edit"; dialogs.Discard = false;
                    if (host.ConfirmApplicationExit()) throw new Exception("Main exit ignored a device modal draft.");
                    dialogs.Discard = true; modal.Close();
                });
            }
            finally { host.Close(); Pump(); }
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }
    private static void CheckPasswordModal(string output, string theme, bool minimum)
    {
        var dialogs = new FakeDialogs(); int copies = 0;
        var vm = new ProjectViewModel(dialogs, new ProjectStore(), new FakeBrowser(), _ => { copies++; return true; });
        var stored = new ProjectDeviceEditor(new ProjectDevice { Name = "Stored password fixture", WebUrl = "https://192.0.2.20/panel" });
        stored.SavePassword = true; stored.Password = "SYNTHETIC-UI-ONLY";
        if (!vm.TryStoreDevice(stored)) throw new Exception(vm.Error);
        var original = vm.SelectedDevice!;
        var detailsHost = new ProjectWindow(vm) { Width = minimum ? 700 : 1000, Height = minimum ? 540 : 850 };
        try
        {
            vm.Section = 1; detailsHost.Show(); Pump();
            var details = Descendants(detailsHost).OfType<DeviceDirectoryView>().Single(v => v.IsVisible);
            var text = (TextBlock)details.FindName("DeviceRevealedPassword");
            if (text.IsVisible || text.Text.Length != 0) throw new Exception("Details showed a stored password without Show.");
            var showDetails = Descendants(details).OfType<System.Windows.Controls.Button>().Single(b => b.Command == vm.RevealPasswordCommand);
            DeviceClick(showDetails); Pump();
            if (!text.IsVisible || text.Text != stored.Password) throw new Exception("Details Show failed.");
            vm.Section = 0; Pump(); vm.Section = 1; Pump();
            if (text.Text.Length != 0 || vm.DeviceEditor.IsPasswordRevealed) throw new Exception("Leaving details retained a revealed password.");
            var copyDetails = Descendants(details).OfType<System.Windows.Controls.Button>().Single(b => b.Command == vm.CopyPasswordCommand);
            DeviceClick(copyDetails); Pump();
            if (copies != 1 || vm.SelectedDevice != original) throw new Exception("Details Copy changed the stored device.");
            copyDetails.BringIntoView(); Pump();
            Capture(detailsHost, Path.Combine(output, $"password-details-{theme}-{detailsHost.Width}.png"));
        }
        finally { detailsHost.Close(); Pump(); }
        var modal = new ProjectDeviceDialog(vm, original) { Width = minimum ? 440 : 620, Height = minimum ? 480 : 760 };
        var tag = $"{theme}-{modal.Width}";
        try
        {
            modal.Show(); Pump();
            ((Expander)modal.FindName("PasswordSection")).IsExpanded = true; Pump();
            var fields = (DevicePasswordFields)modal.FindName("PasswordFields");
            var input = (PasswordBox)fields.FindName("DevicePasswordInput");
            var revealed = (TextBlock)fields.FindName("DeviceRevealedPassword");
            fields.FocusInput(); ScrollDeviceControl(input, (ScrollViewer)modal.FindName("EditorViewport"));
            if (input.Password != "SYNTHETIC-UI-ONLY" || revealed.IsVisible || revealed.Text.Length != 0) throw new Exception("Stored credential unmasked or lost.");
            var peer = UIElementAutomationPeer.CreatePeerForElement(input);
            if (!peer.IsPassword()) throw new Exception("Missing password automation semantics.");
            var provider = peer.GetPattern(PatternInterface.Value) as IValueProvider;
            bool denied = provider == null;
            try { if (provider != null) denied = provider.Value.Length == 0; } catch (InvalidOperationException) { denied = true; }
            if (!denied) throw new Exception("Masked input exposed its automation value.");
            Capture(modal, Path.Combine(output, $"password-stored-{tag}.png"));
            var show = Descendants(fields).OfType<System.Windows.Controls.Button>().Single(b => b.Content?.ToString() == Loc.Get("Password.Show"));
            var copy = Descendants(fields).OfType<System.Windows.Controls.Button>().Single(b => b.Content?.ToString() == Loc.Get("Password.Copy"));
            ScrollDeviceControl(copy, (ScrollViewer)modal.FindName("EditorViewport"));
            Capture(modal, Path.Combine(output, $"password-actions-{tag}.png"));
            DeviceClick(show); Pump();
            if (!revealed.IsVisible || revealed.Text != stored.Password) throw new Exception("Explicit Show did not reveal the draft.");
            ((Expander)modal.FindName("PasswordSection")).IsExpanded = false; Pump();
            if (revealed.Text.Length != 0 || modal.Draft.IsPasswordRevealed) throw new Exception("Collapsing left revealed text.");
            ((Expander)modal.FindName("PasswordSection")).IsExpanded = true; Pump();
            DeviceClick(copy); Pump();
            if (copies != 2 || vm.SelectedDevice != original) throw new Exception("Copy modified retained intent.");
            int prompts = dialogs.DiscardPrompts;
            if (!modal.ConfirmApplicationExit() || dialogs.DiscardPrompts != prompts) throw new Exception("Show/copy dirtied the modal draft.");
            input.Password = ""; DeviceClick((System.Windows.Controls.Button)modal.FindName("KeepButton")); Pump();
            if (!modal.IsVisible || vm.SelectedDevice!.Password != stored.Password) throw new Exception("Empty input silently removed the password.");
            Capture(modal, Path.Combine(output, $"password-invalid-{tag}.png"));
            input.Password = new string('x', 2049); Pump(); DeviceClick((System.Windows.Controls.Button)modal.FindName("KeepButton")); Pump();
            if (modal.Draft.Password.Length != 2049 || !modal.IsVisible) throw new Exception("Long paste was truncated or accepted.");
            var remove = Descendants(fields).OfType<System.Windows.Controls.Button>().Single(b => b.Content?.ToString() == Loc.Get("Password.Remove"));
            DeviceClick(remove); Pump();
            if (input.Password.Length != 0 || modal.Draft.SavePassword) throw new Exception("Explicit Remove did not clear the masked input.");
            Capture(modal, Path.Combine(output, $"password-removed-{tag}.png"));
            DeviceClick((System.Windows.Controls.Button)modal.FindName("KeepButton")); Pump();
            if (modal.IsVisible || vm.SelectedDevice!.Password != null) throw new Exception("Removal was not retained.");
        }
        finally { dialogs.Discard = true; modal.Close(); Pump(); }
        var fresh = new ProjectDeviceDialog(vm) { Width = minimum ? 440 : 620, Height = minimum ? 480 : 760 };
        try { fresh.Show(); Pump(); if (fresh.Draft.SavePassword) throw new Exception("New modal inherited a password."); Capture(fresh, Path.Combine(output, $"password-new-{tag}.png")); }
        finally { fresh.Close(); Pump(); }
    }
    private static void AssertDeviceInViewport(FrameworkElement control, ScrollViewer viewport)
    {
        var bounds = control.TransformToAncestor(viewport).TransformBounds(new Rect(control.RenderSize));
        if (bounds.Top < -1 || bounds.Bottom > viewport.ActualHeight + 1 || bounds.Right > viewport.ActualWidth + 1)
            throw new Exception($"Device field/action clipped: {bounds}; viewport={viewport.ActualWidth}x{viewport.ActualHeight}; scroll={viewport.VerticalOffset}/{viewport.ExtentHeight}.");
    }
    private static void ScrollDeviceControl(FrameworkElement control, ScrollViewer viewport)
    {
        // Fluent disclosure transitions settle on subsequent render ticks, beyond a queue drain.
        var frame = new DispatcherFrame(); var started = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        timer.Tick += (_, _) =>
        {
            control.BringIntoView(); viewport.UpdateLayout();
            var bounds = control.TransformToAncestor(viewport).TransformBounds(new Rect(control.RenderSize));
            if ((bounds.Top >= -1 && bounds.Bottom <= viewport.ActualHeight + 1 && control.IsVisible && DateTime.UtcNow - started > TimeSpan.FromMilliseconds(300))
                || DateTime.UtcNow - started > TimeSpan.FromSeconds(2)) { timer.Stop(); frame.Continue = false; }
        };
        timer.Start(); Dispatcher.PushFrame(frame); Pump();
        AssertDeviceInViewport(control, viewport);
    }
}
