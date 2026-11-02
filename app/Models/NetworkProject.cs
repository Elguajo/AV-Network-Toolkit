using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetworkProfileManager.Models;

// Independent of IpProfile: project intent must never inherit legacy apply defaults.
public sealed record NetworkProject
{
    public int SchemaVersion { get; init; } = 1;
    public ProjectIdentity Project { get; init; } = new();
    public List<ProjectProfile> Profiles { get; init; } = new();
    public List<ProjectDevice> Devices { get; init; } = new();
}

public sealed record ProjectIdentity
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }
}

public sealed record ProjectProfile
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    public string Mode { get; init; } = "dhcp";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProjectAdapterHint? AdapterHint { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProjectIpv4? Ipv4 { get; init; }
}

public sealed record ProjectAdapterHint
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PreferredAlias { get; init; }
}

public sealed record ProjectIpv4
{
    public string Address { get; init; } = "";
    public int PrefixLength { get; init; } = 24;
    // Required even when null: null expresses intent, absence is invalid.
    public string? Gateway { get; init; }
    public ProjectDns Dns { get; init; } = new();
}

public sealed record ProjectDns
{
    public string Mode { get; init; } = "leave-unchanged";
    public List<string> Servers { get; init; } = new();
}

// Shared versioned storage contract, also used by the device directory and launcher.
public sealed record ProjectDevice
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    public string WebUrl { get; init; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Manufacturer { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ip { get; init; }
    public string? ProfileId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Notes { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; init; }
    // Records otherwise include every property in diagnostic/interpolated output.
    public override string ToString() => "ProjectDevice { [redacted] }";
}
