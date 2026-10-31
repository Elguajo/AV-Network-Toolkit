using System.Windows;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ProjectProfileDialog : Wpf.Ui.Controls.FluentWindow
{
    private readonly ProjectViewModel _project;
    private bool _dirty, _kept;
    public ProjectProfileDialog(ProjectViewModel project, ProjectProfile? profile = null)
    {
        InitializeComponent();
        _project = project;
        var draft = new ProjectProfileEditor(profile ?? new ProjectProfile());
        draft.PropertyChanged += (_, _) => _dirty = true;
        DataContext = draft;
    }
    private void Keep_Click(object sender, RoutedEventArgs e)
    {
        if (_project.TryStoreProfile((ProjectProfileEditor)DataContext)) { _kept = true; DialogResult = true; }
        else EditorError.Text = _project.Error;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    public bool ConfirmApplicationExit() => !_dirty || _kept || _project.ConfirmDiscardModalDraft();
}
