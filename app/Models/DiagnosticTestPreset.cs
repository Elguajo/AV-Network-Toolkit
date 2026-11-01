using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Models;

public enum DiagnosticTestKind { PingFour, ContinuousPing, TcpPort, PingOnce }

public sealed record DiagnosticTestPreset(DiagnosticTestKind Kind)
{
    public string Name => Loc.Get($"NetworkTests.{Kind}");
    public string Description => Loc.Get($"NetworkTests.{Kind}.Description");
}

public sealed record DiagnosticTcpObservation(string Status, long Milliseconds);
public sealed record DiagnosticTcpResult(DiagnosticPingRequest Request, int Port, string Destination,
    PredictedRoute? Route, DiagnosticTcpObservation? Observation, string Error)
{
    public bool Success => Error.Length == 0 && Observation?.Status == "Accepted";
    public string Display => Loc.Format("Diagnostics.Request", Request.AdapterName, Request.AdapterId, Request.Source, Request.Target)
        + "\n" + Loc.Format("NetworkTests.TcpEndpoint", Destination.Length == 0 ? Request.Target : Destination, Port)
        + "\n" + (Route == null ? Loc.Get("Diagnostics.NoRoute") : Loc.Format("Diagnostics.Predicted", Route.Index,
            Route.Destination, Route.PrefixLength, Route.NextHop, Route.Metric))
        + (Observation == null ? "" : "\n" + Loc.Format("NetworkTests.TcpObserved",
            Loc.Get($"NetworkTests.Tcp.{Observation.Status}"), Observation.Milliseconds))
        + (Error.Length == 0 ? "" : "\n" + Loc.Get($"NetworkTests.Error.{Error}"));
}
