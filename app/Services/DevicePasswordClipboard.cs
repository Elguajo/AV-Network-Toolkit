using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace NetworkProfileManager.Services;

internal interface IPasswordClipboardBackend : IDisposable
{
    uint Write(string text, string marker);
    bool ClearIfMatches(string text, string marker, uint sequence);
}

// One process-owned lifetime, independent of device view/window lifetime.
internal sealed class DevicePasswordClipboard : IDisposable
{
    private sealed record Lease(string Text, string Marker, uint Sequence)
    {
        public override string ToString() => "Clipboard lease [redacted]";
    }
    private readonly IPasswordClipboardBackend _backend;
    private readonly Func<TimeSpan, Action, IDisposable> _schedule;
    private IDisposable? _timer;
    private Lease? _lease;
    private bool _disposed;
    private static DevicePasswordClipboard? _shared;
    internal static DevicePasswordClipboard Shared
    {
        get
        {
            if (_shared == null)
            {
                _shared = new(new WindowsPasswordClipboard(), Schedule);
                if (Application.Current != null) Application.Current.Exit += (_, _) => _shared.Dispose();
            }
            return _shared;
        }
    }
    internal DevicePasswordClipboard(IPasswordClipboardBackend backend, Func<TimeSpan, Action, IDisposable> schedule)
        => (_backend, _schedule) = (backend, schedule);

    public bool Copy(string value)
    {
        if (_disposed) return false;
        ProjectJson.ValidatePassword(value);
        // A failed replacement must not abandon cleanup of our previous copy.
        Lease? replacement = null;
        try
        {
            var marker = Guid.NewGuid().ToString("N");
            var sequence = _backend.Write(value, marker);
            if (sequence == 0) return false;
            _timer?.Dispose();
            var lease = replacement = new Lease(value, marker, sequence);
            _lease = lease;
            _timer = _schedule(TimeSpan.FromSeconds(30), () =>
            {
                if (ReferenceEquals(_lease, lease)) Clear();
            });
            return true;
        }
        catch
        {
            if (replacement != null && ReferenceEquals(_lease, replacement)) Clear();
            return false; // Clipboard providers may throw value-bearing errors.
        }
    }
    private void Clear(bool release = false)
    {
        _timer?.Dispose(); _timer = null;
        var lease = _lease;
        if (lease == null) return;
        try
        {
            if (_backend.ClearIfMatches(lease.Text, lease.Marker, lease.Sequence)) _lease = null;
        }
        catch { /* Best effort; never clear another application's clipboard. */ }
        finally { if (release) _lease = null; }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Clear(release: true);
        _backend.Dispose();
    }
    private static IDisposable Schedule(TimeSpan delay, Action action) => new DispatcherExpiry(delay, action);
    private sealed class DispatcherExpiry : IDisposable
    {
        private readonly DispatcherTimer _timer;
        public DispatcherExpiry(TimeSpan delay, Action action)
        {
            _timer = new DispatcherTimer { Interval = delay };
            _timer.Tick += (_, _) => { _timer.Stop(); action(); };
            _timer.Start();
        }
        public void Dispose() => _timer.Stop();
    }
}

// Native lock covers check AND clear, preventing a clipboard TOCTOU erase.
// All formats are rendered immediately; no delayed OLE rendering/serialization.
internal sealed class WindowsPasswordClipboard : IPasswordClipboardBackend
{
    private HwndSource? _owner;
    private uint _format;
    public uint Write(string text, string marker)
    {
        _owner ??= new HwndSource(new HwndSourceParameters("AVNetworkToolkit.Clipboard")
        { ParentWindow = new IntPtr(-3), WindowStyle = 0, Width = 0, Height = 0 });
        if (_format == 0) _format = RegisterClipboardFormat("AVNetworkToolkit.PasswordCopy.v1");
        if (_format == 0) throw new InvalidOperationException();
        IntPtr textMemory = IntPtr.Zero, markerMemory = IntPtr.Zero;
        try
        {
            textMemory = Allocate(text); markerMemory = Allocate(marker);
            if (!OpenClipboard(_owner.Handle)) throw new InvalidOperationException();
            try
            {
                if (!EmptyClipboard()) throw new InvalidOperationException();
                if (SetClipboardData(13, textMemory) == IntPtr.Zero) throw new InvalidOperationException();
                textMemory = IntPtr.Zero; // ownership transferred to Windows
                if (SetClipboardData(_format, markerMemory) == IntPtr.Zero)
                {
                    EmptyClipboard(); // still under our exclusive lock
                    throw new InvalidOperationException();
                }
                markerMemory = IntPtr.Zero;
                var sequence = GetClipboardSequenceNumber();
                if (sequence == 0) EmptyClipboard(); // cannot safely track this copy
                return sequence;
            }
            finally { CloseClipboard(); }
        }
        finally
        {
            if (textMemory != IntPtr.Zero) GlobalFree(textMemory);
            if (markerMemory != IntPtr.Zero) GlobalFree(markerMemory);
        }
    }
    public bool ClearIfMatches(string text, string marker, uint sequence)
    {
        if (_owner == null || _format == 0 || !OpenClipboard(_owner.Handle)) return false;
        try
        {
            return sequence != 0 && GetClipboardSequenceNumber() == sequence
                && Read(_format, 128) == marker && Read(13, 8192) == text && EmptyClipboard();
        }
        finally { CloseClipboard(); }
    }
    private static IntPtr Allocate(string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value + '\0');
        var memory = GlobalAlloc(2, (nuint)bytes.Length); // GMEM_MOVEABLE
        if (memory == IntPtr.Zero) throw new InvalidOperationException();
        var address = GlobalLock(memory);
        if (address == IntPtr.Zero) { GlobalFree(memory); throw new InvalidOperationException(); }
        try { Marshal.Copy(bytes, 0, address, bytes.Length); }
        finally { GlobalUnlock(memory); }
        return memory;
    }
    private static string? Read(uint format, int limit)
    {
        var memory = GetClipboardData(format);
        var size = memory == IntPtr.Zero ? 0 : GlobalSize(memory);
        if (size < 2 || size > (nuint)limit || size % 2 != 0) return null;
        var address = GlobalLock(memory);
        if (address == IntPtr.Zero) return null;
        try
        {
            var bytes = new byte[(int)size];
            Marshal.Copy(address, bytes, 0, bytes.Length);
            // Require a terminator inside the allocated block; never over-read.
            for (int i = 0; i < bytes.Length; i += 2)
                if (bytes[i] == 0 && bytes[i + 1] == 0) return Encoding.Unicode.GetString(bytes, 0, i);
            return null;
        }
        finally { GlobalUnlock(memory); }
    }
    public void Dispose() { _owner?.Dispose(); _owner = null; }
    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(uint format, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterClipboardFormat(string name);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint flags, nuint bytes);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern nuint GlobalSize(IntPtr memory);
}
