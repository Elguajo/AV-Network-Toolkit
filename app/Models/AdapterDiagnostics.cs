using System;
using System.Collections.Immutable;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Models;

public sealed record DiagnosticAddress(string Address, int PrefixLength, string State, bool SkipAsSource)
{
    public string Display => $"{Address}/{PrefixLength} · {State}";
}
public sealed record DiagnosticRoute(string Destination, string NextHop, int Metric);
public sealed record DiagnosticAdapter(Guid Id, int Index, string Name, string Description, string Mac,
    string Kind, bool? Physical, bool? HyperVUplink, string Link, string Speed, string Dhcp,
    ImmutableArray<DiagnosticAddress> Addresses, ImmutableArray<string> Dns, ImmutableArray<DiagnosticRoute> Routes)
{
    public string Display => $"{Name} · {Index} · {Id:D}";
    public string KindDisplay => Loc.Get($"Diagnostics.Kind.{Kind}");
}
public sealed record PingBinding(Guid Id, int Index, string Mac, bool Connected, ImmutableArray<string> Sources);
public sealed record DiagnosticPingRequest(Guid AdapterId, int Index, string Mac, string AdapterName, string Source, string Target);
public sealed record PredictedRoute(int Index, string Destination, int PrefixLength, string NextHop, uint Metric);
public sealed record DiagnosticEcho(uint Status, string ReplyAddress, long Milliseconds, int? Ttl = null)
{
    public string StatusDisplay => System.Enum.IsDefined(typeof(System.Net.NetworkInformation.IPStatus), (int)Status)
        ? ((System.Net.NetworkInformation.IPStatus)Status).ToString() : Status.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
public sealed record DiagnosticPingResult(DiagnosticPingRequest Request, string Destination, PredictedRoute? Route,
    DiagnosticEcho? Echo, string Error)
{
    public bool Success => Error.Length == 0 && Echo?.Status == 0;
    public string Display => Loc.Format("Diagnostics.Request", Request.AdapterName, Request.AdapterId, Request.Source, Request.Target)
        + "\n" + (Route == null ? Loc.Get("Diagnostics.NoRoute") : Loc.Format("Diagnostics.Predicted", Route.Index,
            Route.Destination, Route.PrefixLength, Route.NextHop, Route.Metric))
        + "\n" + (Error.Length != 0 ? Loc.Get($"Diagnostics.Error.{Error}")
            : Loc.Format("Diagnostics.Observed", Destination, Echo?.ReplyAddress, Echo?.StatusDisplay, Echo?.Milliseconds, Echo?.Ttl));
}
