using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public interface IDiagnosticPingPlatform
{
    IReadOnlyList<PingBinding> ReadBindings();
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellation);
    PredictedRoute? Predict(IPAddress source, IPAddress destination);
    Task<DiagnosticEcho> EchoAsync(IPAddress source, IPAddress destination, CancellationToken cancellation);
}

// Each result owns an immutable request. No UI selection, NIC mutation or fallback here.
public sealed class DiagnosticPingService(IDiagnosticPingPlatform platform)
{
    public async Task<DiagnosticPingResult> SendAsync(DiagnosticPingRequest request, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var requestedCancellation = cancellation;
        cancellation = timeout.Token;
        string destination = "";
        PredictedRoute? route = null;
        DiagnosticPingResult Fail(string code) => new(request, destination, route, null, code);
        try
        {
            cancellation.ThrowIfCancellationRequested();
            if (request.AdapterId == Guid.Empty || request.Index <= 0 || !IsUnicast(request.Source, out var source)
                || !ValidTarget(request.Target)) return Fail("Input");
            if (!Matches(platform.ReadBindings(), request)) return Fail("Changed");
            // Literal targets bypass DNS entirely. Hostname lookup is OS DNS, not bound to the selected NIC.
            var addresses = IPAddress.TryParse(request.Target, out var literal)
                ? new[] { literal } : await platform.ResolveAsync(request.Target, cancellation).ConfigureAwait(false);
            var candidates = addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork
                && IsUnicast(a.ToString(), out _)).Distinct().ToArray();
            if (candidates.Length == 0) return Fail("Resolve");
            // One deterministic resolved destination; never retry a different endpoint/interface.
            var target = candidates[0]; destination = target.ToString();
            cancellation.ThrowIfCancellationRequested();
            if (!Matches(platform.ReadBindings(), request)) return Fail("Changed");
            route = platform.Predict(source!, target);
            if (route == null || route.Index != request.Index) return Fail("Route");
            cancellation.ThrowIfCancellationRequested();
            var echo = await platform.EchoAsync(source!, target, cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            if (!Matches(platform.ReadBindings(), request) || platform.Predict(source!, target) != route)
                return new(request, destination, route, echo, "Changed");
            return new(request, destination, route, echo, "");
        }
        catch (OperationCanceledException) when (requestedCancellation.IsCancellationRequested) { return Fail("Cancelled"); }
        catch { return Fail("Unavailable"); }
    }
    internal static bool Matches(IReadOnlyList<PingBinding> bindings, DiagnosticPingRequest request)
    {
        var selected = bindings.Where(b => b.Id == request.AdapterId).ToArray();
        return selected.Length == 1 && selected[0].Index == request.Index && selected[0].Connected
            && Mac(selected[0].Mac) == Mac(request.Mac) && Mac(request.Mac).Length != 0
            && selected[0].Sources.Contains(request.Source, StringComparer.Ordinal)
            && bindings.Count(b => b.Sources.Contains(request.Source, StringComparer.Ordinal)) == 1;
    }
    internal static string Mac(string value) => value.Replace("-", "").Replace(":", "").ToUpperInvariant();
    internal static bool ValidTarget(string target) => target.Length is > 0 and <= 253
        && (IsUnicast(target, out _) || (!IPAddress.TryParse(target, out _)
            && Uri.CheckHostName(target) == UriHostNameType.Dns && target.All(c => c < 128 && !char.IsWhiteSpace(c))));
    internal static bool IsUnicast(string value, out IPAddress? address)
    {
        address = null;
        if (!IPAddress.TryParse(value, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork
            || ip.ToString() != value) return false;
        var b = ip.GetAddressBytes();
        if (b[0] == 0 || b[0] == 127 || b[0] >= 224 || value == "255.255.255.255") return false;
        address = ip; return true;
    }
}
