using System;
using System.Windows.Input;
using System.Windows.Threading;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.ViewModels;
using Wpf.Ui.Controls;

namespace NetworkProfileManager.Views;

public partial class ApplyConfirmationContent : System.Windows.Controls.UserControl
{
    internal ApplyConfirmationContent(ApplyConfirmation review)
    { InitializeComponent(); DataContext = review; }

    internal static ContentDialog CreateDialog(ApplyConfirmation review)
    {
        var dialog = new ContentDialog
        {
            Title = review.Title,
            Content = new ApplyConfirmationContent(review),
            DialogMaxWidth = 600,
            PrimaryButtonText = Loc.Get("Common.Apply"),
            CloseButtonText = Loc.Get("Common.Cancel"),
            DefaultButton = ContentDialogButton.Close
        };
        // The close action is last in the existing dialog's traversal order.
        // Set focus after the template loads; DefaultButton alone does not do so.
        dialog.Loaded += (_, _) => dialog.Dispatcher.BeginInvoke(DispatcherPriority.Input,
            new Action(() => dialog.MoveFocus(new TraversalRequest(FocusNavigationDirection.Last))));
        return dialog;
    }
}
