using System.Globalization;

namespace FoaieDeParcurs.App.Converters;

/// <summary>Home list status indicator: "✅ verified & routes attached / ⚠️ incomplete" per spec.</summary>
public sealed class VerifiedStatusIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "✅" : "⚠️";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
