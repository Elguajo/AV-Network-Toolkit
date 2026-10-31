using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public sealed class ProjectProfileEditorValidationTests
{
    private static ProjectProfileEditor Valid() => new()
    { Name = "Video Wall", Mode = "static", Address = "192.0.2.100", Prefix = "24", DnsMode = "leave-unchanged" };

    [Theory]
    [InlineData("invalid")]
    [InlineData("192.0.2")]
    [InlineData("127.0.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("169.254.10.1")]
    [InlineData("192.0.2.0")]
    [InlineData("192.0.2.255")]
    public void InvalidComputerAddressNamesTheFieldAndDoesNotAlterDraft(string value)
    {
        var editor = Valid(); editor.Address = value;
        Assert.Equal(nameof(editor.Address), editor.FirstInvalidField);
        Assert.Empty(editor[nameof(editor.Address)]);
        editor.ShowValidation(); Assert.NotEmpty(editor[nameof(editor.Address)]);
        Assert.Equal(value, editor.Address);
        editor.Address = "192.0.2.101";
        Assert.Null(editor.FirstInvalidField); Assert.Empty(editor[nameof(editor.Address)]);
    }

    [Fact]
    public void ValidationTracksSubnetGatewayAndDnsDependencies()
    {
        var editor = Valid(); editor.Gateway = "198.51.100.1";
        Assert.Equal(nameof(editor.Gateway), editor.FirstInvalidField);
        editor.Gateway = "192.0.2.1"; editor.Prefix = "33";
        Assert.Equal(nameof(editor.Prefix), editor.FirstInvalidField);
        editor.Prefix = "24"; editor.DnsMode = "static";
        Assert.Equal(nameof(editor.Dns1), editor.FirstInvalidField);
        editor.Dns1 = "192.0.2.53"; editor.Dns2 = editor.Dns1;
        Assert.Equal(nameof(editor.Dns2), editor.FirstInvalidField);
        editor.Dns2 = ""; Assert.Null(editor.FirstInvalidField);
        editor.Mode = "dhcp"; editor.Address = "invalid"; editor.Gateway = "invalid"; editor.Dns1 = "invalid";
        Assert.Null(editor.FirstInvalidField); Assert.Equal("invalid", editor.Address);
    }

    [Fact]
    public void BlankNameIsFirstAndStoredPrefixSupportIsNotExpandedByPresentation()
    {
        var editor = Valid(); editor.Name = " "; editor.Address = "invalid";
        Assert.Equal(nameof(editor.Name), editor.FirstInvalidField);
        editor.Name = "Named"; editor.Address = "192.0.2.100"; editor.Prefix = "32";
        Assert.Null(editor.FirstInvalidField); // Storage permits /32; apply policy remains independent.
        Assert.Equal(32, editor.Build().Ipv4!.PrefixLength);
    }
}
