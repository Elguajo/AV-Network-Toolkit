using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

namespace NetworkProfileManager;

public partial class App
{
    private ApplicationUpdateViewModel? _updates;
    private ApplicationUpdateTransport? _updateTransport;
    private System.Windows.Threading.DispatcherTimer? _updateTimer;
    private void InitializeApplicationUpdates()
    {
        _updateTransport = new();
        var key = ApplicationUpdateTrust.PublicKey;
        bool installed = false;
        try { installed = Environment.ProcessPath != null && ApplicationUpdateService.IsInstalled(Environment.ProcessPath); }
        catch { /* Unknown install mode must not launch an installer against a guessed directory. */ }
        _updates = new(new(_updateTransport, key ?? "", ApplicationUpdateService.CacheRoot), new(), key, installed,
            () => { var dialog = new OpenFolderDialog { Title = Loc.Get("Updates.ChooseFolder"), Multiselect = false }; return dialog.ShowDialog() == true ? dialog.FolderName : null; },
            PrepareUpdateExitAsync, ExitApplication,
            message => MessageBox.Show(message, Loc.Get("Updates.Title"), MessageBoxButton.OKCancel,
                MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.OK);
        _updates.UpdateAvailable += () => NotificationService.Info(Loc.Get("Updates.Available"), Loc.Get("Updates.Title"));
        _ = _updates.CheckAutomaticallyAsync(DateTimeOffset.UtcNow);
        _updateTimer = new() { Interval = TimeSpan.FromHours(1) };
        _updateTimer.Tick += async (_, _) => { if (_updates != null) await _updates.CheckAutomaticallyAsync(DateTimeOffset.UtcNow); };
        _updateTimer.Start();
    }
    public static void ShowApplicationUpdates()
    {
        if (Current is not App app || app._updates == null) return;
        var existing = app.Windows.OfType<ApplicationUpdateWindow>().FirstOrDefault();
        if (existing != null) { existing.Activate(); return; }
        var window = new ApplicationUpdateWindow(app._updates);
        if (app._mainWindow?.IsVisible == true) window.Owner = app._mainWindow;
        window.ShowDialog();
    }
    private bool ConfirmApplicationExit(bool approveClose = true)
    {
        foreach (var apply in Windows.OfType<ProjectApplyWindow>().ToArray()) if (!apply.ConfirmApplicationExit()) return false;
        foreach (var project in Windows.OfType<ProjectWindow>().ToArray()) if (!project.ConfirmApplicationExit(approveClose)) return false;
        return _mainWindow == null || _mainWindow.ConfirmApplicationExit();
    }
    private async Task<bool> PrepareUpdateExitAsync(bool installing)
    {
        if (!ConfirmApplicationExit(false)) return false;
        // Scanner has legacy cancellation/lifecycle semantics: require it to be stopped explicitly.
        if (Windows.OfType<IpScannerWindow>().Any(w => w.DataContext is IpScannerViewModel vm && (vm.IsScanning || vm.IsMonitoring))) return false;
        foreach (var window in Windows.OfType<MdnsDiscoveryWindow>().ToArray())
            if (window.DataContext is MdnsDiscoveryViewModel mdns) await mdns.StopAsync();
        if (_mainWindow != null && !await _mainWindow.StopTestsForUpdateAsync()) return false;
        return true;
    }
    private void ExitApplication()
    {
        IsExiting = true;
        foreach (var project in Windows.OfType<ProjectWindow>().ToArray()) project.ApproveApplicationClose();
        if (_trayIcon != null) _trayIcon.Visible = false;
        Shutdown();
    }
}
