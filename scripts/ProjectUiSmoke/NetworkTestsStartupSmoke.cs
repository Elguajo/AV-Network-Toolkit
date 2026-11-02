using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckNetworkTestsStartup(string output)
    {
        AppPaths.OverrideDataDirectory(Path.Combine(output, "isolated-startup"));
        try
        {
            foreach (var theme in new[] { "Dark", "Light" })
            {
                ApplyRenderTheme(theme);
                foreach (bool minimum in new[] { false, true })
                {
                    var platform = new DiagnosticsSurface { Gate = null };
                    var reader = new DelayedDiagnosticReader();
                    using var inline = new AdapterDiagnosticsViewModel(platform, new(platform));
                    var owner = new NetworkProfileManager.MainWindow(new MainViewModel(() => new(), inline),
                        new ProjectViewModel(new FakeDialogs(), new FakeBrowser()));
                    Application.Current.MainWindow = owner;
                    bool fixtureExiting = false;
                    // Match App's close-to-tray handler without creating a tray icon or App startup.
                    owner.Closing += (_, e) =>
                    {
                        if (fixtureExiting) return;
                        e.Cancel = true; owner.Hide();
                    };
                    owner.Show(); SettleDiagnostics();
                    using var vm = new AdapterDiagnosticsViewModel(reader, new(platform));
                    var window = new PingWindow(vm) { Owner = owner, Width = minimum ? 480 : 700, Height = minimum ? 440 : 860 };
                    owner.TrackDiagnosticWindow(window, vm);
                    bool closed = false, ownerClosed = false;
                    window.Closed += (_, _) => closed = true;
                    owner.Closed += (_, _) => ownerClosed = true;
                    window.Show(); SettleDiagnostics();
                    var suffix = theme + (minimum ? "-minimum" : "-normal");
                    AssertPresetSurfaces(window, theme);
                    Capture(window, Path.Combine(output, $"network-tests-loading-{suffix}.png"));
                    if (minimum) { window.WindowState = WindowState.Maximized; SettleDiagnostics(); }
                    CloseDiagnosticCaption(window);
                    if (!closed || ownerClosed || !owner.IsVisible || vm.CanSelect || !reader.Cancellation.IsCancellationRequested)
                        throw new Exception("Caption close affected the main window or failed to cancel loading.");
                    reader.Completion.TrySetResult(platform.Adapters); SettleDiagnostics();
                    if (platform.Sends != 0 || platform.Connects != 0 || vm.Adapters.Count != 0)
                        throw new Exception("Late read changed a closed window or opening sent traffic.");
                    Console.WriteLine($"PASS: {suffix} loading native caption close cancels reader, retains main window and drops late read.");

                    using var runningVm = new AdapterDiagnosticsViewModel(platform, new(platform));
                    var runningWindow = new PingWindow(runningVm) { Owner = owner, Width = minimum ? 480 : 700, Height = minimum ? 440 : 860 };
                    owner.TrackDiagnosticWindow(runningWindow, runningVm);
                    runningWindow.Show(); SettleDiagnostics();
                    runningVm.SelectedAdapter = runningVm.Adapters[0]; runningVm.Source = runningVm.Sources[0]; runningVm.Target = "192.0.2.229";
                    platform.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    var run = runningVm.PingAsync(4); Until(() => platform.Sends == 1);
                    AssertPresetSurfaces(runningWindow, theme);
                    Capture(runningWindow, Path.Combine(output, $"network-tests-running-{suffix}.png"));
                    CloseDiagnosticCaption(runningWindow);
                    if (ownerClosed || !owner.IsVisible || !owner.AreTestsRunning) throw new Exception("Closing test changed owner or lost in-flight activity.");
                    platform.Gate.SetResult(new(0, "192.0.2.229", 2)); Until(() => run.IsCompleted); run.GetAwaiter().GetResult();
                    if (platform.Sends != 1 || runningVm.Results.Count != 0 || owner.AreTestsRunning) throw new Exception("Closed test sent again, updated results or retained activity.");
                    using var errorVm = new AdapterDiagnosticsViewModel(new DiagnosticsSurface { ReadError = true }, new(platform));
                    var errorWindow = new PingWindow(errorVm) { Owner = owner, Width = minimum ? 480 : 700, Height = minimum ? 440 : 860 };
                    errorWindow.Show(); SettleDiagnostics(); AssertPresetSurfaces(errorWindow, theme);
                    Capture(errorWindow, Path.Combine(output, $"network-tests-read-error-{suffix}.png"));
                    CloseDiagnosticCaption(errorWindow);
                    CheckBackgroundDiagnosticDetails(owner, platform, output, suffix);
                    fixtureExiting = true; owner.Close(); Pump();
                }
            }
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }

    private static void CheckBackgroundDiagnosticDetails(Window owner, DiagnosticsSurface platform, string output, string suffix)
    {
        var reader = new DelayedDetailReader(platform.Adapters);
        using var vm = new AdapterDiagnosticsViewModel(reader, new(platform));
        var window = new PingWindow(vm) { Owner = owner, Width = suffix.Contains("minimum") ? 480 : 700, Height = suffix.Contains("minimum") ? 440 : 860 };
        window.Show(); Until(() => reader.Entered);
        var adapter = Descendants(window).OfType<ComboBox>().Single(c => c.Name == "AdapterSelector");
        var source = Descendants(window).OfType<ComboBox>().Single(c => c.Name == "SourceSelector");
        adapter.SelectedItem = vm.Adapters[0]; Pump(); source.SelectedItem = vm.Sources[0]; vm.Target = "192.0.2.229"; Pump();
        if (!vm.CanRun || vm.DetailsCompletion.IsCompleted || !adapter.IsEnabled || !source.IsEnabled)
            throw new Exception("Optional inventory blocks selecting a source or running a test.");
        bool heartbeat = false;
        window.Dispatcher.BeginInvoke(new Action(() => heartbeat = true)); Until(() => heartbeat);
        Capture(window, Path.Combine(output, $"network-tests-background-ready-{suffix}.png"));
        var selectedId = vm.SelectedAdapter!.Id; var selectedSource = vm.Source;
        reader.Completion.SetResult(platform.Adapters.Select(a => a with { Kind = "usb" }).ToArray());
        Until(() => vm.DetailsCompletion.IsCompleted); vm.DetailsCompletion.GetAwaiter().GetResult(); SettleDiagnostics();
        if (vm.SelectedAdapter?.Id != selectedId || vm.Source != selectedSource || !vm.CanRun
            || !Equals(adapter.SelectedItem, vm.SelectedAdapter) || !Equals(source.SelectedItem, selectedSource))
            throw new Exception("Background inventory lost the explicit adapter/source in actual WPF bindings.");
        CloseDiagnosticCaption(window);
    }

    // Explicit local profiling command only, never called by automated fixture modes.
    // Reads local facts; prints durations/counts only. No startup, settings mutation or packets.
    private static void MeasureDiagnosticInventory()
    {
        var reader = new WindowsQuickAdapterDiagnostics();
        IReadOnlyList<DiagnosticAdapter> quick = [];
        for (int i = 0; i < 3; i++)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var read = reader.ReadAsync(CancellationToken.None); Until(() => read.IsCompleted); quick = read.GetAwaiter().GetResult();
            Console.WriteLine($"Native local inventory sample {i + 1}: {watch.Elapsed.TotalMilliseconds:F1} ms; adapters={quick.Count}");
        }
        var slowWatch = System.Diagnostics.Stopwatch.StartNew();
        var details = reader.ReadDetailsAsync(CancellationToken.None); Until(() => details.IsCompleted);
        var full = details.GetAwaiter().GetResult();
        Console.WriteLine($"PowerShell local inventory: {slowWatch.Elapsed.TotalMilliseconds:F1} ms; adapters={full.Count}");
        int checkedCount = 0, addressDifferences = 0, routeDifferences = 0, dnsDifferences = 0;
        foreach (var a in quick)
        {
            var b = full.SingleOrDefault(b => b.Id == a.Id && b.Index == a.Index && DiagnosticPingService.Mac(b.Mac) == DiagnosticPingService.Mac(a.Mac));
            if (b == null) continue;
            checkedCount++;
            if (!a.Addresses.OrderBy(x => x.Address).SequenceEqual(b.Addresses.OrderBy(x => x.Address))) addressDifferences++;
            if (!a.Routes.OrderBy(x => x.Destination).ThenBy(x => x.NextHop).ThenBy(x => x.Metric)
                .SequenceEqual(b.Routes.OrderBy(x => x.Destination).ThenBy(x => x.NextHop).ThenBy(x => x.Metric))) routeDifferences++;
            if (!a.Dns.Order().SequenceEqual(b.Dns.Order())) dnsDifferences++;
        }
        Console.WriteLine($"Sequential provider comparison: matched identities={checkedCount}; address/route/DNS differing adapters={addressDifferences}/{routeDifferences}/{dnsDifferences}; values withheld; volatile, not an atomic OS snapshot.");
    }

    private sealed class DelayedDetailReader(IReadOnlyList<DiagnosticAdapter> adapters) : IAdapterDiagnosticsDetails
    {
        public volatile bool Entered;
        public TaskCompletionSource<IReadOnlyList<DiagnosticAdapter>> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken cancellation) => Task.FromResult(adapters);
        public Task<IReadOnlyList<DiagnosticAdapter>> ReadDetailsAsync(CancellationToken cancellation)
        { Entered = true; return Completion.Task; }
    }

    private static void AssertPresetSurfaces(Window window, string theme)
    {
        var cards = Descendants(window).OfType<ListBox>().Single(c => c.Name == "PresetCards");
        var texts = Descendants(cards).OfType<TextBlock>().Select(t => t.Text).ToArray();
        foreach (var preset in cards.Items.Cast<DiagnosticTestPreset>())
            if (!texts.Contains(preset.Name) || !texts.Contains(preset.Description)) throw new Exception("Preset text missing during load or test.");
        if (theme == "Dark" && Descendants(cards).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.White))
            throw new Exception("Disabled preset list falls back to opaque white in Dark theme.");
    }

    private static void CloseDiagnosticCaption(Window window)
    {
        var caption = Descendants(window).OfType<Wpf.Ui.Controls.TitleBarButton>()
            .Single(b => b.ButtonType == Wpf.Ui.Controls.TitleBarButtonType.Close);
        var point = caption.PointToScreen(new Point(caption.ActualWidth / 2, caption.ActualHeight / 2));
        var coords = (nint)(((int)point.Y << 16) | ((int)point.X & 0xffff));
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        StartupSendMessage(hwnd, 0x84, 0, coords);
        StartupSendMessage(hwnd, 0xA1, 20, coords);
        StartupSendMessage(hwnd, 0xA2, 20, coords);
        SettleDiagnostics();
        if (window.IsVisible) throw new Exception("Native caption click did not close the diagnostic window.");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint StartupSendMessage(nint hwnd, uint msg, nint wParam, nint lParam);

    private sealed class DelayedDiagnosticReader : IAdapterDiagnostics
    {
        public int Reads;
        public CancellationToken Cancellation;
        public TaskCompletionSource<IReadOnlyList<DiagnosticAdapter>> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken cancellation)
        { Reads++; Cancellation = cancellation; return Completion.Task; }
    }
}
