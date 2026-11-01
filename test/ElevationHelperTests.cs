using System.IO;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Tests;

public class ElevationHelperTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("missing-request", "missing-result")]
    [InlineData("C:/Windows/System32/drivers/etc/hosts", "C:/Windows/System32/should-never-write")]
    public void RetiredWorkerRejectsEveryInvocationWithoutReadingOrWriting(string request, string result)
        => Assert.Equal(1, ElevationHelper.RunWorker(request, result));

    [Fact]
    public void RetiredWorkerPreservesValidPayloadAndExistingResult()
    {
        var root = Path.Combine(Path.GetTempPath(), "RetiredWorker_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var request = Path.Combine(root, "request.json"); var result = Path.Combine(root, "result.txt");
            const string payload = "[[\"interface\",\"ip\",\"set\",\"address\",\"name=Fixture\",\"source=dhcp\"]]";
            File.WriteAllText(request, payload); File.WriteAllText(result, "preserve");
            Assert.Equal(1, ElevationHelper.RunWorker(request, result));
            Assert.Equal(payload, File.ReadAllText(request)); Assert.Equal("preserve", File.ReadAllText(result));
        }
        finally { Directory.Delete(root, true); }
    }
}
