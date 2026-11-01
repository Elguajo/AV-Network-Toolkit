[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
# This test moves runtime discovery roots and installs software. NEVER run it on a user's PC.
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted' -or $env:ImageOS -notlike 'win*') {
    throw 'Package installation/runtime isolation is restricted to a disposable GitHub-hosted Windows runner'
}
$packages = (Resolve-Path -LiteralPath $PackageDirectory).Path
$manifest = Get-Content -LiteralPath (Join-Path $packages 'package-manifest.json') -Raw | ConvertFrom-Json
foreach ($file in $manifest.Files) {
    if ($file.Name -ne [IO.Path]::GetFileName($file.Name)) { throw 'Invalid manifest filename' }
    if ((Get-FileHash -LiteralPath (Join-Path $packages $file.Name) -Algorithm SHA256).Hash -ne $file.Sha256) { throw 'Package hash mismatch' }
}
$results = Join-Path $packages 'runner-verification'
if (Test-Path -LiteralPath $results) { throw 'Use fresh verification output' }
New-Item -ItemType Directory -Path $results | Out-Null
function Invoke-Checked([string]$Executable, [string[]]$Arguments, [bool]$MustSucceed = $true) {
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false; $start.CreateNoWindow = $true
    $start.RedirectStandardError = $true; $start.RedirectStandardOutput = $true
    foreach ($value in $Arguments) { $start.ArgumentList.Add($value) }
    $process = [Diagnostics.Process]::Start($start)
    $stderr = $process.StandardError.ReadToEndAsync(); $stdout = $process.StandardOutput.ReadToEndAsync()
    try {
        if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'Package check timed out' }
        $errorText = $stderr.GetAwaiter().GetResult(); $null = $stdout.GetAwaiter().GetResult()
        if ($MustSucceed -and $process.ExitCode -ne 0) { throw "Package process failed with exit $($process.ExitCode)" }
        if (-not $MustSucceed) {
            if ($process.ExitCode -eq 0) { throw 'Expected rejection did not occur' }
            return $errorText
        }
    } finally { $process.Dispose() }
}
$control = Join-Path $results 'control'
& dotnet publish (Join-Path $PSScriptRoot 'RuntimeControl/RuntimeControl.csproj') -c Release -r win-x64 --self-contained false -o $control
if ($LASTEXITCODE -ne 0) { throw 'Runtime negative-control build failed' }
Invoke-Checked (Join-Path $control 'RuntimeControl.exe') @()
& dotnet build-server shutdown
if ($LASTEXITCODE -ne 0) { throw 'Cannot stop build servers before runtime isolation' }
$roots = @((Join-Path $env:ProgramFiles 'dotnet'), (Join-Path ${env:ProgramFiles(x86)} 'dotnet'),
    [IO.Path]::GetDirectoryName((Get-Command dotnet).Source)) | Select-Object -Unique
$moves = @()
$savedEnvironment = @{}
$globalVariables = @(Get-ChildItem Env: | Where-Object { $_.Name -like 'DOTNET_ROOT*' })
foreach ($variable in $globalVariables) { $savedEnvironment[$variable.Name] = $variable.Value }
$savedGui = $env:DOTNET_DISABLE_GUI_ERRORS
$installPath = Join-Path $env:ProgramFiles "AVNetworkToolkit-Acceptance-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT"
if (Test-Path -LiteralPath $installPath) { throw 'Refusing to reuse an existing installation' }
$sentinelDir = Join-Path $env:LOCALAPPDATA 'NetworkProfileManager/data'
New-Item -ItemType Directory -Force -Path $sentinelDir | Out-Null
$sentinel = Join-Path $sentinelDir 'package-acceptance-sentinel.json'
if (Test-Path -LiteralPath $sentinel) { throw 'Sentinel already exists' }
'{"fixture":"retain original user data"}' | Set-Content -LiteralPath $sentinel -Encoding utf8
$sentinelHash = (Get-FileHash -LiteralPath $sentinel -Algorithm SHA256).Hash
try {
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $absolute = [IO.Path]::GetFullPath($root).TrimEnd('\')
        $allowed = @($env:ProgramFiles, ${env:ProgramFiles(x86)}, $env:RUNNER_TOOL_CACHE) |
            Where-Object { $_ -and $absolute.StartsWith([IO.Path]::GetFullPath($_).TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) }
        if (-not $allowed -or [IO.Path]::GetFileName($absolute) -ne 'dotnet') { throw 'Runtime root outside approved runner directories' }
        $backup = $absolute + '.avnet-isolated-' + [guid]::NewGuid().ToString('N')
        if ([IO.Path]::GetDirectoryName($absolute) -ne [IO.Path]::GetDirectoryName($backup)) { throw 'Runtime backup must remain beside its original' }
        Move-Item -LiteralPath $absolute -Destination $backup
        $moves += @{ Original = $absolute; Backup = $backup }
    }
    foreach ($variable in $globalVariables) { [Environment]::SetEnvironmentVariable($variable.Name, $null, 'Process') }
    $env:DOTNET_DISABLE_GUI_ERRORS = '1'
    $runtimeError = Invoke-Checked (Join-Path $control 'RuntimeControl.exe') @() $false
    if ($runtimeError -notmatch '\.NET|hostfxr') { throw 'Negative control did not demonstrate unavailable runtime' }
    $zip = Join-Path $packages "AVNetworkToolkit-$($manifest.Version)-win-x64-portable.zip"
    $portable = Join-Path $results 'portable'
    Expand-Archive -LiteralPath $zip -DestinationPath $portable
    & (Join-Path $PSScriptRoot 'test_package_self_test.ps1') -Executable (Join-Path $portable 'NetworkProfileManager.exe') -ResultsDirectory (Join-Path $results 'portable-tests')
    $setup = Join-Path $packages "AVNetworkToolkit-$($manifest.Version)-win-x64-setup.exe"
    $setupArguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',"/DIR=$installPath")
    Invoke-Checked $setup $setupArguments
    $installed = Join-Path $installPath 'NetworkProfileManager.exe'
    foreach ($payload in Get-ChildItem -LiteralPath $portable -File) {
        if ((Get-FileHash -LiteralPath $payload.FullName -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath (Join-Path $installPath $payload.Name) -Algorithm SHA256).Hash) { throw 'Installed payload differs from ZIP' }
    }
    & (Join-Path $PSScriptRoot 'test_package_self_test.ps1') -Executable $installed -ResultsDirectory (Join-Path $results 'installed-tests')
    $locked = [IO.File]::Open($installed, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $null = Invoke-Checked $setup $setupArguments $false
        $null = Invoke-Checked (Join-Path $installPath 'unins000.exe') @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') $false
        if (-not (Test-Path -LiteralPath $installed)) { throw 'Blocked uninstall removed the executable' }
    } finally { $locked.Dispose() }
    # Same-version reinstall is checked explicitly; this is not a cross-version upgrade claim.
    Invoke-Checked $setup $setupArguments
    if ((Get-FileHash -LiteralPath $sentinel -Algorithm SHA256).Hash -ne $sentinelHash) { throw 'Reinstall changed original user data' }
    Invoke-Checked (Join-Path $installPath 'unins000.exe') @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART')
    if (Test-Path -LiteralPath $installed) { throw 'Uninstall left the program executable' }
    if ((Get-FileHash -LiteralPath $sentinel -Algorithm SHA256).Hash -ne $sentinelHash) { throw 'Uninstall changed original user data' }
    @{
        Success = $true; SourceCommit = $manifest.SourceCommit; Version = $manifest.Version
        NegativeRuntimeControl = 'failed as expected after runtime discovery roots were moved'
        Portable = 'WPF/JSON self-test passed'; Installed = 'identical payload and WPF/JSON self-test passed'
        SameVersionReinstall = 'passed'; UninstallRetainsData = 'passed'
        LockedPayloadUpdateAndUninstall = 'rejected as expected'
        OrdinaryStartup = 'NOT RUN'; CrossVersionUpgrade = 'NOT RUN'; NativeNetwork = 'NOT RUN'
        Environment = 'disposable hosted Windows, global .NET discovery roots temporarily unavailable; not a pristine client-OS claim'
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $results 'result.json') -Encoding utf8
} finally {
    foreach ($move in ($moves | Sort-Object { $_.Original.Length } -Descending)) {
        if (Test-Path -LiteralPath $move.Backup) { Move-Item -LiteralPath $move.Backup -Destination $move.Original }
    }
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process') }
    $env:DOTNET_DISABLE_GUI_ERRORS = $savedGui
}
Write-Output 'PASS: runtime negative control, both self-contained packages, installer/reinstall/uninstall data retention; no native network actions'
