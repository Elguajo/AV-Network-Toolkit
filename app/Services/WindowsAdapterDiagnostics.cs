using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public interface IAdapterDiagnostics
{
    Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken cancellation);
}

public sealed class WindowsAdapterDiagnostics : IAdapterDiagnostics
{
    // One fixed, read-only local query. No alias, target, project value or user text enters script code.
    internal const string Script = """
        $ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.Encoding]::UTF8
        $adapters = @(Get-NetAdapter -IncludeHidden)
        $ip = @(Get-NetIPAddress -AddressFamily IPv4)
        $iface = @(Get-NetIPInterface -AddressFamily IPv4)
        $dns = @(Get-DnsClientServerAddress -AddressFamily IPv4)
        $routes = @(Get-NetRoute -AddressFamily IPv4 -PolicyStore ActiveStore)
        $hardware = @(); try { $hardware = @(Get-CimInstance Win32_NetworkAdapter) } catch {}
        $bindings = @(); $bindingKnown = $true
        try { $bindings = @(Get-NetAdapterBinding -IncludeHidden -AllBindings -ComponentID vms_pp) } catch { $bindingKnown = $false }
        $items = @($adapters | Sort-Object InterfaceIndex | ForEach-Object {
          $a = $_; $idx = [int]$a.InterfaceIndex
          $h = @($hardware | Where-Object { $_.GUID -and ([guid]$_.GUID) -eq ([guid]$a.InterfaceGuid) })
          $kind = 'unknown'
          if ($a.Virtual -eq $true) {
            $kind='virtual'
            if ([string]$a.DriverFileName -ieq 'vmsmp.sys') { $kind='hyperv' }
          }
          elseif ($a.HardwareInterface -eq $true) {
            if ($h.Count -eq 1 -and $h[0].PNPDeviceID -like 'USB\*') { $kind='usb' }
            elseif ([int]$a.InterfaceType -eq 71) { $kind='wifi' }
            elseif ([int]$a.InterfaceType -eq 6) { $kind='ethernet' }
            else { $kind='physical' }
          }
          $b = @($bindings | Where-Object Name -CEQ $a.Name)
          $uplink = $null
          if ($bindingKnown -and $b.Count -eq 1 -and $null -ne $b[0].Enabled -and $a.HardwareInterface -eq $true) { $uplink=[bool]$b[0].Enabled }
          $f = @($iface | Where-Object InterfaceIndex -eq $idx)
          $dhcp='unknown'; if ($f.Count -eq 1) { $dhcp=$f[0].Dhcp.ToString() }
          @{ Id=([guid]$a.InterfaceGuid).ToString(); Index=$idx; Name=[string]$a.Name;
             Description=[string]$a.InterfaceDescription; Mac=[string]$a.MacAddress; Kind=$kind;
             Physical=$a.HardwareInterface; HyperVUplink=$uplink; Link=$a.Status.ToString(); Speed=[string]$a.LinkSpeed; Dhcp=$dhcp;
             Addresses=@($ip | Where-Object InterfaceIndex -eq $idx | ForEach-Object {
               @{ Address=$_.IPAddress; PrefixLength=[int]$_.PrefixLength; State=$_.AddressState.ToString(); SkipAsSource=[bool]$_.SkipAsSource } });
             Dns=@($dns | Where-Object InterfaceIndex -eq $idx | ForEach-Object { $_.ServerAddresses });
             Routes=@($routes | Where-Object InterfaceIndex -eq $idx | ForEach-Object {
               @{ Destination=$_.DestinationPrefix; NextHop=$_.NextHop; Metric=[int]$_.RouteMetric } }) }
        })
        $after = @(Get-NetAdapter -IncludeHidden)
        foreach ($item in $items) {
          $same = @($after | Where-Object { ([guid]$_.InterfaceGuid) -eq ([guid]$item.Id) -and
             [int]$_.InterfaceIndex -eq $item.Index -and $_.Name -ceq $item.Name -and $_.MacAddress -ceq $item.Mac })
          if ($same.Count -ne 1) { throw 'Adapter changed during observation' }
        }
        ConvertTo-Json -InputObject $items -Depth 8 -Compress
        """;

    public async Task<IReadOnlyList<DiagnosticAdapter>> ReadAsync(CancellationToken cancellation)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (var arg in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand",
            Convert.ToBase64String(Encoding.Unicode.GetBytes(Script)) }) start.ArgumentList.Add(arg);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var process = Process.Start(start) ?? throw new IOException();
        Task drain = Task.CompletedTask;
        try
        {
            var output = ReadBoundedAsync(process.StandardOutput, 2_000_000, timeout.Token);
            var errors = ReadBoundedAsync(process.StandardError, 32_000, timeout.Token);
            // A reader failure cancels the process wait too, including a producer blocked on a full pipe.
            async Task Guard(Task<string> reader)
            { try { await reader.ConfigureAwait(false); } catch { timeout.Cancel(); throw; } }
            drain = Task.WhenAll(Guard(output), Guard(errors));
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            await drain.ConfigureAwait(false);
            var json = await output.ConfigureAwait(false);
            if (process.ExitCode != 0) throw new IOException();
            return Parse(json);
        }
        finally
        {
            timeout.Cancel();
            if (!process.HasExited) { try { process.Kill(true); } catch { } }
            try { await drain.ConfigureAwait(false); } catch { /* observe reader failures; preserve the original error */ }
        }
    }
    private static async Task<string> ReadBoundedAsync(StreamReader reader, int limit, CancellationToken token)
    {
        var result = new StringBuilder(); var buffer = new char[4096]; int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) != 0)
        { if (result.Length + count > limit) throw new IOException(); result.Append(buffer, 0, count); }
        return result.ToString();
    }
    internal static IReadOnlyList<DiagnosticAdapter> Parse(string json)
    {
        if (json.Length > 2_000_000) throw new IOException();
        var list = JsonSerializer.Deserialize<ImmutableArray<DiagnosticAdapter>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, MaxDepth = 12 });
        if (list.IsDefault || list.Length > 256 || list.Any(a => a == null || a.Id == Guid.Empty || a.Index <= 0
            || a.Addresses.IsDefault || a.Dns.IsDefault || a.Routes.IsDefault || a.Addresses.Length > 128 || a.Routes.Length > 4096
            || a.Name == null || a.Name.Length > 1024 || a.Mac == null || a.Link == null || a.Kind == null
            || a.Description == null || a.Dhcp == null || a.Speed == null
            || a.Kind is not ("unknown" or "physical" or "ethernet" or "wifi" or "usb" or "virtual" or "hyperv")
            || a.Addresses.Any(ip => ip == null || ip.PrefixLength is < 0 or > 32 || ip.State == null
                || !System.Net.IPAddress.TryParse(ip.Address, out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            || a.Dns.Any(d => d == null) || a.Routes.Any(route => route == null || route.Destination == null || route.NextHop == null))
            || list.Select(a => a.Id).Distinct().Count() != list.Length
            || list.Select(a => a.Index).Distinct().Count() != list.Length) throw new IOException();
        return list;
    }
}
