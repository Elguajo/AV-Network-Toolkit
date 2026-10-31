using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void RenderMdns(string output, string theme, int width, int height)
    {
        var fake = new MdnsSurface(); var clock = new MdnsClock();
        using var session = new MdnsDiscoverySession(fake, fake, clock);
        using var vm = new MdnsDiscoveryViewModel(fake, session, fake);
        var window = new MdnsDiscoveryWindow(vm) { Width = width, Height = height }; window.Show(); Pump();
        var refresh = vm.RefreshAsync(); Until(() => refresh.IsCompleted); refresh.GetAwaiter().GetResult();
        if (vm.SelectedInterface != null || fake.Opened != 0 || vm.StartCommand.CanExecute(null)) throw new Exception("mDNS implicitly selected/started.");
        Capture(window, Path.Combine(output, $"mdns-empty-{theme}-{width}.png"));
        var selector = Descendants(window).OfType<ComboBox>().Single(); selector.SelectedItem = vm.Interfaces[0]; Pump();
        var start = vm.StartAsync(); Until(() => start.IsCompleted); start.GetAwaiter().GetResult(); vm.Poll(); Pump();
        if (vm.CanSelect || selector.IsEnabled || fake.Opened != 1) throw new Exception("mDNS selection not frozen while listening.");
        fake.Packet = MdnsSurface.CreatePacket(); fake.Enqueue();
        Until(() => fake.Reads >= 1);
        clock.Advance(TimeSpan.FromSeconds(2)); fake.EnqueueTick();
        Until(() => session.Snapshot.Services.Length == 2); vm.Poll(); Pump();
        if (vm.Rows.Count != 2 || vm.Rows.Count(r => r.Service.Incomplete) != 1) throw new Exception("mDNS incomplete/complete results missing.");
        foreach (var control in Descendants(window).OfType<Control>())
            if (control is ComboBox or ListBox || control is Wpf.Ui.Controls.TextBox)
                if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control))) throw new Exception("Unnamed mDNS control.");
        var stop = Descendants(window).OfType<Wpf.Ui.Controls.Button>().Single(b => ReferenceEquals(b.Command, vm.StopCommand));
        var footer = stop.TransformToAncestor(window).TransformBounds(new Rect(0, 0, stop.ActualWidth, stop.ActualHeight));
        if (!stop.IsEnabled || footer.Bottom > window.ActualHeight || footer.Top < 0) throw new Exception("mDNS Stop footer clipped.");
        var filter = Descendants(window).OfType<Wpf.Ui.Controls.TextBox>().Single(); filter.Focus(); Pump();
        bool reached = stop.IsKeyboardFocusWithin;
        for (int i = 0; i < 50 && !reached; i++)
        {
            if (Keyboard.FocusedElement is not UIElement focus) break;
            focus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)); Pump(); reached = stop.IsKeyboardFocusWithin;
        }
        if (!reached) throw new Exception("mDNS Stop unreachable by forward focus.");
        Descendants(window).OfType<ScrollViewer>().First().ScrollToBottom(); Pump();
        Capture(window, Path.Combine(output, $"mdns-listening-{theme}-{width}.png"));
        var list = Descendants(window).OfType<ListBox>().Single(); list.SelectedIndex = 0; Pump();
        var selected = list.SelectedItem; vm.Poll(); Pump();
        if (!ReferenceEquals(selected, list.SelectedItem)) throw new Exception("mDNS poll reset selected row.");
        if (fake.Saved != 0 || fake.Opened != 1) throw new Exception("mDNS selection caused a side effect.");
        filter.Text = "no matches"; Pump();
        if (vm.Rows.Count != 0 || vm.ExportCommand.CanExecute(null)) throw new Exception("mDNS filter binding failed.");
        Capture(window, Path.Combine(output, $"mdns-filter-empty-{theme}-{width}.png"));
        filter.Text = "_HTTP"; Pump(); vm.ExportCommand.Execute(null);
        if (fake.Saved != 1 || !fake.Csv.Contains("192.0.2.44")) throw new Exception("mDNS explicit filtered export failed.");
        var stopping = vm.StopAsync(); Until(() => stopping.IsCompleted); stopping.GetAwaiter().GetResult(); vm.Poll(); Pump();
        if (!fake.Disposed || vm.Rows.Any(r => !r.Service.Stale)) throw new Exception("mDNS stop failed to dispose/mark stale.");
        Capture(window, Path.Combine(output, $"mdns-stopped-{theme}-{width}.png"));
        fake.Fail = true; var failing = vm.StartAsync(); Until(() => failing.IsCompleted); failing.GetAwaiter().GetResult(); vm.Poll(); Pump();
        if (!vm.CanSelect || vm.Status.Contains("NEVER_ECHO")) throw new Exception("mDNS failure not redacted/recoverable.");
        Capture(window, Path.Combine(output, $"mdns-error-{theme}-{width}.png"));
        fake.Fail = false; var restarting = vm.StartAsync(); Until(() => restarting.IsCompleted); restarting.GetAwaiter().GetResult();
        window.Close(); Pump(); Until(() => !session.IsActive);
        if (!fake.Disposed || window.IsVisible) throw new Exception("mDNS close leaked active receiver.");
        void Until(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition() && DateTime.UtcNow < deadline) { clock.Advance(TimeSpan.FromMilliseconds(20)); Pump(); Thread.Sleep(1); }
            if (!condition()) throw new Exception("mDNS fake surface timed out.");
        }
    }
    private sealed class MdnsSurface : IMdnsInterfaces, IMdnsReceiverFactory, IMdnsReceiver, IMdnsExport
    {
        private Channel<MdnsDatagram?> _queue = Channel.CreateUnbounded<MdnsDatagram?>();
        private readonly MdnsInterface _nic = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006"), 6,
            "Fixture Ethernet (isolated fake)", "020000000006", ImmutableArray.Create("192.0.2.10/24"));
        public int Opened, Reads, Saved; public bool Disposed, Fail; public string Csv = ""; public byte[] Packet = Array.Empty<byte>();
        public IReadOnlyList<MdnsInterface> Read() => new[] { _nic };
        public IMdnsReceiver Open(MdnsInterface scope)
        {
            if (!scope.SameLink(_nic)) throw new Exception("mDNS wrong fixture scope.");
            Opened++; if (Fail) throw new Exception("NEVER_ECHO");
            Disposed = false; _queue = Channel.CreateUnbounded<MdnsDatagram?>(); return this;
        }
        public void Enqueue() => _queue.Writer.TryWrite(new(Packet, 6, MdnsDiscoverySession.Group, 5353));
        public void EnqueueTick() => _queue.Writer.TryWrite(null);
        public async ValueTask<MdnsDatagram?> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellation)
        { var packet = await _queue.Reader.ReadAsync(cancellation); Interlocked.Increment(ref Reads); return packet; }
        public bool Save(string csv) { Saved++; Csv = csv; return true; }
        public void Dispose() { Disposed = true; _queue.Writer.TryComplete(); }
        public static byte[] CreatePacket()
        {
            byte[] Name(string value) => value.Split('.').SelectMany(s => new[] { (byte)Encoding.UTF8.GetByteCount(s) }.Concat(Encoding.UTF8.GetBytes(s))).Append((byte)0).ToArray();
            byte[] Rr(string name, ushort type, byte[] data) => Name(name).Concat(new byte[] { (byte)(type >> 8), (byte)type, 0, 1, 0, 0, 0, 120, (byte)(data.Length >> 8), (byte)data.Length }).Concat(data).ToArray();
            return new byte[] { 0, 0, 0x84, 0, 0, 0, 0, 5, 0, 0, 0, 0 }
                .Concat(Rr("_http._tcp.local", 12, Name("panel-main._http._tcp.local")))
                .Concat(Rr("panel-main._http._tcp.local", 33, new byte[] { 0, 0, 0, 0, 0x20, 0xfb }.Concat(Name("panel-host.local")).ToArray()))
                .Concat(Rr("panel-host.local", 1, IPAddress.Parse("192.0.2.44").GetAddressBytes()))
                .Concat(Rr("panel-host.local", 28, IPAddress.Parse("fe80::44").GetAddressBytes()))
                .Concat(Rr("_http._tcp.local", 12, Name("incomplete-service._http._tcp.local"))).ToArray();
        }
    }
    private sealed class MdnsClock : TimeProvider
    {
        private long _ticks; private readonly ConcurrentBag<ClockTimer> _timers = new();
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        { var timer = new ClockTimer(this, callback, state); _timers.Add(timer); timer.Change(dueTime, period); return timer; }
        public void Advance(TimeSpan time)
        { Interlocked.Add(ref _ticks, time.Ticks); foreach (var timer in _timers) timer.Fire(GetTimestamp()); }
        private sealed class ClockTimer(MdnsClock clock, TimerCallback callback, object? state) : ITimer
        {
            private long _due = long.MaxValue;
            public bool Change(TimeSpan dueTime, TimeSpan period) { Interlocked.Exchange(ref _due, clock.GetTimestamp() + dueTime.Ticks); return true; }
            public void Fire(long now)
            {
                long due = Interlocked.Read(ref _due);
                if (due <= now && Interlocked.CompareExchange(ref _due, long.MaxValue, due) == due) callback(state);
            }
            public void Dispose() => Interlocked.Exchange(ref _due, long.MaxValue);
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
