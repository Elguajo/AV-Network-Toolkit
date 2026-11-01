using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;
using NetworkProfileManager.Models;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace NetworkProfileManager
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly SnackbarService        _snackbarService = new();
        private readonly ContentDialogService   _dialogService   = new();
        private ProjectWindow? _projectWindow;
        private ProjectApplyWindow? _linkLocalWindow;
        private MdnsDiscoveryWindow? _mdnsWindow;
        public ProjectViewModel Project { get; }
        private readonly Action<ProjectProfile, ProjectDevice?, Guid>? _applyRequest;

        private void ShowProjectApply(ProjectProfile profile, ProjectDevice? device)
        {
            if (DataContext is not MainViewModel vm || vm.SelectedAdapter == null
                || !Guid.TryParse(vm.SelectedAdapter.Info.Id, out var target) || target == Guid.Empty)
            {
                System.Windows.MessageBox.Show(this, Loc.Get("Workspace.TargetMissing")); return;
            }
            if (_applyRequest != null) { _applyRequest(profile, device, target); return; }
            try
            {
                var window = ProjectApplyWindow.Create(profile, device, Project.ProjectName, targetAdapterId: target);
                window.Owner = this; window.ShowDialog();
                vm.Refresh();
            }
            catch { System.Windows.MessageBox.Show(this, Loc.Get("Apply.Unsupported")); }
        }

        private void Recovery_Click(object sender, RoutedEventArgs e)
        {
            var window = ProjectApplyWindow.Create(null, null); window.Owner = this; window.ShowDialog();
        }
        private void ProjectOpened_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceTabs.SelectedIndex = 0; ProfileScopes.SelectedIndex = 1;
        }
        private void ToolExpanded(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, e.OriginalSource) && sender is FrameworkElement control) control.BringIntoView();
        }
        public bool ConfirmApplicationExit()
        {
            if (OwnedWindows.OfType<ProjectApplyWindow>().Any(w => !w.ConfirmApplicationExit())) return false;
            if (OwnedWindows.OfType<ProjectProfileDialog>().Any(w => !w.ConfirmApplicationExit())) return false;
            return Project.CanDiscard();
        }

        private void OpenMdnsClick(object sender, RoutedEventArgs e)
        {
            if (_mdnsWindow != null) { _mdnsWindow.Activate(); return; }
            _mdnsWindow = MdnsDiscoveryWindow.Create();
            _mdnsWindow.Owner = this;
            _mdnsWindow.Closed += (_, _) => _mdnsWindow = null;
            _mdnsWindow.Show();
        }

        private void OpenLinkLocalClick(object sender, RoutedEventArgs e)
        {
            if (_linkLocalWindow != null) { _linkLocalWindow.Activate(); return; }
            _linkLocalWindow = ProjectApplyWindow.Create(null, null, linkLocalTool: true);
            _linkLocalWindow.Owner = this;
            _linkLocalWindow.Closed += (_, _) => _linkLocalWindow = null;
            _linkLocalWindow.Show();
        }

        private void OpenProjectClick(object sender, RoutedEventArgs e)
        {
            if (_projectWindow == null)
            {
                _projectWindow = new ProjectWindow(Project) { Owner = this };
                _projectWindow.Closed += (_, _) => _projectWindow = null;
                _projectWindow.Show();
            }
            else _projectWindow.Activate();
        }

        private void ProjectMore_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement button || button.ContextMenu == null) return;
            button.ContextMenu.DataContext = Project;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }

        public MainWindow() : this(new MainViewModel(), null) { }

        // Component harness supplies fake NIC enumeration and isolated project IO.
        internal MainWindow(MainViewModel main, ProjectViewModel? project, Action<ProjectProfile, ProjectDevice?, Guid>? applyRequest = null)
        {
            _applyRequest = applyRequest;
            Project = project ?? new ProjectViewModel(new ProjectDialogs(this));
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            DataContext = main;
            Closed += (_, _) => main.Diagnostics.Dispose();
            ProjectProfiles.ApplyRequested += profile => ShowProjectApply(profile, null);
            Project.ApplyAndOpenRequested += (profile, device) => ShowProjectApply(profile, device);
            RegisterProfileHotkeys();

            // Fix maximized window overlapping the taskbar
            StateChanged += (s, e) =>
            {
                if (WindowState == WindowState.Maximized)
                {
                    var area = SystemParameters.WorkArea;
                    MaxWidth  = area.Width;
                    MaxHeight = area.Height;
                }
                else
                {
                    MaxWidth  = double.PositiveInfinity;
                    MaxHeight = double.PositiveInfinity;
                }
            };
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Wire up WPF-UI services
            _snackbarService.SetSnackbarPresenter(RootSnackbar);
            _dialogService.SetDialogHost(RootDialogHost);
            NotificationService.Initialize(_snackbarService);
        }

        // ── Copy current IP ──────────────────────────────────────────
        private void CopyIp_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe && fe.Tag is string ip)) return;
            if (string.IsNullOrEmpty(ip) || ip == "—") return;

            if (ClipboardHelper.TrySetText(ip))
                NotificationService.Info(Loc.Format("Status.Copied", ip));
            else
                NotificationService.Warning(Loc.Get("Status.CopyFailed", "Panoya kopyalanamadı."));
        }

        // ── Theme settings ───────────────────────────────────────────
        private void OpenThemeClick(object sender, RoutedEventArgs e)
        {
            var win = new ThemeSettingsWindow { Owner = this };
            win.ShowDialog();
        }

        // ── Ping window ──────────────────────────────────────────────
        private void OpenPingWindowClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAdapter == null) return;

            var diagnostics = vm.Diagnostics.CreatePeer();
            if (string.IsNullOrWhiteSpace(diagnostics.Target))
                diagnostics.Target = vm.SelectedAdapter.PingTarget.Trim();
            new PingWindow(diagnostics) { Owner = this }.Show();
        }

        // ── IP Scanner window ────────────────────────────────────────
        private void OpenScannerClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            string adapterIp = vm?.SelectedAdapter?.Info.CurrentIp ?? "";

            string start = "", end = "";
            if (System.Net.IPAddress.TryParse(adapterIp, out var parsed))
            {
                var b = parsed.GetAddressBytes();
                start = $"{b[0]}.{b[1]}.{b[2]}.1";
                end   = $"{b[0]}.{b[1]}.{b[2]}.254";
            }

            new IpScannerWindow(start, end, ip =>
            {
                if (vm?.SelectedAdapter == null) return;
                vm.SelectedAdapter.EditIp       = ip;
                vm.SelectedAdapter.EditIsStatic = true;
            }) { Owner = this }.Show();
        }

        // ── Right panel fade-in on adapter change ────────────────────
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is MainViewModel vm)) return;

            // Route destructive-action confirmations through the Fluent dialog host.
            vm.ConfirmHandler = ConfirmAsync;

            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.SelectedAdapter))
                {
                    RightPanel.ScrollToTop();
                    FadeIn(RightPanel);
                }
            };
        }

        /// <summary>
        /// Ctrl+1..5 apply the selected adapter's Nth saved profile — the whole point of
        /// the app for someone hopping between machine networks all day.
        /// </summary>
        private void RegisterProfileHotkeys()
        {
            for (int i = 0; i < MainViewModel.MaxProfilesPerAdapter; i++)
            {
                int index = i;   // captured per iteration
                var command = new NetworkProfileManager.Commands.RelayCommand(
                    async _ =>
                    {
                        if (DataContext is MainViewModel vm)
                            await vm.ApplyProfileByIndexAsync(index);
                    });

                InputBindings.Add(new KeyBinding(
                    command,
                    Key.D1 + index,
                    ModifierKeys.Control));
            }
        }

        private async Task<bool> ConfirmAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title             = title,
                Content           = message,
                PrimaryButtonText = Loc.Get("Common.Yes", "Evet"),
                CloseButtonText   = Loc.Get("Common.Cancel", "İptal")
            };
            var result = await _dialogService.ShowAsync(dialog, System.Threading.CancellationToken.None);
            return result == ContentDialogResult.Primary;
        }

        private static void FadeIn(UIElement el)
        {
            el.Opacity = 0;
            el.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200))));
        }
    }
}
