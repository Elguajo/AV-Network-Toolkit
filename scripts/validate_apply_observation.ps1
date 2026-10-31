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
foreach ($variant in @('ordinary', 'policy', 'finite-address', 'registration', 'global-dns', 'suffix')) {
    $result = & {
        param($embedded, $fixtureVariant)
        function Get-NetAdapter { [pscustomobject]@{ InterfaceGuid='{00000000-0000-0000-0000-000000000012}'; InterfaceIndex=12; Name='Ethernet fixture'; MacAddress='02-00-00-00-00-01' } }
        function Get-NetIPInterface { param($AddressFamily) [pscustomobject]@{ InterfaceIndex=12; Dhcp='Disabled' } }
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
            [pscustomobject]@{ InterfaceIndex=12; IPAddress='192.0.2.10'; PrefixLength=24; PrefixOrigin='Manual';
                SuffixOrigin=$(if ($fixtureVariant -eq 'suffix') { 'Other' } else { 'Manual' });
                AddressState=$(if ($PolicyStore -eq 'ActiveStore') { 'Preferred' } else { 'Invalid' });
                SkipAsSource=$false; Type='Unicast'; ValidLifetime=[timespan]::FromSeconds([uint32]::MaxValue);
                PreferredLifetime=[timespan]::FromSeconds($(if ($fixtureVariant -eq 'finite-address') { 3600 } else { [uint32]::MaxValue })) }
        }
        function Get-NetRoute {
            param($AddressFamily, $PolicyStore)
            [pscustomobject]@{ InterfaceIndex=12; DestinationPrefix='0.0.0.0/0'; NextHop='192.0.2.1'; RouteMetric=1; Protocol='NetMgmt' }
        }
        $idx = 12
        & ([scriptblock]::Create($embedded))
    } $match.Groups[1].Value $variant | ConvertFrom-Json
    if ($result.ClassificationComplete -ne ($variant -eq 'ordinary')) { throw "Fixture classification failed: $variant" }
    if ($result.Identity.InterfaceIndex -ne 12 -or $result.Addresses.Count -ne 1 -or $result.PersistentAddresses.Count -ne 1 -or $result.Routes.Count -ne 2 -or $result.DnsServers.Count -ne 2) { throw 'Fixture snapshot lost affected state' }
}
Write-Output 'PASS: embedded observer parses; 6 fake-query fixtures classify state/DNS/policy/lifetimes/origins; no real OS queries, processes, probes or mutations'
