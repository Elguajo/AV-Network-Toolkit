using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace NetworkProfileManager.Helpers;

// Owner-selected toolbar colors, scoped to these three actions (ADR-031).
internal static class ProfileToolbarAppearance
{
    internal static void Apply(ApplicationTheme theme)
    {
        if (Application.Current == null) return;
        var highContrast = theme == ApplicationTheme.HighContrast;
        var light = theme == ApplicationTheme.Light;
        Brush FromColor(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        var resources = Application.Current.Resources;
        resources["ProfileToolbarLinkBrush"] = highContrast ? SystemColors.ControlTextBrush : FromColor(light ? "#245C85" : "#ADD6EF");
        resources["ProfileToolbarDividerBrush"] = highContrast ? SystemColors.ControlTextBrush : FromColor(light ? "#BCCAD5" : "#506170");
        resources["ProfileToolbarFillBrush"] = highContrast ? SystemColors.HighlightBrush : FromColor("#2E5E8D");
        resources["ProfileToolbarBorderBrush"] = highContrast ? SystemColors.ControlTextBrush : FromColor("#3F74A1");
        resources["ProfileToolbarTextBrush"] = highContrast ? SystemColors.HighlightTextBrush : FromColor("#F7FBFF");
        resources["ProfileToolbarHoverBrush"] = highContrast ? SystemColors.HighlightBrush : FromColor("#376FA3");
        resources["ProfileToolbarPressedBrush"] = highContrast ? SystemColors.HighlightBrush : FromColor("#244D75");
        resources["ProfileCurrentRowFillBrush"] = highContrast ? SystemColors.HighlightBrush : FromColor(light ? "#E8F1FA" : "#263849");
        resources["ProfileCurrentRowBorderBrush"] = highContrast ? SystemColors.ControlTextBrush : FromColor(light ? "#7A9FBD" : "#466B89");
        resources["ProfileCurrentButtonBrush"] = highContrast ? SystemColors.HighlightBrush : FromColor(light ? "#D5E4F1" : "#3B4C60");
        resources["ProfileCurrentTextBrush"] = highContrast ? SystemColors.HighlightTextBrush : FromColor(light ? "#172E43" : "#F0F6FC");
        resources["ProfileCurrentCaptionBrush"] = highContrast ? SystemColors.HighlightTextBrush : FromColor(light ? "#33546F" : "#BDCEE0");
        resources["ProfileCurrentTickBrush"] = highContrast ? SystemColors.HighlightTextBrush : FromColor(light ? "#326D9F" : "#8CCDF4");
    }
}
