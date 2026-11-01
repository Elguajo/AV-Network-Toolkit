[CmdletBinding()]
param([Parameter(Mandatory)][string]$Compiler, [Parameter(Mandatory)][string]$ResultsDirectory)
$ErrorActionPreference = 'Stop'
$Compiler = (Resolve-Path -LiteralPath $Compiler).Path
$helper = Join-Path $PSScriptRoot 'install_inno_setup.ps1'
$verified = & $helper -CompilerPath $Compiler -ValidateOnly
if ($verified -ne $Compiler) { throw 'Pinned compiler was not reusable' }
$results = [IO.Path]::GetFullPath($ResultsDirectory)
if (Test-Path -LiteralPath $results) { throw 'Use a fresh tool-test directory' }
New-Item -ItemType Directory -Path $results | Out-Null
function Expect-Rejection([string]$Path, [string]$Message) {
    try { $null = & $helper -CompilerPath $Path -ValidateOnly } catch {
        if ($_.Exception.Message -notlike $Message) { throw }
        return
    }
    throw 'Untrusted/incomplete compiler was accepted'
}
Expect-Rejection (Join-Path $results 'missing/ISCC.exe') 'Pinned compiler is not installed*'
Expect-Rejection (Join-Path $results 'other.exe') 'Expected the pinned ISCC.exe compiler'
Copy-Item -LiteralPath $Compiler -Destination (Join-Path $results 'ISCC.exe')
Expect-Rejection (Join-Path $results 'ISCC.exe') 'Pinned compiler file mismatch: ISCmplr.dll'
Copy-Item -LiteralPath (Join-Path (Split-Path $Compiler) 'ISCmplr.dll') -Destination (Join-Path $results 'ISCmplr.dll')
[IO.File]::WriteAllText((Join-Path $results 'ISPP.dll'), 'intentionally corrupt test fixture')
Expect-Rejection (Join-Path $results 'ISCC.exe') 'Pinned compiler file mismatch: ISPP.dll'
Write-Output 'PASS: pinned compiler reuse and four missing/incorrect/tampered compiler rejections; real tool untouched'
