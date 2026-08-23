using System.Globalization;
using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.App.Converters;

/// <summary>
/// Romanian display text for enum values shown in a Picker — enum member names are C#
/// identifiers (English, no diacritics), so Pickers bound directly to them via SelectedItem
/// need this to render Romanian labels without changing the underlying stored value.
/// </summary>
public sealed class EnumDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        FuelType.Benzina => "Benzină",
        FuelType.Motorina => "Motorină",
        FuelType.Gpl => "GPL",
        KnownLocationType.Work => "Serviciu",
        KnownLocationType.Home => "Acasă",
        KnownLocationType.GasStation => "Benzinărie",
        KnownLocationType.Custom => "Personalizat",
        ReportingCadence.PerFillUp => "La fiecare alimentare",
        ReportingCadence.Monthly => "Lunar",
        null => string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
