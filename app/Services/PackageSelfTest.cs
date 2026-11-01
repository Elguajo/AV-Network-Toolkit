using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetworkProfileManager.Services;

// Explicit packaging diagnostic only. Never enter normal startup, stores or OS/network services.
internal static class PackageSelfTest
{
    internal const string Switch = "--package-self-test";

    internal static int Run(string[] arguments)
    {
        if (arguments.Length != 2 || !Path.IsPathFullyQualified(arguments[1])) return 2;
        bool success = false;
        string? error = null;
        try
        {
            _ = ProjectJson.Parse("""{"schemaVersion":1,"project":{"id":"self-test","name":"Package self-test"},"profiles":[],"devices":[]}""");
            var title = Application.Current.FindResource("App.Title") as string;
            if (title != "AV Network Toolkit") throw new InvalidDataException();
            var card = new Wpf.Ui.Controls.Card { Content = new TextBlock { Text = title } };
            card.Measure(new Size(320, 100));
            card.Arrange(new Rect(0, 0, 320, 100));
            var bitmap = new RenderTargetBitmap(320, 100, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(card); // Forces WPF rendering/native dependencies, without showing a window.
            success = true;
        }
        catch (Exception exception) { error = exception.GetType().Name; }
        try
        {
            // Do not overwrite an existing file, create parent paths, or expose exception input.
            using var output = new FileStream(arguments[1], FileMode.CreateNew, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(output, new
            {
                Success = success,
                Version = typeof(PackageSelfTest).Assembly.GetName().Version?.ToString(),
                InformationalVersion = typeof(PackageSelfTest).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                Runtime = RuntimeInformation.FrameworkDescription,
                JsonValidated = success,
                WpfRendered = success,
                ErrorType = error
            });
        }
        catch { return 2; }
        return success ? 0 : 1;
    }
}
