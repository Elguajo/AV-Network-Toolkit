using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class ProjectJsonConcurrencyTests
{
    // Build input without calling the validation gate: input and oracle are independent.
    private static string Fixture(string variant)
    {
        var root = JsonSerializer.SerializeToNode(ProjectJsonTests.Sample(), new JsonSerializerOptions
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
        var profile = root["profiles"]![0]!;
        var dns = profile["ipv4"]!["dns"]!;
        switch (variant)
        {
            case "valid": break;
            case "dhcpIpv4": profile["mode"] = "dhcp"; break;
            case "emptyStaticDns": dns["servers"] = new JsonArray(); break;
            case "automaticServers": dns["mode"] = "automatic"; break;
            case "unchangedServers": dns["mode"] = "leave-unchanged"; break;
            default: throw new ArgumentException(nameof(variant));
        }
        return root.ToJsonString();
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("dhcpIpv4")]
    [InlineData("emptyStaticDns")]
    [InlineData("automaticServers")]
    [InlineData("unchangedServers")]
    public void ConditionalRules_AreEnforcedSequentially(string variant)
    {
        var json = Fixture(variant);
        for (int i = 0; i < 20; i++)
        {
            if (variant == "valid") Assert.Equal("static", ProjectJson.Parse(json).Profiles[0].Mode);
            else Assert.Equal("Structure", Assert.Throws<ProjectValidationException>(() => ProjectJson.Parse(json)).Code);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentValidation_EnforcesConditionalRulesAndAcceptsValidInput(bool includeSerialize)
    {
        var variants = new[] { "valid", "dhcpIpv4", "emptyStaticDns", "automaticServers", "unchangedServers" };
        var fixtures = variants.Select(Fixture).ToArray();
        var models = fixtures.Select(json => JsonSerializer.Deserialize<NetworkProject>(json, new JsonSerializerOptions
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!).ToArray();
        var failures = new ConcurrentQueue<string>();
        using var start = new Barrier(8);
        var workers = Enumerable.Range(0, 8).Select(worker => Task.Factory.StartNew(() =>
        {
            Assert.True(start.SignalAndWait(TimeSpan.FromSeconds(20)));
            for (int round = 0; round < 100; round++)
            {
                var index = (round + worker) % variants.Length;
                try
                {
                    if (includeSerialize && worker % 2 == 0) _ = ProjectJson.Serialize(models[index]);
                    else Assert.Equal("project", ProjectJson.Parse(fixtures[index]).Project.Id);
                    if (index != 0) failures.Enqueue($"{variants[index]} accepted (worker {worker}, round {round})");
                }
                catch (ProjectValidationException ex) when (index != 0 && ex.Code == "Structure") { }
                catch (Exception ex) { failures.Enqueue($"{variants[index]}: {ex.GetType().Name}"); }
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
        await Task.WhenAll(workers);
        Assert.True(failures.IsEmpty, $"{failures.Count} failures: {string.Join("; ", failures.Take(12))}");
    }
}
