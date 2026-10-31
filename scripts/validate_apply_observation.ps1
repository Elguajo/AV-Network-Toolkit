$ErrorActionPreference = 'Stop'
# Parse the embedded Windows observation script; do not execute it or enumerate NICs.
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../app/Services/WindowsProjectApplyObservation.cs') -Raw
$match = [regex]::Match($source, 'internal const string SnapshotScript = """\r?\n([\s\S]*?)\r?\n\s*""";')
if (-not $match.Success) { throw 'Observation script not found' }
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseInput($match.Groups[1].Value, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
$commands = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)
$allow = @('Get-NetAdapter','Where-Object','Get-NetIPInterface','Get-ItemProperty','Get-DnsClientServerAddress',
    'Test-Path','Get-DnsClient','Get-DnsClientNrptPolicy','Get-NetIPAddress','Sort-Object','ForEach-Object','Get-NetRoute','ConvertTo-Json')
foreach ($command in $commands) {
    if ($command.GetCommandName() -notin $allow) { throw 'Unexpected command in read-only observer' }
}
# Execute only with every OS-query command shadowed by fixture functions in a child scope.
foreach ($variant in @('ordinary', 'policy', 'finite-address', 'registration', 'global-dns', 'suffix',
    'dad-disabled', 'dad-unknown', 'connection-unknown', 'disconnected', 'manual-link-local')) {
    $result = & {
        param($embedded, $fixtureVariant)
        function Get-NetAdapter { [pscustomobject]@{ InterfaceGuid='{00000000-0000-0000-0000-000000000012}'; InterfaceIndex=12; Name='Ethernet fixture'; MacAddress='02-00-00-00-00-01' } }
        function Get-NetIPInterface { param($AddressFamily) [pscustomobject]@{ InterfaceIndex=12; Dhcp='Disabled';
            DadTransmits=$(if ($fixtureVariant -eq 'dad-disabled') { 0 } elseif ($fixtureVariant -eq 'dad-unknown') { $null } else { 1 });
            ConnectionState=$(if ($fixtureVariant -eq 'connection-unknown') { $null } elseif ($fixtureVariant -eq 'disconnected') { 'Disconnected' } else { 'Connected' }) } }
        function Get-ItemProperty {
            param($LiteralPath)
            if ($LiteralPath -like '*Interfaces*') { [pscustomobject]@{ EnableDHCP=0; NameServer='192.0.2.53 192.0.2.54' } }
            else { [pscustomobject]@{ NameServer=$(if ($fixtureVariant -eq 'global-dns') { '192.0.2.57' } else { '' }) } }
        }
        function Get-DnsClientServerAddress { param($AddressFamily) [pscustomobject]@{ InterfaceIndex=12; ServerAddresses=@('192.0.2.53','192.0.2.54') } }
        function Get-DnsClient { [pscustomobject]@{ InterfaceIndex=12; RegisterThisConnectionsAddress=($fixtureVariant -ne 'registration'); UseSuffixWhenRegistering=($fixtureVariant -eq 'registration') } }
        function Test-Path { param($LiteralPath) $fixtureVariant -eq 'policy' }
        function Get-DnsClientNrptPolicy { param([switch]$Effective) }
        function Get-NetIPAddress {
            param($AddressFamily, $PolicyStore)
            [pscustomobject]@{ InterfaceIndex=12;
                IPAddress=$(if ($fixtureVariant -eq 'manual-link-local') { '169.254.42.10' } else { '192.0.2.10' });
                PrefixLength=$(if ($fixtureVariant -eq 'manual-link-local') { 16 } else { 24 }); PrefixOrigin='Manual';
                SuffixOrigin=$(if ($fixtureVariant -eq 'suffix') { 'Other' } else { 'Manual' });
                AddressState=$(if ($PolicyStore -eq 'ActiveStore') { 'Preferred' } else { 'Invalid' });
                SkipAsSource=$false; Type='Unicast'; ValidLifetime=[timespan]::FromSeconds([uint32]::MaxValue);
                PreferredLifetime=[timespan]::FromSeconds($(if ($fixtureVariant -eq 'finite-address') { 3600 } else { [uint32]::MaxValue })) }
        }
        function Get-NetRoute {
            param($AddressFamily, $PolicyStore)
            if ($fixtureVariant -eq 'manual-link-local') { return }
            [pscustomobject]@{ InterfaceIndex=12; DestinationPrefix='0.0.0.0/0'; NextHop='192.0.2.1'; RouteMetric=1; Protocol='NetMgmt' }
        }
        $idx = 12
        & ([scriptblock]::Create($embedded))
    } $match.Groups[1].Value $variant | ConvertFrom-Json
    if ($result.ClassificationComplete -ne ($variant -in @('ordinary','dad-disabled','dad-unknown','connection-unknown','disconnected','manual-link-local'))) { throw "Fixture classification failed: $variant" }
    $routeCount = 2; if ($variant -eq 'manual-link-local') { $routeCount = 0 }
    if ($result.Identity.InterfaceIndex -ne 12 -or $result.Addresses.Count -ne 1 -or $result.PersistentAddresses.Count -ne 1 -or $result.Routes.Count -ne $routeCount -or $result.DnsServers.Count -ne 2) { throw 'Fixture snapshot lost affected state' }
    if ($variant -in @('dad-unknown','connection-unknown')) { if ($null -ne $result.Dad) { throw 'Missing DAD must remain unknown' } }
    elseif ($result.Dad.Transmits -ne $(if ($variant -eq 'dad-disabled') { 0 } else { 1 }) -or $result.Dad.Connected -ne ($variant -ne 'disconnected')) { throw 'DAD/link state lost' }
    if ($variant -eq 'manual-link-local' -and $result.Addresses[0].Address -ne '169.254.42.10') { throw 'Link-local hidden from full snapshot' }
}
Write-Output 'PASS: embedded observer parses; 11 fake-query fixtures classify state/DNS/policy/lifetimes/origins/DAD/link-local; no real OS queries, processes, probes or mutations'
