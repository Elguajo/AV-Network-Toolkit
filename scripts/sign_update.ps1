# Requires PowerShell 7+. Consumes an existing owner-managed RSA-3072 key; never generates or publishes keys.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackageDirectory,
    [Parameter(Mandatory)][string]$PrivateKeyFile,
    [ValidateSet('Preview','Stable')][string]$Channel = 'Preview'
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 or later is required' }
$directory = [IO.Path]::GetFullPath($PackageDirectory)
$manifestPath = Join-Path $directory 'update-manifest.json'
$signaturePath = Join-Path $directory 'update-manifest.sig'
if ((Test-Path -LiteralPath $manifestPath) -or (Test-Path -LiteralPath $signaturePath)) { throw 'Update metadata already exists; use a fresh candidate directory' }
$packageManifest = Get-Content -LiteralPath (Join-Path $directory 'package-manifest.json') -Raw | ConvertFrom-Json
$diagnostic = Get-Content -LiteralPath (Join-Path $directory 'payload-self-test.json') -Raw | ConvertFrom-Json
if (-not $diagnostic.Success -or $diagnostic.ReleaseChannel -ne $Channel -or -not $diagnostic.UpdatePublicKeySha256) {
    throw 'Build and self-test candidates with the approved public key and matching channel before signing'
}
$rsa = [Security.Cryptography.RSA]::Create()
try {
    $rsa.ImportFromPem([IO.File]::ReadAllText([IO.Path]::GetFullPath($PrivateKeyFile)))
    $fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($rsa.ExportSubjectPublicKeyInfo()))
    if ($rsa.KeySize -ne 3072 -or $fingerprint -ne $diagnostic.UpdatePublicKeySha256) { throw 'Signing key does not match the packaged public trust root' }
    $version = $packageManifest.Version
    if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$' -or $packageManifest.SourceCommit -notmatch '^[a-f0-9]{40}$' `
        -or $diagnostic.Version -ne "$version.0" -or $diagnostic.InformationalVersion -notlike "*+$($packageManifest.SourceCommit)") { throw 'Candidate source/version rejected' }
    function PackageRecord([string]$name) {
        $entry = @($packageManifest.Files | Where-Object { $_.Name -ceq $name })
        $path = Join-Path $directory $name
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        if ($entry.Count -ne 1 -or $entry[0].Sha256 -ne $hash) { throw 'Package checksum mismatch' }
        return [ordered]@{ Name = $name; Size = (Get-Item -LiteralPath $path).Length; Sha256 = $hash }
    }
    $names = @('NetworkProfileManager.exe','D3DCompiler_47_cor3.dll','PenImc_cor3.dll','PresentationNative_cor3.dll','vcruntime140_cor3.dll','wpfgfx_cor3.dll','LICENSE','THIRD_PARTY_NOTICES.md')
    $payloadDirectory = Join-Path $directory 'payload'
    if (@(Get-ChildItem -LiteralPath $payloadDirectory -File).Count -ne $names.Count -or @(Get-ChildItem -LiteralPath $payloadDirectory -Directory).Count -ne 0) { throw 'Unexpected payload inventory' }
    $payload = @($names | ForEach-Object {
        $path = Join-Path $payloadDirectory $_
        [ordered]@{ Name = $_; Size = (Get-Item -LiteralPath $path).Length; Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
    })
    $manifest = [ordered]@{
        SchemaVersion = 1; Product = 'AV Network Toolkit'; Repository = 'Elguajo/AV-Network-Toolkit'
        Version = $version; Channel = $Channel; SourceCommit = $packageManifest.SourceCommit
        RuntimeIdentifier = 'win-x64'; MinimumWindowsBuild = 22000; DataCompatibility = 1
        Installer = (PackageRecord "AVNetworkToolkit-$version-win-x64-setup.exe")
        Portable = (PackageRecord "AVNetworkToolkit-$version-win-x64-portable.zip")
        Payload = $payload
    }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($manifest | ConvertTo-Json -Depth 6))
    $signature = $rsa.SignData($bytes, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)
    if (-not $rsa.VerifyData($bytes, $signature, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)) { throw 'Signature verification failed' }
    $output = [IO.File]::Open($manifestPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $output.Write($bytes); $output.Flush($true) } finally { $output.Dispose() }
    $output = [IO.File]::Open($signaturePath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $output.Write($signature); $output.Flush($true) } finally { $output.Dispose() }
    Write-Output 'PASS: signed update manifest and detached signature created; nothing published'
} finally { $rsa.Dispose() }
