using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkProfileManager.Services;

internal interface IApplicationUpdateTransport
{
    Task<byte[]> ReadAsync(Uri url, int limit, CancellationToken cancellation);
    Task DownloadAsync(Uri url, string path, long size, IProgress<double>? progress, CancellationToken cancellation);
}

internal sealed class ApplicationUpdateTransport : IApplicationUpdateTransport, IDisposable
{
    private readonly HttpClient _http;
    private DateTimeOffset _retryAfter;
    internal ApplicationUpdateTransport() : this(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { }
    internal ApplicationUpdateTransport(HttpMessageHandler handler)
    {
        _http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("AVNetworkToolkit-Updater/1.0");
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    }
    internal static bool Allowed(Uri url) => url.IsAbsoluteUri && url.Scheme == "https" && url.IsDefaultPort
        && url.UserInfo.Length == 0 && url.Fragment.Length == 0
        && url.Host is "api.github.com" or "github.com" or "release-assets.githubusercontent.com";
    private async Task<HttpResponseMessage> OpenAsync(Uri url, CancellationToken cancellation)
    {
        if (DateTimeOffset.UtcNow < _retryAfter) throw new IOException("GitHub requested a pause. Try again after the rate-limit cooldown.");
        for (int redirects = 0; redirects <= 5; redirects++)
        {
            if (!Allowed(url)) throw new InvalidDataException("Update download origin rejected.");
            var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                var location = response.Headers.Location; response.Dispose();
                if (location == null) throw new IOException("Invalid update redirect.");
                url = new Uri(url, location); continue;
            }
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
            {
                var now = DateTimeOffset.UtcNow;
                _retryAfter = response.Headers.RetryAfter?.Date ?? now.Add(response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(15));
                if (response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
                    && long.TryParse(System.Linq.Enumerable.FirstOrDefault(values), out var reset) && reset > now.ToUnixTimeSeconds() && reset < now.AddDays(1).ToUnixTimeSeconds())
                    _retryAfter = DateTimeOffset.FromUnixTimeSeconds(reset).AddSeconds(1);
                if (_retryAfter <= now) _retryAfter = now.AddMinutes(1);
                response.Dispose(); throw new IOException("GitHub rate limit or access restriction. Try again later.");
            }
            try { response.EnsureSuccessStatusCode(); return response; }
            catch { response.Dispose(); throw; }
        }
        throw new IOException("Too many update redirects.");
    }
    public async Task<byte[]> ReadAsync(Uri url, int limit, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var response = await OpenAsync(url, timeout.Token).ConfigureAwait(false);
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("Update metadata too large.");
        using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var output = new MemoryStream();
        await CopyAsync(input, output, limit, null, timeout.Token).ConfigureAwait(false); return output.ToArray();
    }
    public async Task DownloadAsync(Uri url, string path, long size, IProgress<double>? progress, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromMinutes(15));
        using var response = await OpenAsync(url, timeout.Token).ConfigureAwait(false);
        if (response.Content.Headers.ContentLength is long length && length != size) throw new InvalidDataException("Update length changed.");
        using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true);
        await CopyAsync(input, output, size, progress, timeout.Token).ConfigureAwait(false);
        if (output.Length != size) throw new InvalidDataException("Update download incomplete.");
        output.Flush(true);
    }
    private static async Task CopyAsync(Stream input, Stream output, long limit, IProgress<double>? progress, CancellationToken cancellation)
    {
        var buffer = new byte[65536]; long total = 0; int count;
        while ((count = await input.ReadAsync(buffer, cancellation).ConfigureAwait(false)) != 0)
        {
            total += count; if (total > limit) throw new InvalidDataException("Update exceeds declared size.");
            await output.WriteAsync(buffer.AsMemory(0, count), cancellation).ConfigureAwait(false);
            progress?.Report(100d * total / Math.Max(1, limit));
        }
    }
    public void Dispose() => _http.Dispose();
}
