using System.Windows;
using System.Windows.Media;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Tests;

public class TrayPopupPlacementTests
{
    [Fact]
    public void HiddenIconMenuIsCenteredAboveClickedIconRatherThanScreenCorner()
    {
        var position = TrayPopupPlacement.Calculate(new(900, 650), new(0, 0, 1920, 1040), new(260, 190));
        Assert.Equal(new Point(770, 456), position);
    }

    [Theory]
    [InlineData(15, 600, 0, 406)]
    [InlineData(1910, 600, 1660, 406)]
    [InlineData(900, 1060, 770, 850)]
    [InlineData(900, 10, 770, 14)]
    public void MenuStaysOnScreenAndFallsBelowTopEdge(double x, double y, double left, double top)
    {
        Assert.Equal(new Point(left, top), TrayPopupPlacement.Calculate(new(x, y), new(0, 0, 1920, 1040), new(260, 190)));
    }

    [Fact]
    public void MenuUsesSecondaryMonitorWithNegativeCoordinates()
    {
        Assert.Equal(new Point(-1030, 456), TrayPopupPlacement.Calculate(new(-900, 650), new(-1920, -200, 1920, 1240), new(260, 190)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.5)]
    [InlineData(2)]
    public void PixelToDipConversionKeepsMenuAtClickedIcon(double dpiScale)
    {
        var fromDevice = new Matrix(1 / dpiScale, 0, 0, 1 / dpiScale, 0, 0);
        var anchor = fromDevice.Transform(new Point(1500, 900));
        var workArea = Rect.Transform(new Rect(0, 0, 2560, 1400), fromDevice);
        var position = TrayPopupPlacement.Calculate(anchor, workArea, new(260, 190));
        Assert.Equal(1500, (position.X + 130) * dpiScale, 5);
        Assert.Equal(900, (position.Y + 194) * dpiScale, 5);
    }
}
