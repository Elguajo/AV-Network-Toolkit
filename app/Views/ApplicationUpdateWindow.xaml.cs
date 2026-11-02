using System.ComponentModel;
using System.Windows;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class ApplicationUpdateWindow : Wpf.Ui.Controls.FluentWindow
{
    internal ApplicationUpdateWindow(ApplicationUpdateViewModel viewModel) { InitializeComponent(); DataContext = viewModel; }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is ApplicationUpdateViewModel vm && vm.IsBusy) { vm.Cancel(); e.Cancel = true; }
        base.OnClosing(e);
    }
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
