[CmdletBinding()]
param([Parameter(Mandatory)][string]$Executable, [Parameter(Mandatory)][string]$ResultsDirectory)
$ErrorActionPreference = 'Stop'
$Executable = (Resolve-Path -LiteralPath $Executable).Path
$results = [IO.Path]::GetFullPath($ResultsDirectory)
if (Test-Path -LiteralPath $results) { throw 'Use a fresh self-test results directory' }
New-Item -ItemType Directory -Path $results | Out-Null
function Invoke-SelfTest([string[]]$Arguments, [int]$Expected) {
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false; $start.CreateNoWindow = $true
    foreach ($value in $Arguments) { $start.ArgumentList.Add($value) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Self-test timed out' }
        if ($process.ExitCode -ne $Expected) { throw "Unexpected self-test exit $($process.ExitCode), expected $Expected" }
    } finally { $process.Dispose() }
}
$report = Join-Path $results 'success.json'
Invoke-SelfTest @('--package-self-test',$report) 0
$data = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
if (-not $data.Success -or -not $data.WpfRendered -or -not $data.JsonValidated) { throw 'Self-test did not verify payload' }
$hash = (Get-FileHash -LiteralPath $report -Algorithm SHA256).Hash
Invoke-SelfTest @('--package-self-test',$report) 2
if ((Get-FileHash -LiteralPath $report -Algorithm SHA256).Hash -ne $hash) { throw 'Existing report was overwritten' }
Invoke-SelfTest @('--package-self-test','relative-report.json') 2
Invoke-SelfTest @('--package-self-test') 2
Invoke-SelfTest @('--package-self-test',(Join-Path $results 'extra.json'),'unexpected') 2
if (Test-Path -LiteralPath (Join-Path $results 'extra.json')) { throw 'Malformed invocation wrote a report' }
# Retired privilege boundary must reject before normal startup for every arity,
# without consuming a seemingly valid command file or overwriting its result path.
$request = Join-Path $results 'retired-worker-request.json'
$retiredResult = Join-Path $results 'retired-worker-result.txt'
[IO.File]::WriteAllText($request, '[["interface","ip","set","address","name=fixture","source=dhcp"]]')
[IO.File]::WriteAllText($retiredResult, 'preserve sentinel')
$requestHash = (Get-FileHash -LiteralPath $request -Algorithm SHA256).Hash
Invoke-SelfTest @('--run-netsh') 1
Invoke-SelfTest @('--run-netsh',$request) 1
Invoke-SelfTest @('--run-netsh',$request,$retiredResult) 1
Invoke-SelfTest @('--run-netsh',$request,$retiredResult,'unexpected') 1
if ((Get-FileHash -LiteralPath $request -Algorithm SHA256).Hash -ne $requestHash -or
    [IO.File]::ReadAllText($retiredResult) -ne 'preserve sentinel') { throw 'Retired worker touched its file arguments' }
Write-Output 'PASS: packaged WPF/JSON load, report create-only, three malformed diagnostic cases and four retired-worker rejections; no normal startup/network'
