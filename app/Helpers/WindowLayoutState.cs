using System;
using System.Windows;

namespace NetworkProfileManager.Helpers;

internal sealed record WindowLayoutState(int Version, string Monitor, double OffsetX, double OffsetY,
    double Width, double Height, bool Maximized, double SidebarWidth, bool SidebarExpanded)
{
    internal bool IsValid => Version == 1 && Monitor != null && Monitor.Length <= 128
        && Finite(OffsetX, -100000, 100000) && Finite(OffsetY, -100000, 100000)
        && Finite(Width, 760, 10000) && Finite(Height, 520, 10000) && Finite(SidebarWidth, 200, 420);
    private static bool Finite(double value, double min, double max) => double.IsFinite(value) && value >= min && value <= max;

    // Sizes/offsets are DIPs relative to one monitor's work area; positions below are physical pixels.
    internal Rect Place(Rect workAreaPixels, double dpi)
    {
        var scale = dpi / 96;
        if (!IsValid || !double.IsFinite(scale) || scale < 0.5 || scale > 8 || workAreaPixels.IsEmpty)
            throw new ArgumentException("Invalid window placement.");
        var width = Math.Clamp(Width, 760, Math.Max(760, workAreaPixels.Width / scale)) * scale;
        var height = Math.Clamp(Height, 520, Math.Max(520, workAreaPixels.Height / scale)) * scale;
        return new Rect(Math.Clamp(workAreaPixels.Left + OffsetX * scale, workAreaPixels.Left, Math.Max(workAreaPixels.Left, workAreaPixels.Right - width)),
            Math.Clamp(workAreaPixels.Top + OffsetY * scale, workAreaPixels.Top, Math.Max(workAreaPixels.Top, workAreaPixels.Bottom - height)), width, height);
    }
}
