using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;
using NetworkProfileManager.Views;
using Newtonsoft.Json;

internal static partial class Program
{
    private static void PreparePackageStorage(string output)
    {
        // Never run on a workstation: all profile-root fixture writes are hosted-runner-only.
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true"
            || Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") != "github-hosted"
            || Environment.GetEnvironmentVariable("ImageOS")?.StartsWith("win", StringComparison.OrdinalIgnoreCase) != true)
            throw new InvalidOperationException("Storage package fixture is restricted to a disposable Windows runner.");
        var exeFixture = Path.Combine(output, "exe-fixture"); Directory.CreateDirectory(exeFixture);
        var locations = UserStorageLocations.ForUser(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), exeFixture);
        if (Directory.Exists(locations.Home) || Directory.Exists(locations.Backups)) throw new IOException("Refusing existing storage roots.");
        var source = Path.Combine(locations.Legacy, "data", "package-acceptance-sentinel.json");
        var existing = Directory.GetFiles(locations.Legacy, "*", SearchOption.AllDirectories);
        if (existing.Length != 1 || existing[0] != source) throw new IOException("Only the runner-owned source fixture is permitted.");
        var context = new UserStorage(locations).Open(_ => true);
        var homeSentinel = Path.Combine(context.Data, "package-home-sentinel.fixture");
        var recoverySentinel = Path.Combine(context.Recovery, "package-recovery-sentinel.fixture");
        File.WriteAllText(homeSentinel, "retain home fixture"); File.WriteAllText(recoverySentinel, "retain recovery fixture");
        var snapshot = Path.Combine(locations.Backups, UserStorage.ReadRecord(context.Root).Snapshot, "local/data/package-acceptance-sentinel.json");
        var files = new[] { source, homeSentinel, recoverySentinel, snapshot }.Select(path => new { Path = path, Hash = StorageSafety.HashFile(path) });
        File.WriteAllText(Path.Combine(output, "retention-files.json"), JsonConvert.SerializeObject(files, Formatting.Indented));
        Console.WriteLine("PASS: isolated runner legacy-to-home migration, protected backup and recovery/home retention fixtures prepared.");
    }
    private static void CheckUserStorage(string output)
    {
        foreach (var theme in new[] { "Dark", "Light" })
        foreach (var minimum in new[] { false, true })
        foreach (var state in new[] { "copy", "diverged", "error" })
        {
            ApplyRenderTheme(theme);
            var review = new StorageReview(@"C:\FixtureUser\AppData\Local\NetworkProfileManager",
                @"C:\FixtureUser\.av-network-toolkit", @"C:\FixtureUser\.av-network-toolkit-backups\0123456789abcdef0123456789abcdef",
                38, 1234567, state == "diverged", 2);
            var text = state == "error" ? StorageStartup.FailureMessage(new IOException("Storage.RootConflict"),
                new(review.Destination, review.Source, @"D:\FixtureExe", review.Backup)) : StorageStartup.ReviewMessage(review);
            var action = Loc.Get(state == "error" ? "Common.Retry" : state == "diverged" ? "Storage.KeepHome" : "Storage.Copy");
            var window = new StorageReviewWindow(Loc.Get("Storage.Title"), text, action);
            if (minimum) { window.Width = window.MinWidth; window.Height = window.MinHeight; }
            Exception? error = null;
            window.Loaded += (_, _) => window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                try
                {
                    Pump();
                    var accept = (Wpf.Ui.Controls.Button)window.FindName("AcceptButton");
                    var exit = (Wpf.Ui.Controls.Button)window.FindName("ExitButton");
                    if (Keyboard.FocusedElement != exit) throw new Exception("Initial focus must remain on Exit.");
                    if (AutomationProperties.GetName(accept) != action || AutomationProperties.GetName(exit) != Loc.Get("Storage.Exit"))
                        throw new Exception("Storage actions have no matching accessible name.");
                    foreach (var button in new[] { accept, exit })
                    {
                        var location = button.TranslatePoint(new Point(), window);
                        if (location.Y < 0 || location.Y + button.ActualHeight > window.ActualHeight
                            || location.X < 0 || location.X + button.ActualWidth > window.ActualWidth)
                            throw new Exception("Storage footer action was clipped.");
                    }
                    Capture(window, Path.Combine(output, $"storage-{state}-{theme}-{(minimum ? "minimum" : "normal")}.png"));
                    // Exercise real modal handlers, including Cancel: no filesystem or App.OnStartup invoked.
                    (state == "copy" ? accept : exit).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                }
                catch (Exception e) { error = e; window.Close(); }
            }));
            bool? accepted = window.ShowDialog(); Pump();
            if (error != null) throw error;
            if (accepted != (state == "copy")) throw new Exception("Storage review/cancel result changed.");
        }
    }
}
