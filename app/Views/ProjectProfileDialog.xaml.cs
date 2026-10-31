using System.Windows;
using System.ComponentModel;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ProjectProfileDialog : Wpf.Ui.Controls.FluentWindow
{
    private readonly ProjectViewModel _project;
    private bool _dirty, _kept, _exitApproved, _validationShown;
    public ProjectProfileDialog(ProjectViewModel project, ProjectProfile? profile = null)
    {
        InitializeComponent();
        _project = project;
        var draft = new ProjectProfileEditor(profile ?? new ProjectProfile());
        draft.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName)) return;
            _dirty = true; _exitApproved = false;
            if (_validationShown) EditorError.Text = draft.Error;
        };
        DataContext = draft;
    }
    private void Keep_Click(object sender, RoutedEventArgs e)
    {
        var draft = (ProjectProfileEditor)DataContext;
        _validationShown = true;
        draft.ShowValidation();
        if (draft.FirstInvalidField is { } field)
        {
            EditorError.Text = draft.Error; Fields.FocusField(field); return;
        }
        if (_project.TryStoreProfile((ProjectProfileEditor)DataContext)) { _kept = true; DialogResult = true; }
        else EditorError.Text = _project.Error;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    public bool ConfirmApplicationExit()
    {
        _exitApproved = !_dirty || _kept || _project.ConfirmDiscardModalDraft();
        return _exitApproved;
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitApproved && !ConfirmApplicationExit()) e.Cancel = true;
        base.OnClosing(e);
    }
}
