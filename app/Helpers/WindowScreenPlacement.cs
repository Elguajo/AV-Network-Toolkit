using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;

namespace NetworkProfileManager.Helpers;

internal static class WindowScreenPlacement
{
    internal static Screen SelectMonitor(string name) => Screen.AllScreens.FirstOrDefault(s => s.DeviceName == name)
        ?? Screen.PrimaryScreen ?? Screen.AllScreens[0];
    internal static Rect WorkArea(Screen screen) => new(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);
    internal static double Dpi(Screen screen)
    {
        var monitor = MonitorFromPoint(new NativePoint { X = screen.Bounds.Left, Y = screen.Bounds.Top }, 2);
        return GetDpiForMonitor(monitor, 0, out var x, out _) == 0 && x != 0 ? x : 96;
    }
    internal static Rect WindowPixels(Window window)
    {
        if (!GetWindowRect(new WindowInteropHelper(window).Handle, out var rect)) return Rect.Empty;
        return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
    internal static double WindowDpi(Window window) => GetDpiForWindow(new WindowInteropHelper(window).Handle);
    internal static Screen CurrentMonitor(Window window) => Screen.FromHandle(new WindowInteropHelper(window).Handle);
    internal static bool Move(Window window, Rect pixels) => SetWindowPos(new WindowInteropHelper(window).Handle, IntPtr.Zero,
        (int)Math.Round(pixels.X), (int)Math.Round(pixels.Y), (int)Math.Round(pixels.Width), (int)Math.Round(pixels.Height), 0x0014);
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int kind, out uint x, out uint y);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
