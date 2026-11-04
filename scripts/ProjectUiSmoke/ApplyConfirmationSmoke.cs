using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;
using Wpf.Ui.Controls;

internal static partial class Program
{
    private static void CheckApplyConfirmation(string output)
    {
        AppPaths.OverrideDataDirectory(Path.Combine(output, "isolated-data"));
        try
        {
            foreach (var theme in new[] { "Dark", "Light" })
            foreach (var minimum in new[] { false, true })
            foreach (var kind in new[] { "static", "offline", "dhcp", "warning" })
            {
                ApplyRenderTheme(theme);
                var f = new ApplyFixture { Fail = false };
                f.Snapshot = f.Snapshot with { Dad = new(3, kind != "offline") };
                if (kind == "offline") f.Snapshot = f.Snapshot with
                {
                    Addresses = f.Snapshot.Addresses.Select(a => a with { State = "deprecated" }).ToImmutableArray(),
                    DnsSource = "none", DnsServers = ImmutableArray<string>.Empty
                };
                if (kind == "dhcp") f.Snapshot = f.Snapshot with
                {
                    Dhcp = true, DnsSource = "automatic", Addresses = f.Snapshot.Addresses.Select(a => a with { Origin = "dhcp" }).ToImmutableArray(),
                    PersistentAddresses = ImmutableArray<ApplyAddress>.Empty
                };
                if (kind == "warning") f.Snapshot = f.Snapshot with
                { Addresses = f.Snapshot.Addresses.Select(a => a with { State = "duplicate" }).ToImmutableArray() };
                var service = new LocalProfileApplyService(f, f,
                    _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { f.Snapshot.Identity }), _ => throw new Exception(), 1);
                var vm = new MainViewModel(() => new() { new() { Id = f.Snapshot.Identity.Id.ToString(), Name = f.Snapshot.Identity.Alias,
                    CurrentIp = "192.0.2.10", CurrentSubnet = "255.255.255.0", IsConnected = kind != "offline" } }, apply: service);
                var window = new MainWindow(vm, new ProjectViewModel(new FakeDialogs(), new FakeBrowser()), (_, _, _) => { })
                { Width = minimum ? 760 : 1200, Height = minimum ? 520 : 850 };
                try
                {
                    window.Show(); Pump();
                    vm.SelectedAdapter!.EditIsDhcp = false; vm.SelectedAdapter.EditIp = "192.0.2.20";
                    vm.SelectedAdapter.EditSubnet = "255.255.255.0";
                    foreach (var action in new[] { "cancel", "enter", "escape", "apply" })
                    {
                        var pending = vm.ApplyAsync();
                        AwaitUi(() => Descendants(window).OfType<ContentDialog>().Any(d => d.IsVisible), "Apply confirmation did not open.");
                        var dialog = Descendants(window).OfType<ContentDialog>().Single(d => d.IsVisible);
                        var content = (ApplyConfirmationContent)dialog.Content;
                        var review = (ApplyConfirmation)content.DataContext;
                        var details = (Expander)content.FindName("TechnicalDetails");
                        if (details.IsExpanded || dialog.DefaultButton != ContentDialogButton.Close
                            || dialog.PrimaryButtonText != Loc.Get("Common.Apply") || dialog.CloseButtonText != Loc.Get("Common.Cancel"))
                            throw new Exception("Apply must be deliberate, named, and compact by default.");
                        if (review.Rows.Count != 4 || review.Rows.Any(r => r.Label.StartsWith("EditBar.", StringComparison.Ordinal)))
                            throw new Exception("Compact comparison is incomplete or unlocalized.");
                        if (kind == "offline" && !review.Warning.Contains(Loc.Get("Apply.CompactOffline"))
                            || kind == "warning" && !review.Warning.Contains(Loc.Get("Apply.SourceConflict")))
                            throw new Exception("Important warning is hidden in details.");
                        var apply = Descendants(dialog).OfType<Wpf.Ui.Controls.Button>().Single(b => b.IsVisible && b.Content?.ToString() == Loc.Get("Common.Apply"));
                        var cancel = Descendants(dialog).OfType<Wpf.Ui.Controls.Button>().Single(b => b.IsVisible && b.Content?.ToString() == Loc.Get("Common.Cancel"));
                        if (Keyboard.FocusedElement != cancel) throw new Exception("Cancel must receive initial keyboard focus.");
                        AssertDialogActionVisible(window, apply); AssertDialogActionVisible(window, cancel);
                        if (action == "cancel")
                        {
                            Capture(window, Path.Combine(output, $"confirm-{kind}-{theme}-{(minimum ? "minimum" : "normal")}.png"));
                            details.IsExpanded = true; Pump();
                            AssertDialogActionVisible(window, apply); AssertDialogActionVisible(window, cancel);
                            Capture(window, Path.Combine(output, $"details-{kind}-{theme}-{(minimum ? "minimum" : "normal")}.png"));
                            details.IsExpanded = false; Pump();
                        }
                        switch (action)
                        {
                            case "enter": DiscardKey(window, Key.Enter); break;
                            case "escape": details.Focus(); DiscardKey(window, Key.Escape); break;
                            case "apply": apply.Focus(); DiscardKey(window, Key.Enter); break;
                            default: DeviceClick(cancel); break;
                        }
                        AwaitUi(() => pending.IsCompleted, $"Apply dialog did not complete: {action}, {kind}, {theme}, minimum={minimum}.");
                        pending.GetAwaiter().GetResult();
                        if (f.Executed != (action == "apply" ? 1 : 0)) throw new Exception("Confirmation bypassed or lost explicit consent.");
                    }
                    if (f.Opened != 0) throw new Exception("Local Apply opened a browser.");
                }
                finally { window.Close(); vm.Diagnostics.Dispose(); Pump(); }
            }
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }
    private static void AwaitUi(Func<bool> ready, string failure)
    {
        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (!ready() && DateTime.UtcNow < deadline) Pump();
        if (!ready()) throw new Exception(failure);
        Pump();
    }
    private static void AssertDialogActionVisible(Window window, FrameworkElement button)
    {
        var rect = button.TransformToAncestor(window).TransformBounds(new Rect(button.RenderSize));
        if (!button.IsVisible || rect.Left < 0 || rect.Top < 0 || rect.Right > window.ActualWidth || rect.Bottom > window.ActualHeight)
            throw new Exception("Confirmation footer action is clipped.");
    }
}
