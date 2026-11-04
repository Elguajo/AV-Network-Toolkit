using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ProjectWindow : Wpf.Ui.Controls.FluentWindow
{
    private bool _exitApproved;
    private readonly bool _shared;
    public ProjectWindow() : this(null) { }
    internal ProjectWindow(ProjectViewModel? shared)
    {
        InitializeComponent();
        _shared = shared != null;
        var vm = shared ?? new ProjectViewModel(new ProjectDialogs(this));
        if (!_shared) vm.ApplyAndOpenRequested += (profile, device) =>
        {
            try { var window = ProjectApplyWindow.Create(profile, device, vm.ProjectName); window.Owner = this; window.ShowDialog(); }
            catch { System.Windows.MessageBox.Show(this, Loc.Get("Apply.Unsupported"), Loc.Get("Apply.Title")); }
        };
        DataContext = vm;
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (OwnedWindows.OfType<ProjectApplyWindow>().Any(w => !w.ConfirmApplicationExit())) e.Cancel = true;
        if (OwnedWindows.OfType<ProjectDeviceDialog>().Any(w => !w.ConfirmApplicationExit())) e.Cancel = true;
        if (!_shared && !_exitApproved && DataContext is ProjectViewModel vm && !vm.CanDiscard()) e.Cancel = true;
        base.OnClosing(e);
    }

    public bool ConfirmApplicationExit(bool approveClose = true)
    {
        if (OwnedWindows.OfType<ProjectApplyWindow>().Any(w => !w.ConfirmApplicationExit())) return false;
        if (OwnedWindows.OfType<ProjectDeviceDialog>().Any(w => !w.ConfirmApplicationExit())) return false;
        if (!_shared && DataContext is ProjectViewModel vm && !vm.CanDiscard()) return false;
        if (approveClose) _exitApproved = true;
        return true;
    }
    internal void ApproveApplicationClose() => _exitApproved = true;
    private void Recovery_Click(object sender, RoutedEventArgs e)
    {
        var window = ProjectApplyWindow.Create(null, null); window.Owner = this; window.ShowDialog();
    }
}

internal sealed class ProjectDialogs(Window owner) : IProjectDialogs
{
    public string? OpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = Loc.Get("Project.OpenFilter"), Title = Loc.Get("Project.Open"), CheckFileExists = true
        };
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
            Filter = Loc.Get("Project.SaveFilter"), Title = Loc.Get("Project.SaveAs"), DefaultExt = ".avnet.json",
            FileName = "project.avnet.json", InitialDirectory = folder ?? "", OverwritePrompt = false,
            CheckPathExists = true
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }
    public bool ConfirmDiscard()
    {
        // The shared project VM can be used by an owned editor. Keep its
        // confirmation above that editor and restore focus there on Cancel.
        var active = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        var dialogOwner = owner;
        for (var candidate = active; candidate != null; candidate = candidate.Owner)
            if (candidate == owner) { dialogOwner = active!; break; }
        return new ProjectDiscardDialog { Owner = dialogOwner }.ShowDialog() == true;
    }
    public bool ConfirmDelete() => System.Windows.MessageBox.Show(owner,
        Loc.Get("Project.DeleteConfirm", "Delete the selected profile?"), Loc.Get("Project.Title", "Project"),
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    public bool ConfirmPasswordSave(bool initialV2, bool backupHasPassword)
    {
        var message = Loc.Get("Password.Warning");
        if (initialV2) message += "\n\n" + Loc.Get("Password.VersionCopy");
        if (backupHasPassword) message += "\n\n" + Loc.Get("Password.BackupWarning");
        return System.Windows.MessageBox.Show(owner, message, Loc.Get("Password.Title"),
            MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.OK;
    }
    public bool ConfirmOverwrite(string path) => System.Windows.MessageBox.Show(owner,
        Loc.Get("Project.Overwrite", "Replace this file and retain a backup?") + "\n\n" + path,
        Loc.Get("Project.Title", "Project"), MessageBoxButton.YesNo, MessageBoxImage.Question,
        MessageBoxResult.No) == MessageBoxResult.Yes;
    public bool ConfirmDeleteDevice() => System.Windows.MessageBox.Show(owner,
        Loc.Get("Device.DeleteConfirm"), Loc.Get("Device.Header"), MessageBoxButton.YesNo,
        MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    public bool ConfirmOpenDevice(string destination, bool isHttp, bool hostMismatch, bool unlinked)
    {
        var message = Loc.Get("Device.OpenConfirm") + "\n\n" + destination + "\n\n" + Loc.Get("Device.BrowserOwnsTls");
        if (isHttp) message += "\n\n" + Loc.Get("Device.HttpWarning");
        if (hostMismatch) message += "\n\n" + Loc.Get("Device.HostWarning");
        if (unlinked) message += "\n\n" + Loc.Get("Device.UnlinkedWarning");
        return System.Windows.MessageBox.Show(owner, message, Loc.Get("Device.Open"), MessageBoxButton.OKCancel,
            isHttp || hostMismatch ? MessageBoxImage.Warning : MessageBoxImage.Question,
            MessageBoxResult.Cancel) == MessageBoxResult.OK;
    }
}
