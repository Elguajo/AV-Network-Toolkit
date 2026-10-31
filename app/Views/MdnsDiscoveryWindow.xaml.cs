using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Views;

public partial class MdnsDiscoveryWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly MdnsDiscoveryViewModel _viewModel;
    private readonly DispatcherTimer _timer;
    // Tests provide all boundaries. Constructor never enumerates or opens a socket.
    public MdnsDiscoveryWindow(MdnsDiscoveryViewModel viewModel)
    {
        InitializeComponent(); DataContext = _viewModel = viewModel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => _viewModel.Poll();
        Loaded += (_, _) => _timer.Start();
    }
    internal static MdnsDiscoveryWindow Create()
    {
        var interfaces = new WindowsMdnsInterfaces();
        var export = new CsvDialog();
        var window = new MdnsDiscoveryWindow(new(interfaces, new MdnsDiscoverySession(interfaces, new WindowsMdnsReceiverFactory()), export));
        export.Owner = window;
        // Opening the window is read-only. Refresh lists NICs; Start stays deliberate.
        window.Loaded += async (_, _) => await window._viewModel.RefreshAsync();
        return window;
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        _timer.Stop(); _viewModel.Dispose(); base.OnClosing(e);
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private sealed class CsvDialog : IMdnsExport
    {
        public Window? Owner;
        public bool Save(string csv)
        {
            var dialog = new SaveFileDialog { Title = Loc.Get("Mdns.ExportWarning"), Filter = Loc.Get("Mdns.CsvFilter"),
                DefaultExt = ".csv", AddExtension = true, FileName = "mdns.csv", OverwritePrompt = true };
            if (dialog.ShowDialog(Owner) != true) return false;
            File.WriteAllText(dialog.FileName, csv, new UTF8Encoding(true)); return true;
        }
    }
}
