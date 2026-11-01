[CmdletBinding()]
param(
    [string]$Dotnet = 'dotnet',
    [string]$Compiler,
    [string]$SourceCommit,
    [string]$OutputDirectory = 'out/packages'
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
if (Test-Path -LiteralPath $output) { throw 'Use a fresh output directory; package builds do not overwrite prior evidence' }
$version = ([xml](Get-Content -LiteralPath (Join-Path $root 'app/NetworkProfileManager.csproj') -Raw)).Project.PropertyGroup.Version |
    Where-Object { $_ } | Select-Object -First 1
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected existing three-part application version' }
if (-not $SourceCommit) {
    $SourceCommit = (& git -C $root rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source commit' }
    $changes = & git -C $root status --porcelain -- app test schemas examples packaging scripts
    if ($LASTEXITCODE -ne 0 -or $changes) { throw 'Build from a clean checkout or an explicitly pinned git archive' }
}
if ($SourceCommit -notmatch '^[a-f0-9]{40}$') { throw 'Expected a full source commit SHA' }
# Local immutable git-archive builds pass their verified commit explicitly.
if ($Compiler) { $Compiler = & (Join-Path $PSScriptRoot 'install_inno_setup.ps1') -CompilerPath $Compiler -ValidateOnly }
else { $Compiler = & (Join-Path $PSScriptRoot 'install_inno_setup.ps1') }
if (-not (Test-Path -LiteralPath $Compiler)) { throw 'Inno Setup compiler is required' }
$compilerPin = Get-Content -LiteralPath (Join-Path $root 'packaging/inno-setup.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Path $output | Out-Null
$payload = Join-Path $output 'payload'
& $Dotnet publish (Join-Path $root 'app/NetworkProfileManager.csproj') -c Release -r win-x64 --self-contained true `
    -p:DebugType=none -p:DebugSymbols=false -p:PublishReadyToRun=false "-p:SourceRevisionId=$SourceCommit" -o $payload
if ($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed' }
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $payload 'LICENSE')
$exe = Join-Path $payload 'NetworkProfileManager.exe'
foreach ($name in @('NetworkProfileManager.exe','D3DCompiler_47_cor3.dll','PenImc_cor3.dll','PresentationNative_cor3.dll','vcruntime140_cor3.dll','wpfgfx_cor3.dll','LICENSE','THIRD_PARTY_NOTICES.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $payload $name))) { throw "Missing payload dependency: $name" }
}
$report = Join-Path $output 'payload-self-test.json'
$start = [Diagnostics.ProcessStartInfo]::new($exe)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.ArgumentList.Add('--package-self-test'); $start.ArgumentList.Add($report)
$process = [Diagnostics.Process]::Start($start)
try {
    if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Package self-test timed out' }
    if ($process.ExitCode -ne 0) { throw "Package self-test failed: exit $($process.ExitCode)" }
} finally { $process.Dispose() }
$result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
if (-not $result.Success -or -not $result.JsonValidated -or -not $result.WpfRendered) { throw 'Invalid self-test report' }
if ($result.Version -ne "$version.0" -or $result.InformationalVersion -notlike "*+$SourceCommit") { throw 'Packaged version/source identity mismatch' }
$zip = Join-Path $output "AVNetworkToolkit-$version-win-x64-portable.zip"
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $zip
& $Compiler "/DAppVersion=$version" "/DPayloadDir=$payload" "/DOutputDir=$output" (Join-Path $root 'packaging/AVNetworkToolkit.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
$setup = Join-Path $output "AVNetworkToolkit-$version-win-x64-setup.exe"
if (-not (Test-Path -LiteralPath $setup)) { throw 'Installer missing' }
@{
    SourceCommit = $SourceCommit; Version = $version; RuntimeIdentifier = 'win-x64'; SelfContained = $true
    Compiler = @{ PinnedRelease = $compilerPin.Version; Files = $compilerPin.CompilerFiles }
    Files = @($zip,$setup) | ForEach-Object { @{ Name = [IO.Path]::GetFileName($_); Sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash } }
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'package-manifest.json') -Encoding utf8
Write-Output "PASS: installer and portable ZIP from $SourceCommit, version $version; package WPF/JSON self-test passed"
