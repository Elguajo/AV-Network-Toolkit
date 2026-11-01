[CmdletBinding()]
param([string]$ToolsDirectory = (Join-Path $env:LOCALAPPDATA 'AVNetworkToolkit.BuildTools'))
$ErrorActionPreference = 'Stop'
$version = '6.7.4'
$expectedHash = 'A549929319F9120716677C685535CA4671E4B937B5FDC87E2585F32D8EB30E8C'
$destination = [IO.Path]::GetFullPath((Join-Path $ToolsDirectory "InnoSetup-$version"))
$compiler = Join-Path $destination 'ISCC.exe'
if (Test-Path -LiteralPath $compiler) {
    if ((Get-Item -LiteralPath $compiler).VersionInfo.FileVersion -notlike "$version*") { throw 'Unexpected compiler version' }
    return $compiler
}
New-Item -ItemType Directory -Force -Path $ToolsDirectory | Out-Null
$download = Join-Path $ToolsDirectory "innosetup-$version.exe"
Invoke-WebRequest -Uri "https://github.com/jrsoftware/issrc/releases/download/is-6_7_4/innosetup-$version.exe" -OutFile $download
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $expectedHash) { throw 'Compiler installer hash mismatch' }
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
return $compiler
