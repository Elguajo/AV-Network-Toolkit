using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ProjectDeviceDialog : Wpf.Ui.Controls.FluentWindow
{
    private bool _dirty, _kept, _exitApproved;
    public ProjectViewModel Project { get; }
    public ProjectDeviceEditor Draft { get; }
    public string EditorTitle { get; }
    public string KeepCaption { get; }
    public ProjectDeviceDialog(ProjectViewModel project, ProjectDevice? device = null)
    {
        Project = project;
        Draft = new ProjectDeviceEditor(device ?? new ProjectDevice());
        EditorTitle = Loc.Get(device == null ? "Device.AddTitle" : "Device.EditTitle");
        KeepCaption = Loc.Get(device == null ? "Device.Add" : "Device.SaveChanges");
        InitializeComponent();
        // Fluent disclosures can consume a descendant's bring-into-view request.
        // Keep the form's outer viewport responsible for keyboard/validation focus.
        EditorViewport.AddHandler(RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(EditorBringIntoView), true);
        DataContext = Draft;
        PasswordFields.CopyPassword = project.CopyDevicePassword;
        Draft.PropertyChanged += DraftChanged;
        Loaded += (_, _) => DeviceNameInput.Focus();
        Deactivated += (_, _) => Draft.HidePassword();
        Closed += (_, _) => { Draft.HidePassword(); Draft.PropertyChanged -= DraftChanged; };
    }
    private void DraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProjectDeviceEditor.IsPasswordRevealed) or nameof(ProjectDeviceEditor.RevealCaption)
            or nameof(ProjectDeviceEditor.CanChoosePasswordStorage)) return;
        _dirty = true; _exitApproved = false;
    }
    private void Keep_Click(object sender, RoutedEventArgs e)
    {
        if (Project.TryStoreDevice(Draft)) { _kept = true; Close(); return; }
        EditorError.Text = Project.Error;
        if (string.IsNullOrWhiteSpace(Draft.Name)) { DeviceNameInput.Focus(); DeviceNameInput.BringIntoView(); return; }
        try { ProjectJson.ValidateUrl(Draft.WebUrl); }
        catch (ProjectValidationException) { DeviceUrlInput.Focus(); DeviceUrlInput.BringIntoView(); return; }
        if (Draft.SavePassword)
        {
            try { ProjectJson.ValidatePassword(Draft.Password); }
            catch (ProjectValidationException) { PasswordSection.IsExpanded = true; PasswordFields.FocusInput(); return; }
        }
        DetailsSection.IsExpanded = true; DeviceIpInput.Focus(); DeviceIpInput.BringIntoView();
    }
    private void Unlink_Click(object sender, RoutedEventArgs e) => Draft.ProfileId = "";
    private void EditorBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        if (e.TargetObject is not FrameworkElement field || field == EditorViewport || !field.IsDescendantOf(EditorViewport)) return;
        // Use arranged positions: a disclosure's transient render translation must
        // not make a field appear reachable before the expansion has finished.
        double top = 0;
        for (DependencyObject node = field; node != EditorViewport; node = VisualTreeHelper.GetParent(node))
            if (node is Visual visual) top += VisualTreeHelper.GetOffset(visual).Y;
        var bounds = new Rect(0, top, field.ActualWidth, field.ActualHeight);
        var delta = bounds.Top < 0 || bounds.Height > EditorViewport.ActualHeight ? bounds.Top
            : bounds.Bottom > EditorViewport.ActualHeight ? bounds.Bottom - EditorViewport.ActualHeight : 0;
        if (delta != 0) EditorViewport.ScrollToVerticalOffset(EditorViewport.VerticalOffset + delta);
        e.Handled = true;
    }
    private void Password_Collapsed(object sender, RoutedEventArgs e) => Draft.HidePassword();
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    public bool ConfirmApplicationExit()
    {
        _exitApproved = !_dirty || _kept || Project.ConfirmDiscardModalDraft();
        return _exitApproved;
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitApproved && !ConfirmApplicationExit()) e.Cancel = true;
        if (!e.Cancel) Draft.HidePassword();
        base.OnClosing(e);
    }
}
