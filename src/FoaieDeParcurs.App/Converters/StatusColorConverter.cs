using System.Globalization;

namespace FoaieDeParcurs.App.Converters;

/// <summary>
/// Colours a single status line by severity, so one label can carry both "that didn't work"
/// and "that worked" without needing two overlapping labels with competing visibility rules.
/// </summary>
public sealed class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Colors.Red : Colors.Green;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
