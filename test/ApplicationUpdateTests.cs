using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ApplicationUpdateTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AvUpdates-" + Guid.NewGuid().ToString("N"));
    private readonly RSA _rsa = RSA.Create(3072);
    private string Key => _rsa.ExportSubjectPublicKeyInfoPem();
    public ApplicationUpdateTests() => Directory.CreateDirectory(_root);
    public void Dispose() { _rsa.Dispose(); Directory.Delete(_root, true); }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static ApplicationUpdateManifest Manifest(string version = "1.2.0", string channel = "Preview") => new(1,
        "AV Network Toolkit", ApplicationUpdateTrust.Repository, version, channel, new string('a', 40), "win-x64", 22000, 1,
        new($"AVNetworkToolkit-{version}-win-x64-setup.exe", 3, Hash(new byte[] { 1, 2, 3 })),
        new($"AVNetworkToolkit-{version}-win-x64-portable.zip", 3, Hash(new byte[] { 1, 2, 3 })),
        ApplicationUpdateTrust.PayloadNames.Select(name => new UpdatePayloadFile(name, 3, Hash(new byte[] { 1, 2, 3 }))).ToArray());
    private ApplicationUpdateOffer Offer(ApplicationUpdateManifest? manifest = null)
    {
        manifest ??= Manifest(); var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(manifest));
        var offer = new ApplicationUpdateOffer(7, "v" + manifest.Version, "Fixture release notes", manifest, bytes,
            _rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "");
        return offer with { AssetIdentity = ApplicationUpdateService.AssetIdentity(Release(offer)) };
    }
    private static JObject Release(ApplicationUpdateOffer offer) => new()
    {
        ["id"] = offer.ReleaseId, ["tag_name"] = offer.Tag, ["draft"] = false, ["prerelease"] = offer.Manifest.Channel == "Preview",
        ["target_commitish"] = offer.Manifest.SourceCommit, ["body"] = offer.Notes,
        ["assets"] = new JArray(new[] { offer.Manifest.Installer.Name, offer.Manifest.Portable.Name, "update-manifest.json", "update-manifest.sig" }
            .Select((name, index) => new JObject { ["id"] = index + 1, ["name"] = name, ["state"] = "uploaded", ["size"] = 3 }))
    };
    private FakeTransport Transport(ApplicationUpdateOffer offer, JObject? release = null)
    {
        release ??= Release(offer);
        var transport = new FakeTransport();
        transport.Read = url => url.AbsolutePath.EndsWith("update-manifest.json") ? offer.ManifestBytes
            : url.AbsolutePath.EndsWith("update-manifest.sig") ? offer.Signature
            : Encoding.UTF8.GetBytes(url.AbsolutePath.EndsWith("/releases") ? new JArray(release).ToString() : release.ToString());
        return transport;
    }
    [Fact]
    public void SignatureAuthenticatesExactBytesAndWrongKeysNeverFallback()
    {
        var offer = Offer(); Assert.Equal("1.2.0", ApplicationUpdateTrust.Verify(offer.ManifestBytes, offer.Signature, Key).Version);
        var changed = (byte[])offer.ManifestBytes.Clone(); changed[10] ^= 1;
        Assert.Throws<InvalidDataException>(() => ApplicationUpdateTrust.Verify(changed, offer.Signature, Key));
        using var wrong = RSA.Create(3072);
        Assert.Throws<InvalidDataException>(() => ApplicationUpdateTrust.Verify(offer.ManifestBytes, offer.Signature, wrong.ExportSubjectPublicKeyInfoPem()));
        Assert.Throws<InvalidDataException>(() => ApplicationUpdateTrust.Verify(offer.ManifestBytes, Array.Empty<byte>(), Key));
    }
    [Theory]
    [InlineData("SchemaVersion", "2")]
    [InlineData("Repository", "\"attacker/repository\"")]
    [InlineData("DataCompatibility", "2")]
    [InlineData("MinimumWindowsBuild", "19045")]
    [InlineData("RuntimeIdentifier", "\"linux-x64\"")]
    [InlineData("Channel", "\"Nightly\"")]
    public void ValidSignatureDoesNotPermitUnsupportedContracts(string field, string value)
    {
        var json = JObject.FromObject(Manifest()); json[field] = JToken.Parse(value);
        var bytes = Encoding.UTF8.GetBytes(json.ToString());
        var signature = _rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        Assert.Throws<InvalidDataException>(() => ApplicationUpdateTrust.Verify(bytes, signature, Key));
    }
    [Fact]
    public void DuplicateFieldsAndTrailingJsonAreRejectedEvenWithValidSignature()
    {
        foreach (var content in new[] { JsonConvert.SerializeObject(Manifest()).Replace("\"SchemaVersion\":1", "\"SchemaVersion\":1,\"SchemaVersion\":1"), JsonConvert.SerializeObject(Manifest()) + "{}" })
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            Assert.ThrowsAny<Exception>(() => ApplicationUpdateTrust.Verify(bytes, _rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), Key));
        }
    }
    [Theory]
    [InlineData("1.10.0", true)] [InlineData("1.1.6", true)] [InlineData("1.1.5", false)] [InlineData("1.0.99", false)]
    [InlineData("1.2.0-preview", false)] [InlineData("01.2.0", false)] [InlineData("1.2.0.0", false)] [InlineData("../../1.2.0", false)]
    public void NumericVersionPolicyRejectsSameVersionDowngradeAndMalformedTags(string version, bool newer) =>
        Assert.Equal(newer, ApplicationUpdateTrust.IsNewer(version, new Version(1, 1, 5, 0)));
    [Fact]
    public async Task PreviewDiscoveryUsesListAndStableChannelDoesNotOfferPreview()
    {
        var offer = Offer(); var transport = Transport(offer);
        var service = new ApplicationUpdateService(transport, Key, _root);
        Assert.Null(await service.CheckAsync(new(1, 1, 5), false, 26100, default));
        Assert.NotNull(await service.CheckAsync(new(1, 1, 5), true, 26100, default));
        Assert.DoesNotContain(transport.Urls, u => u.AbsolutePath.EndsWith("/latest"));
    }
    [Fact]
    public async Task DiscoveryRejectsMissingSignatureAndMismatchedSourceAndDigest()
    {
        var offer = Offer();
        foreach (var kind in new[] { "signature", "source", "digest" })
        {
            var release = Release(offer);
            if (kind == "signature") ((JArray)release["assets"]!).Last!.Remove();
            if (kind == "source") release["target_commitish"] = new string('b', 40);
            if (kind == "digest") release["assets"]![0]!["digest"] = "sha256:" + new string('0', 64);
            await Assert.ThrowsAsync<InvalidDataException>(() => new ApplicationUpdateService(Transport(offer, release), Key, _root).CheckAsync(new(1, 1, 5), true, 26100, default));
        }
    }
    [Fact]
    public async Task IncompletePaginationNeverClaimsUpToDate()
    {
        var offer = Offer();
        var page = Encoding.UTF8.GetBytes(new JArray(Enumerable.Range(0, 100).Select(_ => Release(offer))).ToString());
        var t = new FakeTransport { Read = _ => page };
        await Assert.ThrowsAsync<IOException>(() => new ApplicationUpdateService(t, Key, _root).CheckAsync(new(1, 1, 5), true, 26100, default));
        Assert.Equal(10, t.Urls.Count);
    }
    [Fact]
    public async Task DownloadRechecksReleaseAndRejectsTamperingAndCancellation()
    {
        var offer = Offer(); var t = Transport(offer); var s = new ApplicationUpdateService(t, Key, _root);
        var result = await s.DownloadAsync(offer, true, null, default);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(result.PackagePath));
        t.Download = (_, path, _, token) => { File.WriteAllBytes(path, new byte[] { 1, 2, 4 }); return Task.CompletedTask; };
        await Assert.ThrowsAsync<InvalidDataException>(() => s.DownloadAsync(offer, true, null, default));
        Assert.Empty(Directory.GetFiles(_root, "*.partial", SearchOption.AllDirectories));
        t.Download = (_, _, _, _) => throw new OperationCanceledException();
        await Assert.ThrowsAsync<OperationCanceledException>(() => s.DownloadAsync(offer, true, null, default));
    }
    [Fact]
    public async Task ChangedReleaseDuringDownloadCannotBecomeInstallable()
    {
        var offer = Offer(); var t = Transport(offer); var read = t.Read;
        t.Read = url => url.AbsolutePath.EndsWith("/releases/7") ? Encoding.UTF8.GetBytes(Release(offer with { Manifest = offer.Manifest with { SourceCommit = new string('b', 40) } }).ToString()) : read(url);
        await Assert.ThrowsAsync<InvalidDataException>(() => new ApplicationUpdateService(t, Key, _root).DownloadAsync(offer, true, null, default));
        Assert.Empty(Directory.GetFiles(_root, "*.partial", SearchOption.AllDirectories));
    }
    [Fact]
    public async Task AssetReplacementIsRejectedEvenWhenSignedBytesStayTheSame()
    {
        var offer = Offer(); var t = Transport(offer); var read = t.Read;
        var replaced = Release(offer); replaced["assets"]![0]!["id"] = 999;
        t.Read = url => url.AbsolutePath.EndsWith("/releases/7") ? Encoding.UTF8.GetBytes(replaced.ToString()) : read(url);
        await Assert.ThrowsAsync<InvalidDataException>(() => new ApplicationUpdateService(t, Key, _root).DownloadAsync(offer, true, null, default));
    }
    private DownloadedApplicationUpdate Portable(string? maliciousEntry = null)
    {
        var package = Path.Combine(_root, Guid.NewGuid() + ".zip");
        using (var zip = ZipFile.Open(package, ZipArchiveMode.Create))
            foreach (var name in ApplicationUpdateTrust.PayloadNames)
            {
                using var output = zip.CreateEntry(name == "LICENSE" && maliciousEntry != null ? maliciousEntry : name).Open();
                output.Write(new byte[] { 1, 2, 3 });
            }
        var bytes = File.ReadAllBytes(package); var m = Manifest();
        m = m with { Portable = m.Portable with { Size = bytes.Length, Sha256 = Hash(bytes) } };
        return new(Offer(m), _root, package, false);
    }
    [Fact]
    public void PortableCreatesOnlyFreshFolderAndPreservesExistingVersionAndData()
    {
        var data = Path.Combine(_root, "user-project.json"); File.WriteAllText(data, "keep");
        var old = Path.Combine(_root, "old"); Directory.CreateDirectory(old); File.WriteAllText(Path.Combine(old, "NetworkProfileManager.exe"), "old version");
        var update = Portable(); var destination = Path.Combine(_root, "new");
        var exe = ApplicationUpdateService.ExtractPortable(update, destination, Key, default);
        Assert.True(File.Exists(exe)); Assert.Equal(8, Directory.GetFiles(destination).Length);
        Assert.Equal("old version", File.ReadAllText(Path.Combine(old, "NetworkProfileManager.exe"))); Assert.Equal("keep", File.ReadAllText(data));
        Assert.Throws<IOException>(() => ApplicationUpdateService.ExtractPortable(update, old, Key, default));
    }
    [Theory]
    [InlineData("../escape.txt")] [InlineData("LICENSE:evil")] [InlineData("C:/absolute")] [InlineData("networkprofilemanager.exe")]
    public void UnsafeZipNeverPublishesReadyFolder(string entry)
    {
        var destination = Path.Combine(_root, "new");
        Assert.Throws<InvalidDataException>(() => ApplicationUpdateService.ExtractPortable(Portable(entry), destination, Key, default));
        Assert.False(Directory.Exists(destination)); Assert.False(File.Exists(Path.Combine(_root, "escape.txt")));
    }
    [Fact]
    public void CanceledOrDamagedPortableNeverReplacesAnExistingDirectory()
    {
        var update = Portable(); var destination = Path.Combine(_root, "new");
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => ApplicationUpdateService.ExtractPortable(update, destination, Key, cancel.Token));
        Assert.False(Directory.Exists(destination));
        var m = update.Offer.Manifest;
        m = m with { Payload = m.Payload.Select(f => f.Name == "LICENSE" ? f with { Sha256 = new string('0', 64) } : f).ToArray() };
        var damaged = update with { Offer = Offer(m) };
        Assert.Throws<InvalidDataException>(() => ApplicationUpdateService.ExtractPortable(damaged, destination, Key, default));
        Assert.False(Directory.Exists(destination));
    }
    [Fact]
    public void StagedFileIsLockedAgainstPostVerificationReplacement()
    {
        var file = Path.Combine(_root, "package.exe"); File.WriteAllBytes(file, new byte[] { 1, 2, 3 });
        using var verified = ApplicationUpdateService.OpenVerified(file, Manifest().Installer);
        Assert.Throws<IOException>(() => File.WriteAllBytes(file, new byte[] { 4, 5, 6 }));
        Assert.Throws<IOException>(() => File.Delete(file));
    }
    [Fact]
    public void PreferencesPreserveCorruptionFutureVersionsAndConflicts()
    {
        var path = Path.Combine(_root, "preferences.json");
        foreach (var content in new[] { "broken", "[null]", "[{\"Version\":2}]" })
        {
            File.WriteAllText(path, content); var store = new ApplicationUpdatePreferences(path);
            Assert.True(store.IsBlocked); Assert.False(store.IsDue(DateTimeOffset.UtcNow));
            Assert.False(store.Save(new(1, true, true, null))); Assert.Equal(content, File.ReadAllText(path));
        }
        File.Delete(path); var first = new ApplicationUpdatePreferences(path); var stale = new ApplicationUpdatePreferences(path);
        Assert.False(first.IsDue(DateTimeOffset.UtcNow)); Assert.True(first.Save(new(1, true, true, null)));
        Assert.False(stale.Save(new(1, false, false, null))); Assert.True(stale.IsBlocked);
        Assert.True(new ApplicationUpdatePreferences(path).Current.AutomaticChecks);
    }
    [Fact]
    public void AutomaticCheckHonorsOptInAndDailyInterval()
    {
        var store = new ApplicationUpdatePreferences(Path.Combine(_root, "preferences.json")); var now = DateTimeOffset.UtcNow;
        Assert.False(store.IsDue(now)); Assert.True(store.Save(new(1, true, false, now)));
        Assert.False(store.IsDue(now.AddHours(23))); Assert.True(store.IsDue(now.AddHours(24)));
    }
    [Fact]
    public void UpdateBarrierRejectsBusyMutationAndRestoresNormalApplyAfterCancel()
    {
        using (var active = NetworkMutationLease.TryAcquire()) { Assert.NotNull(active); Assert.Null(NetworkMutationLease.TrySuspendForUpdate()); }
        using (var barrier = NetworkMutationLease.TrySuspendForUpdate())
        { Assert.NotNull(barrier); Assert.Null(NetworkMutationLease.TryAcquire()); Assert.Null(NetworkMutationLease.TrySuspendForUpdate()); }
        using var restored = NetworkMutationLease.TryAcquire(); Assert.NotNull(restored);
    }
    [Theory]
    [InlineData("https://github.com/Elguajo/AV-Network-Toolkit/releases", true)]
    [InlineData("https://release-assets.githubusercontent.com/file?signature=fixture", true)]
    [InlineData("http://github.com/file", false)] [InlineData("https://github.com.attacker.test/file", false)]
    [InlineData("https://user:pass@github.com/file", false)] [InlineData("https://github.com:8443/file", false)]
    [InlineData("file:///C:/file", false)]
    public void RedirectOriginsAreRestricted(string url, bool allowed) => Assert.Equal(allowed, ApplicationUpdateTransport.Allowed(new Uri(url)));
    [Fact]
    public async Task TransportBoundsStreamsAndRejectsRedirectToUntrustedHost()
    {
        using var http = new ApplicationUpdateTransport(new FakeHandler(_ => new(HttpStatusCode.Found) { Headers = { Location = new Uri("https://attacker.test/payload") } }));
        await Assert.ThrowsAsync<InvalidDataException>(() => http.ReadAsync(new Uri("https://github.com/file"), 10, default));
        using var bounded = new ApplicationUpdateTransport(new FakeHandler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[11]) }));
        await Assert.ThrowsAsync<InvalidDataException>(() => bounded.ReadAsync(new Uri("https://github.com/file"), 10, default));
    }
    [Fact]
    public async Task RateLimitCooldownPreventsAnImmediateRepeatRequest()
    {
        var requests = 0;
        using var http = new ApplicationUpdateTransport(new FakeHandler(_ => { requests++; return new(HttpStatusCode.TooManyRequests); }));
        await Assert.ThrowsAsync<IOException>(() => http.ReadAsync(new Uri("https://api.github.com/repos/fixture/releases"), 10, default));
        await Assert.ThrowsAsync<IOException>(() => http.ReadAsync(new Uri("https://api.github.com/repos/fixture/releases"), 10, default));
        Assert.Equal(1, requests);
    }
    [Fact]
    public async Task ViewModelSerializesCheckAndCancelsWithoutExitOrDownload()
    {
        var transport = new HeldTransport(); var store = new ApplicationUpdatePreferences(Path.Combine(_root, "preferences.json"));
        using var vm = new ApplicationUpdateViewModel(new(transport, Key, _root), store, Key, false, () => null,
            _ => Task.FromResult(false), () => throw new Exception("Exit forbidden"), _ => true);
        var first = vm.CheckAsync(); Assert.True(vm.IsBusy);
        await vm.CheckAsync(); Assert.Equal(1, transport.Requests);
        vm.Cancel(); await first;
        Assert.False(vm.IsBusy); Assert.False(vm.CanDownload); Assert.False(vm.CanInstall);
    }
    [Fact]
    public async Task MissingProductionKeyAndDisabledAutomaticChecksNeverContactNetwork()
    {
        var transport = new FakeTransport(); var store = new ApplicationUpdatePreferences(Path.Combine(_root, "preferences.json"));
        using var vm = new ApplicationUpdateViewModel(new(transport, "", _root), store, null, false, () => null, _ => Task.FromResult(true), () => throw new Exception("Exit forbidden"), _ => true);
        await vm.CheckAutomaticallyAsync(DateTimeOffset.UtcNow); await vm.CheckAsync();
        Assert.Empty(transport.Urls); Assert.False(vm.CanDownload); Assert.False(vm.CanInstall);
    }
    [Fact]
    public async Task WorkerMalformedArgumentsFailBeforeServicesOrInstaller()
    {
        Assert.Equal(2, await ApplicationUpdateHandoff.RunAsync(new[] { ApplicationUpdateHandoff.WorkerSwitch }));
        Assert.Equal(2, await ApplicationUpdateHandoff.RunAsync(new[] { ApplicationUpdateHandoff.WorkerSwitch, "bad", "1", "1" }));
    }
    private sealed class FakeTransport : IApplicationUpdateTransport
    {
        internal readonly List<Uri> Urls = new();
        internal Func<Uri, byte[]> Read = _ => throw new IOException("No fake response.");
        internal Func<Uri, string, long, CancellationToken, Task> Download = (_, path, _, token) => { token.ThrowIfCancellationRequested(); File.WriteAllBytes(path, new byte[] { 1, 2, 3 }); return Task.CompletedTask; };
        public Task<byte[]> ReadAsync(Uri url, int limit, CancellationToken cancellation) { cancellation.ThrowIfCancellationRequested(); Urls.Add(url); return Task.FromResult(Read(url)); }
        public Task DownloadAsync(Uri url, string path, long size, IProgress<double>? progress, CancellationToken cancellation) => Download(url, path, size, cancellation);
    }
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(reply(request)); }
    private sealed class HeldTransport : IApplicationUpdateTransport
    {
        internal int Requests;
        public async Task<byte[]> ReadAsync(Uri url, int limit, CancellationToken cancellation)
        { Requests++; await Task.Delay(Timeout.Infinite, cancellation); return Array.Empty<byte>(); }
        public Task DownloadAsync(Uri url, string path, long size, IProgress<double>? progress, CancellationToken cancellation) => throw new Exception("Download forbidden");
    }
}
