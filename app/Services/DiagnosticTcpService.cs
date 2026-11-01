using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public interface IDiagnosticTcpConnector
{
    Task<DiagnosticTcpObservation> ConnectAsync(IPAddress source, IPAddress destination, int port, CancellationToken cancellation);
}

public sealed class DiagnosticTcpService(IDiagnosticPingPlatform platform, IDiagnosticTcpConnector connector)
{
    public async Task<DiagnosticTcpResult> CheckAsync(DiagnosticPingRequest request, int port, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var requestedCancellation = cancellation;
        cancellation = timeout.Token;
        string destination = "";
        PredictedRoute? route = null;
        DiagnosticTcpResult Fail(string code) => new(request, port, destination, route, null, code);
        try
        {
            cancellation.ThrowIfCancellationRequested();
            if (port is < 1 or > 65535 || request.AdapterId == Guid.Empty || request.Index <= 0
                || !DiagnosticPingService.IsUnicast(request.Source, out var source)
                || !DiagnosticPingService.ValidTarget(request.Target)) return Fail("Input");
            if (!DiagnosticPingService.Matches(platform.ReadBindings(), request)) return Fail("Changed");
            var addresses = IPAddress.TryParse(request.Target, out var literal) ? new[] { literal }
                : await platform.ResolveAsync(request.Target, cancellation).ConfigureAwait(false);
            var target = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork
                && DiagnosticPingService.IsUnicast(a.ToString(), out _));
            if (target == null) return Fail("Resolve");
            destination = target.ToString();
            cancellation.ThrowIfCancellationRequested();
            if (!DiagnosticPingService.Matches(platform.ReadBindings(), request)) return Fail("Changed");
            route = platform.Predict(source!, target);
            if (route == null || route.Index != request.Index) return Fail("Route");
            cancellation.ThrowIfCancellationRequested();
            var observation = await connector.ConnectAsync(source!, target, port, cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            if (!DiagnosticPingService.Matches(platform.ReadBindings(), request) || platform.Predict(source!, target) != route)
                return new(request, port, destination, route, observation, "Changed");
            return new(request, port, destination, route, observation, "");
        }
        catch (OperationCanceledException) when (requestedCancellation.IsCancellationRequested) { return Fail("Cancelled"); }
        catch (OperationCanceledException) { return Fail("Timeout"); }
        catch { return Fail("Unavailable"); }
    }
}

// One source-bound IPv4 connection, no payload and no endpoint/interface retry.
public sealed class WindowsDiagnosticTcpConnector : IDiagnosticTcpConnector
{
    public async Task<DiagnosticTcpObservation> ConnectAsync(IPAddress source, IPAddress destination, int port,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(source, 0));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        var watch = Stopwatch.StartNew();
        string status;
        try
        {
            await socket.ConnectAsync(new IPEndPoint(destination, port), timeout.Token).ConfigureAwait(false);
            status = "Accepted";
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { status = "Timeout"; }
        catch (SocketException error) when (StatusFor(error.SocketErrorCode) != null) { status = StatusFor(error.SocketErrorCode)!; }
        cancellation.ThrowIfCancellationRequested();
        return new(status, watch.ElapsedMilliseconds);
    }
    internal static string? StatusFor(SocketError error) => error switch
    {
        SocketError.ConnectionRefused => "Refused",
        SocketError.TimedOut => "Timeout",
        SocketError.HostUnreachable or SocketError.NetworkUnreachable => "Unreachable",
        _ => null
    };
}
