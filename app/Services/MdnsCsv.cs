using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public static class MdnsCsv
{
    public static IReadOnlyList<MdnsService> Filter(IEnumerable<MdnsService> rows, string filter) =>
        rows.Where(r => r.SearchText.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
    public static string Export(MdnsSnapshot snapshot, IEnumerable<MdnsService> rows)
    {
        var result = new StringBuilder("\"Interface\",\"Interface ID\",\"State\",\"Instance\",\"Service type\",\"Hostname\",\"Addresses\",\"Port\",\"TTL seconds\",\"Incomplete\",\"Stale\",\"Trust\"\r\n");
        foreach (var r in rows.Take(MdnsCache.MaxServices))
        {
            var cells = new[] { snapshot.Scope?.Name ?? "", snapshot.Scope?.Id.ToString() ?? "", snapshot.State.ToString(),
                r.Instance, r.ServiceType, r.Hostname, r.Addresses, r.Port, r.Ttl.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.Incomplete.ToString(), r.Stale.ToString(), "Untrusted advertisement; reachability unverified" };
            result.AppendJoin(",", cells.Select(Cell)).Append("\r\n");
        }
        return result.ToString();
    }
    private static string Cell(string value)
    {
        var clean = new string(value.Where(c => !char.IsControl(c) && char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.Format).ToArray());
        var leading = clean.TrimStart();
        if (leading.Length != 0 && leading[0] is '=' or '+' or '-' or '@') clean = "'" + clean;
        return "\"" + clean.Replace("\"", "\"\"") + "\"";
    }
}
