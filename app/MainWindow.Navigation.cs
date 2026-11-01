using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NetworkProfileManager.Commands;

namespace NetworkProfileManager
{
    public partial class MainWindow
    {
        // View-only navigation state; optional layout persistence is handled separately.
        private const double SidebarMinimum = 200;
        private const double SidebarMaximum = 420;
        private const double WorkspaceMinimum = 500;
        private const double SplitterWidth = 6;
        private double _preferredSidebarWidth = 240;

        public static readonly DependencyProperty IsSidebarExpandedProperty = DependencyProperty.Register(
            nameof(IsSidebarExpanded), typeof(bool), typeof(MainWindow),
            new PropertyMetadata(true, (owner, _) => ((MainWindow)owner).UpdateSidebarLayout()));

        public bool IsSidebarExpanded
        {
            get => (bool)GetValue(IsSidebarExpandedProperty);
            set => SetValue(IsSidebarExpandedProperty, value);
        }

        private void InitializeNavigation()
        {
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => IsSidebarExpanded = !IsSidebarExpanded),
                Key.B, ModifierKeys.Control));
            Loaded += (_, _) => UpdateSidebarLayout();
        }

        private void RevealAdaptersClick(object sender, RoutedEventArgs e) => IsSidebarExpanded = true;

        private void NavigationSizeChanged(object sender, SizeChangedEventArgs e) => UpdateSidebarLayout();

        private void UpdateSidebarLayout()
        {
            // SizeChanged can be raised while InitializeComponent is still constructing the tree.
            if (AdapterSidebar == null || AdapterSplitter == null || NavigationLayout.ActualWidth <= 0) return;
            if (!IsSidebarExpanded)
            {
                if (AdapterSplitter.IsDragging) AdapterSplitter.CancelDrag();
                if (AdapterSidebar.IsKeyboardFocusWithin || AdapterSplitter.IsKeyboardFocusWithin) SidebarToggle.Focus();
                AdapterSidebar.Visibility = Visibility.Collapsed;
                AdapterSplitter.Visibility = Visibility.Collapsed;
                SidebarColumn.MinWidth = 0;
                SidebarColumn.Width = new GridLength(0);
                SplitterColumn.Width = new GridLength(0);
                return;
            }

            var available = NavigationLayout.ActualWidth - RailColumn.ActualWidth - SplitterWidth - WorkspaceMinimum;
            var maximum = Math.Max(SidebarMinimum, Math.Min(SidebarMaximum, available));
            SidebarColumn.MaxWidth = maximum;
            SidebarColumn.MinWidth = SidebarMinimum;
            SidebarColumn.Width = new GridLength(Math.Min(_preferredSidebarWidth, maximum));
            SplitterColumn.Width = new GridLength(SplitterWidth);
            AdapterSidebar.Visibility = Visibility.Visible;
            AdapterSplitter.Visibility = Visibility.Visible;
        }

        private void SidebarResizeCompleted(object sender, DragCompletedEventArgs e)
        {
            if (!e.Canceled) RememberSidebarWidth();
        }

        private void SidebarResizeKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right) RememberSidebarWidth();
        }

        private void RememberSidebarWidth()
        {
            if (IsSidebarExpanded) _preferredSidebarWidth = Math.Clamp(SidebarColumn.Width.Value, SidebarMinimum, SidebarMaximum);
        }
    }
}
