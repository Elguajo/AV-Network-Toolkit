using System;
using System.Globalization;
using System.Windows.Data;

namespace NetworkProfileManager.Converters;

public sealed class ToolbarNarrowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double width && width < double.Parse((string)parameter, CultureInfo.InvariantCulture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
