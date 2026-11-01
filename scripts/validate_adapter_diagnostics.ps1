$ErrorActionPreference = 'Stop'
# Execute only the extracted query with ALL OS commands replaced by local fixtures.
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../app/Services/WindowsAdapterDiagnostics.cs') -Raw
$match = [regex]::Match($source, 'internal const string Script = """\r?\n([\s\S]*?)\r?\n\s*""";')
if (-not $match.Success) { throw 'Diagnostic script not found' }
$tokens = $null; $parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseInput($match.Groups[1].Value, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
$commands = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)
$allow = @('Get-NetAdapter','Get-NetIPAddress','Get-NetIPInterface','Get-DnsClientServerAddress','Get-NetRoute',
    'Get-CimInstance','Get-NetAdapterBinding','Sort-Object','ForEach-Object','Where-Object','ConvertTo-Json')
foreach ($command in $commands) {
    if ($command.GetCommandName() -notin $allow) { throw 'Unexpected command in read-only diagnostic query' }
}
$variants = @('ethernet','wifi','usb','virtual','hyperv','unknown','name-is-not-topology','uplink',
    'binding-denied','binding-ambiguous','binding-unknown','unplugged','identity-race','primary-failure','empty')
foreach ($variant in $variants) {
    $expectedFailure = $variant -in @('identity-race','primary-failure')
    $failed = $false
    try {
        $result = & {
            param($embedded, $fixture)
            $reads = [ref]0
            function Get-NetAdapter {
                param([switch]$IncludeHidden)
                $reads.Value++
                if ($fixture -eq 'empty') { return }
                $id = '11111111-1111-4111-8111-111111111111'
                if ($fixture -eq 'identity-race' -and $reads.Value -gt 1) { $id = '22222222-2222-4222-8222-222222222222' }
                [pscustomobject]@{ InterfaceGuid=$id; InterfaceIndex=7;
                    Name=$(if ($fixture -eq 'name-is-not-topology') { 'vEthernet Hyper-V USB' } else { 'fixture' });
                    InterfaceDescription='Fixture description'; MacAddress='00-11-22-33-44-55';
                    Virtual=($fixture -in @('virtual','hyperv'));
                    DriverFileName=$(if ($fixture -eq 'hyperv') { 'vmsmp.sys' } else { 'fixture.sys' });
                    HardwareInterface=$(if ($fixture -eq 'unknown') { $null } else { $fixture -notin @('virtual','hyperv') });
                    InterfaceType=$(if ($fixture -eq 'wifi') { 71 } else { 6 });
                    Status=$(if ($fixture -eq 'unplugged') { 'Disconnected' } else { 'Up' }); LinkSpeed='1 Gbps' }
            }
            function Get-NetIPAddress {
                param($AddressFamily)
                if ($fixture -eq 'primary-failure') { throw 'Fixture unavailable' }
                [pscustomobject]@{ InterfaceIndex=7; IPAddress='192.0.2.7'; PrefixLength=24; AddressState='Preferred'; SkipAsSource=$false }
                [pscustomobject]@{ InterfaceIndex=7; IPAddress='192.0.2.8'; PrefixLength=24; AddressState='Tentative'; SkipAsSource=$true }
            }
            function Get-NetIPInterface { param($AddressFamily) [pscustomobject]@{ InterfaceIndex=7; Dhcp='Disabled' } }
            function Get-DnsClientServerAddress { param($AddressFamily) [pscustomobject]@{ InterfaceIndex=7; ServerAddresses=@('192.0.2.53','192.0.2.54') } }
            function Get-NetRoute { param($AddressFamily,$PolicyStore) [pscustomobject]@{ InterfaceIndex=7; DestinationPrefix='0.0.0.0/0'; NextHop='192.0.2.1'; RouteMetric=25 } }
            function Get-CimInstance {
                param($ClassName)
                [pscustomobject]@{ GUID='11111111-1111-4111-8111-111111111111';
                    PNPDeviceID=$(if ($fixture -eq 'usb') { 'USB\FIXTURE' } else { 'PCI\FIXTURE' }) }
            }
            function Get-NetAdapterBinding {
                param([switch]$IncludeHidden,[switch]$AllBindings,$ComponentID)
                if ($fixture -eq 'binding-denied') { throw 'Fixture denied' }
                if ($fixture -eq 'binding-unknown') { [pscustomobject]@{ Name='fixture'; Enabled=$null }; return }
                if ($fixture -in @('uplink','binding-ambiguous')) {
                    [pscustomobject]@{ Name='fixture'; Enabled=$true }
                    if ($fixture -eq 'binding-ambiguous') { [pscustomobject]@{ Name='fixture'; Enabled=$true } }
                }
            }
            & ([scriptblock]::Create($embedded))
        } $match.Groups[1].Value $variant | ConvertFrom-Json
    }
    catch { if (-not $expectedFailure) { throw }; $failed = $true }
    if ($failed -ne $expectedFailure) { throw "Unexpected query disposition: $variant" }
    if ($failed) { continue }
    if ($variant -eq 'empty') { if (@($result).Count -ne 0) { throw 'Empty snapshot guessed adapter' }; continue }
    $result = @($result)[0]
    $expectedKind = if ($variant -in @('ethernet','wifi','usb','virtual','hyperv','unknown')) { $variant } else { 'ethernet' }
    if ($result.Kind -ne $expectedKind) { throw "Wrong type for $variant" }
    if (($result.HyperVUplink -eq $true) -ne ($variant -eq 'uplink')) { throw "Wrong uplink classification: $variant" }
    if ($variant -in @('binding-denied','binding-ambiguous','binding-unknown') -and $null -ne $result.HyperVUplink) { throw 'Unknown binding became known' }
    if ($result.Id -ne '11111111-1111-4111-8111-111111111111' -or $result.Index -ne 7 -or
        @($result.Addresses).Count -ne 2 -or @($result.Dns).Count -ne 2 -or @($result.Routes).Count -ne 1 -or
        $result.Routes[0].NextHop -ne '192.0.2.1' -or -not $result.Addresses[1].SkipAsSource) {
        throw "Snapshot lost reported facts: $variant, adapters=$(@($result).Count), addresses=$($result.Addresses.Count), dns=$($result.Dns.Count), routes=$($result.Routes.Count), skip=$($result.Addresses[1].SkipAsSource)"
    }
    if ($variant -eq 'unplugged' -and $result.Link -ne 'Disconnected') { throw 'Link state lost' }
}
Write-Output "PASS: embedded query parses/allowlist; $($variants.Count) fake OS fixtures; adapter type/identity/Hyper-V/unknown/failure/disconnected state; no real OS queries or probes"
