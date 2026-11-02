using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace NetworkProfileManager.Services;

internal sealed class ApplicationUpdateService
{
    private const string Api = "https://api.github.com/repos/" + ApplicationUpdateTrust.Repository;
    private readonly IApplicationUpdateTransport _transport;
    private readonly string _key;
    private readonly string _cache;
    internal ApplicationUpdateService(IApplicationUpdateTransport transport, string key, string cache)
    { _transport = transport; _key = key; _cache = Path.GetFullPath(cache); }
    internal static string CacheRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetworkProfileManager", "updates");
    internal async Task<ApplicationUpdateOffer?> CheckAsync(Version current, bool preview, int windowsBuild, CancellationToken cancellation)
    {
        var candidates = new List<JObject>(); bool complete = false;
        for (int page = 1; page <= 10; page++)
        {
            var bytes = await _transport.ReadAsync(new Uri($"{Api}/releases?per_page=100&page={page}"), 1024 * 1024, cancellation);
            var wrapped = Encoding.UTF8.GetBytes("{\"releases\":" + new UTF8Encoding(false, true).GetString(bytes) + "}");
            var array = ApplicationUpdateTrust.ReadObject(wrapped)["releases"] as JArray ?? throw new InvalidDataException("Invalid release listing.");
            if (array.Count > 100) throw new InvalidDataException("Invalid release page.");
            foreach (var token in array)
            {
                if (token is not JObject release) throw new InvalidDataException("Invalid release entry.");
                var tag = (string?)release["tag_name"];
                if (release.Value<bool?>("draft") == false && (preview || release.Value<bool?>("prerelease") == false)
                    && tag != null && tag.StartsWith("v", StringComparison.Ordinal)
                    && ApplicationUpdateTrust.IsNewer(tag.Substring(1), current)) candidates.Add(release);
            }
            if (array.Count < 100) { complete = true; break; }
        }
        if (!complete) throw new IOException("Release listing incomplete. Check GitHub Releases manually.");
        // Do not silently fall through to an older candidate when newer metadata cannot be authenticated.
        foreach (var release in candidates.OrderByDescending(r => Version.Parse(((string)r["tag_name"]!).Substring(1))))
        {
            var offer = await AuthenticateAsync(release, cancellation);
            if (!preview && offer.Manifest.Channel == "Preview") throw new InvalidDataException("Release channel mismatch.");
            if (offer.Manifest.MinimumWindowsBuild <= windowsBuild) return offer;
        }
        return null;
    }
    private async Task<ApplicationUpdateOffer> AuthenticateAsync(JObject release, CancellationToken cancellation)
    {
        var id = release.Value<long?>("id") ?? 0;
        var tag = release.Value<string>("tag_name") ?? "";
        if (id <= 0 || !tag.StartsWith("v", StringComparison.Ordinal) || !ApplicationUpdateTrust.TryVersion(tag.Substring(1), out _)
            || release.Value<bool?>("draft") != false) throw new InvalidDataException("Release identity rejected.");
        RequireAsset(release, "update-manifest.json"); RequireAsset(release, "update-manifest.sig");
        var bytes = await _transport.ReadAsync(AssetUrl(tag, "update-manifest.json"), 65536, cancellation);
        var signature = await _transport.ReadAsync(AssetUrl(tag, "update-manifest.sig"), 384, cancellation);
        var manifest = ApplicationUpdateTrust.Verify(bytes, signature, _key);
        if (tag != "v" + manifest.Version || release.Value<string>("target_commitish") != manifest.SourceCommit
            || release.Value<bool?>("prerelease") != (manifest.Channel == "Preview")) throw new InvalidDataException("Signed release identity mismatch.");
        foreach (var p in new[] { manifest.Installer, manifest.Portable })
        {
            var asset = RequireAsset(release, p.Name);
            if (asset.Value<long?>("size") != p.Size || asset.Value<string>("state") != "uploaded"
                || asset.Value<long?>("id") is not > 0) throw new InvalidDataException("Release asset mismatch.");
            var digest = asset.Value<string>("digest");
            if (digest != null && !digest.Equals("sha256:" + p.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Release asset digest mismatch.");
        }
        return new(id, tag, release.Value<string>("body") ?? "", manifest, bytes, signature, AssetIdentity(release));
    }
    internal static string AssetIdentity(JObject release) => string.Join("|", ((JArray)release["assets"]!).OfType<JObject>()
        .OrderBy(a => a.Value<string>("name"), StringComparer.Ordinal)
        .Select(a => $"{a.Value<string>("name")}:{a.Value<long?>("id")}:{a.Value<long?>("size")}:{a.Value<string>("digest")}:{a.Value<string>("updated_at")}"));
    private static JObject RequireAsset(JObject release, string name)
    {
        var assets = release["assets"] as JArray ?? throw new InvalidDataException("Release assets missing.");
        var matches = assets.OfType<JObject>().Where(a => a.Value<string>("name") == name).ToArray();
        return matches.Length == 1 ? matches[0] : throw new InvalidDataException("Signed update assets unavailable. Use the manual release download.");
    }
    internal static Uri AssetUrl(string tag, string name)
    {
        if (!tag.StartsWith("v", StringComparison.Ordinal) || !ApplicationUpdateTrust.TryVersion(tag.Substring(1), out _)
            || !ApplicationUpdateTrust.SafeName(name)) throw new InvalidDataException("Release URL rejected.");
        return new Uri($"https://github.com/{ApplicationUpdateTrust.Repository}/releases/download/{tag}/{name}");
    }
    internal async Task<DownloadedApplicationUpdate> DownloadAsync(ApplicationUpdateOffer offer, bool installed,
        IProgress<double>? progress, CancellationToken cancellation)
    {
        var manifest = ApplicationUpdateTrust.Verify(offer.ManifestBytes, offer.Signature, _key);
        EnsureNoReparse(_cache); Directory.CreateDirectory(_cache); EnsureNoReparse(_cache);
        var job = Path.Combine(_cache, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(job);
        var package = installed ? manifest.Installer : manifest.Portable;
        var partial = Path.Combine(job, package.Name + ".partial");
        try
        {
            await _transport.DownloadAsync(AssetUrl(offer.Tag, package.Name), partial, package.Size, progress, cancellation);
            using (var file = OpenVerified(partial, package)) { }
            // Detect replacement/re-upload while downloading; bind release ID, source, signature and exact asset metadata.
            var freshBytes = await _transport.ReadAsync(new Uri($"{Api}/releases/{offer.ReleaseId}"), 1024 * 1024, cancellation);
            var fresh = await AuthenticateAsync(ApplicationUpdateTrust.ReadObject(freshBytes), cancellation);
            if (fresh.AssetIdentity != offer.AssetIdentity || !fresh.ManifestBytes.SequenceEqual(offer.ManifestBytes) || !fresh.Signature.SequenceEqual(offer.Signature))
                throw new InvalidDataException("The offered release changed. Check again before downloading.");
            cancellation.ThrowIfCancellationRequested();
            var path = Path.Combine(job, package.Name); File.Move(partial, path);
            File.WriteAllBytes(Path.Combine(job, "update-manifest.json"), offer.ManifestBytes);
            File.WriteAllBytes(Path.Combine(job, "update-manifest.sig"), offer.Signature);
            return new(offer, job, path, installed);
        }
        catch
        {
            // Only the exact incomplete file owned by this operation; never recursively clean user paths.
            if (File.Exists(partial)) File.Delete(partial);
            throw;
        }
    }
    internal static FileStream OpenVerified(string path, UpdatePackage package)
    {
        EnsureNoReparse(path);
        var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try { ApplicationUpdateTrust.VerifyFile(input, package.Size, package.Sha256); return input; }
        catch { input.Dispose(); throw; }
    }
    internal static void EnsureNoReparse(string path)
    {
        for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Update paths cannot use symbolic links or junctions.");
    }
    internal static string ExtractPortable(DownloadedApplicationUpdate update, string destination, string key, CancellationToken cancellation)
    {
        var m = ApplicationUpdateTrust.Verify(update.Offer.ManifestBytes, update.Offer.Signature, key);
        if (update.Installed) throw new InvalidOperationException("Portable package required.");
        destination = Path.GetFullPath(destination);
        var appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetworkProfileManager");
        foreach (var protectedRoot in new[] { AppContext.BaseDirectory, appDataRoot })
            if (destination.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(protectedRoot)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || destination.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(protectedRoot)), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Choose a destination outside the current application and shared data folders.");
        EnsureNoReparse(destination);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Choose a new version folder that does not exist.");
        // Only this fresh destination is written. Current binaries and shared data are never replaced.
        var staging = destination + ".partial-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging); EnsureNoReparse(staging);
        using var input = OpenVerified(update.PackagePath, m.Portable);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);
        if (archive.Entries.Count != m.Payload.Length) throw new InvalidDataException("Portable inventory mismatch.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            cancellation.ThrowIfCancellationRequested();
            var expected = m.Payload.SingleOrDefault(f => f.Name == entry.FullName);
            if (expected == null || !ApplicationUpdateTrust.SafeName(entry.FullName) || !seen.Add(entry.FullName)
                || entry.Length != expected.Size || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                throw new InvalidDataException("Unsafe portable entry rejected.");
            var target = Path.Combine(staging, expected.Name); EnsureNoReparse(target);
            using var source = entry.Open();
            using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[65536]; long written = 0; int count;
                while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                {
                    cancellation.ThrowIfCancellationRequested(); written += count;
                    if (written > expected.Size) throw new InvalidDataException("Portable entry too large.");
                    output.Write(buffer, 0, count);
                }
                output.Flush(true);
            }
            using var verify = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read);
            ApplicationUpdateTrust.VerifyFile(verify, expected.Size, expected.Sha256);
        }
        cancellation.ThrowIfCancellationRequested(); EnsureNoReparse(destination);
        Directory.Move(staging, destination);
        return Path.Combine(destination, "NetworkProfileManager.exe");
    }
    internal static bool IsInstalled(string executable)
    {
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{02E72C36-F6DD-4AB0-92EC-A66017AA227A}_is1");
        return MatchesInstall(executable, key?.GetValue("InstallLocation") as string);
    }
    internal static bool MatchesInstall(string executable, string? directory) => !string.IsNullOrWhiteSpace(directory)
        && Path.IsPathFullyQualified(directory) && Path.GetFileName(executable) == "NetworkProfileManager.exe"
        && Path.GetFullPath(executable).Equals(Path.Combine(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)), "NetworkProfileManager.exe"), StringComparison.OrdinalIgnoreCase);
}
