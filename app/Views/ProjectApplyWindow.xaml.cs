using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ProjectApplyWindow : Wpf.Ui.Controls.FluentWindow
{
    // No parameterless production composition: smoke tests provide all boundaries.
    public ProjectApplyWindow(ProjectApplyViewModel viewModel)
    { InitializeComponent(); DataContext = viewModel; }

    internal static ProjectApplyWindow Create(ProjectProfile? profile, ProjectDevice? device, string projectName = "")
    {
        var observation = new WindowsProjectApplyObservation();
        var journal = new ProjectApplyJournal();
        var dialogs = new ApplyDialogs();
        var vm = new ProjectApplyViewModel(profile, device, new WindowsProjectApplyBoundary(), journal,
            new DefaultBrowserLauncher(), async token => await observation.EnumerateAsync(token), journal.ReadSnapshots, dialogs, projectName: projectName);
        var window = new ProjectApplyWindow(vm);
        dialogs.Owner = window;
        // Enumerate only after entering this explicit action; no automatic selection.
        window.Loaded += async (_, _) => await vm.RefreshAsync();
        return window;
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is ProjectApplyViewModel { IsBusy: true } vm) { vm.Cancel(); e.Cancel = true; }
        base.OnClosing(e);
    }
    public bool ConfirmApplicationExit()
    {
        if (DataContext is ProjectApplyViewModel { IsBusy: true } vm) { vm.Cancel(); return false; }
        return true;
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private sealed class ApplyDialogs : IProjectApplyDialogs
    {
        public Window? Owner;
        public bool Confirm(string review, bool recovery) => System.Windows.MessageBox.Show(Owner,
            review, Loc.Get(recovery ? "Apply.Recovery" : "Apply.Title"), MessageBoxButton.OKCancel,
            MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.OK;
    }
}
