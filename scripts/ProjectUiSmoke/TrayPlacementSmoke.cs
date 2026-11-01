using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckTrayPlacement(string output)
    {
        var area = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        foreach (var theme in new[] { "Dark", "Light" })
        {
            ApplyRenderTheme(theme);
            foreach (var (name, anchor) in new[]
            {
                ("overflow", new System.Drawing.Point(area.Left + area.Width / 2, area.Bottom - 250)),
                ("right", new System.Drawing.Point(area.Right - 8, area.Bottom + 8)),
                ("left", new System.Drawing.Point(area.Left + 8, area.Bottom - 100))
            })
            {
                var closed = 0; var actions = 0;
                var window = new TrayPopupWindow(anchor) { OnBeforeClose = () => closed++ };
                window.Rebuild(null, () => actions++, () => actions++, () => actions++);
                window.Show(); Pump();
                try
                {
                    var fromDevice = PresentationSource.FromVisual(window)!.CompositionTarget!.TransformFromDevice;
                    var workArea = Rect.Transform(new Rect(area.X, area.Y, area.Width, area.Height), fromDevice);
                    var point = fromDevice.Transform(new Point(anchor.X, anchor.Y));
                    var expected = TrayPopupPlacement.Calculate(point, workArea, new(window.ActualWidth, window.ActualHeight));
                    var actual = fromDevice.Transform(window.PointToScreen(new Point(0, 0)));
                    if (Math.Abs(expected.X - actual.X) > 2 || Math.Abs(expected.Y - actual.Y) > 2)
                        throw new Exception($"Tray placement {name}: expected {expected}, actual {actual}.");
                    if (name == "overflow" && (Math.Abs(actual.X + window.ActualWidth / 2 - point.X) > 2 || actual.Y >= point.Y))
                        throw new Exception("Overflow menu was not centered above the icon anchor.");
                    Capture(window, Path.Combine(output, $"tray-{name}-{theme}.png"));
                    var buttons = Descendants(window).OfType<Button>().ToArray();
                    // Exercise all three callbacks across cases, including Exit.
                    buttons[name == "overflow" ? 2 : name == "right" ? 0 : 1]
                        .RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Pump();
                    if (actions != 1 || closed != 1 || window.IsVisible)
                        throw new Exception("Tray action or close callback changed during positioning.");
                }
                finally { window.Close(); }
            }
        }
    }
}
