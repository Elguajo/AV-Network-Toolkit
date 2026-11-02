using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static void CheckApplicationUpdates(string output)
    {
        using var rsa = RSA.Create(3072);
        var key = rsa.ExportSubjectPublicKeyInfoPem();
        foreach (var theme in new[] { "Dark", "Light" })
        foreach (var minimum in new[] { false, true })
        {
            ApplyRenderTheme(theme);
            var root = Path.Combine(output, "update-fixture-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            var transport = new UpdateSurface(rsa);
            using var vm = new ApplicationUpdateViewModel(new(transport, key, root), new(Path.Combine(root, "preferences.json")),
                key, false, () => root, _ => Task.FromResult(false), () => throw new Exception("Real app exit forbidden"), _ => true);
            var window = new ApplicationUpdateWindow(vm);
            if (minimum) { window.Width = window.MinWidth; window.Height = window.MinHeight; }
            window.Show(); Pump();
            var suffix = $"{theme}-{(minimum ? "minimum" : "normal")}.png";
            Capture(window, Path.Combine(output, "update-ready-" + suffix));
            UpdateWait(vm.CheckAsync()); Pump();
            if (!vm.CanDownload || !vm.Details.Contains("1.2.0")) throw new Exception("Signed update not offered.");
            Capture(window, Path.Combine(output, "update-available-" + suffix));
            UpdateWait(vm.DownloadAsync()); Pump();
            if (!vm.CanInstall || vm.Progress != 100) throw new Exception("Verified download state missing.");
            Capture(window, Path.Combine(output, "update-downloaded-" + suffix));
            // Closing an active operation cancels it and preserves the surface until it has drained.
            transport.Hold = true;
            var pending = vm.CheckAsync(); Pump(); window.Close(); Pump();
            if (!window.IsVisible) throw new Exception("Busy update window closed before cancellation drained.");
            UpdateWait(pending); Pump();
            Capture(window, Path.Combine(output, "update-canceled-" + suffix));
            var details = Descendants(window).OfType<TextBox>().Single(t => t.IsReadOnly);
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(details))) throw new Exception("Update details lack an accessible name.");
            window.Close(); Pump();
            using var disabled = new ApplicationUpdateViewModel(new(transport, "", root), new(Path.Combine(root, "preferences.json")),
                null, true, () => null, _ => Task.FromResult(false), () => throw new Exception("Exit forbidden"), _ => false);
            var noKey = new ApplicationUpdateWindow(disabled);
            if (minimum) { noKey.Width = noKey.MinWidth; noKey.Height = noKey.MinHeight; }
            noKey.Show(); Pump(); Capture(noKey, Path.Combine(output, "update-no-key-" + suffix)); noKey.Close(); Pump();
        }
    }
    private static void UpdateWait(Task task)
    {
        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (!task.IsCompleted) { if (DateTime.UtcNow > timeout) throw new Exception("Fake update did not drain."); Pump(); Thread.Sleep(1); }
        task.GetAwaiter().GetResult();
    }
    private sealed class UpdateSurface : IApplicationUpdateTransport
    {
        private readonly byte[] _manifest, _signature, _release;
        internal bool Hold;
        internal UpdateSurface(RSA rsa)
        {
            var hash = Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 3 }));
            var m = new ApplicationUpdateManifest(1, "AV Network Toolkit", ApplicationUpdateTrust.Repository, "1.2.0", "Preview",
                new string('a', 40), "win-x64", 22000, 1,
                new("AVNetworkToolkit-1.2.0-win-x64-setup.exe", 3, hash), new("AVNetworkToolkit-1.2.0-win-x64-portable.zip", 3, hash),
                ApplicationUpdateTrust.PayloadNames.Select(n => new UpdatePayloadFile(n, 3, hash)).ToArray());
            _manifest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(m));
            _signature = rsa.SignData(_manifest, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            _release = Encoding.UTF8.GetBytes(new JObject { ["id"] = 7, ["tag_name"] = "v1.2.0", ["draft"] = false, ["prerelease"] = true,
                ["target_commitish"] = m.SourceCommit, ["body"] = "Fixture Preview: a signed update with preserved profiles and device projects.\nReview Windows compatibility before installing.",
                ["assets"] = new JArray(new[] { m.Installer.Name, m.Portable.Name, "update-manifest.json", "update-manifest.sig" }
                    .Select((n, i) => new JObject { ["id"] = i + 1, ["name"] = n, ["size"] = 3, ["state"] = "uploaded" })) }.ToString());
        }
        public async Task<byte[]> ReadAsync(Uri url, int limit, CancellationToken cancellation)
        {
            if (Hold) await Task.Delay(Timeout.Infinite, cancellation);
            if (url.AbsolutePath.EndsWith("update-manifest.json")) return _manifest;
            if (url.AbsolutePath.EndsWith("update-manifest.sig")) return _signature;
            return url.AbsolutePath.EndsWith("/releases") ? Encoding.UTF8.GetBytes("[" + Encoding.UTF8.GetString(_release) + "]") : _release;
        }
        public Task DownloadAsync(Uri url, string path, long size, IProgress<double>? progress, CancellationToken cancellation)
        { cancellation.ThrowIfCancellationRequested(); File.WriteAllBytes(path, new byte[] { 1, 2, 3 }); return Task.CompletedTask; }
    }
}
