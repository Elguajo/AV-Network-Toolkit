using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

internal static partial class Program
{
    private static void ApplyRenderTheme(string theme)
    {
        // Swapping only ThemesDictionary leaves WPF-UI's cached theme unchanged.
        // A subsequently opened Mica window then uses a different native theme.
        ApplicationThemeManager.Apply(theme == "Dark" ? ApplicationTheme.Dark : ApplicationTheme.Light,
            WindowBackdropType.Mica, updateAccent: false);
        NetworkProfileManager.App.RefreshWindowThemes();
    }

    private static void RenderWindowThemes(string output)
    {
        AppPaths.OverrideDataDirectory(System.IO.Path.Combine(output, "theme-fixture"));
        var windows = new List<Window>();
        try
        {
            ApplyRenderTheme("Dark");
            var root = new FluentWindow { Width = 700, Height = 540, ExtendsContentIntoTitleBar = true,
                WindowBackdropType = WindowBackdropType.Mica };
            windows.Add(root); root.Show();
            var project = new ProjectWindow { Owner = root, Width = 700, Height = 540 };
            windows.Add(project); project.Show();
            var nested = new ThemeSettingsWindow { Owner = project };
            windows.Add(nested); nested.Show(); Pump();
            // Exercise the library's opaque fallback after removing a native effect.
            WindowBackdrop.RemoveBackdrop(nested);
            foreach (var theme in new[] { "Light", "Dark", "Light" })
            {
                ApplyRenderTheme(theme); Pump();
                foreach (var window in windows) AssertWindowTheme(window, theme);
                Capture(project, System.IO.Path.Combine(output, $"project-theme-{theme}.png"));
                Capture(nested, System.IO.Path.Combine(output, $"nested-theme-{theme}.png"));
                ((NetworkProfileManager.ViewModels.ProjectViewModel)project.DataContext).Section = 1;
                Pump();
                Capture(project, System.IO.Path.Combine(output, $"devices-theme-{theme}.png"));
                project.Width = 900; project.Height = 780; Pump();
                Capture(project, System.IO.Path.Combine(output, $"devices-normal-theme-{theme}.png"));
                project.Width = 700; project.Height = 540;
                ((NetworkProfileManager.ViewModels.ProjectViewModel)project.DataContext).Section = 0;
                Pump();
                var fresh = new ProjectWindow { Owner = project, Width = 700, Height = 540 };
                windows.Add(fresh); fresh.Show(); Pump();
                AssertWindowTheme(fresh, theme);
                fresh.Close(); windows.Remove(fresh);
                // Repeat the fallback before the next transition in either direction.
                WindowBackdrop.RemoveBackdrop(nested);
            }
        }
        finally
        {
            foreach (var window in windows.AsEnumerable().Reverse()) window.Close();
            AppPaths.OverrideDataDirectory(null);
        }
    }

    private static void AssertWindowTheme(Window window, string theme)
    {
        var light = theme == "Light";
        var foreground = ((SolidColorBrush)window.Foreground).Color;
        if ((foreground.R < 128) != light) throw new Exception($"{window.GetType().Name}: wrong text theme.");
        var background = ((SolidColorBrush)window.Background).Color;
        if (background.A != 0 && (background.R > 128) != light)
            throw new Exception($"{window.GetType().Name}: stale opaque background after theme change.");
        // Native Mica is outside RenderTargetBitmap: check the actual HWND as well.
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            var result = DwmGetWindowAttribute(new WindowInteropHelper(window).Handle, 20, out var dark, sizeof(int));
            if (result != 0 || (dark != 0) == light)
                throw new Exception($"{window.GetType().Name}: native backdrop disagrees with text ({result}, {dark}).");
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);
}
