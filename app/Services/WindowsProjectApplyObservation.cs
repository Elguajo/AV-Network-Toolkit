using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

internal sealed class ApplyObservationException : IOException { }

// Explicit profile preparation/verification/recovery only; no startup monitor or probe.
internal sealed class WindowsProjectApplyObservation
{
    internal async Task<ImmutableArray<ApplyAdapterIdentity>> EnumerateAsync(CancellationToken cancellation)
        => ProjectApplyWire.Parse<ImmutableArray<ApplyAdapterIdentity>>(await QueryAsync("""
            $items = @(Get-NetAdapter -IncludeHidden | Sort-Object InterfaceIndex | ForEach-Object {
              @{ Id=([guid]$_.InterfaceGuid).ToString(); InterfaceIndex=[int]$_.InterfaceIndex; Alias=$_.Name; PhysicalAddress=$_.MacAddress }
            })
            ConvertTo-Json -InputObject $items -Depth 4 -Compress
            """, cancellation));

    internal async Task<ApplyAdapterSnapshot> ReadAsync(ApplyAdapterIdentity identity, CancellationToken cancellation)
    {
        if (identity.Id == Guid.Empty || identity.InterfaceIndex <= 0) throw new InvalidOperationException("Invalid adapter.");
        // Index is a positive integer, not project text. The script is otherwise constant.
        var json = await QueryAsync("$idx=" + identity.InterfaceIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";\n" + SnapshotScript, cancellation);
        return ProjectApplyWire.Parse<ApplyAdapterSnapshot>(json);
    }

    internal const string SnapshotScript = """
        $nic = @(Get-NetAdapter -IncludeHidden | Where-Object InterfaceIndex -eq $idx)
        if ($nic.Count -ne 1) { throw 'Adapter unavailable' }
        $nic = $nic[0]
        $guid = [guid]$nic.InterfaceGuid
        $iface = @(Get-NetIPInterface -AddressFamily IPv4 | Where-Object InterfaceIndex -eq $idx)
        if ($iface.Count -ne 1) { throw 'Ambiguous interface' }
        $dhcp = $iface[0].Dhcp.ToString() -eq 'Enabled'
        $keyPath = 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\' + $guid.ToString('B')
        $reg = Get-ItemProperty -LiteralPath $keyPath
        $complete = ($reg.EnableDHCP -eq [int]$dhcp)
        $global = Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'
        if ([string]$global.NameServer) { $complete = $false }
        $rawDns = @(([string]$reg.NameServer -split '[,;\s]+') | Where-Object { $_ })
        $dns = @(Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object InterfaceIndex -eq $idx | ForEach-Object { $_.ServerAddresses })
        $source = 'automatic'
        $client = @(Get-DnsClient | Where-Object InterfaceIndex -eq $idx)
        if ($client.Count -ne 1) { throw 'Unknown DNS registration' }
        $registration = 'primary'
        if ($client[0].UseSuffixWhenRegistering) { $registration = 'both' }
        if (-not $client[0].RegisterThisConnectionsAddress) {
          $registration = 'none'
          if ($client[0].UseSuffixWhenRegistering) { $complete = $false }
        }
        if ($rawDns.Count -gt 0) {
          $source = 'static'
          if (($rawDns -join ',') -cne ($dns -join ',')) { $complete = $false }
        }
        foreach ($p in @('HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient',
            'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DNSClient',
            'HKLM:\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DnsPolicyConfig',
            'HKLM:\SOFTWARE\Microsoft\PolicyManager\current\device\ADMX_DnsClient')) {
          if (Test-Path -LiteralPath $p) {
            $policyKey = Get-Item -LiteralPath $p
            if ($policyKey.ValueCount -gt 0 -or $policyKey.SubKeyCount -gt 0) { $complete = $false }
          }
        }
        if (@(Get-DnsClientNrptPolicy -Effective).Count -gt 0) { $complete = $false }
        if ($dhcp -and @($reg.IPAddress | Where-Object { $_ -and $_ -ne '0.0.0.0' }).Count -gt 0) { $complete = $false }
        $addresses = @()
        $persistent = @()
        foreach ($store in @('ActiveStore','PersistentStore')) {
          # A filtered CIM query throws ObjectNotFound for a normally empty
          # PersistentStore (e.g. DHCP). Enumerate, then filter without suppressing errors.
          $list = @(Get-NetIPAddress -PolicyStore $store | Where-Object { $_.AddressFamily -eq 'IPv4' -and $_.InterfaceIndex -eq $idx })
          $out = @($list | Sort-Object IPAddress,PrefixLength | ForEach-Object {
            if ($_.SkipAsSource -or $_.Type.ToString() -ne 'Unicast') { $complete = $false }
            $fallback = $store -eq 'ActiveStore' -and $_.IPAddress -like '169.254.*' -and $_.PrefixLength -eq 16 -and
              [string]$_.PrefixOrigin -eq 'WellKnown' -and [string]$_.SuffixOrigin -eq 'Link'
            if (-not $fallback -and $_.PrefixOrigin.ToString() -ne $_.SuffixOrigin.ToString()) { $complete = $false }
            if ($_.PrefixOrigin.ToString() -eq 'Manual' -and
                ($_.ValidLifetime.TotalSeconds -lt [uint32]::MaxValue -or $_.PreferredLifetime.TotalSeconds -lt [uint32]::MaxValue)) { $complete = $false }
            @{ Address=$_.IPAddress; PrefixLength=[int]$_.PrefixLength;
               Origin=$_.PrefixOrigin.ToString().ToLowerInvariant(); State=$_.AddressState.ToString().ToLowerInvariant() }
          })
          if ($store -eq 'ActiveStore') { $addresses=$out } else { $persistent=$out }
        }
        $routes = @()
        foreach ($store in @('ActiveStore','PersistentStore')) {
          $label = 'active'; if ($store -eq 'PersistentStore') { $label='persistent' }
          $routes += @(Get-NetRoute -PolicyStore $store | Where-Object { $_.AddressFamily -eq 'IPv4' -and $_.InterfaceIndex -eq $idx } |
            Sort-Object DestinationPrefix,NextHop,RouteMetric | ForEach-Object {
              $origin = ([string]$_.Protocol).ToLowerInvariant()
              if (-not $origin) {
                $origin = 'unknown'
                if ($store -eq 'PersistentStore') { $origin = 'persistent' } else { $complete = $false }
              }
              @{ Destination=$_.DestinationPrefix; NextHop=$_.NextHop; Metric=[int]$_.RouteMetric;
                 Store=$label; Origin=$origin }
            })
        }
        if (-not $dhcp -and $rawDns.Count -eq 0 -and $dns.Count -eq 0) { $source = 'none' }
        @{ Identity=@{ Id=$guid.ToString(); InterfaceIndex=[int]$nic.InterfaceIndex;
             Alias=$nic.Name; PhysicalAddress=$nic.MacAddress }; Dhcp=$dhcp; Addresses=@($addresses);
           PersistentAddresses=@($persistent); DnsSource=$source; DnsServers=@($dns); Routes=@($routes);
           ClassificationComplete=[bool]$complete; DnsRegistration=$registration;
           Dad=$(if ($null -ne $iface[0].DadTransmits -and [string]$iface[0].ConnectionState -in @('Connected','Disconnected')) {
             @{ Transmits=[int]$iface[0].DadTransmits; Connected=([string]$iface[0].ConnectionState -eq 'Connected') }
           } else { $null }) } | ConvertTo-Json -Depth 8 -Compress
        """;

    private static async Task<string> QueryAsync(string script, CancellationToken cancellation)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand",
            Convert.ToBase64String(Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.Encoding]::UTF8;\n" + script)) }) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi) ?? throw new IOException("Observation unavailable.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellation);
        var stderr = process.StandardError.ReadToEndAsync(cancellation);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            var output = await stdout;
            _ = await stderr;
            if (process.ExitCode != 0 || output.Length > ProjectApplyWire.MaxBytes) throw new ApplyObservationException();
            return output;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            try { process.Kill(true); } catch { }
            throw;
        }
        catch
        {
            try { process.Kill(true); } catch { }
            throw new ApplyObservationException();
        }
    }
}
