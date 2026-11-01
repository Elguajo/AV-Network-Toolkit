using System;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace NetworkProfileManager.Helpers
{
    /// <summary>
    /// Static facade over WPF-UI's ISnackbarService.
    /// Call Initialize() once from MainWindow.OnContentRendered.
    /// </summary>
    public static class NotificationService
    {
        private static ISnackbarService? _snackbar;

        /// <summary>
        /// Optional tray balloon fallback. Set by App when tray icon is created.
        /// Invoked when the main window is not visible.
        /// </summary>
        public static Action<string, string>? TrayFallback { get; set; }

        public static void Initialize(ISnackbarService snackbar) => _snackbar = snackbar;

        public static void Success(string message, string? title = null)
            => Show(title ?? Loc.Get("Notify.Success", "Başarılı"), message, ControlAppearance.Success,
                   SymbolRegular.CheckmarkCircle20);

        public static void Info(string message, string? title = null)
            => Show(title ?? Loc.Get("Notify.Info", "Bilgi"), message, ControlAppearance.Info,
                   SymbolRegular.Info20);

        public static void Warning(string message, string? title = null)
            => Show(title ?? Loc.Get("Notify.Warning", "Uyarı"), message, ControlAppearance.Caution,
                   SymbolRegular.Warning20);

        public static void Error(string message, string? title = null)
            => Show(title ?? Loc.Get("Notify.Error", "Hata"), message, ControlAppearance.Danger,
                   SymbolRegular.DismissCircle20);

        private static void Show(string title, string message,
                                  ControlAppearance appearance, SymbolRegular icon)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current?.MainWindow;
                    if (mainWindow != null && !mainWindow.IsVisible)
                    {
                        // Main window is hidden — show a tray balloon instead
                        TrayFallback?.Invoke(title, message);
                        return;
                    }
                    _snackbar?.Show(title, message, appearance, new SymbolIcon(icon),
                                    TimeSpan.FromSeconds(4));
                });
            }
            catch { /* never crash on notification */ }
        }
    }
}
