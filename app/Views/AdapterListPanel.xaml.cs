using System.Windows.Controls;

namespace NetworkProfileManager.Views
{
    public partial class AdapterListPanel : UserControl
    {
        public AdapterListPanel() => InitializeComponent();

        private void Options_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is not System.Windows.FrameworkElement button || button.ContextMenu == null) return;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }
    }
}
