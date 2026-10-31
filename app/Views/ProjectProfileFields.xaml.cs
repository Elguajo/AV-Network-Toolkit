using System.Windows.Controls;
using System.Windows;

namespace NetworkProfileManager.Views;

public partial class ProjectProfileFields : UserControl
{
    public ProjectProfileFields() => InitializeComponent();
    internal void FocusField(string property)
    {
        if (FindName(property + "Field") is not FrameworkElement field) return;
        field.BringIntoView(); field.Focus();
        if (field is System.Windows.Controls.TextBox text) text.SelectAll();
    }
}
