using System.Windows;

namespace NetworkProfileManager.Views;

public partial class ProjectDiscardDialog : Wpf.Ui.Controls.FluentWindow
{
    internal ProjectDiscardDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => CancelButton.Focus();
    }

    private void Discard_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
