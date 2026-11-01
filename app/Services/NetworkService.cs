using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Win32;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services
{
    public class NetworkService
    {
        public List<AdapterInfo> GetAdapters()
        {
#if DEBUG
            if (DemoMode.IsActive) return DemoMode.Adapters();
#endif
            return GetAdapters(NetworkInterface.GetAllNetworkInterfaces());
        }

        // Same projection for production and fixture interfaces; tests never enumerate the host.
        internal List<AdapterInfo> GetAdapters(IEnumerable<NetworkInterface> interfaces)
        {
            var result = new List<AdapterInfo>();

            foreach (var nic in interfaces)
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)   continue;

                bool connected = nic.OperationalStatus == OperationalStatus.Up;
                var props = nic.GetIPProperties();

                // Only IPv4, no loopback
                var unicast = props.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork
                                     && !IPAddress.IsLoopback(a.Address));
                var gw = props.GatewayAddresses
                    .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);

                string ip      = unicast?.Address.ToString() ?? "";
                string subnet  = unicast?.IPv4Mask.ToString() ?? "";
                string gateway = gw?.Address.ToString() ?? "";
                bool   isDhcp  = false;
                bool   dhcpKnown = true;

                try
                {
                    var ipv4 = props.GetIPv4Properties();
                    dhcpKnown = ipv4 != null;
                    isDhcp = ipv4?.IsDhcpEnabled ?? false;
                }
                catch { dhcpKnown = false; }
                bool? manualAddress = null;
                if (LinkLocalAddressing.IsLinkLocal(ip) && unicast != null)
                {
                    try { manualAddress = unicast.PrefixOrigin == PrefixOrigin.Manual && unicast.SuffixOrigin == SuffixOrigin.Manual; }
                    catch { }
                }

                // DNS servers (IPv4 only)
                var dnsAddrs = props.DnsAddresses
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.ToString())
                    .ToList();
                string primaryDns   = dnsAddrs.Count > 0 ? dnsAddrs[0] : "";
                string secondaryDns = dnsAddrs.Count > 1 ? dnsAddrs[1] : "";

                // If not connected and no active IP, try registry for configured static IP
                if (!connected && string.IsNullOrEmpty(ip))
                {
                    var reg = ReadRegistryIp(nic.Id);
                    ip      = reg.ip;
                    subnet  = reg.subnet;
                    gateway = reg.gateway;
                    isDhcp  = reg.isDhcp;
                    dhcpKnown = !string.IsNullOrEmpty(reg.ip);
                }

                result.Add(new AdapterInfo
                {
                    Id                = nic.Id,
                    Name              = nic.Name,
                    Description       = nic.Description,
                    IsConnected       = connected,
                    CurrentIp         = ip,
                    CurrentSubnet     = subnet,
                    CurrentGateway    = gateway,
                    IsDhcp            = isDhcp,
                    IsDhcpKnown       = dhcpKnown,
                    IsCurrentIpv4Manual = manualAddress,
                    AdapterType       = nic.NetworkInterfaceType.ToString(),
                    CurrentPrimaryDns   = primaryDns,
                    CurrentSecondaryDns = secondaryDns
                });
            }

            return result;
        }

        /// <summary>Applies a static IP configuration. Optionally sets DNS servers.</summary>
        // ── Validation (public for testability + defense-in-depth) ─────

        /// <summary>
        /// Validates an IPv4 address in canonical dotted-quad form.
        /// Rejects IPAddress.TryParse's legacy shorthand (e.g. "192.168.1" → 192.168.0.1)
        /// to prevent silent misconfiguration.
        /// </summary>
        public static void EnsureValidIp(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(Loc.Format("Error.InvalidIpv4", paramName), paramName);

            var parts = value.Split('.');
            if (parts.Length != 4)
                throw new ArgumentException(Loc.Format("Error.InvalidIpv4", paramName), paramName);

            foreach (var part in parts)
            {
                if (part.Length == 0 || part.Length > 3) goto invalid;
                if (!byte.TryParse(part, System.Globalization.NumberStyles.None,
                                   System.Globalization.CultureInfo.InvariantCulture, out _))
                    goto invalid;
            }

            if (!IPAddress.TryParse(value, out var addr)
                || addr.AddressFamily != AddressFamily.InterNetwork)
                goto invalid;

            return;

        invalid:
            throw new ArgumentException(Loc.Format("Error.InvalidIpv4", paramName), paramName);
        }

        /// <summary>
        /// Rejects adapter names containing characters that could escape netsh's
        /// argument parsing (control chars, quotes, CR/LF) — defense-in-depth even
        /// though ArgumentList already quotes safely.
        /// </summary>
        public static void EnsureValidAdapterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    Loc.Get("Error.AdapterNameEmpty", "Adaptör adı boş olamaz."), nameof(name));

            foreach (var ch in name)
            {
                if (ch < 0x20 || ch == '"' || ch == '\r' || ch == '\n' || ch == '\0')
                    throw new ArgumentException(
                        Loc.Get("Error.AdapterNameInvalidChar", "Adaptör adı geçersiz karakter içeriyor."),
                        nameof(name));
            }

            if (name.Length > 256)
                throw new ArgumentException(
                    Loc.Get("Error.AdapterNameTooLong", "Adaptör adı çok uzun."), nameof(name));
        }

        public async Task<(bool reachable, long ms)> PingAsync(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return (false, -1);
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(host, 2000);
                    return (reply.Status == IPStatus.Success, reply.RoundtripTime);
                }
            }
            catch
            {
                return (false, -1);
            }
        }

        // ── Private helpers ────────────────────────────────────────────

        /// <summary>
        /// Reads configured static IPv4 values for display when the adapter is offline.
        /// </summary>
        private static (string ip, string subnet, string gateway, bool isDhcp) ReadRegistryIp(string adapterId)
        {
            try
            {
                var keyPath = $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{adapterId}";
                using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) return ("", "", "", true);

                    int enableDhcp = (int)(key.GetValue("EnableDHCP") ?? 1);
                    if (enableDhcp == 1) return ("", "", "", true);

                    var ipArr      = key.GetValue("IPAddress")     as string[];
                    var subnetArr  = key.GetValue("SubnetMask")    as string[];
                    var gwArr      = key.GetValue("DefaultGateway") as string[];

                    string ip      = ipArr?.FirstOrDefault(x => x != "0.0.0.0" && x != "") ?? "";
                    string subnet  = subnetArr?.FirstOrDefault() ?? "";
                    string gateway = gwArr?.FirstOrDefault(x => x != "") ?? "";

                    return (ip, subnet, gateway, false);
                }
            }
            catch
            {
                return ("", "", "", true);
            }
        }

        private static string PrefixToMask(int prefix)
        {
            if (prefix == 0)  return "0.0.0.0";
            if (prefix == 32) return "255.255.255.255";
            uint mask  = ~(uint.MaxValue >> prefix);
            var  bytes = BitConverter.GetBytes(mask);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return string.Join(".", bytes);
        }
    }
}
