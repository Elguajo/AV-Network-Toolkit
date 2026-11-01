using System;
using System.Globalization;
using System.Windows.Data;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Converters;

public sealed class ToolActivityTextConverter : IValueConverter
{
    public string IdleKey { get; set; } = "";
    public string ActiveKey { get; set; } = "";
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Loc.Get(value is true ? ActiveKey : IdleKey);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
