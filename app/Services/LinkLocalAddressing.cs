using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public static class LinkLocalAddressing
{
    public static bool IsLinkLocal(string? value) => IPAddress.TryParse(value, out var ip)
        && ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() == value
        && ip.GetAddressBytes()[0] == 169 && ip.GetAddressBytes()[1] == 254;

    public static bool IsCandidate(string? value) => IsLinkLocal(value)
        && IPAddress.Parse(value!).GetAddressBytes()[2] is >= 1 and <= 254;

    // Candidate generation is local only: it neither probes nor assigns an address.
    public static string GenerateCandidate()
    {
        var host = RandomNumberGenerator.GetInt32(256, 255 * 256);
        return $"169.254.{host / 256}.{host % 256}";
    }

    public static ApplyIntent Intent(string address, string dns1 = "", string dns2 = "")
    {
        if (dns1.Length == 0 && dns2.Length != 0) throw new InvalidOperationException("Primary DNS required.");
        return new("link-local", address, 16, null, dns1.Length == 0 ? "leave-unchanged" : "static",
            dns1.Length == 0 ? ImmutableArray<string>.Empty : dns2.Length == 0
                ? ImmutableArray.Create(dns1) : ImmutableArray.Create(dns1, dns2));
    }

    internal static void RequireDad(ApplyAdapterSnapshot snapshot)
    {
        if (snapshot.Dad is not { Transmits: > 0, Connected: true })
            throw new InvalidOperationException("Connected interface with enabled DAD required.");
    }
}
