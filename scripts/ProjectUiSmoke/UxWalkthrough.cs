using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NetworkProfileManager;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void RenderUxWalkthrough(string output, bool before)
    {
        var root = Path.Combine(output, "ux-fixture"); Directory.CreateDirectory(root);
        AppPaths.OverrideDataDirectory(root);
        try
        {
            var dialogs = new UxDialogs { Save = Path.Combine(root, "demo.avnet.json") };
            var project = new ProjectViewModel(dialogs, new FakeBrowser()) { ProjectName = "Demo Hall" };
            var draft = new ProjectProfileEditor { Name = "Woxcon Video Wall", Mode = "static", Address = "192.0.2.100", DnsMode = "static", Dns1 = "198.51.100.53" };
            if (!project.TryStoreProfile(draft)) throw new Exception(project.Error);
            var main = new MainViewModel(() => new()
            {
                new() { Id = "11111111-1111-4111-8111-111111111111", Name = "Ethernet", Description = "Realtek PCIe GbE", AdapterType = "Ethernet",
                    CurrentIp = "192.0.2.10", CurrentSubnet = "255.255.255.0", IsConnected = true }
            });
            var window = new MainWindow(main, project, (_, _, _) => { }) { Width = 1200, Height = 850 };
            window.Show(); Pump();
            var scopes = (TabControl)window.FindName("ProfileScopes"); scopes.SelectedIndex = 1; Pump();
            Capture(window, Path.Combine(output, "main-project.png"));
            if (!before)
            {
                var save = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(x => x.Content?.ToString() == Loc.Get("Ux.SaveProject"));
                InvokeUxButton(save);
                if (dialogs.SavePickers != 1 || project.HasUnsavedChanges || !File.Exists(dialogs.Save)) throw new Exception("First Save project did not create the explicit fixture file.");
                var manager = new ProjectWindow(project) { Owner = window }; manager.Show(); Pump();
                var name = Descendants(manager).OfType<Wpf.Ui.Controls.TextBox>().Single(x => System.Windows.Automation.AutomationProperties.GetName(x) == Loc.Get("Project.Name"));
                name.Text = "Demo Hall — updated"; name.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource(); Pump();
                manager.Close(); Pump(); InvokeUxButton(save);
                if (dialogs.SavePickers != 1 || project.HasUnsavedChanges || project.FileFeedback.Length == 0) throw new Exception("Repeat Save asked for a path or omitted success feedback.");
                Capture(window, Path.Combine(output, "main-project-saved.png"));
            }
            // Exercise the actual editor controls, not an image of a proposed interface.
            var editor = new ProjectProfileDialog(project, project.Profiles[0]) { Owner = window, Height = 480 };
            editor.Show(); Pump();
            var fields = Descendants(editor).OfType<Wpf.Ui.Controls.TextBox>().ToList();
            var address = fields.Single(x => System.Windows.Automation.AutomationProperties.GetName(x) == Loc.Get(before ? "EditBar.Ip" : "Ux.ComputerIp"));
            address.Text = "invalid"; address.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource(); Pump();
            var keep = Descendants(editor).OfType<Wpf.Ui.Controls.Button>().Single(x => x.Content?.ToString() == Loc.Get("Project.KeepProfile"));
            keep.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
            Capture(editor, Path.Combine(output, "editor-invalid-ip.png"));
            var message = ((TextBlock)editor.FindName("EditorError")).Text;
            if (before && !message.Contains("schema", StringComparison.OrdinalIgnoreCase)) throw new Exception("Baseline generic error not reproduced.");
            if (!before && (!message.StartsWith("Computer IP", StringComparison.Ordinal) || !address.IsKeyboardFocusWithin))
                throw new Exception("Invalid IP did not receive specific feedback/focus.");
            if (!before)
            {
                address.Text = "192.0.2.101"; address.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource(); Pump();
                if (((TextBlock)editor.FindName("EditorError")).Text.Length != 0) throw new Exception("Corrected field retained a stale error.");
                address.Text = "invalid"; address.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource(); Pump();
            }
            var prompts = dialogs.DiscardPrompts; editor.Close(); Pump();
            if (before && (editor.IsVisible || dialogs.DiscardPrompts != prompts)) throw new Exception("Baseline silent close not reproduced.");
            if (!before)
            {
                if (!editor.IsVisible || dialogs.DiscardPrompts == prompts) throw new Exception("Declined discard did not retain editor.");
                dialogs.Discard = true; editor.Close(); Pump(); dialogs.Discard = false;
                ExerciseUxAddProfile(window, project, dialogs);
            }
            main.SelectedAdapter = null; Pump();
            Capture(window, Path.Combine(output, "main-no-adapter.png"));
            if (!before)
            {
                var refresh = (Wpf.Ui.Controls.Button)window.FindName("EmptyRefresh");
                if (!refresh.IsVisible) throw new Exception("No-adapter state omitted the next action.");
                InvokeUxButton(refresh);
                if (main.SelectedAdapter == null) throw new Exception("Empty-state refresh did not use the fixture adapter source.");
                main.SelectedAdapter.Info.IsConnected = false; main.SelectedAdapter.UpdateInfo(main.SelectedAdapter.Info); Pump();
                if (((TextBlock)window.FindName("CurrentIpText")).Text != "192.0.2.10") throw new Exception("Disconnected link hid the configured IP.");
                Capture(window, Path.Combine(output, "main-disconnected.png"));
                project.ProfileForApply("missing-profile"); Pump();
                var feedback = (FrameworkElement)window.FindName("WorkspaceFeedback"); AssertWorkspaceBounds(window, feedback);
                Capture(window, Path.Combine(output, "main-action-error.png"));
            }
            var fixture = new ApplyFixture { Fail = false };
            var apply = new ProjectApplyViewModel(project.Profiles[0], null, fixture, fixture, fixture,
                _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { fixture.Snapshot.Identity }),
                () => Array.Empty<ProjectApplyRequest>(), fixture, verificationAttempts: 1, targetAdapterId: fixture.Snapshot.Identity.Id);
            var applyWindow = new ProjectApplyWindow(apply) { Width = 620, Height = 520 };
            applyWindow.Show(); apply.RefreshAsync().GetAwaiter().GetResult(); Pump();
            Capture(applyWindow, Path.Combine(output, "apply-fixed-target.png"));
            if (before && apply.CanSelectAdapter) throw new Exception("Baseline fixed target unexpectedly editable.");
            if (!before)
            {
                if (!apply.Status.Contains("Review settings") || !apply.Status.Contains(fixture.Snapshot.Identity.Alias)) throw new Exception("Fixed target guidance omitted the next action.");
                AssertWorkspaceBounds(applyWindow, (FrameworkElement)applyWindow.FindName("ApplyFeedback"));
            }
            File.WriteAllText(Path.Combine(output, "observations.txt"),
                $"Phase: {(before ? "before" : "after")}\nEditor error: {message}\nClose discard prompts: {dialogs.DiscardPrompts - prompts}\nFixed-target initial status: {apply.Status}\n");
            if (!before) ExerciseUxApplyStates(output, applyWindow, apply, fixture);
            applyWindow.Close(); dialogs.Discard = true; window.Close(); Pump();
            if (!before)
            {
                var emptyMain = new MainViewModel(() => new());
                var emptyWindow = new MainWindow(emptyMain, new ProjectViewModel(dialogs, new FakeBrowser()), (_, _, _) => { }) { Width = 760, Height = 520 };
                emptyWindow.Show(); Pump();
                var refresh = (Wpf.Ui.Controls.Button)emptyWindow.FindName("EmptyRefresh"); InvokeUxButton(refresh);
                if (emptyMain.Adapters.Count != 0 || emptyMain.SelectedAdapter != null) throw new Exception("Absent adapter fixture invented a target.");
                AssertWorkspaceBounds(emptyWindow, refresh);
                Capture(emptyWindow, Path.Combine(output, "main-empty-minimum.png")); emptyWindow.Close(); Pump();
            }
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }

    private static void ExerciseUxAddProfile(MainWindow window, ProjectViewModel project, UxDialogs dialogs)
    {
        var count = project.Profiles.Count; Exception? failure = null; var kept = false;
        // ContextIdle runs in ShowDialog's nested dispatcher after the real Add click opens it.
        window.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<ProjectProfileDialog>().Single(w => w.IsVisible);
            try
            {
                var name = Descendants(dialog).OfType<Wpf.Ui.Controls.TextBox>().Single(b => System.Windows.Automation.AutomationProperties.GetName(b) == Loc.Get("AddProfile.NameLabel"));
                name.Text = "DSP control"; name.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                var mode = Descendants(dialog).OfType<ComboBox>().Single(c => c.Name == "ModeField"); mode.SelectedValue = "static"; Pump();
                var ip = Descendants(dialog).OfType<Wpf.Ui.Controls.TextBox>().Single(b => b.Name == "AddressField");
                ip.Text = "203.0.113.100"; ip.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                var keep = Descendants(dialog).OfType<Wpf.Ui.Controls.Button>().Single(b => b.Content?.ToString() == Loc.Get("Project.KeepProfile"));
                InvokeUxButton(keep); kept = !dialog.IsVisible;
                if (!kept) throw new Exception("Valid modal did not close after Keep.");
            }
            catch (Exception ex) { failure = ex; dialogs.Discard = true; dialog.Close(); dialogs.Discard = false; }
        }));
        var panel = (ProjectProfilesPanel)window.FindName("ProjectProfiles");
        var add = Descendants(panel).OfType<Wpf.Ui.Controls.Button>().Single(b => b.Content?.ToString() == Loc.Get("Profiles.AddButton"));
        add.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
        if (failure != null) throw new Exception("Actual Add/Keep dialog failed.", failure);
        if (!kept || project.Profiles.Count != count + 1 || !project.HasUnsavedChanges || project.FileFeedback.Length == 0)
            throw new Exception("Actual Add/Keep did not retain a detached profile as an unsaved project change.");
    }

    private static void ExerciseUxApplyStates(string output, ProjectApplyWindow window, ProjectApplyViewModel vm, ApplyFixture fixture)
    {
        var read = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => ReferenceEquals(b.Command, vm.ReadSnapshotsCommand));
        InvokeUxButton(read);
        if (vm.Snapshots.Count != 0 || !vm.Status.StartsWith("No saved recovery snapshots")) throw new Exception("Empty recovery omitted an explanation.");
        Capture(window, Path.Combine(output, "apply-no-recovery.png"));
        var review = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => ReferenceEquals(b.Command, vm.ReviewCommand));
        var confirm = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => ReferenceEquals(b.Command, vm.ApplyCommand));
        InvokeUxButton(review); InvokeUxButton(confirm);
        if (fixture.Executed != 0) throw new Exception("Declined final confirmation ran a fake worker.");
        fixture.Consent = true; fixture.ElevationDenied = true; InvokeUxButton(confirm);
        if (!vm.Status.Contains("Administrator consent was declined") || fixture.Opened != 0) throw new Exception("UAC-denied fixture misreported result.");
        AssertWorkspaceBounds(window, (FrameworkElement)window.FindName("ApplyFeedback")); AssertWorkspaceBounds(window, confirm);
        Capture(window, Path.Combine(output, "apply-uac-denied.png"));
        fixture.ElevationDenied = false; fixture.Fail = true;
        InvokeUxButton(review); InvokeUxButton(confirm);
        if (!vm.Status.Contains("Changes may be partial") || !vm.Status.Contains("Secondary DNS") || fixture.Opened != 0 || vm.Observed.Length == 0)
            throw new Exception("Partial fake failure omitted stage/recovery/actual state.");
        AssertWorkspaceBounds(window, (FrameworkElement)window.FindName("ApplyFeedback")); AssertWorkspaceBounds(window, confirm);
        var recovery = (Wpf.Ui.Controls.Button)window.FindName("RecoveryReviewFooter"); AssertWorkspaceBounds(window, recovery);
        if (!recovery.IsEnabled) throw new Exception("Partial failure hid the next recovery action.");
        Capture(window, Path.Combine(output, "apply-partial-failure.png"));

        var loading = new TaskCompletionSource<IReadOnlyList<ApplyAdapterIdentity>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingVm = new ProjectApplyViewModel(new ProjectProfile { Name = "Loading fixture" }, null, fixture, fixture, fixture,
            _ => loading.Task, () => Array.Empty<ProjectApplyRequest>(), fixture, targetAdapterId: fixture.Snapshot.Identity.Id);
        var pendingWindow = new ProjectApplyWindow(pendingVm) { Width = 620, Height = 520 }; pendingWindow.Show();
        var refresh = pendingVm.RefreshAsync(); Pump();
        if (!pendingVm.IsBusy || !pendingVm.Status.StartsWith("Reading adapter settings") || pendingVm.ReviewCommand.CanExecute(null))
            throw new Exception("Loading allowed review or retained stale status.");
        Capture(pendingWindow, Path.Combine(output, "apply-loading.png"));
        loading.SetResult(new[] { fixture.Snapshot.Identity });
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!refresh.IsCompleted && DateTime.UtcNow < deadline) Pump();
        if (!refresh.IsCompleted) throw new Exception("Loading fixture did not finish.");
        refresh.GetAwaiter().GetResult(); Pump();
        if (pendingVm.IsBusy || pendingVm.SelectedAdapter?.Id != fixture.Snapshot.Identity.Id) throw new Exception("Loading did not restore fixed target.");
        pendingWindow.Close();
    }

    private static void InvokeUxButton(Wpf.Ui.Controls.Button button)
    {
        var peer = new System.Windows.Automation.Peers.ButtonAutomationPeer(button);
        var invoke = (System.Windows.Automation.Provider.IInvokeProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke);
        invoke.Invoke(); Pump();
    }

    private sealed class UxDialogs : IProjectDialogs
    {
        public string? Save;
        public bool Discard;
        public int DiscardPrompts, SavePickers;
        public string? OpenPath() => null;
        public string? ChooseFolder() => null;
        public string? SavePath(string? folder) { SavePickers++; return Save; }
        public bool ConfirmDiscard() { DiscardPrompts++; return Discard; }
        public bool ConfirmDelete() => true;
        public bool ConfirmOverwrite(string path) => true;
        public bool ConfirmDeleteDevice() => true;
        public bool ConfirmOpenDevice(string destination, bool isHttp, bool hostMismatch, bool unlinked) => false;
    }
}
