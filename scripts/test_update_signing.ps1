[CmdletBinding()]
param([Parameter(Mandatory)][string]$ResultsDirectory)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($ResultsDirectory)
if (Test-Path -LiteralPath $root) { throw 'Use a fresh signing-test results directory' }
New-Item -ItemType Directory -Path $root | Out-Null
$rsa = [Security.Cryptography.RSA]::Create(3072)
$wrong = [Security.Cryptography.RSA]::Create(3072)
try {
    # Disposable test keys stay in ignored local results, never in a release or repository.
    $privatePath = Join-Path $root 'disposable-test-private.pem'
    $publicPath = Join-Path $root 'disposable-test-public.pem'
    $wrongPath = Join-Path $root 'wrong-test-private.pem'
    [IO.File]::WriteAllText($privatePath, $rsa.ExportPkcs8PrivateKeyPem())
    [IO.File]::WriteAllText($publicPath, $rsa.ExportSubjectPublicKeyInfoPem())
    [IO.File]::WriteAllText($wrongPath, $wrong.ExportPkcs8PrivateKeyPem())
    $sha = 'a' * 40
    $fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($rsa.ExportSubjectPublicKeyInfo()))
    function Fixture([string]$name) {
        $directory = Join-Path $root $name
        New-Item -ItemType Directory -Path (Join-Path $directory 'payload') | Out-Null
        foreach ($file in @('NetworkProfileManager.exe','D3DCompiler_47_cor3.dll','PenImc_cor3.dll','PresentationNative_cor3.dll','vcruntime140_cor3.dll','wpfgfx_cor3.dll','LICENSE','THIRD_PARTY_NOTICES.md')) {
            [IO.File]::WriteAllBytes((Join-Path $directory "payload/$file"), [byte[]]@(1,2,3))
        }
        $files = @('AVNetworkToolkit-1.2.0-win-x64-setup.exe','AVNetworkToolkit-1.2.0-win-x64-portable.zip') | ForEach-Object {
            $path = Join-Path $directory $_
            [IO.File]::WriteAllBytes($path, [byte[]]@(1,2,3))
            @{ Name = $_; Sha256 = (Get-FileHash -LiteralPath $path).Hash }
        }
        @{ Version = '1.2.0'; SourceCommit = $sha; Files = @($files) } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $directory 'package-manifest.json')
        @{ Success = $true; ReleaseChannel = 'Preview'; UpdatePublicKeySha256 = $fingerprint; Version = '1.2.0.0'; InformationalVersion = "1.2.0+$sha" } | ConvertTo-Json | Set-Content (Join-Path $directory 'payload-self-test.json')
        return $directory
    }
    $valid = Fixture 'valid'
    & "$PSScriptRoot/sign_update.ps1" -PackageDirectory $valid -PrivateKeyFile $privatePath
    $bytes = [IO.File]::ReadAllBytes((Join-Path $valid 'update-manifest.json'))
    $signature = [IO.File]::ReadAllBytes((Join-Path $valid 'update-manifest.sig'))
    if ($signature.Length -ne 384 -or -not $rsa.VerifyData($bytes, $signature, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)) { throw 'Signed fixture rejected' }
    $manifest = [Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
    if ($manifest.Payload.Count -ne 8 -or $manifest.SourceCommit -ne $sha -or $manifest.Channel -ne 'Preview') { throw 'Signing contract mismatch' }
    $passed = 1
    foreach ($case in @('wrong-key','tampered-package','wrong-channel','existing-output')) {
        $directory = if ($case -eq 'existing-output') { $valid } else { Fixture $case }
        if ($case -eq 'tampered-package') { [IO.File]::WriteAllBytes((Join-Path $directory 'AVNetworkToolkit-1.2.0-win-x64-setup.exe'), [byte[]]@(9,9,9)) }
        $arguments = @{ PackageDirectory = $directory; PrivateKeyFile = $(if ($case -eq 'wrong-key') { $wrongPath } else { $privatePath }) }
        if ($case -eq 'wrong-channel') { $arguments.Channel = 'Stable' }
        $rejected = $false
        try { & "$PSScriptRoot/sign_update.ps1" @arguments | Out-Null } catch { $rejected = $true }
        if (-not $rejected) { throw "Signing negative case was accepted: $case" }
        $passed++
    }
    @{ Success = $true; Cases = $passed; ProductionKeys = $false; Network = $false; Installation = $false } | ConvertTo-Json | Set-Content (Join-Path $root 'result.json')
    Write-Output "PASS: $passed signing-tool cases; disposable fixtures only"
} finally {
    $rsa.Dispose(); $wrong.Dispose()
    # Remove only the three exact disposable key files created by this test.
    foreach ($path in @($privatePath,$publicPath,$wrongPath)) { if ($path -and (Test-Path -LiteralPath $path)) { Remove-Item -LiteralPath $path } }
}
