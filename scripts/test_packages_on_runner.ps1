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
$checks = [Collections.Generic.List[object]]::new()
function Invoke-Checked([string]$Executable, [string[]]$Arguments, [bool]$MustSucceed = $true, [string]$Check = 'Process') {
    Write-Host "CHECK: $Check ($([IO.Path]::GetFileName($Executable)))"
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false; $start.CreateNoWindow = $true
    $start.RedirectStandardError = $true; $start.RedirectStandardOutput = $true
    foreach ($value in $Arguments) { $start.ArgumentList.Add($value) }
    $process = [Diagnostics.Process]::Start($start)
    $stderr = $process.StandardError.ReadToEndAsync(); $stdout = $process.StandardOutput.ReadToEndAsync()
    try {
        if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'Package check timed out' }
        $errorText = $stderr.GetAwaiter().GetResult(); $null = $stdout.GetAwaiter().GetResult()
        $checks.Add(@{ Check = $Check; ExitCode = $process.ExitCode; MustSucceed = $MustSucceed })
        $checks | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $results 'process-checks.json') -Encoding utf8
        if ($MustSucceed -and $process.ExitCode -ne 0) { throw "$Check failed ($([IO.Path]::GetFileName($Executable))) with exit $($process.ExitCode)" }
        if (-not $MustSucceed) {
            if ($process.ExitCode -eq 0) { throw 'Expected rejection did not occur' }
            return $errorText
        }
    } finally { $process.Dispose() }
}
function Test-OrdinaryStartup([string]$Executable) {
    # Disposable runner only: normal startup reads local adapter facts, but no UI command is invoked.
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false; $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $process = [Diagnostics.Process]::Start($start)
    try {
        if (-not $process.WaitForInputIdle(30000)) { throw 'Ordinary startup did not reach an idle UI' }
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        do {
            if ($process.HasExited) { throw "Ordinary startup exited: $($process.ExitCode)" }
            $process.Refresh()
            if ($process.MainWindowHandle -ne [IntPtr]::Zero) { break }
            Start-Sleep -Milliseconds 200
        } while ([DateTime]::UtcNow -lt $deadline)
        if ($process.MainWindowHandle -eq [IntPtr]::Zero -or $process.MainWindowTitle -notlike '*AV Network Toolkit*') {
            throw 'Ordinary startup did not expose the expected main window'
        }
    } finally {
        # Never drive Apply/Run/Open or synthesize clicks. Terminate only this smoke process.
        if (-not $process.HasExited) { $process.Kill($true); $null = $process.WaitForExit(10000) }
        $process.Dispose()
    }
}
$control = Join-Path $results 'control'
& dotnet publish (Join-Path $PSScriptRoot 'RuntimeControl/RuntimeControl.csproj') -c Release -r win-x64 --self-contained false -o $control
if ($LASTEXITCODE -ne 0) { throw 'Runtime negative-control build failed' }
Invoke-Checked (Join-Path $control 'RuntimeControl.exe') @() $true 'RuntimeAvailable'
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
$storageFixture = Join-Path $results 'storage-fixture'
# Use the real resolver on synthetic runner data before removing runtime discovery.
# This avoids accepting a migration dialog or forging a completion marker during ordinary startup.
& dotnet run --project (Join-Path $PSScriptRoot 'ProjectUiSmoke/ProjectUiSmoke.csproj') -c Release -- $storageFixture --prepare-package-storage
if ($LASTEXITCODE -ne 0) { throw 'Cannot prepare isolated home-storage fixtures' }
$retainedStorage = @(Get-Content -LiteralPath (Join-Path $storageFixture 'retention-files.json') -Raw | ConvertFrom-Json)
function Assert-StorageRetained {
    foreach ($entry in $retainedStorage) {
        if ((Get-FileHash -LiteralPath $entry.Path -Algorithm SHA256).Hash -ne $entry.Hash) { throw 'Package operation changed a storage fixture' }
    }
}
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
    $runtimeError = Invoke-Checked (Join-Path $control 'RuntimeControl.exe') @() $false 'RuntimeUnavailable'
    if ($runtimeError -notmatch '\.NET|hostfxr') { throw 'Negative control did not demonstrate unavailable runtime' }
    $zip = Join-Path $packages "AVNetworkToolkit-$($manifest.Version)-win-x64-portable.zip"
    $portable = Join-Path $results 'portable'
    Expand-Archive -LiteralPath $zip -DestinationPath $portable
    & (Join-Path $PSScriptRoot 'test_package_self_test.ps1') -Executable (Join-Path $portable 'NetworkProfileManager.exe') -ResultsDirectory (Join-Path $results 'portable-tests')
    Test-OrdinaryStartup (Join-Path $portable 'NetworkProfileManager.exe')
    $setup = Join-Path $packages "AVNetworkToolkit-$($manifest.Version)-win-x64-setup.exe"
    $setupArguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',"/DIR=$installPath")
    Invoke-Checked $setup ($setupArguments + "/LOG=$(Join-Path $results 'initial-install.log')") $true 'InitialInstall'
    $installed = Join-Path $installPath 'NetworkProfileManager.exe'
    foreach ($payload in Get-ChildItem -LiteralPath $portable -File) {
        if ((Get-FileHash -LiteralPath $payload.FullName -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath (Join-Path $installPath $payload.Name) -Algorithm SHA256).Hash) { throw 'Installed payload differs from ZIP' }
    }
    & (Join-Path $PSScriptRoot 'test_package_self_test.ps1') -Executable $installed -ResultsDirectory (Join-Path $results 'installed-tests')
    Test-OrdinaryStartup $installed
    $locked = [IO.File]::Open($installed, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $null = Invoke-Checked $setup ($setupArguments + "/LOG=$(Join-Path $results 'locked-update.log')") $false 'LockedUpdate'
        $null = Invoke-Checked (Join-Path $installPath 'unins000.exe') @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/LOG=$(Join-Path $results 'locked-uninstall.log')") $false 'LockedUninstall'
        if (-not (Test-Path -LiteralPath $installed)) { throw 'Blocked uninstall removed the executable' }
    } finally { $locked.Dispose() }
    # Same-version reinstall is checked explicitly; this is not a cross-version upgrade claim.
    Invoke-Checked $setup ($setupArguments + "/LOG=$(Join-Path $results 'same-version-reinstall.log')") $true 'SameVersionReinstall'
    Assert-StorageRetained
    if ((Get-FileHash -LiteralPath $sentinel -Algorithm SHA256).Hash -ne $sentinelHash) { throw 'Reinstall changed original user data' }
    Invoke-Checked (Join-Path $installPath 'unins000.exe') @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/LOG=$(Join-Path $results 'uninstall.log')") $true 'Uninstall'
    Assert-StorageRetained
    if (Test-Path -LiteralPath $installed) { throw 'Uninstall left the program executable' }
    if ((Get-FileHash -LiteralPath $sentinel -Algorithm SHA256).Hash -ne $sentinelHash) { throw 'Uninstall changed original user data' }
    @{
        Success = $true; SourceCommit = $manifest.SourceCommit; Version = $manifest.Version
        NegativeRuntimeControl = 'failed as expected after runtime discovery roots were moved'
        Portable = 'WPF/JSON self-test passed'; Installed = 'identical payload and WPF/JSON self-test passed'
        SameVersionReinstall = 'passed'; UninstallRetainsData = 'passed'
        UserStorage = 'legacy source, home data, recovery and separate migration backup hashes retained'
        LockedPayloadUpdateAndUninstall = 'rejected as expected'
        OrdinaryStartup = 'both main window handles/titles observed; smoke processes terminated without invoking commands'
        CrossVersionUpgrade = 'NOT RUN'; NativeNetwork = 'NOT RUN'
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
