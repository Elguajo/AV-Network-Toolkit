using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views
{
    public partial class ProfilePanel : UserControl
    {
        public ProfilePanel() => InitializeComponent();

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is MainViewModel vm)) return;
            if (vm.SelectedAdapter == null) return;

            var target = vm.SelectedAdapter;
            var dialog = new AddProfileDialog
            {
                Owner = Window.GetWindow(this), Commit = profile => vm.TryAddProfile(profile, target)
            };
            dialog.ShowDialog();
        }

        // The "…" button opens its menu on a normal click, under the button.
        private void More_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe) || fe.ContextMenu == null) return;
            fe.ContextMenu.PlacementTarget = fe;
            fe.ContextMenu.Placement = PlacementMode.Bottom;
            fe.ContextMenu.IsOpen = true;
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe && fe.Tag is ProfileViewModel pvm)) return;
            if (!(DataContext is MainViewModel vm) || vm.SelectedAdapter == null) return;

            var target = vm.SelectedAdapter;
            var dialog = new AddProfileDialog(pvm.Model)
            {
                Owner = Window.GetWindow(this), Commit = profile => vm.TryUpdateProfile(pvm, profile, target)
            };
            dialog.ShowDialog();
        }

        private void ApplyAgain_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ProfileViewModel profile } && DataContext is MainViewModel vm
                && vm.ApplyProfileCommand.CanExecute(profile)) vm.ApplyProfileCommand.Execute(profile);
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe && fe.Tag is ProfileViewModel pvm)) return;
            if (DataContext is MainViewModel vm && vm.DeleteProfileCommand.CanExecute(pvm))
                vm.DeleteProfileCommand.Execute(pvm);
        }
    }
}
