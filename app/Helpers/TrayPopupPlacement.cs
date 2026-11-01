using System;
using System.Windows;

namespace NetworkProfileManager.Helpers;

internal static class TrayPopupPlacement
{
    internal static Point Calculate(Point anchor, Rect workArea, Size popup)
    {
        const double gap = 4;
        var left = anchor.X - popup.Width / 2;
        var top = anchor.Y - popup.Height - gap;
        // Prefer above the clicked icon; use below when the top edge has no room.
        if (top < workArea.Top) top = anchor.Y + gap;
        return new Point(
            Math.Clamp(left, workArea.Left, Math.Max(workArea.Left, workArea.Right - popup.Width)),
            Math.Clamp(top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - popup.Height)));
    }
}
