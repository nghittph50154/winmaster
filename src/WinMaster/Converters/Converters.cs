using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WinMaster.Models;

namespace WinMaster.Converters;

/// <summary>bool → Visibility (true = Visible, false = Collapsed)</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString() == "invert";
        bool boolValue = value is bool b && b;
        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>InstallStatus → StatusColor brush</summary>
public class InstallStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InstallStatus status)
        {
            return status switch
            {
                InstallStatus.Success => new SolidColorBrush(Color.FromRgb(0, 200, 150)),
                InstallStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 76, 106)),
                InstallStatus.Installing or InstallStatus.Downloading => new SolidColorBrush(Color.FromRgb(108, 99, 255)),
                InstallStatus.Verifying => new SolidColorBrush(Color.FromRgb(255, 179, 71)),
                InstallStatus.Skipped => new SolidColorBrush(Color.FromRgb(136, 136, 170)),
                _ => new SolidColorBrush(Color.FromRgb(80, 80, 110))
            };
        }
        return Brushes.Transparent;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>LogLevel → color brush for log entries</summary>
public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Success => new SolidColorBrush(Color.FromRgb(0, 200, 150)),
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(255, 76, 106)),
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(255, 179, 71)),
                LogLevel.Debug => new SolidColorBrush(Color.FromRgb(100, 100, 130)),
                _ => new SolidColorBrush(Color.FromRgb(200, 200, 220))
            };
        }
        return new SolidColorBrush(Color.FromRgb(200, 200, 220));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>int count → "Selected: N" text</summary>
public class CountToSelectedTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int n)
            return n == 0 ? "No applications selected" : $"Selected: {n} application{(n == 1 ? "" : "s")}";
        return string.Empty;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>bool active → nav button style indicator</summary>
public class ActiveMenuToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool active = value is bool b && b;
        return active
            ? new SolidColorBrush(Color.FromRgb(108, 99, 255))
            : Brushes.Transparent;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>InstallStatus → status label text</summary>
public class InstallStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InstallStatus status)
        {
            return status switch
            {
                InstallStatus.Pending => "",
                InstallStatus.Downloading => "Downloading...",
                InstallStatus.Installing => "Installing...",
                InstallStatus.Verifying => "Verifying...",
                InstallStatus.Success => "✓ Installed",
                InstallStatus.Failed => "✗ Failed",
                InstallStatus.Skipped => "⚠ Skipped",
                _ => ""
            };
        }
        return "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
