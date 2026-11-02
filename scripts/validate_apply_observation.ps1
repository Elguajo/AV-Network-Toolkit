param([string]$SourcePath = (Join-Path $PSScriptRoot '../app/Services/WindowsProjectApplyObservation.cs'))
$ErrorActionPreference = 'Stop'
# Parse the embedded Windows observation script; do not execute it or enumerate NICs.
$source = Get-Content -LiteralPath $SourcePath -Raw
$match = [regex]::Match($source, 'internal const string SnapshotScript = """\r?\n([\s\S]*?)\r?\n\s*""";')
if (-not $match.Success) { throw 'Observation script not found' }
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseInput($match.Groups[1].Value, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
$commands = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)
$allow = @('Get-NetAdapter','Where-Object','Get-NetIPInterface','Get-ItemProperty','Get-DnsClientServerAddress',
    'Test-Path','Get-Item','Get-DnsClient','Get-DnsClientNrptPolicy','Get-NetIPAddress','Sort-Object','ForEach-Object','Get-NetRoute','ConvertTo-Json')
foreach ($command in $commands) {
    if ($command.GetCommandName() -notin $allow) { throw 'Unexpected command in read-only observer' }
}
# Execute only with every OS-query command shadowed by fixture functions in a child scope.
foreach ($variant in @('ordinary', 'policy', 'finite-address', 'registration', 'global-dns', 'suffix',
    'dad-disabled', 'dad-unknown', 'connection-unknown', 'disconnected', 'manual-link-local',
    'empty-policy', 'policy-child', 'persistent-null-protocol', 'active-null-protocol', 'empty-dns', 'automatic-fallback', 'dhcp-empty-persistent',
    'dhcp-apipa', 'dhcp-no-address')) {
    $result = & {
        param($embedded, $fixtureVariant)
        $dhcpFixture = $fixtureVariant -in @('dhcp-empty-persistent','dhcp-apipa','dhcp-no-address')
        $withoutLease = $fixtureVariant -in @('dhcp-apipa','dhcp-no-address')
        function Get-NetAdapter { [pscustomobject]@{ InterfaceGuid='{00000000-0000-0000-0000-000000000012}'; InterfaceIndex=12; Name='Ethernet fixture'; MacAddress='02-00-00-00-00-01' } }
        function Get-NetIPInterface { param($AddressFamily) [pscustomobject]@{ InterfaceIndex=12; Dhcp=$(if ($dhcpFixture) { 'Enabled' } else { 'Disabled' });
            DadTransmits=$(if ($fixtureVariant -eq 'dad-disabled') { 0 } elseif ($fixtureVariant -eq 'dad-unknown') { $null } else { 1 });
            ConnectionState=$(if ($fixtureVariant -eq 'connection-unknown') { $null } elseif ($fixtureVariant -eq 'disconnected') { 'Disconnected' } else { 'Connected' }) } }
        function Get-ItemProperty {
            param($LiteralPath)
            if ($LiteralPath -like '*Interfaces*') { [pscustomobject]@{ EnableDHCP=[int]$dhcpFixture; NameServer=$(if ($fixtureVariant -eq 'empty-dns' -or $dhcpFixture) { '' } else { '192.0.2.53 192.0.2.54' }) } }
            else { [pscustomobject]@{ NameServer=$(if ($fixtureVariant -eq 'global-dns') { '192.0.2.57' } else { '' }) } }
        }
        function Get-DnsClientServerAddress { param($AddressFamily) [pscustomobject]@{ InterfaceIndex=12; ServerAddresses=$(if ($fixtureVariant -eq 'empty-dns' -or $withoutLease) { @() } else { @('192.0.2.53','192.0.2.54') }) } }
        function Get-DnsClient { [pscustomobject]@{ InterfaceIndex=12; RegisterThisConnectionsAddress=($fixtureVariant -ne 'registration'); UseSuffixWhenRegistering=($fixtureVariant -eq 'registration') } }
        function Test-Path { param($LiteralPath) $fixtureVariant -in @('policy','empty-policy','policy-child') }
        function Get-Item { param($LiteralPath) [pscustomobject]@{ ValueCount=$(if ($fixtureVariant -eq 'policy') { 1 } else { 0 }); SubKeyCount=$(if ($fixtureVariant -eq 'policy-child') { 1 } else { 0 }) } }
        function Get-DnsClientNrptPolicy { param([switch]$Effective) }
        function Get-NetIPAddress {
            param($AddressFamily, $PolicyStore)
            if ($dhcpFixture -and $PolicyStore -eq 'PersistentStore') {
                if ($AddressFamily) { throw 'CIM ObjectNotFound for filtered empty persistent store' }; return
            }
            if ($fixtureVariant -eq 'dhcp-no-address') { return }
            [pscustomobject]@{ InterfaceIndex=12; AddressFamily='IPv4';
                IPAddress=$(if ($fixtureVariant -in @('manual-link-local','dhcp-apipa')) { '169.254.42.10' } else { '192.0.2.10' });
                PrefixLength=$(if ($fixtureVariant -in @('manual-link-local','dhcp-apipa')) { 16 } else { 24 }); PrefixOrigin=$(if ($withoutLease) { 'WellKnown' } elseif ($dhcpFixture) { 'Dhcp' } else { 'Manual' });
                SuffixOrigin=$(if ($fixtureVariant -eq 'suffix') { 'Other' } elseif ($withoutLease) { 'Link' } elseif ($dhcpFixture) { 'Dhcp' } else { 'Manual' });
                AddressState=$(if ($PolicyStore -eq 'ActiveStore') { 'Preferred' } else { 'Invalid' });
                SkipAsSource=$false; Type='Unicast'; ValidLifetime=[timespan]::FromSeconds([uint32]::MaxValue);
                PreferredLifetime=[timespan]::FromSeconds($(if ($fixtureVariant -eq 'finite-address') { 3600 } else { [uint32]::MaxValue })) }
            if ($fixtureVariant -eq 'automatic-fallback' -and $PolicyStore -eq 'ActiveStore') {
                [pscustomobject]@{ InterfaceIndex=12; AddressFamily='IPv4'; IPAddress='169.254.42.10'; PrefixLength=16;
                    PrefixOrigin='WellKnown'; SuffixOrigin='Link'; AddressState='Preferred'; SkipAsSource=$false; Type='Unicast' }
            }
        }
        function Get-NetRoute {
            param($AddressFamily, $PolicyStore)
            if ($fixtureVariant -eq 'manual-link-local') { return }
            if ($dhcpFixture -and $PolicyStore -eq 'PersistentStore') {
                if ($AddressFamily) { throw 'CIM ObjectNotFound for filtered empty persistent store' }; return
            }
            if ($withoutLease) {
                [pscustomobject]@{ InterfaceIndex=12; AddressFamily='IPv4'; DestinationPrefix='224.0.0.0/4'; NextHop='0.0.0.0'; RouteMetric=256; Protocol='Local' }; return
            }
            [pscustomobject]@{ InterfaceIndex=12; AddressFamily='IPv4'; DestinationPrefix='0.0.0.0/0'; NextHop='192.0.2.1'; RouteMetric=1;
                Protocol=$(if (($fixtureVariant -eq 'persistent-null-protocol' -and $PolicyStore -eq 'PersistentStore') -or
                    ($fixtureVariant -eq 'active-null-protocol' -and $PolicyStore -eq 'ActiveStore')) { $null } elseif ($fixtureVariant -eq 'dhcp-empty-persistent') { 'Dhcp' } else { 'NetMgmt' }) }
        }
        $idx = 12
        & ([scriptblock]::Create($embedded))
    } $match.Groups[1].Value $variant | ConvertFrom-Json
    if ($result.ClassificationComplete -ne ($variant -in @('ordinary','dad-disabled','dad-unknown','connection-unknown','disconnected','manual-link-local',
        'empty-policy','persistent-null-protocol','empty-dns','automatic-fallback','dhcp-empty-persistent','dhcp-apipa','dhcp-no-address'))) { throw "Fixture classification failed: $variant" }
    $routeCount = 2; if ($variant -eq 'manual-link-local') { $routeCount = 0 }
    $addressCount = 1; if ($variant -eq 'automatic-fallback') { $addressCount = 2 }
    if ($variant -eq 'dhcp-no-address') { $addressCount = 0 }
    $dnsCount = 2; if ($variant -eq 'empty-dns') { $dnsCount = 0 }
    if ($variant -in @('dhcp-apipa','dhcp-no-address')) { $dnsCount = 0; if ($result.DnsSource -ne 'automatic') { throw 'DHCP DNS policy lost' } }
    $persistentCount = 1; if ($variant -in @('dhcp-empty-persistent','dhcp-apipa','dhcp-no-address')) { $persistentCount = 0; $routeCount = 1 }
    if ($result.Identity.InterfaceIndex -ne 12 -or $result.Addresses.Count -ne $addressCount -or $result.PersistentAddresses.Count -ne $persistentCount -or $result.Routes.Count -ne $routeCount -or $result.DnsServers.Count -ne $dnsCount) { throw "Fixture snapshot lost affected state: $variant" }
    if ($variant -eq 'persistent-null-protocol' -and $result.Routes[1].Origin -ne 'persistent') { throw 'Missing persistent origin was fabricated' }
    if ($variant -eq 'empty-dns' -and $result.DnsSource -ne 'none') { throw 'Known empty DNS was classified as automatic' }
    if ($variant -in @('dad-unknown','connection-unknown')) { if ($null -ne $result.Dad) { throw 'Missing DAD must remain unknown' } }
    elseif ($result.Dad.Transmits -ne $(if ($variant -eq 'dad-disabled') { 0 } else { 1 }) -or $result.Dad.Connected -ne ($variant -ne 'disconnected')) { throw 'DAD/link state lost' }
    if ($variant -eq 'manual-link-local' -and $result.Addresses[0].Address -ne '169.254.42.10') { throw 'Link-local hidden from full snapshot' }
}
Write-Output 'PASS: embedded observer parses; 20 fake-query fixtures classify state/DNS/policy/lifetimes/origins/DAD/link-local; no real OS queries, processes, probes or mutations'
