using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckDiagnosticHide(string output)
    {
        foreach (bool main in new[] { false, true })
        {
            var root = Path.Combine(output, "isolated-" + (main ? "main-" : "separate-") + Guid.NewGuid());
            AppPaths.OverrideDataDirectory(root);
            try
            {
                var fake = new DiagnosticsSurface { Gate = null };
                var delay = new DiagnosticDelay();
                using var vm = new AdapterDiagnosticsViewModel(fake, new(fake), delay: delay.WaitAsync);
                Window window = main
                    ? new MainWindow(new MainViewModel(() => new(), vm), new ProjectViewModel(new FakeDialogs(), new FakeBrowser()))
                    : new PingWindow(vm);
                window.Show(); Pump();
                var refresh = vm.RefreshAsync(); Until(() => refresh.IsCompleted); refresh.GetAwaiter().GetResult();
                vm.SelectedAdapter = vm.Adapters[0]; vm.Source = vm.Sources[0]; vm.Target = "192.0.2.229";
                vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.ContinuousPing);
                for (int i = 0; i < 10; i++) AssertDiagnosticHide(window, vm, fake, delay);

                // A native echo may finish after cancellation. It must not schedule another one.
                fake.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
                int before = fake.Sends;
                var run = vm.RunSelectedTestAsync(); Until(() => fake.Sends == before + 1);
                window.Hide();
                fake.Gate.SetResult(new(0, "192.0.2.229", 2));
                Until(() => run.IsCompleted); run.GetAwaiter().GetResult();
                if (vm.CanStop || !vm.CanSelect || fake.Sends != before + 1 || vm.Results.Single().Error != "Cancelled")
                    throw new Exception("Hide failed to drain a late echo without scheduling another.");
                window.Show(); SettleDiagnostics();
                if (fake.Sends != before + 1 || vm.CanStop) throw new Exception("Show restarted a cancelled echo.");
                window.Close(); Pump();
                Console.WriteLine($"PASS: {(main ? "main" : "separate")} diagnostic window: 10 gated Hide/Show cycles and in-flight echo cancellation.");
            }
            finally { AppPaths.OverrideDataDirectory(null); }
        }
    }

    private sealed class DiagnosticDelay
    {
        public bool Pause, Entered;
        public CancellationToken Token;
        public Task WaitAsync(TimeSpan interval, CancellationToken token)
        {
            if (!Pause) return Task.Delay(10, token);
            Token = token; Entered = true;
            return Task.Delay(Timeout.Infinite, token);
        }
    }

    private static void AssertDiagnosticHide(Window window, AdapterDiagnosticsViewModel vm, DiagnosticsSurface fake, DiagnosticDelay delay)
    {
        delay.Entered = false; delay.Pause = true;
        int before = fake.Sends;
        var run = vm.RunSelectedTestAsync(); Until(() => delay.Entered);
        if (fake.Sends != before + 1 || !vm.CanStop) throw new Exception("Hide fixture did not pause after exactly one echo.");
        window.Hide();
        if (window.IsVisible || !delay.Token.IsCancellationRequested)
            throw new Exception("Hiding diagnostic owner did not cancel its active delay.");
        Until(() => run.IsCompleted); run.GetAwaiter().GetResult();
        if (vm.CanStop || !vm.CanSelect || fake.Sends != before + 1)
            throw new Exception("Hidden diagnostic owner continued sending or stayed busy.");
        window.Show(); SettleDiagnostics();
        if (fake.Sends != before + 1 || vm.CanStop) throw new Exception("Showing diagnostic owner restarted its test.");
        delay.Pause = false;
    }

    private static void RenderDiagnostics(string output, string theme, bool minimum)
    {
        var fake = new DiagnosticsSurface();
        var delay = new DiagnosticDelay();
        using var vm = new AdapterDiagnosticsViewModel(fake, new(fake), tcp: new(fake, fake), delay: delay.WaitAsync);
        var window = new PingWindow(vm) { Width = minimum ? 480 : 700, Height = minimum ? 440 : 860 };
        window.Show(); Pump();
        if (fake.Reads != 1 || vm.SelectedAdapter != null || fake.Sends != 0) throw new Exception("Ping window sent or guessed adapter on open.");
        var suffix = theme + (minimum ? "-minimum" : "-normal");
        Capture(window, Path.Combine(output, $"diagnostics-empty-{suffix}.png"));
        var selectors = Descendants(window).OfType<ComboBox>().Where(c => c.Name is "AdapterSelector" or "SourceSelector").ToArray();
        if (selectors.Length != 2) throw new Exception("Missing diagnostic/source selectors.");
        selectors[0].SelectedItem = vm.Adapters[0]; Pump();
        if (selectors[1].SelectedItem != null || vm.CanPing) throw new Exception("Source was implicit.");
        selectors[1].SelectedItem = vm.Sources[0]; vm.Target = "192.0.2.229"; Pump();
        var facts = Descendants(window).OfType<Expander>().Single(); facts.IsExpanded = true; SettleDiagnostics();
        if (!vm.Details.Contains("\nIndex:") || !vm.Details.Contains("192.0.2.0/24")) throw new Exception("Reported facts lost line breaks or routes.");
        Capture(window, Path.Combine(output, $"diagnostics-facts-{suffix}.png"));
        facts.IsExpanded = false; SettleDiagnostics();
        foreach (var input in Descendants(window).OfType<Control>().Where(c => c is ComboBox or Wpf.Ui.Controls.TextBox))
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(input))) throw new Exception("Unnamed diagnostic input.");
        var ping = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => b.IsVisible && ReferenceEquals(b.Command, vm.RunTestCommand));
        if (!ping.IsEnabled) throw new Exception("Ping not enabled after explicit source.");
        if (!selectors[0].Focus()) throw new Exception("Diagnostic adapter cannot receive keyboard focus.");
        selectors[0].MoveFocus(new(System.Windows.Input.FocusNavigationDirection.Next)); Pump();
        if (!selectors[1].IsKeyboardFocusWithin) throw new Exception("Diagnostic keyboard order skipped source IPv4.");
        // Explicit four-echo command uses only fake platform. Observe busy/Stop with delayed fake echo.
        var run = vm.PingAsync(4); Until(() => fake.Sends == 1);
        Pump();
        if (selectors.Any(c => c.IsEnabled) || ping.IsEnabled) throw new Exception("Ping inputs were editable during flight.");
        Capture(window, Path.Combine(output, $"diagnostics-busy-{suffix}.png"));
        vm.StopCommand.Execute(null); fake.Gate!.TrySetResult(new(0, "192.0.2.229", 2));
        Until(() => run.IsCompleted); run.GetAwaiter().GetResult(); Pump();
        if (vm.Results.Count != 1 || vm.Results[0].Error != "Cancelled" || fake.Sends != 1) throw new Exception("Stop scheduled further echo requests.");
        fake.Gate = null;
        var success = vm.PingAsync(); Until(() => success.IsCompleted); success.GetAwaiter().GetResult(); Pump();
        if (vm.Results.Count != 1 || !vm.Results[0].Success) throw new Exception("Successful diagnostic result missing.");
        Descendants(window).OfType<ScrollViewer>().First().ScrollToEnd(); SettleDiagnostics();
        Capture(window, Path.Combine(output, $"diagnostics-reply-{suffix}.png"));
        ExerciseReadyMadeTests(window, vm, fake, delay, output, suffix);
        fake.Route = new(8, "192.0.2.0", 24, "198.51.100.1", 1);
        int sends = fake.Sends;
        var blocked = vm.PingAsync(); Until(() => blocked.IsCompleted); blocked.GetAwaiter().GetResult(); Pump();
        if (fake.Sends != sends || vm.Results.Single().Error != "Route") throw new Exception("Conflicting interface route sent a probe.");
        Capture(window, Path.Combine(output, $"diagnostics-route-blocked-{suffix}.png"));
        var scroll = Descendants(window).OfType<ScrollViewer>().First(); scroll.ScrollToEnd(); Pump();
        Capture(window, Path.Combine(output, $"diagnostics-scroll-end-{suffix}.png"));
        var close = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => b.Content?.ToString() == Loc.Get("Common.Close"));
        var closeBounds = close.TransformToAncestor(window).TransformBounds(new Rect(close.RenderSize));
        if (closeBounds.Bottom > window.ActualHeight || closeBounds.Top < 0 || !close.Focus()) throw new Exception("Diagnostic close unreachable at minimum size.");
        window.Close(); Pump(); if (vm.CanSelect) throw new Exception("Closing Ping did not dispose its operation owner.");

        var root = Path.Combine(output, "diagnostic-main-" + suffix); AppPaths.OverrideDataDirectory(root);
        try
        {
            var inlineFake = new DiagnosticsSurface { Gate = null };
            var inlineDelay = new DiagnosticDelay();
            var inline = new AdapterDiagnosticsViewModel(inlineFake, new(inlineFake), tcp: new(inlineFake, inlineFake), delay: inlineDelay.WaitAsync);
            var main = new MainViewModel(() => new() { new() { Id = inlineFake.Adapters[0].Id.ToString(), Name = "Ethernet fixture", IsConnected = true } }, inline);
            var mainWindow = new MainWindow(main, new ProjectViewModel(new FakeDialogs(), new FakeBrowser()))
                { Width = minimum ? 760 : 1200, Height = minimum ? 520 : 850 };
            mainWindow.Show(); Pump();
            var section = Descendants(mainWindow).OfType<Expander>().Single(e => e.Header?.ToString() == Loc.Get("Diagnostics.Title"));
            if (section.IsExpanded) throw new Exception("Network tests should start collapsed.");
            Capture(mainWindow, Path.Combine(output, $"network-tests-collapsed-{suffix}.png"));
            section.IsExpanded = true; SettleDiagnostics(); section.BringIntoView(); Pump();
            var refresh = inline.RefreshAsync(); Until(() => refresh.IsCompleted); refresh.GetAwaiter().GetResult();
            if (inline.SelectedAdapter != null || inline.Source != null || inlineFake.Sends != 0)
                throw new Exception("Main navigation implicitly selected diagnostic adapter/source.");
            inline.SelectedAdapter = inline.Adapters[0];
            inline.Source = inline.Sources[0]; inline.Target = "192.0.2.229";
            var probe = inline.PingAsync(); Until(() => probe.IsCompleted); probe.GetAwaiter().GetResult(); Pump();
            Capture(mainWindow, Path.Combine(output, $"diagnostics-inline-{suffix}.png"));
            var selected = inline.Presets.Single(p => p.Kind == DiagnosticTestKind.TcpPort);
            var preset = Descendants(mainWindow).OfType<ComboBox>().Single(c => c.Name == "PresetSelector");
            preset.SelectedItem = selected; Pump();
            if (!inline.IsTcp || !inline.CanRun || !preset.IsVisible || inlineFake.Connects != 0)
                throw new Exception("Inline preset selection did not reveal TCP fields, or sent traffic.");
            var portInput = Descendants(mainWindow).OfType<Wpf.Ui.Controls.TextBox>().Single(c => c.Name == "PortInput");
            portInput.BringIntoView(); SettleDiagnostics();
            Capture(mainWindow, Path.Combine(output, $"network-tests-inline-tcp-{suffix}.png"));
            section.IsExpanded = false; SettleDiagnostics(); section.IsExpanded = true; SettleDiagnostics();
            if (inline.SelectedTest != selected || inlineFake.Connects != 0) throw new Exception("Expansion lost a draft or started a test.");
            inline.SelectedTest = inline.Presets.Single(p => p.Kind == DiagnosticTestKind.ContinuousPing);
            AssertDiagnosticHide(mainWindow, inline, inlineFake, inlineDelay);
            inline.HideHyperVUplinks = true; Pump();
            if (inline.Adapters.Count != 1 || main.Adapters.Count != 1) throw new Exception("Filter affected legacy adapter list.");
            inlineFake.ReadError = true;
            var failed = inline.RefreshAsync(); Until(() => failed.IsCompleted); failed.GetAwaiter().GetResult(); Pump();
            Capture(mainWindow, Path.Combine(output, $"diagnostics-unavailable-{suffix}.png"));
            if (inline.SelectedAdapter != null || inline.Source != null) throw new Exception("Failed refresh retained sendable stale identity.");
            mainWindow.Close(); Pump(); if (inline.CanSelect) throw new Exception("Main close did not stop diagnostics.");
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }

    private static void Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition() && DateTime.UtcNow < deadline) { Pump(); Thread.Sleep(1); }
        if (!condition()) throw new Exception("Diagnostic fixture timed out.");
        Pump();
    }
    private static void SettleDiagnostics()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 400) { Pump(); Thread.Sleep(5); }
    }

    private sealed class DiagnosticsSurface : IAdapterDiagnostics, IDiagnosticPingPlatform, IDiagnosticTcpConnector
    {
        public int Reads, Sends, Connects;
        public bool ReadError;
        public TaskCompletionSource<DiagnosticEcho>? Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public PredictedRoute? Route = new(7, "192.0.2.0", 24, "0.0.0.0", 256);
        public IReadOnlyList<DiagnosticAdapter> Adapters { get; } = new[]
        {
            new DiagnosticAdapter(Guid.Parse("11111111-1111-4111-8111-111111111111"), 7, "Ethernet fixture · AV lab",
                "USB Gigabit Ethernet", "001122334455", "usb", true, false, "Up", "1 Gbps", "Disabled",
                [new("192.0.2.7",24,"Preferred",false), new("192.0.2.8",24,"Preferred",false)], ["192.0.2.53"],
                [new("192.0.2.0/24","0.0.0.0",256),new("0.0.0.0/0","192.0.2.1",25)]),
            new DiagnosticAdapter(Guid.Parse("22222222-2222-4222-8222-222222222222"), 8, "Confirmed Hyper-V uplink",
                "Physical fixture", "001122334466", "ethernet", true, true, "Up", "1 Gbps", "Disabled", [], [], [])
        };
        public Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken cancellation)
        { Reads++; if (ReadError) throw new IOException(); return Task.FromResult(Adapters); }
        public IReadOnlyList<PingBinding> ReadBindings() => [new(Adapters[0].Id, 7, Adapters[0].Mac, true, ["192.0.2.7", "192.0.2.8"])];
        public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellation) => throw new Exception("Unexpected DNS");
        public PredictedRoute? Predict(IPAddress source, IPAddress destination) => Route;
        public Task<DiagnosticEcho> EchoAsync(IPAddress source, IPAddress destination, CancellationToken cancellation)
        { Interlocked.Increment(ref Sends); return Gate?.Task ?? Task.FromResult(new DiagnosticEcho(0, destination.ToString(), 2, 63)); }
        public DiagnosticTcpObservation TcpObservation = new("Accepted", 2);
        public Task<DiagnosticTcpObservation> ConnectAsync(IPAddress source, IPAddress destination, int port, CancellationToken cancellation)
        { Interlocked.Increment(ref Connects); return Task.FromResult(TcpObservation); }
    }

    private static void ExerciseReadyMadeTests(Window window, AdapterDiagnosticsViewModel vm, DiagnosticsSurface fake, DiagnosticDelay delay, string output, string suffix)
    {
        var scroll = Descendants(window).OfType<ScrollViewer>().First();
        var cards = Descendants(window).OfType<ListBox>().Single(c => c.Name == "PresetCards");
        if (!cards.IsVisible || cards.Items.Count != 4) throw new Exception("Separate window lacks descriptive preset cards.");
        if (!cards.Focus()) throw new Exception("Preset cards cannot receive keyboard focus.");
        foreach (var item in cards.Items.Cast<DiagnosticTestPreset>())
        {
            var container = (ListBoxItem)cards.ItemContainerGenerator.ContainerFromItem(item);
            if (AutomationProperties.GetName(container) != item.Name) throw new Exception("Preset card lacks an accessible name.");
        }
        var tcp = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.TcpPort);
        cards.SelectedItem = tcp; Pump();
        var port = Descendants(window).OfType<Wpf.Ui.Controls.TextBox>().Single(c => c.Name == "PortInput");
        port.BringIntoView(); SettleDiagnostics();
        if (!port.IsVisible || fake.Connects != 0) throw new Exception("TCP selection sent traffic or hid the port.");
        vm.Port = "0"; Pump();
        if (vm.CanRun || vm.PortError.Length == 0) throw new Exception("Invalid TCP port enabled Run.");
        var validation = Descendants(window).OfType<TextBlock>().Single(c => c.Name == "PortValidation");
        validation.BringIntoView(); SettleDiagnostics();
        if (AutomationProperties.GetHelpText(port) != vm.PortError) throw new Exception("Port error is not associated with its input.");
        Capture(window, Path.Combine(output, $"network-tests-port-error-{suffix}.png"));
        var quick = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => b.CommandParameter?.ToString() == "443");
        quick.Command.Execute(quick.CommandParameter); Pump();
        if (vm.Port != "443" || !vm.CanRun) throw new Exception("Quick port did not restore valid TCP input.");
        if (!suffix.Contains("minimum")) { scroll.ScrollToHome(); SettleDiagnostics(); }
        Capture(window, Path.Combine(output, $"network-tests-tcp-ready-{suffix}.png"));
        foreach (var status in new[] { "Accepted", "Refused", "Timeout" })
        {
            fake.TcpObservation = new(status, 2); var run = vm.RunSelectedTestAsync(); Until(() => run.IsCompleted); run.GetAwaiter().GetResult();
            scroll.ScrollToEnd(); SettleDiagnostics();
            if (vm.TcpResult?.Observation?.Status != status) throw new Exception("TCP outcome not shown.");
            Capture(window, Path.Combine(output, $"network-tests-tcp-{status.ToLowerInvariant()}-{suffix}.png"));
        }
        foreach (var action in Descendants(window).OfType<Wpf.Ui.Controls.Button>().Where(b => b.IsVisible &&
                     (ReferenceEquals(b.Command, vm.RunTestCommand) || ReferenceEquals(b.Command, vm.StopCommand))))
        {
            var bounds = action.TransformToAncestor(window).TransformBounds(new Rect(action.RenderSize));
            if (bounds.Bottom > window.ActualHeight || bounds.Top < 0) throw new Exception("Run/Stop unreachable at minimum size.");
        }
        vm.SelectedTest = vm.Presets.Single(p => p.Kind == DiagnosticTestKind.ContinuousPing);
        int sent = fake.Sends; var continuous = vm.RunSelectedTestAsync(); Until(() => vm.AttemptCount >= 3);
        vm.StopCommand.Execute(null); Until(() => continuous.IsCompleted); continuous.GetAwaiter().GetResult();
        if (fake.Sends <= sent || vm.CanStop || !vm.CanSelect) throw new Exception("Continuous Ping stop failed.");
        AssertDiagnosticHide(window, vm, fake, delay);
        scroll.ScrollToEnd(); SettleDiagnostics(); Capture(window, Path.Combine(output, $"network-tests-continuous-stopped-{suffix}.png"));
        vm.SelectedTest = vm.Presets[0];
    }
}
