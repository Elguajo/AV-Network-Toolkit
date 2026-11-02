using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void RenderProfileSwitching(string output)
    {
        foreach (var theme in new[] { "Light", "Dark" })
        foreach (var minimum in new[] { false, true })
        foreach (var kind in new[] { "dhcp", "no-dns", "duplicate" })
        {
            ApplyRenderTheme(theme);
            var f = new ApplyFixture();
            f.Snapshot = kind == "dhcp" ? f.Snapshot with
            {
                Dhcp = true, DnsSource = "automatic",
                Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "dhcp", "preferred")),
                PersistentAddresses = ImmutableArray<ApplyAddress>.Empty
            } : f.Snapshot with { DnsSource = "none", DnsServers = ImmutableArray<string>.Empty };
            if (kind == "duplicate") f.Snapshot = f.Snapshot with
            {
                Addresses = ImmutableArray.Create(new ApplyAddress("192.0.2.10", 24, "manual", "duplicate"),
                    new ApplyAddress("169.254.42.10", 16, "wellknown", "preferred"))
            };
            var profile = new ProjectProfile { Name = "Equipment network", Mode = "static", Ipv4 = new()
                { Address = "192.0.2.20", PrefixLength = 24, Dns = new() { Mode = "leave-unchanged" } } };
            var vm = new ProjectApplyViewModel(profile, null, f, f, f,
                _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { f.Snapshot.Identity }),
                () => Array.Empty<ProjectApplyRequest>(), f, verificationAttempts: 1, targetAdapterId: f.Snapshot.Identity.Id);
            var window = new ProjectApplyWindow(vm) { Width = minimum ? 620 : 760, Height = minimum ? 520 : 760 };
            window.Show(); Pump(); vm.RefreshAsync().GetAwaiter().GetResult();
            vm.ReviewAsync(false).GetAwaiter().GetResult(); Pump();
            if (!vm.ApplyCommand.CanExecute(null) || f.Executed != 0) throw new Exception("Switching review blocked or applied settings.");
            if (kind == "dhcp" && !vm.Review.Contains("DNS: static 192.0.2.53, 192.0.2.54")) throw new Exception("Frozen DNS hidden from review.");
            if (kind == "no-dns" && !vm.Review.Contains(Loc.Get("Apply.NoDns"))) throw new Exception("Empty DNS not explained.");
            if (kind == "duplicate" && !vm.Review.Contains(Loc.Get("Apply.SourceConflict"))) throw new Exception("Conflict warning missing.");
            var confirm = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => ReferenceEquals(b.Command, vm.ApplyCommand));
            var bounds = confirm.TransformToAncestor(window).TransformBounds(new Rect(confirm.RenderSize));
            if (!confirm.IsEnabled || bounds.Bottom > window.ActualHeight || bounds.Right > window.ActualWidth) throw new Exception("Switching confirmation clipped.");
            var review = Descendants(window).OfType<TextBlock>().Single(t => t.Text == vm.Review);
            Descendants(window).OfType<ScrollViewer>().First().ScrollToVerticalOffset(review.TransformToAncestor(window).Transform(new Point()).Y);
            Pump(); Capture(window, Path.Combine(output, $"switch-{kind}-{theme}-{(minimum ? "minimum" : "normal")}.png"));
            vm.Cancel(); window.Close();
        }
    }
}
