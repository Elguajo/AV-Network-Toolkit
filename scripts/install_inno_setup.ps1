[CmdletBinding()]
param(
    [string]$ToolsDirectory = (Join-Path $env:LOCALAPPDATA 'AVNetworkToolkit.BuildTools'),
    [string]$CompilerPath,
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$pin = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../packaging/inno-setup.json') -Raw | ConvertFrom-Json
$version = $pin.Version
$destination = [IO.Path]::GetFullPath((Join-Path $ToolsDirectory "InnoSetup-$version"))
$compiler = Join-Path $destination 'ISCC.exe'
if ($CompilerPath) { $compiler = [IO.Path]::GetFullPath($CompilerPath); $destination = [IO.Path]::GetDirectoryName($compiler); $ValidateOnly = $true }
if ([IO.Path]::GetFileName($compiler) -ne 'ISCC.exe') { throw 'Expected the pinned ISCC.exe compiler' }
function Assert-CompilerIntegrity {
    foreach ($file in $pin.CompilerFiles.PSObject.Properties) {
        $path = Join-Path $destination $file.Name
        if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Value) {
            throw "Pinned compiler file mismatch: $($file.Name)"
        }
    }
}
if (Test-Path -LiteralPath $compiler) {
    Assert-CompilerIntegrity
    return $compiler
}
if ($ValidateOnly) { throw 'Pinned compiler is not installed at the supplied path' }
New-Item -ItemType Directory -Force -Path $ToolsDirectory | Out-Null
$download = Join-Path $ToolsDirectory "innosetup-$version.exe"
Invoke-WebRequest -Uri $pin.Url -OutFile $download
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $pin.InstallerSha256) { throw 'Compiler installer hash mismatch' }
$start = [Diagnostics.ProcessStartInfo]::new($download)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
foreach ($argument in @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/NOICONS',"/DIR=$destination")) {
    $start.ArgumentList.Add($argument)
}
$process = [Diagnostics.Process]::Start($start)
try {
    if (-not $process.WaitForExit(120000)) { throw 'Compiler setup timed out' }
    if ($process.ExitCode -ne 0) { throw "Compiler setup exit $($process.ExitCode)" }
} finally { $process.Dispose() }
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Compiler setup did not produce ISCC.exe' }
Assert-CompilerIntegrity
return $compiler
