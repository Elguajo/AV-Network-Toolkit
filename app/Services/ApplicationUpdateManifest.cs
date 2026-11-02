using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetworkProfileManager.Services;

internal sealed record UpdatePayloadFile(string Name, long Size, string Sha256);
internal sealed record UpdatePackage(string Name, long Size, string Sha256);
internal sealed record ApplicationUpdateManifest(int SchemaVersion, string Product, string Repository,
    string Version, string Channel, string SourceCommit, string RuntimeIdentifier, int MinimumWindowsBuild,
    int DataCompatibility, UpdatePackage Installer, UpdatePackage Portable, UpdatePayloadFile[] Payload);
internal sealed record ApplicationUpdateOffer(long ReleaseId, string Tag, string Notes,
    ApplicationUpdateManifest Manifest, byte[] ManifestBytes, byte[] Signature, string AssetIdentity);
internal sealed record DownloadedApplicationUpdate(ApplicationUpdateOffer Offer, string Directory, string PackagePath, bool Installed);

internal static class ApplicationUpdateTrust
{
    internal const long MaximumPackageBytes = 512L * 1024 * 1024;
    internal const string Repository = "Elguajo/AV-Network-Toolkit";
    internal static readonly string[] PayloadNames = { "NetworkProfileManager.exe", "D3DCompiler_47_cor3.dll",
        "PenImc_cor3.dll", "PresentationNative_cor3.dll", "vcruntime140_cor3.dll", "wpfgfx_cor3.dll", "LICENSE", "THIRD_PARTY_NOTICES.md" };
    internal static Version CurrentVersion => typeof(App).Assembly.GetName().Version!;
    internal static string BuildChannel => typeof(App).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ReleaseChannel")?.Value ?? "Stable";
    internal static string? PublicKey
    {
        get
        {
            using var input = typeof(App).Assembly.GetManifestResourceStream("ApplicationUpdatePublicKey.pem");
            if (input == null) return null;
            using var reader = new StreamReader(input); return reader.ReadToEnd();
        }
    }
    internal static string? PublicKeyFingerprint
    {
        get
        {
            var key = PublicKey; if (key == null) return null;
            using var rsa = RSA.Create(); rsa.ImportFromPem(key);
            if (rsa.KeySize != 3072 || key.Contains("PRIVATE", StringComparison.Ordinal)) throw new InvalidDataException("A public RSA-3072 update key is required.");
            return Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()));
        }
    }
    internal static bool TryVersion(string text, out Version version)
    {
        version = new Version(0, 0, 0);
        return Regex.IsMatch(text, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$", RegexOptions.CultureInvariant)
            && System.Version.TryParse(text, out version!);
    }
    internal static bool IsNewer(string text, Version current) => TryVersion(text, out var next)
        && next > new Version(current.Major, current.Minor, Math.Max(0, current.Build));
    internal static bool HashValid(string value) => value != null && Regex.IsMatch(value, "^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant);
    internal static bool SafeName(string name) => !string.IsNullOrEmpty(name) && name.Length <= 120
        && Regex.IsMatch(name, "^[A-Za-z0-9][A-Za-z0-9_.-]*$", RegexOptions.CultureInvariant)
        && name != "." && name != "..";
    internal static JObject ReadObject(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > 2 * 1024 * 1024) throw new InvalidDataException("Update metadata size rejected.");
        using var input = new StringReader(new UTF8Encoding(false, true).GetString(bytes));
        using var reader = new JsonTextReader(input) { MaxDepth = 16, DateParseHandling = DateParseHandling.None };
        var obj = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        if (reader.Read()) throw new InvalidDataException("Trailing update metadata rejected.");
        return obj;
    }
    internal static ApplicationUpdateManifest Verify(byte[] bytes, byte[] signature, string publicKey)
    {
        if (bytes.Length == 0 || bytes.Length > 65536 || signature.Length != 384) throw new InvalidDataException("Update signature size rejected.");
        using var rsa = RSA.Create(); rsa.ImportFromPem(publicKey);
        if (rsa.KeySize != 3072 || !rsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
            throw new InvalidDataException("Update publisher signature rejected.");
        var manifest = ReadObject(bytes).ToObject<ApplicationUpdateManifest>(JsonSerializer.Create(new JsonSerializerSettings
        { MissingMemberHandling = MissingMemberHandling.Error })) ?? throw new InvalidDataException("Update manifest missing.");
        Validate(manifest); return manifest;
    }
    internal static void Validate(ApplicationUpdateManifest m)
    {
        if (m.SchemaVersion != 1 || m.Product != "AV Network Toolkit" || m.Repository != Repository
            || !TryVersion(m.Version, out _) || m.Channel is not ("Preview" or "Stable")
            || m.SourceCommit == null || !Regex.IsMatch(m.SourceCommit, "^[a-f0-9]{40}$", RegexOptions.CultureInvariant)
            || m.RuntimeIdentifier != "win-x64" || m.MinimumWindowsBuild < 22000 || m.DataCompatibility != 1)
            throw new InvalidDataException("Update compatibility metadata rejected.");
        ValidatePackage(m.Installer, $"AVNetworkToolkit-{m.Version}-win-x64-setup.exe");
        ValidatePackage(m.Portable, $"AVNetworkToolkit-{m.Version}-win-x64-portable.zip");
        if (m.Payload == null || m.Payload.Length != PayloadNames.Length
            || m.Payload.Any(f => f == null || !PayloadNames.Contains(f.Name, StringComparer.Ordinal)
                || f.Size < 0 || f.Size > MaximumPackageBytes || !HashValid(f.Sha256))
            || m.Payload.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != m.Payload.Length
            || m.Payload.Sum(f => f.Size) > 1024L * 1024 * 1024)
            throw new InvalidDataException("Update payload inventory rejected.");
    }
    private static void ValidatePackage(UpdatePackage? p, string expected)
    {
        if (p == null || p.Name != expected || !SafeName(p.Name) || p.Size <= 0 || p.Size > MaximumPackageBytes || !HashValid(p.Sha256))
            throw new InvalidDataException("Update package rejected.");
    }
    internal static void VerifyFile(Stream input, long size, string hash)
    {
        if (input.Length != size || !Convert.ToHexString(SHA256.HashData(input)).Equals(hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Downloaded update checksum or size rejected.");
        input.Position = 0;
    }
}
