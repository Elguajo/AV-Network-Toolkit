using System.Diagnostics;
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
internal static class Program
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
        app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary { Theme = Wpf.Ui.Appearance.ApplicationTheme.Dark });
        app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/NetworkProfileManager;component/Themes/Styles.xaml") });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/NetworkProfileManager;component/Strings/En.xaml") });
        app.Resources.Add("BoolToVis", new NetworkProfileManager.Converters.BoolToVisibilityConverter());
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
            }
            if (errors.ToString().Length != 0) throw new Exception("WPF binding errors: " + errors);
            Console.WriteLine("PASS: 20 isolated project/device renders (Light/Dark, normal/minimum; empty/selected/error/details), 24 linked devices, bound edits/selection/search/group retention, fake launch/cancel, visible footer, named controls, forward focus traversal, close/exit device draft guards, 0 WPF binding errors; no browser/NIC/OS-network calls.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { app.Shutdown(); }
    }
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Capture(Window window, string path)
    {
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
