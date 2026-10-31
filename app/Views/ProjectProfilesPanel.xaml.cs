using System;
using System.Windows;
using System.Windows.Controls;
using NetworkProfileManager.Models;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ProjectProfilesPanel : UserControl
{
    public ProjectProfilesPanel() => InitializeComponent();
    public static readonly DependencyProperty TargetLabelProperty = DependencyProperty.Register(
        nameof(TargetLabel), typeof(string), typeof(ProjectProfilesPanel), new PropertyMetadata("Apply"));
    public string TargetLabel { get => (string)GetValue(TargetLabelProperty); set => SetValue(TargetLabelProperty, value); }
    public static readonly DependencyProperty TargetNameProperty = DependencyProperty.Register(
        nameof(TargetName), typeof(string), typeof(ProjectProfilesPanel), new PropertyMetadata(""));
    public string TargetName { get => (string)GetValue(TargetNameProperty); set => SetValue(TargetNameProperty, value); }
    public event Action<ProjectProfile>? ApplyRequested;
    private void Add_Click(object sender, RoutedEventArgs e) => Edit(null);
    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement button || button.ContextMenu == null) return;
        button.ContextMenu.DataContext = button.Tag;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }
    private void Edit_Click(object sender, RoutedEventArgs e) => Edit((sender as FrameworkElement)?.Tag as ProjectProfile);
    private void Edit(ProjectProfile? profile)
    {
        if (DataContext is ProjectViewModel vm)
            new ProjectProfileDialog(vm, profile) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProjectViewModel vm && (sender as FrameworkElement)?.Tag is ProjectProfile profile)
        {
            var frozen = vm.ProfileForApply(profile.Id);
            if (frozen != null) ApplyRequested?.Invoke(frozen);
        }
    }
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProjectViewModel vm || (sender as FrameworkElement)?.Tag is not ProjectProfile profile) return;
        vm.SelectedProfile = profile;
        if (vm.SelectedProfile?.Id == profile.Id && vm.DeleteProfileCommand.CanExecute(null)) vm.DeleteProfileCommand.Execute(null);
    }
}
