using System;
using System.Globalization;
using System.Windows.Data;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Converters;

public sealed class ProjectProfileSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ProjectProfile profile) return "";
        if (profile.Mode == "dhcp") return Loc.Get("Workspace.AutomaticSummary");
        var ip = profile.Ipv4!;
        var dns = ip.Dns.Mode == "static" ? string.Join(", ", ip.Dns.Servers)
            : Loc.Get(ip.Dns.Mode == "automatic" ? "Project.DnsAutomatic" : "Project.DnsUnchanged");
        return Loc.Format("Workspace.ProfileSummary", ip.Address, ip.PrefixLength, ip.Gateway ?? Loc.Get("Apply.None"), dns);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
