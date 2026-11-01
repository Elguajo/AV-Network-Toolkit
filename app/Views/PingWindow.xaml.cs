using System;
using System.Windows;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class PingWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly AdapterDiagnosticsViewModel _viewModel;
    public PingWindow(AdapterDiagnosticsViewModel viewModel)
    {
        InitializeComponent(); DataContext = _viewModel = viewModel;
        _viewModel.ProbeCount = 4;
        Loaded += async (_, _) => await _viewModel.RefreshAsync();
        Closed += (_, _) => _viewModel.Dispose();
    }
    // Retained destination constructor; opening only reads facts, never sends traffic.
    public PingWindow(string host) : this(AdapterDiagnosticsViewModel.Create()) => _viewModel.Target = host;
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
