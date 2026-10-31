using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ProjectWindow : Wpf.Ui.Controls.FluentWindow
{
    private bool _exitApproved;
    public ProjectWindow()
    {
        InitializeComponent();
        DataContext = new ProjectViewModel(new ProjectDialogs(this));
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitApproved && DataContext is ProjectViewModel vm && !vm.CanDiscard()) e.Cancel = true;
        base.OnClosing(e);
    }

    public bool ConfirmApplicationExit()
    {
        if (DataContext is ProjectViewModel vm && !vm.CanDiscard()) return false;
        _exitApproved = true;
        return true;
    }
}

internal sealed class ProjectDialogs(Window owner) : IProjectDialogs
{
    public string? OpenPath()
    {
        var dialog = new OpenFileDialog { Filter = "AV Network project (*.json;*.bak)|*.json;*.bak", CheckFileExists = true };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }
    public string? ChooseFolder()
    {
        var dialog = new OpenFolderDialog { Title = Loc.Get("Project.ChooseFolder", "Choose folder"), Multiselect = false };
        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }
    public string? SavePath(string? folder)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "AV Network project (*.avnet.json)|*.avnet.json", DefaultExt = ".avnet.json",
            FileName = "project.avnet.json", InitialDirectory = folder ?? "", OverwritePrompt = false,
            CheckPathExists = true
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }
    public bool ConfirmDiscard() => System.Windows.MessageBox.Show(owner,
        Loc.Get("Project.Discard", "Discard unsaved changes?"), Loc.Get("Project.Title", "Project"),
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    public bool ConfirmDelete() => System.Windows.MessageBox.Show(owner,
        Loc.Get("Project.DeleteConfirm", "Delete the selected profile?"), Loc.Get("Project.Title", "Project"),
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    public bool ConfirmOverwrite(string path) => System.Windows.MessageBox.Show(owner,
        Loc.Get("Project.Overwrite", "Replace this file and retain a backup?") + "\n\n" + path,
        Loc.Get("Project.Title", "Project"), MessageBoxButton.YesNo, MessageBoxImage.Question,
        MessageBoxResult.No) == MessageBoxResult.Yes;
}
