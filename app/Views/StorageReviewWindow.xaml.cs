using System.Windows;
using System.Windows.Automation;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Views;

public partial class StorageReviewWindow : Wpf.Ui.Controls.FluentWindow
{
    internal StorageReviewWindow(string heading, string message, string action)
    {
        InitializeComponent();
        Title = DialogTitle.Title = Loc.Get("Storage.Title");
        Heading.Text = heading; ReviewText.Text = message;
        AcceptButton.Content = action; ExitButton.Content = Loc.Get("Storage.Exit");
        AutomationProperties.SetName(AcceptButton, action);
        AutomationProperties.SetName(ExitButton, Loc.Get("Storage.Exit"));
        // Deliberate confirmation: Enter does not bypass review immediately on opening.
        Loaded += (_, _) => ExitButton.Focus();
    }
    private void Accept_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Exit_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
