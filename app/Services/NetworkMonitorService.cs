using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Services
{
    /// <summary>
    /// Monitors network adapter state changes and shows balloon-tip notifications
    /// via the application's system-tray icon.
    /// </summary>
    public sealed class NetworkMonitorService : IDisposable
    {
        private readonly Func<IEnumerable<NetworkInterface>> _readInterfaces;
        private readonly Action<string, string, ToolTipIcon> _showNotification;
        private readonly object _gate = new();
        private Dictionary<string, AdapterSnapshot> _snapshot;
        private bool _disposed;

        private readonly record struct AdapterSnapshot(
            string Name,
            string IP,
            OperationalStatus Status);

        /// <param name="trayIcon">The tray icon used to show balloon tips.</param>
        public NetworkMonitorService(NotifyIcon trayIcon)
            : this(NetworkInterface.GetAllNetworkInterfaces,
                   (title, message, icon) => trayIcon.ShowBalloonTip(4000, title, message, icon))
        {
            NetworkChange.NetworkAddressChanged += OnAddressChanged;
        }

        // Tests supply interfaces/notification capture without native enumeration or tray UI.
        internal NetworkMonitorService(Func<IEnumerable<NetworkInterface>> readInterfaces,
                                       Action<string, string, ToolTipIcon> showNotification)
        {
            _readInterfaces = readInterfaces;
            _showNotification = showNotification;
            _snapshot = TakeSnapshot();
        }

        private void OnAddressChanged(object? sender, EventArgs e) => CheckForChanges();

        internal void CheckForChanges()
        {
            var current = TakeSnapshot();

            // NetworkAddressChanged fires on threadpool threads; serialize the
            // compare-then-swap so two rapid events can't interleave and diff
            // against a half-updated snapshot.
            Dictionary<string, AdapterSnapshot> previous;
            lock (_gate)
            {
                previous = _snapshot;
                _snapshot = current;
            }

            foreach (var (id, now) in current)
            {
                if (!previous.TryGetValue(id, out var prev)) continue;

                bool wasUp  = prev.Status == OperationalStatus.Up;
                bool isUp   = now.Status  == OperationalStatus.Up;
                bool ipDiff = prev.IP != now.IP;

                if (!wasUp && isUp)
                {
                    Show(Loc.Get("Tray.ConnectionUp", "Bağlantı Kuruldu"),
                         string.IsNullOrEmpty(now.IP)
                             ? now.Name
                             : $"{now.Name}  —  {now.IP}",
                         ToolTipIcon.Info);
                }
                else if (wasUp && !isUp)
                {
                    Show(Loc.Get("Tray.ConnectionDown", "Bağlantı Kesildi"),
                         now.Name, ToolTipIcon.Warning);
                }
                else if (isUp && ipDiff && !string.IsNullOrEmpty(now.IP))
                {
                    Show(Loc.Get("Tray.IpChanged", "IP Adresi Değişti"),
                         $"{now.Name}\n{prev.IP}  →  {now.IP}",
                         ToolTipIcon.Info);
                }
            }
        }

        private Dictionary<string, AdapterSnapshot> TakeSnapshot()
        {
            var dict = new Dictionary<string, AdapterSnapshot>(StringComparer.Ordinal);
            try
            {
                foreach (var nic in _readInterfaces())
                {
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    // Match the profile-adapter list: OS tunnel lifecycle is not a user connection.
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    var ip = nic.GetIPProperties()
                                .UnicastAddresses
                                .FirstOrDefault(a => a.Address.AddressFamily
                                                     == System.Net.Sockets.AddressFamily.InterNetwork
                                                  && !System.Net.IPAddress.IsLoopback(a.Address))
                               ?.Address.ToString() ?? "";

                    dict[nic.Id] = new AdapterSnapshot(nic.Name, ip, nic.OperationalStatus);
                }
            }
            catch { /* network stack may be temporarily unavailable */ }
            return dict;
        }

        private void Show(string title, string message, ToolTipIcon icon)
        {
            try
            {
                _showNotification(title, message, icon);
            }
            catch { /* tray icon may have been disposed */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            NetworkChange.NetworkAddressChanged -= OnAddressChanged;
        }
    }
}
