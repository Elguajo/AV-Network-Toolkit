using System.IO;
using System.Windows;
using System.Windows.Controls;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void RenderAuditFixes(string output)
    {
        foreach (var theme in new[] { "Dark", "Light" })
        foreach (var minimum in new[] { false, true })
        {
            Application.Current.Resources.MergedDictionaries[0] = new Wpf.Ui.Markup.ThemesDictionary
            { Theme = theme == "Dark" ? Wpf.Ui.Appearance.ApplicationTheme.Dark : Wpf.Ui.Appearance.ApplicationTheme.Light };
            var suffix = $"{theme}-{(minimum ? "minimum" : "normal")}";
            var dialog = new AddProfileDialog(new IpProfile { Name = "Draft retained after save failure", IsDhcp = true })
            { Width = minimum ? 380 : 440, Height = minimum ? 400 : 560, Commit = _ => false };
            dialog.Show(); Pump();
            var save = Descendants(dialog).OfType<Wpf.Ui.Controls.Button>().Single(b => b.IsDefault);
            save.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); Pump();
            var draft = (DialogViewModel)dialog.DataContext;
            if (!dialog.IsVisible || draft.SaveError.Length == 0 || draft.Name != "Draft retained after save failure")
                throw new Exception("Failed save lost draft or closed its dialog.");
            var scroll = Descendants(dialog).OfType<ScrollViewer>().First(); scroll.ScrollToTop(); Pump();
            var bounds = save.TransformToAncestor(dialog).TransformBounds(new Rect(save.RenderSize));
            if (!save.IsEnabled || bounds.Bottom > dialog.ActualHeight) throw new Exception("Retry save footer clipped.");
            Capture(dialog, Path.Combine(output, $"profile-save-error-{suffix}.png")); dialog.Close(); Pump();

            var fixture = new ApplyFixture();
            var request = new ProjectApplyRequest(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1), fixture.Snapshot,
                new("dhcp", null, 0, null, "automatic", System.Collections.Immutable.ImmutableArray<string>.Empty));
            int offset = -1;
            var vm = new ProjectApplyViewModel(null, null, fixture, fixture, fixture,
                _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { fixture.Snapshot.Identity }),
                () => new[] { request }, fixture, readPage: page => { offset = page; return new[] { request }; },
                hasMore: () => offset == 0, unreadable: () => 2);
            var recovery = new ProjectApplyWindow(vm) { Width = minimum ? 620 : 760, Height = minimum ? 520 : 760 };
            recovery.Show(); Pump(); vm.ReadSnapshotsCommand.Execute(null); Pump();
            if (!vm.NextSnapshotsCommand.CanExecute(null) || !vm.Status.Contains(Loc.Format("Storage.UnreadableSnapshots", 2)))
                throw new Exception("Recovery pagination or corrupt-file warning missing.");
            Descendants(recovery).OfType<ScrollViewer>().First().ScrollToBottom(); Pump();
            Capture(recovery, Path.Combine(output, $"recovery-pagination-{suffix}.png"));
            vm.SelectedSnapshot = vm.Snapshots[0]; vm.NextSnapshotsCommand.Execute(null); Pump();
            if (offset != 1000 || vm.SelectedSnapshot != null || vm.NextSnapshotsCommand.CanExecute(null) || fixture.Executed != 0)
                throw new Exception("Recovery page retained a stale selection or executed a mutation.");
            recovery.Close(); Pump();
        }
    }
}
