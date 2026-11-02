using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class DevicePasswordFields : UserControl
{
    private ProjectDeviceEditor? _editor;
    private bool _sync;
    public Func<string, string>? CopyPassword { get; set; }
    public DevicePasswordFields()
    {
        InitializeComponent();
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
        DataContextChanged += (_, _) => { Detach(); if (IsLoaded) Attach(); };
    }
    private void Attach()
    {
        _editor = DataContext as ProjectDeviceEditor;
        if (_editor != null) _editor.PropertyChanged += EditorChanged;
        Sync();
    }
    private void Detach()
    {
        if (_editor != null) { _editor.HidePassword(); _editor.PropertyChanged -= EditorChanged; }
        _editor = null;
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
            DeviceRevealedPassword.Text = _editor?.IsPasswordRevealed == true ? value : "";
            DeviceRevealedPassword.Visibility = _editor?.IsPasswordRevealed == true ? Visibility.Visible : Visibility.Collapsed;
        }
        finally { _sync = false; }
    }
    public void FocusInput() { DevicePasswordInput.Focus(); DevicePasswordInput.BringIntoView(); }
    private void Password_Changed(object sender, RoutedEventArgs e)
    {
        if (!_sync && _editor?.SavePassword == true) _editor.Password = DevicePasswordInput.Password;
    }
    private void Reveal_Click(object sender, RoutedEventArgs e)
    {
        try { _editor?.TogglePassword(); PasswordFeedback.Text = ""; }
        catch (ProjectValidationException) { PasswordFeedback.Text = Loc.Get("Project.ErrorPassword"); }
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (_editor?.SavePassword != true) return;
        try { PasswordFeedback.Text = CopyPassword?.Invoke(_editor.Password) ?? Loc.Get("Password.CopyFailed"); }
        catch (ProjectValidationException) { PasswordFeedback.Text = Loc.Get("Project.ErrorPassword"); }
    }
    private void Remove_Click(object sender, RoutedEventArgs e) { _editor?.RemovePassword(); PasswordFeedback.Text = ""; }
}
