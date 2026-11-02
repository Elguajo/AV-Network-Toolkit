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
    private bool _sync;
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
        Sync();
    }
    private void EditorChanged(object? sender, PropertyChangedEventArgs e) => Sync();
    private void Sync()
    {
        _sync = true;
        try
        {
            var value = _editor?.SavePassword == true ? _editor.Password : "";
            if (DevicePasswordInput.Password != value) DevicePasswordInput.Password = value;
            bool reveal = _editor?.IsPasswordRevealed == true;
            DeviceRevealedPassword.Text = reveal ? value : "";
            DeviceRevealedPassword.Visibility = reveal ? Visibility.Visible : Visibility.Collapsed;
        }
        finally { _sync = false; }
    }
    private void Password_Changed(object sender, RoutedEventArgs e)
    {
        if (!_sync && _editor?.SavePassword == true) _editor.Password = DevicePasswordInput.Password;
    }
}
