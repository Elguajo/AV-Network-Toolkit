using System.Text.Json.Nodes;
using System.IO;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class ProjectJsonTests
{
    internal static NetworkProject Sample() => new()
    {
        Project = new ProjectIdentity { Id = "project", Name = "Example", Description = "Portable" },
        Profiles = new()
        {
            new ProjectProfile
            {
                Id = "static", Name = "Static", Mode = "static",
                AdapterHint = new ProjectAdapterHint { PreferredAlias = "Ethernet" },
                Ipv4 = new ProjectIpv4
                {
                    Address = "192.0.2.10", PrefixLength = 24, Gateway = "192.0.2.1",
                    Dns = new ProjectDns { Mode = "static", Servers = new() { "192.0.2.53", "198.51.100.53" } }
                }
            },
            new ProjectProfile { Id = "dhcp", Name = "DHCP", Mode = "dhcp" }
        },
        Devices = new()
        {
            new ProjectDevice { Id = "device", Name = "Panel", ProfileId = "static", Ip = "192.0.2.20",
                WebUrl = "https://192.0.2.20:8443/en_US/panel?view=main&lang=en#!/main", Notes = "Example" }
        }
    };

    private static JsonObject Json() => JsonNode.Parse(ProjectJson.Serialize(Sample()))!.AsObject();

    [Fact]
    public void RoundTrip_PreservesFieldsAndExactDeepUrl()
    {
        var source = Sample();
        var result = ProjectJson.Parse(ProjectJson.Serialize(source));
        Assert.Equal(source.Devices[0], result.Devices[0]);
        Assert.Equal(source.Project, result.Project);
        Assert.Equal(source.Profiles[0].Id, result.Profiles[0].Id);
        Assert.Equal("192.0.2.1", result.Profiles[0].Ipv4!.Gateway);
        Assert.Equal(new[] { "192.0.2.53", "198.51.100.53" }, result.Profiles[0].Ipv4!.Dns.Servers);
        Assert.Equal(source.Profiles[0].AdapterHint, result.Profiles[0].AdapterHint);
        Assert.Equal(source.Profiles[1], result.Profiles[1]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(ProjectJson.Serialize(result)), Json()));
    }

    [Theory]
    [InlineData("automatic")]
    [InlineData("leave-unchanged")]
    [InlineData("static")]
    public void DnsPolicyAndNullGateway_RoundTrip(string mode)
    {
        var json = Json();
        var ip = json["profiles"]![0]!["ipv4"]!;
        ip["gateway"] = null;
        ip["dns"]!["mode"] = mode;
        ip["dns"]!["servers"] = mode == "static" ? new JsonArray("192.0.2.53") : new JsonArray();
        var parsed = ProjectJson.Parse(json.ToJsonString());
        var result = ProjectJson.Parse(ProjectJson.Serialize(parsed));
        Assert.Null(result.Profiles[0].Ipv4!.Gateway);
        Assert.Equal(mode, result.Profiles[0].Ipv4!.Dns.Mode);
    }

    public static IEnumerable<object[]> InvalidCases()
    {
        yield return new object[] { "missingGateway", "Structure" };
        yield return new object[] { "emptyGateway", "Structure" };
        yield return new object[] { "unknown", "Structure" };
        yield return new object[] { "unknownNested", "Structure" };
        yield return new object[] { "future", "Version" };
        yield return new object[] { "past", "Version" };
        yield return new object[] { "linkLocal", "Structure" };
        yield return new object[] { "linkLocalStatic", "LinkLocal" };
        yield return new object[] { "invalidIp", "Structure" };
        yield return new object[] { "shortIp", "Structure" };
        yield return new object[] { "loopback", "Address" };
        yield return new object[] { "multicast", "Address" };
        yield return new object[] { "network", "Subnet" };
        yield return new object[] { "broadcast", "Subnet" };
        yield return new object[] { "prefix0", "Structure" };
        yield return new object[] { "prefix33", "Structure" };
        yield return new object[] { "gatewaySelf", "Gateway" };
        yield return new object[] { "gatewayOutside", "Gateway" };
        yield return new object[] { "duplicateIds", "Identity" };
        yield return new object[] { "crossDuplicate", "Identity" };
        yield return new object[] { "blankId", "Identity" };
        yield return new object[] { "blankName", "Name" };
        yield return new object[] { "orphan", "Reference" };
        yield return new object[] { "dhcpIpv4", "Structure" };
        yield return new object[] { "emptyStaticDns", "Structure" };
        yield return new object[] { "automaticServers", "Structure" };
        yield return new object[] { "unchangedServers", "Structure" };
        yield return new object[] { "duplicateDns", "Structure" };
        yield return new object[] { "invalidDns", "Structure" };
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void RejectsInvalidStructureOrSemantics(string variant, string code)
    {
        var root = Json();
        var profile = root["profiles"]![0]!;
        var ip = profile["ipv4"]!;
        switch (variant)
        {
            case "missingGateway": ip.AsObject().Remove("gateway"); break;
            case "emptyGateway": ip["gateway"] = ""; break;
            case "unknown": root["password"] = "never-save"; break;
            case "unknownNested": profile["extra"] = true; break;
            case "future": root["schemaVersion"] = 2; break;
            case "past": root["schemaVersion"] = 0; break;
            case "linkLocal": profile["mode"] = "link-local"; break;
            case "linkLocalStatic": ip["address"] = "169.254.10.10"; break;
            case "invalidIp": ip["address"] = "999.0.0.1"; break;
            case "shortIp": ip["address"] = "192.0.2"; break;
            case "loopback": ip["address"] = "127.0.0.1"; break;
            case "multicast": ip["address"] = "224.0.0.1"; break;
            case "network": ip["address"] = "192.0.2.0"; break;
            case "broadcast": ip["address"] = "192.0.2.255"; break;
            case "prefix0": ip["prefixLength"] = 0; break;
            case "prefix33": ip["prefixLength"] = 33; break;
            case "gatewaySelf": ip["gateway"] = "192.0.2.10"; break;
            case "gatewayOutside": ip["gateway"] = "198.51.100.1"; break;
            case "duplicateIds": root["profiles"]![1]!["id"] = "static"; break;
            case "crossDuplicate": root["devices"]![0]!["id"] = "project"; break;
            case "blankId": profile["id"] = " "; break;
            case "blankName": profile["name"] = " "; break;
            case "orphan": root["devices"]![0]!["profileId"] = "missing"; break;
            case "dhcpIpv4": profile["mode"] = "dhcp"; break;
            case "emptyStaticDns": ip["dns"]!["servers"] = new JsonArray(); break;
            case "automaticServers": ip["dns"]!["mode"] = "automatic"; break;
            case "unchangedServers": ip["dns"]!["mode"] = "leave-unchanged"; break;
            case "duplicateDns": ip["dns"]!["servers"] = new JsonArray("192.0.2.53", "192.0.2.53"); break;
            case "invalidDns": ip["dns"]!["servers"] = new JsonArray("invalid"); break;
            default: throw new Exception("Missing fixture");
        }
        var exception = Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(root.ToJsonString()));
        Assert.Equal(code, exception.Code);
    }

    [Theory]
    [InlineData("file:///C:/test.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:pass@192.0.2.20/")]
    [InlineData("https://@192.0.2.20/")]
    [InlineData("https://192.0.2.20/?access_token=NEVER_ECHO")]
    [InlineData("https://192.0.2.20/#!/route?%74oken=NEVER_ECHO")]
    [InlineData("https://192.0.2.20/#api-key:NEVER_ECHO")]
    [InlineData("https://192.0.2.20/?%2570assword=NEVER_ECHO")]
    [InlineData("https://192.0.2.20/#%25252574oken=NEVER_ECHO")]
    [InlineData("https://192.0.2.20/ bad")]
    [InlineData("https://192.0.2.20/#x%0a")]
    public void UnsafeUrlsAreRejectedWithoutEchoingValues(string url)
    {
        var root = Json(); root["devices"]![0]!["webUrl"] = url;
        var exception = Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(root.ToJsonString()));
        Assert.DoesNotContain("NEVER_ECHO", exception.ToString());
        Assert.DoesNotContain(url, exception.ToString());
    }

    [Theory]
    [InlineData("{} trailing")]
    [InlineData("{\"schemaVersion\":1,\"schemaVersion\":1}")]
    [InlineData("{ /* comment */ }")]
    [InlineData("{\"profiles\":[],}")]
    public void CorruptOrAmbiguousJsonIsRejected(string text)
        => Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(text));

    [Fact]
    public void LargeInputIsBounded()
        => Assert.Equal("TooLarge", Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(new string(' ', ProjectJson.MaxBytes + 1))).Code);

    [Fact]
    public void ProjectCollectionHasNoLegacyFiveSlotLimit()
    {
        var source = Sample();
        for (int i = 0; i < 10; i++) source.Profiles.Add(new ProjectProfile { Name = "Profile " + i });
        Assert.Equal(12, ProjectJson.Parse(ProjectJson.Serialize(source)).Profiles.Count);
    }

    [Fact]
    public void PublicExampleIsAcceptedByEmbeddedSchemaAndSemanticValidation()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AVNetworkToolkit.sln"))) root = root.Parent;
        var example = File.ReadAllText(Path.Combine(root!.FullName, "examples", "project.avnet.json"));
        var project = ProjectJson.Parse(example);
        Assert.Equal(3, project.Profiles.Count);
        Assert.Equal("leave-unchanged", project.Profiles[2].Ipv4!.Dns.Mode);
        Assert.Equal(project.Devices[0].WebUrl, ProjectJson.Parse(ProjectJson.Serialize(project)).Devices[0].WebUrl);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(32)]
    public void PointToPointIntentCanBeStoredWithNoGateway(int prefix)
    {
        var root = Json(); var ip = root["profiles"]![0]!["ipv4"]!;
        ip["prefixLength"] = prefix; ip["gateway"] = null;
        Assert.Null(ProjectJson.Parse(root.ToJsonString()).Profiles[0].Ipv4!.Gateway);
    }
}
