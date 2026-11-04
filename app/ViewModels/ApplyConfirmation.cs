using System.Collections.Generic;
using System.Linq;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.ViewModels;

// Display only: the immutable request remains the authority at every apply boundary.
internal sealed record ApplyConfirmation(ProjectApplyRequest Request, bool AddressOnlyRecovery = false)
{
    public string Title => Loc.Get(Request.Recovery || AddressOnlyRecovery ? "Apply.Recovery" : "Apply.CompactTitle");
    public string Adapter => Request.Before.Identity.Alias;
    public string Warning => string.Join("\n\n", new[]
    {
        AddressOnlyRecovery ? Loc.Get("LocalApply.OldHistory") : "",
        ProjectApplyPolicy.IsKnownOffline(Request.Before) ? Loc.Get("Apply.CompactOffline") : "",
        Request.Before.Addresses.Any(a => a.State == "duplicate") ? Loc.Get("Apply.SourceConflict") : ""
    }.Where(s => s.Length > 0));
    public string Details => ProjectApplyViewModel.FormatSnapshot(Request.Before) + "\n\n"
        + Loc.Get("Apply.Requested") + "\n" + ProjectApplyViewModel.FormatIntent(Request.Desired)
        + "\n\n" + Loc.Get("Apply.Disclaimer");
    public IReadOnlyList<ApplyConfirmationRow> Rows
    {
        get
        {
            var before = Request.Before; var desired = Request.Desired;
            var address = before.Dhcp ? null : ProjectApplyPolicy.ConfiguredAddress(before);
            string automatic = Loc.Get("Apply.AutomaticDhcp"), none = Loc.Get("Apply.None");
            string currentIp = before.Dhcp ? automatic : address?.Address ?? none;
            string currentMask = before.Dhcp ? automatic : address == null ? none : Mask(address.PrefixLength);
            string currentGateway = before.Dhcp ? automatic : before.Routes.SingleOrDefault(r => r.Destination == "0.0.0.0/0"
                && r.Store == (ProjectApplyPolicy.IsKnownOffline(before) ? "persistent" : "active"))?.NextHop ?? none;
            string currentDns = before.DnsSource == "automatic" ? Loc.Get("Project.DnsAutomatic")
                : before.DnsServers.Length == 0 ? Loc.Get("Apply.CompactNoDns") : string.Join(", ", before.DnsServers);
            string requestedDns = desired.DnsMode switch
            {
                "automatic" => Loc.Get("Project.DnsAutomatic"),
                "leave-unchanged" => currentDns,
                "none" => Loc.Get("Apply.CompactNoDns"),
                _ => string.Join(", ", desired.DnsServers)
            };
            var rows = new List<ApplyConfirmationRow>
            {
                Row(Loc.Get("EditBar.Ip"), currentIp, desired.Mode == "dhcp" ? automatic : desired.Address!),
                Row(Loc.Get("EditBar.Subnet"), currentMask, desired.Mode == "dhcp" ? automatic : Mask(desired.PrefixLength)),
                Row(Loc.Get("EditBar.Gateway"), currentGateway, desired.Mode == "dhcp" ? automatic : desired.Gateway ?? none),
                Row(Loc.Get("Adapter.Dns"), currentDns, requestedDns)
            };
            if (desired.DnsRegistration != null && desired.DnsRegistration != before.DnsRegistration)
                rows.Add(Row(Loc.Get("Apply.DnsRegistration"), before.DnsRegistration, desired.DnsRegistration));
            return rows;
        }
    }
    private static ApplyConfirmationRow Row(string label, string current, string requested)
        => new(label, current == requested ? Loc.Format("Apply.UnchangedValue", requested) : current + " → " + requested);
    private static string Mask(int prefix)
    {
        uint mask = uint.MaxValue << (32 - prefix);
        return $"{mask >> 24}.{(mask >> 16) & 255}.{(mask >> 8) & 255}.{mask & 255}";
    }
}

internal sealed record ApplyConfirmationRow(string Label, string Value);
