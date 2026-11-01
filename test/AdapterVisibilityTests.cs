using System.Runtime.InteropServices;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public sealed class AdapterVisibilityTests
{
    [Theory]
    [InlineData("Ethernet", 0x05, false, false)]
    [InlineData("USB Ethernet", 0x15, false, false)]
    [InlineData("Wi-Fi", 0x75, false, false)]
    [InlineData("Famatech Radmin VPN Ethernet Adapter", 0x00, false, false)]
    [InlineData("Hyper-V Virtual Ethernet Adapter", 0x00, false, false)]
    [InlineData("VMware Virtual Ethernet Adapter for VMnet1", 0x00, false, false)]
    [InlineData("Bluetooth Device (Personal Area Network)", 0x00, false, false)]
    [InlineData("Renamed filter", 0x02, false, true)]
    [InlineData("Renamed filter", 0x02, true, true)]
    [InlineData("Unknown adapter", 0x80, false, false)]
    [InlineData("WAN Miniport (IP)", 0, false, true)]
    [InlineData("WAN Miniport (IPv6)", 0, false, true)]
    [InlineData("WAN Miniport (Network Monitor)", 0, false, true)]
    [InlineData("WAN Miniport (PPPOE)", 0x10, false, true)]
    [InlineData("WAN Miniport (SSTP)", 0, true, false)]
    [InlineData("Microsoft Kernel Debug Network Adapter", 0, false, true)]
    [InlineData("Microsoft Kernel Debug Network Adapter", 0, true, false)]
    [InlineData("Unknown adapter", null, false, false)]
    [InlineData("Ethernet-WFP Native MAC Layer LightWeight Filter-0000", null, false, false)]
    public void ClassifiesOnlyConfirmedFiltersAndIdleWindowsPlumbing(string description, int? flags, bool hasIpv4, bool hidden)
        => Assert.Equal(hidden, WindowsAdapterVisibility.IsSystemInterface(description, (byte?)flags, hasIpv4));

    [Fact]
    public void NativeOutputReservesFullRowAndDocumentedFieldOffsets()
    {
        Assert.Equal(1352, Marshal.SizeOf<WindowsAdapterVisibility.InterfaceRow>());
        Assert.Equal(12, Marshal.OffsetOf<WindowsAdapterVisibility.InterfaceRow>(nameof(WindowsAdapterVisibility.InterfaceRow.Id)).ToInt32());
        Assert.Equal(1152, Marshal.OffsetOf<WindowsAdapterVisibility.InterfaceRow>(nameof(WindowsAdapterVisibility.InterfaceRow.Flags)).ToInt32());
    }
}
