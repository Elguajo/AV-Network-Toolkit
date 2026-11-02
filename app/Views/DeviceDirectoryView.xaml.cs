using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class DeviceDirectoryView : UserControl
{
    private ProjectViewModel? _vm;
    private ProjectDeviceEditor? _editor;
    private Window? _window;
    public DeviceDirectoryView()
    {
        InitializeComponent();
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
        DataContextChanged += (_, _) => { Detach(); if (IsLoaded) Attach(); };
        IsVisibleChanged += (_, _) => { if (!IsVisible) _editor?.HidePassword(); };
    }
    private void Attach()
    {
        if (_vm != null) return;
        _vm = DataContext as ProjectViewModel;
        if (_vm == null) return;
        _vm.PropertyChanged += VmChanged;
        _window = Window.GetWindow(this);
        if (_window != null) _window.Deactivated += Deactivated;
        AttachEditor();
    }
    private void Detach()
    {
        if (_vm != null) _vm.PropertyChanged -= VmChanged;
        if (_window != null) _window.Deactivated -= Deactivated;
        if (_editor != null) { _editor.HidePassword(); _editor.PropertyChanged -= EditorChanged; }
        _vm = null; _editor = null; _window = null;
        Sync();
    }
    private void Deactivated(object? sender, EventArgs e) => _editor?.HidePassword();
    private void VmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(ProjectViewModel.DeviceEditor)) AttachEditor();
    }
    private void AttachEditor()
    {
        if (_editor != null) { _editor.HidePassword(); _editor.PropertyChanged -= EditorChanged; }
        _editor = _vm?.DeviceEditor;
        if (_editor != null) _editor.PropertyChanged += EditorChanged;
        DeviceDetailsViewport.ScrollToTop();
        Sync();
    }
    private void EditorChanged(object? sender, PropertyChangedEventArgs e) => Sync();
    private void Sync()
    {
        bool reveal = _editor?.IsPasswordRevealed == true;
        DeviceRevealedPassword.Text = reveal ? _editor!.Password : "";
        DeviceRevealedPassword.Visibility = reveal ? Visibility.Visible : Visibility.Collapsed;
    }
    private void New_Click(object sender, RoutedEventArgs e) => Edit(false);
    private void Edit_Click(object sender, RoutedEventArgs e) => Edit(true);
    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement button || button.ContextMenu == null) return;
        button.ContextMenu.DataContext = DataContext;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }
    private void Edit(bool existing)
    {
        if (DataContext is not ProjectViewModel vm || (existing && vm.SelectedDevice == null)) return;
        new ProjectDeviceDialog(vm, existing ? vm.SelectedDevice : null) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
