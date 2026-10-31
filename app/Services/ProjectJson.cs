using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Json.Schema;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

public sealed class ProjectValidationException : Exception
{
    public string Code { get; }
    public string Field { get; }
    // Do not echo untrusted input (particularly URL credentials or query values).
    public ProjectValidationException(string code, string field = "$") : base($"{code}: {field}")
        => (Code, Field) = (code, field);
}

public static class ProjectJson
{
    public const int MaxBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        MaxDepth = 64
    };
    private static readonly Lazy<JsonSchema> Schema = new(() =>
    {
        using var stream = typeof(ProjectJson).Assembly.GetManifestResourceStream("ProjectSchema.json")!;
        using var document = JsonDocument.Parse(stream);
        return JsonSchema.FromText(document.RootElement.GetRawText());
    });
    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwd", "pwd", "token", "access_token", "refresh_token", "id_token",
        "api_key", "apikey", "secret", "authorization", "auth", "session", "sessionid",
        "cookie", "code", "signature", "sig", "key"
    };

    public static NetworkProject Parse(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new ProjectValidationException("TooLarge");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
            var root = document.RootElement;
            RejectDuplicateProperties(root);
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("schemaVersion", out var version)
                && version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out var number) && number != 1)
                throw new ProjectValidationException("Version", "schemaVersion");
            var result = Schema.Value.Evaluate(root, new EvaluationOptions
            { EvaluateAs = SpecVersion.Draft202012, RequireFormatValidation = true });
            if (!result.IsValid) throw new ProjectValidationException("Structure");
            var project = JsonSerializer.Deserialize<NetworkProject>(root.GetRawText(), Options)!;
            ValidateSemantics(project);
            return project;
        }
        catch (JsonException) { throw new ProjectValidationException("Json"); }
    }

    public static string Serialize(NetworkProject project)
    {
        var json = JsonSerializer.Serialize(project, Options);
        _ = Parse(json); // Same gate on both input and output; never write invalid intent.
        json += Environment.NewLine;
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new ProjectValidationException("TooLarge");
        return json;
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new ProjectValidationException("DuplicateProperty");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    private static void ValidateSemantics(NetworkProject project)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        void Id(string id, string field)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new ProjectValidationException("Identity", field);
        }
        void Name(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ProjectValidationException("Name", field);
        }
        Id(project.Project.Id, "project.id");
        Name(project.Project.Name, "project.name");
        var profileIds = project.Profiles.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        for (int i = 0; i < project.Profiles.Count; i++)
        {
            var profile = project.Profiles[i];
            var field = $"profiles[{i}]";
            Id(profile.Id, field + ".id");
            Name(profile.Name, field + ".name");
            if (profile.Mode != "static") continue;
            var ip = profile.Ipv4!;
            var address = Host(ip.Address, field + ".ipv4.address");
            if ((address & 0xffff0000) == 0xa9fe0000) throw new ProjectValidationException("LinkLocal", field);
            var mask = uint.MaxValue << (32 - ip.PrefixLength);
            if (ip.PrefixLength <= 30 && ((address & ~mask) == 0 || (address & ~mask) == ~mask))
                throw new ProjectValidationException("Subnet", field + ".ipv4.address");
            if (ip.Gateway != null)
            {
                var gateway = Host(ip.Gateway, field + ".ipv4.gateway");
                if (gateway == address || (address & mask) != (gateway & mask)
                    || (ip.PrefixLength <= 30 && ((gateway & ~mask) == 0 || (gateway & ~mask) == ~mask)))
                    throw new ProjectValidationException("Gateway", field + ".ipv4.gateway");
            }
            foreach (var server in ip.Dns.Servers) Host(server, field + ".ipv4.dns.servers");
        }
        for (int i = 0; i < project.Devices.Count; i++)
        {
            var device = project.Devices[i];
            var field = $"devices[{i}]";
            Id(device.Id, field + ".id");
            Name(device.Name, field + ".name");
            if (device.ProfileId != null && !profileIds.Contains(device.ProfileId))
                throw new ProjectValidationException("Reference", field + ".profileId");
            if (device.Ip != null) Host(device.Ip, field + ".ip");
            ValidateUrl(device.WebUrl, field + ".webUrl");
        }
    }

    private static uint Host(string text, string field)
    {
        // Schema enforces canonical dotted quad, not IPAddress's legacy abbreviated forms.
        var bytes = IPAddress.Parse(text).GetAddressBytes();
        if (bytes[0] == 0 || bytes[0] == 127 || bytes[0] >= 224)
            throw new ProjectValidationException("Address", field);
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }

    internal static void ValidateUrl(string text, string field = "devices.webUrl")
    {
        if (text.Any(char.IsWhiteSpace) || text.Any(char.IsControl) || text.Contains('\\')
            || !Uri.TryCreate(text, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrEmpty(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo) || text.Split(new[] { '/', '?', '#' }).Skip(2).FirstOrDefault()?.Contains('@') == true)
            throw new ProjectValidationException("Url", field);
        // Decode for inspection only. The stored text is never canonicalized or stripped.
        var parameters = uri.Query + uri.Fragment;
        for (int i = 0; i < 8; i++)
        {
            var decoded = Uri.UnescapeDataString(parameters);
            if (decoded == parameters) break;
            parameters = decoded;
        }
        if (Uri.UnescapeDataString(parameters) != parameters) throw new ProjectValidationException("Url", field);
        if (parameters.Any(char.IsControl)) throw new ProjectValidationException("Url", field);
        foreach (var part in Regex.Split(parameters, @"[?&#;/]"))
        {
            var key = part.Split(new[] { '=', ':' }, 2)[0].Trim().Replace('-', '_');
            if (SecretKeys.Contains(key)) throw new ProjectValidationException("UrlSecret", field);
        }
    }
}
