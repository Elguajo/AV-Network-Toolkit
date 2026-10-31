using System.Diagnostics;
using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;
using Wpf.Ui.Controls;

// Isolated component render. Never calls App.Run/OnStartup, MainWindow, NIC APIs,
// tray/monitor, OS network commands or native file dialogs.
internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var output = Path.GetFullPath(args.Length > 0 ? args[0] : "TestResults/ProjectUiSmoke");
        Directory.CreateDirectory(output);
        var errors = new StringWriter();
        PresentationTraceSources.DataBindingSource.Listeners.Add(new TextWriterTraceListener(errors));
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        // Match a real WPF event's continuation owner without running App startup.
        System.Threading.SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary { Theme = Wpf.Ui.Appearance.ApplicationTheme.Dark });
        app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/NetworkProfileManager;component/Themes/Styles.xaml") });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/NetworkProfileManager;component/Strings/En.xaml") });
        app.Resources.Add("BoolToVis", new NetworkProfileManager.Converters.BoolToVisibilityConverter());
        app.Resources.Add("AdapterTypeToSymbol", new NetworkProfileManager.Converters.AdapterTypeToSymbolConverter());
        try
        {
            foreach (var (language, theme, width, height) in new[]
            {
                ("En", "Dark", 900, 780), ("En", "Light", 700, 540), ("En", "Dark", 700, 540), ("En", "Light", 900, 780)
            })
            {
                var dictionaries = app.Resources.MergedDictionaries;
                dictionaries[0] = new Wpf.Ui.Markup.ThemesDictionary
                { Theme = theme == "Dark" ? Wpf.Ui.Appearance.ApplicationTheme.Dark : Wpf.Ui.Appearance.ApplicationTheme.Light };
                dictionaries[dictionaries.Count - 1] = new ResourceDictionary
                { Source = new Uri($"pack://application:,,,/NetworkProfileManager;component/Strings/{language}.xaml") };
                var dialogs = new FakeDialogs();
                var browser = new FakeBrowser();
                var vm = new ProjectViewModel(dialogs, browser);
                var window = new ProjectWindow { DataContext = vm, Width = width, Height = height };
                window.Show();
                Pump();
                NetworkProfileManager.Helpers.LanguageManager.ApplyPreference("tr", persist: false);
                Pump();
                if (NetworkProfileManager.Helpers.LanguageManager.EffectiveLanguage != "en")
                    throw new Exception("Retired Turkish preference did not use English resources.");
                if (vm.HasUnsavedChanges) throw new Exception("Opening a blank form marked it dirty without input.");
                vm.Editor.Name = "AV static network";
                vm.Editor.Mode = "static";
                vm.Editor.Address = "192.0.2.10";
                vm.Editor.Gateway = "192.0.2.1";
                vm.Editor.DnsMode = "static";
                vm.Editor.Dns1 = "192.0.2.53";
                vm.KeepProfileCommand.Execute(null);
                Pump();
                if (vm.Profiles.Count != 1 || vm.SelectedProfile?.Id != vm.Editor.Id || vm.Error.Length != 0)
                    throw new Exception("Profile bindings did not retain the selected editor identity.");
                vm.Editor.Name = "Edited profile";
                vm.KeepProfileCommand.Execute(null);
                Pump();
                if (vm.Profiles.Count != 1 || vm.Profiles[0].Name != "Edited profile" || vm.SelectedProfile?.Id != vm.Editor.Id)
                    throw new Exception("List replacement broke profile editor bindings.");
                if (dialogs.DiscardPrompts != 0) throw new Exception("Keeping an edit prompted to discard it.");
                var delete = Descendants(window).OfType<Wpf.Ui.Controls.Button>()
                    .Single(b => ReferenceEquals(b.Command, vm.DeleteProfileCommand));
                if (!delete.IsEnabled) throw new Exception("Delete did not enable after selection.");
                foreach (var control in Descendants(window).OfType<Control>())
                {
                    if (control is System.Windows.Controls.TextBox or ComboBox or ListBox)
                    {
                        // Template-part text boxes inherit their public control's name.
                        if (control is System.Windows.Controls.TextBox && control.TemplatedParent != null) continue;
                        if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
                            throw new Exception("Unnamed form control: " + control.GetType().Name);
                    }
                }
                var image = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                image.Render(window);
                // Native Mica is outside RenderTargetBitmap; composite transparent content
                // over the theme's existing application surface for an inspectable PNG.
                var composite = new DrawingVisual();
                using (var context = composite.RenderOpen())
                {
                    var bounds = new Rect(0, 0, image.PixelWidth, image.PixelHeight);
                    context.DrawRectangle((Brush)window.FindResource("ApplicationBackgroundBrush"), null, bounds);
                    context.DrawImage(image, bounds);
                }
                var flattened = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Pbgra32);
                flattened.Render(composite);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(flattened));
                using (var stream = File.Create(Path.Combine(output, $"project-{language}-{theme}-{width}.png"))) encoder.Save(stream);
                vm.NewProfileCommand.Execute(null);
                vm.Editor.Name = "Second profile";
                vm.KeepProfileCommand.Execute(null);
                Pump();
                vm.Editor.Name = "Unsaved selection edit";
                dialogs.Discard = false;
                var list = Descendants(window).OfType<ListBox>().Single();
                var selected = vm.SelectedProfile;
                list.SelectedItem = vm.Profiles[0]; Pump();
                if (!ReferenceEquals(vm.SelectedProfile, selected) || !ReferenceEquals(list.SelectedItem, selected))
                    throw new Exception("Declining selection discard left the list and editor inconsistent.");
                vm.Section = 1; Pump();
                Capture(window, Path.Combine(output, $"devices-empty-{language}-{theme}-{width}.png"));
                if (!vm.NoDeviceMatches || vm.OpenDeviceCommand.CanExecute(null) || vm.DeleteDeviceCommand.CanExecute(null))
                    throw new Exception("Empty device directory has incorrect command state.");
                for (int i = 0; i < 24; i++)
                {
                    vm.NewDeviceCommand.Execute(null);
                    vm.DeviceEditor.Name = "Control panel " + i;
                    vm.DeviceEditor.Ip = "192.0.2.20";
                    vm.DeviceEditor.WebUrl = $"https://192.0.2.20:8443/web/panel?view={i}&lang=en#!/main";
                    vm.DeviceEditor.Type = i < 12 ? "Control panels" : "Displays";
                    vm.DeviceEditor.ProfileId = vm.Profiles[0].Id;
                    vm.KeepDeviceCommand.Execute(null); Pump();
                    if (vm.Error.Length != 0 || vm.DeviceEditor.ProfileId != vm.Profiles[0].Id)
                        throw new Exception("Bound device retention lost its profile: " + vm.Error);
                }
                if (vm.Devices.Count != 24 || vm.SelectedDevice?.Id != vm.DeviceEditor.Id)
                    throw new Exception("Device list replacement lost identity.");
                var deviceList = Descendants(window).OfType<ListBox>().Single(b => b.ItemsSource == vm.DeviceView);
                if ((deviceList.SelectedItem as DeviceDirectoryEntry)?.Device.Id != vm.DeviceEditor.Id)
                    throw new Exception("Selected device row does not match the editor.");
                deviceList.ScrollIntoView(deviceList.SelectedItem); Pump();
                Capture(window, Path.Combine(output, $"devices-selected-{language}-{theme}-{width}.png"));
                var selectedDevice = vm.SelectedDevice;
                vm.DeviceEditor.Name = "Unsaved device draft";
                deviceList.SelectedItem = vm.DeviceView.Cast<DeviceDirectoryEntry>().First(r => ReferenceEquals(r.Device, vm.Devices[0])); Pump();
                if (!ReferenceEquals(vm.SelectedDevice, selectedDevice) || (deviceList.SelectedItem as DeviceDirectoryEntry)?.Device.Id != selectedDevice?.Id)
                    throw new Exception("Declining device selection discard left list/editor inconsistent.");
                var deviceId = vm.DeviceEditor.Id;
                vm.DeviceSearch = "no match"; Pump();
                if (vm.DeviceEditor.Id != deviceId || vm.DeviceEditor.Name != "Unsaved device draft" || vm.SelectedDevice?.Id != deviceId)
                    throw new Exception("Filtering lost the device draft.");
                vm.DeviceGroupBy = "profile"; Pump();
                vm.DeviceSearch = ""; Pump();
                if ((deviceList.SelectedItem as DeviceDirectoryEntry)?.Device.Id != vm.SelectedDevice?.Id)
                    throw new Exception("Clearing the search did not restore the selected device row.");
                if (vm.DeviceEditor.ProfileId != vm.Profiles[0].Id)
                    throw new Exception("Grouping reset the related profile.");
                vm.DeviceEditor.WebUrl = "file:///C:/NEVER_ECHO";
                vm.KeepDeviceCommand.Execute(null); Pump();
                if (vm.Error.Length == 0 || vm.Error.Contains("NEVER_ECHO") || vm.DeviceDestination.Contains("NEVER_ECHO"))
                    throw new Exception("Invalid device URL did not produce a redacted error.");
                Capture(window, Path.Combine(output, $"devices-error-{language}-{theme}-{width}.png"));
                vm.DeviceEditor.WebUrl = selectedDevice!.WebUrl;
                vm.OpenDeviceCommand.Execute(null); Pump();
                if (dialogs.OpenPrompts != 1 || browser.Opened != 0) throw new Exception("Browser cancellation was ignored.");
                dialogs.Open = true;
                vm.OpenDeviceCommand.Execute(null); Pump();
                if (browser.Opened != 1 || vm.DeviceNotice.Length == 0) throw new Exception("Fake browser handoff failed.");
                foreach (var control in Descendants(window).OfType<Control>())
                    if (control is System.Windows.Controls.TextBox or ComboBox or ListBox)
                    {
                        if (control is System.Windows.Controls.TextBox && control.TemplatedParent != null) continue;
                        if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
                            throw new Exception("Unnamed device control: " + control.GetType().Name);
                    }
                var openButton = Descendants(window).OfType<Wpf.Ui.Controls.Button>()
                    .Single(b => ReferenceEquals(b.Command, vm.OpenDeviceCommand));
                var buttonBounds = openButton.TransformToAncestor(window).TransformBounds(new Rect(openButton.RenderSize));
                if (!openButton.IsEnabled || buttonBounds.Bottom > window.ActualHeight || buttonBounds.Right > window.ActualWidth)
                    throw new Exception("Device footer action is clipped or disabled.");
                var urlInput = Descendants(window).OfType<Wpf.Ui.Controls.TextBox>()
                    .Single(t => AutomationProperties.GetName(t) == "Full web interface URL");
                urlInput.Focus(); Pump();
                bool reachedOpen = openButton.IsKeyboardFocusWithin;
                for (int i = 0; i < 80 && !reachedOpen; i++)
                {
                    if (System.Windows.Input.Keyboard.FocusedElement is not UIElement focused) break;
                    focused.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
                    Pump(); reachedOpen = openButton.IsKeyboardFocusWithin;
                }
                if (!reachedOpen) throw new Exception("Forward focus traversal did not reach Open Web Interface.");
                Capture(window, Path.Combine(output, $"devices-details-{language}-{theme}-{width}.png"));
                dialogs.Discard = false;
                window.Close(); Pump();
                if (!window.IsVisible || window.ConfirmApplicationExit()) throw new Exception("Unsaved draft exit refusal was ignored.");
                dialogs.Discard = true;
                if (!window.ConfirmApplicationExit()) throw new Exception("Confirmed exit was rejected.");
                window.Close();
                RenderApply(output, theme, width == 900 ? 760 : 620, height == 780 ? 760 : 520);
                RenderLinkLocal(output, theme, width == 900 ? 760 : 620, height == 780 ? 760 : 520);
                RenderMdns(output, theme, width == 900 ? 820 : 620, height == 780 ? 760 : 540);
                RenderWorkspace(output, theme, width == 900 ? 1200 : 760, height == 780 ? 850 : 520);
            }
            if (errors.ToString().Length != 0) throw new Exception("WPF binding errors: " + errors);
            Console.WriteLine("PASS: isolated project/device/apply/link-local/adapter/mDNS/main-workspace renders (Light/Dark, normal/minimum), modal draft isolation, per-row explicit target routing, device Open, named controls, footer/focus/exit guards, 0 WPF binding errors; all NIC/socket/process/elevation/browser boundaries fake.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { app.Shutdown(); }
    }
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void RenderLinkLocal(string output, string theme, int width, int height)
    {
        var fixture = new ApplyFixture { Fail = false };
        fixture.Snapshot = fixture.Snapshot with { Dad = new(1, true), Dhcp = true, DnsSource = "automatic",
            PersistentAddresses = ImmutableArray<ApplyAddress>.Empty,
            Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "dhcp", "preferred")) };
        var vm = new ProjectApplyViewModel(null, null, fixture, fixture, fixture,
            _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { fixture.Snapshot.Identity }),
            () => Array.Empty<ProjectApplyRequest>(), fixture, verificationAttempts: 1, linkLocalTool: true);
        var window = new ProjectApplyWindow(vm) { Width = width, Height = height };
        window.Show(); vm.RefreshAsync().GetAwaiter().GetResult(); Pump();
        if (window.Title != "Link-Local" || vm.SelectedAdapter != null || fixture.Executed != 0) throw new Exception("Link-local implicitly selected/applied.");
        Capture(window, Path.Combine(output, $"link-local-empty-{theme}-{width}.png"));
        var inputs = Descendants(window).OfType<Wpf.Ui.Controls.TextBox>().Where(t => t.IsVisible).ToArray();
        if (inputs.Length != 3 || inputs.Any(t => string.IsNullOrWhiteSpace(AutomationProperties.GetName(t)))) throw new Exception("Link-local inputs missing accessible names.");
        var adapter = Descendants(window).OfType<ComboBox>().First(); adapter.SelectedIndex = 0;
        inputs.Single(t => AutomationProperties.GetName(t).StartsWith("Manual IPv4")).Text = "169.254.42.10";
        inputs.Single(t => AutomationProperties.GetName(t) == "Primary DNS").Text = "192.0.2.53";
        inputs.Single(t => AutomationProperties.GetName(t) == "Secondary DNS").Text = "192.0.2.54"; Pump();
        vm.ReviewAsync(false).GetAwaiter().GetResult(); Pump();
        if (!vm.ApplyCommand.CanExecute(null) || !vm.Review.Contains("169.254.42.10/16") || !vm.Review.Contains("during assignment")) throw new Exception("Link-local review/DNS binding failed.");
        var confirm = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => ReferenceEquals(b.Command, vm.ApplyCommand));
        var bounds = confirm.TransformToAncestor(window).TransformBounds(new Rect(confirm.RenderSize));
        if (!confirm.IsEnabled || bounds.Bottom > height || bounds.Right > width) throw new Exception("Link-local confirmation clipped.");
        adapter.Focus(); Pump(); var reached = confirm.IsKeyboardFocusWithin;
        for (var i = 0; i < 80 && !reached; i++)
        {
            if (System.Windows.Input.Keyboard.FocusedElement is not UIElement focused) break;
            focused.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next)); Pump();
            reached = confirm.IsKeyboardFocusWithin;
        }
        if (!reached) throw new Exception("Link-local confirm inaccessible by forward focus.");
        var scroll = Descendants(window).OfType<ScrollViewer>().First(); scroll.ScrollToBottom(); Pump();
        Capture(window, Path.Combine(output, $"link-local-review-{theme}-{width}.png"));
        vm.ApplyAsync().GetAwaiter().GetResult(); if (fixture.Executed != 0) throw new Exception("Link-local ignored declined consent.");
        fixture.Consent = true; fixture.Conflict = true; vm.ApplyAsync().GetAwaiter().GetResult(); Pump();
        if (fixture.Executed != 1 || fixture.Opened != 0 || !vm.Status.Contains("duplicate address") || !vm.Observed.Contains("duplicate")) throw new Exception("Link-local conflict was hidden.");
        scroll.ScrollToBottom(); Pump(); Capture(window, Path.Combine(output, $"link-local-conflict-{theme}-{width}.png"));
        fixture.Conflict = false; vm.ReviewAsync(true).GetAwaiter().GetResult(); vm.ApplyAsync().GetAwaiter().GetResult(); Pump();
        if (!fixture.Snapshot.Dhcp || fixture.Snapshot.DnsSource != "automatic" || fixture.Opened != 0) throw new Exception("Original DHCP/DNS recovery failed.");
        vm.ReviewAsync(false).GetAwaiter().GetResult(); vm.ApplyAsync().GetAwaiter().GetResult(); Pump();
        if (!vm.Status.Contains("No browser was opened") || fixture.Snapshot.Dhcp) throw new Exception("Link-local success misreported.");
        scroll.ScrollToBottom(); Pump(); Capture(window, Path.Combine(output, $"link-local-verified-{theme}-{width}.png"));
        vm.ReviewDhcpAsync().GetAwaiter().GetResult(); Pump();
        Capture(window, Path.Combine(output, $"link-local-dhcp-{theme}-{width}.png"));
        fixture.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously); var pending = vm.ApplyAsync();
        window.Close(); Pump();
        if (!window.IsVisible || window.ConfirmApplicationExit() || inputs.Any(t => t.IsEnabled)) throw new Exception("Busy link-local allowed edits/exit.");
        fixture.Hold.SetResult(); var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!pending.IsCompleted && DateTime.UtcNow < deadline) Pump();
        if (!pending.IsCompleted) throw new Exception("Link-local cancel/observe did not finish.");
        pending.GetAwaiter().GetResult(); if (fixture.Opened != 0 || !window.ConfirmApplicationExit()) throw new Exception("Link-local cancellation launched browser or retained busy.");
        window.Close();
        var adapters = new System.Collections.ObjectModel.ObservableCollection<AdapterViewModel>();
        foreach (var (name, address, dhcp, known) in new[]
        {
            ("Manual Link-Local", "169.254.42.10", false, true),
            ("DHCP / APIPA", "169.254.42.20", true, true),
            ("Unknown DHCP policy", "169.254.42.30", false, false)
        }) adapters.Add(new(new AdapterInfo { Id = name, Name = name, CurrentIp = address, CurrentSubnet = "255.255.0.0",
            IsDhcp = dhcp, IsDhcpKnown = known, IsCurrentIpv4Manual = !dhcp, IsConnected = false, AdapterType = "Ethernet" }, new(), new()));
        var panel = new AdapterListPanel { DataContext = new AdapterFixture { Adapters = adapters, SelectedAdapter = adapters[0] } };
        var host = new Wpf.Ui.Controls.FluentWindow { Width = 320, Height = 340, Content = panel, Title = "Fixture adapters" };
        host.Show(); Pump();
        foreach (var model in adapters)
            if (!Descendants(panel).OfType<System.Windows.Controls.TextBlock>().Any(t => t.Text == model.CurrentIp && t.IsVisible))
                throw new Exception("Disconnected Link-Local address hidden from adapter list.");
        Capture(host, Path.Combine(output, $"link-local-adapters-{theme}-{width}.png")); host.Close();
    }
    private sealed class AdapterFixture
    {
        public System.Collections.ObjectModel.ObservableCollection<AdapterViewModel> Adapters { get; init; } = new();
        public AdapterViewModel? SelectedAdapter { get; set; }
        public System.Windows.Input.ICommand RefreshCommand { get; } = new NetworkProfileManager.Commands.RelayCommand(_ => { });
    }
    private static void RenderApply(string output, string theme, int width, int height)
    {
        var fixture = new ApplyFixture();
        var profile = new ProjectProfile { Name = "AV static network", Mode = "static", Ipv4 = new()
        { Address = "192.0.2.20", PrefixLength = 24, Gateway = null, Dns = new() { Mode = "static", Servers = new() { "192.0.2.55", "192.0.2.56" } } } };
        var vm = new ProjectApplyViewModel(profile, new ProjectDevice { Name = "Panel", ProfileId = profile.Id, WebUrl = "https://192.0.2.44:8443/panel?view=main#!/status" },
            fixture, fixture, fixture, _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { fixture.Snapshot.Identity }),
            () => Array.Empty<ProjectApplyRequest>(), fixture, verificationAttempts: 1);
        var window = new ProjectApplyWindow(vm) { Width = width, Height = height }; window.Show(); Pump();
        vm.RefreshAsync().GetAwaiter().GetResult(); Pump();
        if (vm.SelectedAdapter != null || vm.ApplyCommand.CanExecute(null) || vm.ReviewCommand.CanExecute(null)) throw new Exception("Apply selected a NIC implicitly.");
        Capture(window, Path.Combine(output, $"apply-empty-{theme}-{width}.png"));
        vm.SelectedAdapter = vm.Adapters[0]; vm.ReviewAsync(false).GetAwaiter().GetResult(); Pump();
        if (!vm.Review.Contains("192.0.2.10") || !vm.Review.Contains("192.0.2.20") || fixture.Executed != 0) throw new Exception("Apply review lacks old/new state.");
        var confirm = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => ReferenceEquals(b.Command, vm.ApplyCommand));
        var bounds = confirm.TransformToAncestor(window).TransformBounds(new Rect(confirm.RenderSize));
        if (!confirm.IsEnabled || bounds.Bottom > window.ActualHeight || bounds.Right > window.ActualWidth) throw new Exception("Apply footer clipped.");
        foreach (var control in Descendants(window).OfType<ComboBox>())
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control))) throw new Exception("Unnamed Apply selector.");
        var adapterSelector = Descendants(window).OfType<ComboBox>().Single(c => ReferenceEquals(c.ItemsSource, vm.Adapters));
        adapterSelector.Focus(); Pump(); bool reached = confirm.IsKeyboardFocusWithin;
        for (var i = 0; i < 40 && !reached; i++)
        {
            if (System.Windows.Input.Keyboard.FocusedElement is not UIElement focused) break;
            focused.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next)); Pump(); reached = confirm.IsKeyboardFocusWithin;
        }
        if (!reached) throw new Exception("Apply confirmation unreachable by forward focus.");
        Descendants(window).OfType<ScrollViewer>().First().ScrollToTop(); Pump();
        Capture(window, Path.Combine(output, $"apply-review-{theme}-{width}.png"));
        vm.ApplyAsync().GetAwaiter().GetResult(); if (fixture.Executed != 0) throw new Exception("Apply ignored declined confirmation.");
        fixture.Consent = true; vm.ApplyAsync().GetAwaiter().GetResult(); Pump();
        if (fixture.Executed != 1 || fixture.Opened != 0 || vm.Observed.Length == 0) throw new Exception("Apply failure opened browser or hid observation.");
        Descendants(window).OfType<ScrollViewer>().First().ScrollToBottom(); Pump();
        Capture(window, Path.Combine(output, $"apply-error-{theme}-{width}.png"));
        fixture.Fail = false; vm.ReviewAsync(true).GetAwaiter().GetResult(); Pump();
        Capture(window, Path.Combine(output, $"apply-recovery-{theme}-{width}.png"));
        vm.ApplyAsync().GetAwaiter().GetResult(); Pump();
        if (fixture.Executed != 2 || fixture.Opened != 0) throw new Exception("Recovery did not remain explicit and browser-free.");
        Capture(window, Path.Combine(output, $"apply-recovered-{theme}-{width}.png"));
        fixture.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.ReviewAsync(false).GetAwaiter().GetResult();
        var pending = vm.ApplyAsync();
        if (!vm.IsBusy) throw new Exception("Pending apply did not freeze the surface.");
        window.Close(); Pump();
        if (!window.IsVisible || window.ConfirmApplicationExit()) throw new Exception("Busy Apply allowed window/application exit.");
        fixture.Hold.SetResult();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!pending.IsCompleted && DateTime.UtcNow < deadline) Pump();
        if (!pending.IsCompleted) throw new Exception("Cancelled fake apply did not finish observation.");
        pending.GetAwaiter().GetResult();
        if (vm.IsBusy || fixture.Opened != 0 || !window.ConfirmApplicationExit()) throw new Exception("Cancel/observe/exit ordering failed.");
        window.Close();
    }
    private sealed class ApplyFixture : IProjectApplyBoundary, IProjectApplyJournal, IBrowserLauncher, IProjectApplyDialogs
    {
        internal ApplyAdapterSnapshot Snapshot = new(new(Guid.NewGuid(), 12, "Ethernet fixture", "02-00-00-00-00-01"), false,
            ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")), "static", ImmutableArray.Create("192.0.2.53", "192.0.2.54"),
            ImmutableArray<ApplyRoute>.Empty, true, ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "preferred")));
        internal bool Consent, Fail = true, Conflict;
        internal int Executed, Opened;
        internal TaskCompletionSource? Hold;
        public Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity identity, CancellationToken cancellation) => Task.FromResult(Snapshot);
        public void ValidateSupported(ApplyAdapterSnapshot s, ApplyIntent d, bool recovery) { if (!s.ClassificationComplete) throw new InvalidOperationException(); }
        public async Task<ApplyExecutionResult> ExecuteAsync(ProjectApplyRequest request, CancellationToken cancellation)
        {
            Executed++;
            if (Hold != null) await Hold.Task;
            if (Fail) return new ApplyExecutionResult(ApplyExecutionStatus.Failed, true, "DnsSecondary");
            var oldSource = Snapshot.DnsSource; var oldServers = Snapshot.DnsServers;
            var d = request.Desired; var a = new ApplyAddress(d.Address ?? "198.51.100.10", d.Mode == "dhcp" ? 24 : d.PrefixLength,
                d.Mode == "dhcp" ? "dhcp" : "manual", Conflict ? "duplicate" : "preferred");
            Snapshot = Snapshot with { Dhcp = d.Mode == "dhcp", Addresses = ImmutableArray.Create(a),
                PersistentAddresses = d.Mode == "dhcp" ? ImmutableArray<ApplyAddress>.Empty : ImmutableArray.Create(a),
                DnsSource = d.DnsMode == "leave-unchanged" ? Snapshot.DnsSource : d.DnsMode,
                DnsServers = d.DnsMode == "leave-unchanged" ? Snapshot.DnsServers : d.DnsServers };
            if (Conflict)
            {
                Snapshot = Snapshot with { DnsSource = oldSource, DnsServers = oldServers };
                return new ApplyExecutionResult(ApplyExecutionStatus.Failed, true, "AddressConflict");
            }
            return new ApplyExecutionResult(ApplyExecutionStatus.Finished, true, "CommandsFinished");
        }
        public Task BeginAsync(ProjectApplyRequest request, CancellationToken cancellation) => Task.CompletedTask;
        public Task FinishAsync(Guid id, ProjectApplyOutcome outcome) => Task.CompletedTask;
        public void Open(string destination) => Opened++;
        public bool Confirm(string review, bool recovery) => Consent;
    }
    private static void Capture(Window window, string path)
    {
        // Capture the settled endpoint of the existing adapter-change fade, not a transient frame.
        if (window is NetworkProfileManager.MainWindow main && main.FindName("RightPanel") is UIElement panel)
        { panel.BeginAnimation(UIElement.OpacityProperty, null); panel.Opacity = 1; }
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var bounds = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
            context.DrawRectangle((Brush)window.FindResource("ApplicationBackgroundBrush"), null, bounds);
            context.DrawImage(bitmap, bounds);
        }
        var flattened = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        flattened.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(flattened));
        using var stream = File.Create(path); encoder.Save(stream);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private sealed class FakeDialogs : IProjectDialogs
    {
        public bool Discard = true, Open;
        public int DiscardPrompts, OpenPrompts;
        public string? OpenPath() => null;
        public string? ChooseFolder() => null;
        public string? SavePath(string? folder) => null;
        public bool ConfirmDiscard() { DiscardPrompts++; return Discard; }
        public bool ConfirmDelete() => true;
        public bool ConfirmDeleteDevice() => true;
        public bool ConfirmOpenDevice(string destination, bool isHttp, bool hostMismatch, bool unlinked) { OpenPrompts++; return Open; }
        public bool ConfirmOverwrite(string path) => true;
    }
    private sealed class FakeBrowser : IBrowserLauncher
    {
        public int Opened;
        public void Open(string destination) => Opened++;
    }
}
