using System;
using System.Globalization;
using System.Windows.Data;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Converters;

// Display evidence only; never substitutes for apply preflight/verification.
public sealed class ProfileCurrentConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Length >= 2 && values[1] is AdapterInfo adapter
            && NetworkProfileManager.Services.ProfileCurrentState.Matches(values[0], adapter);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
