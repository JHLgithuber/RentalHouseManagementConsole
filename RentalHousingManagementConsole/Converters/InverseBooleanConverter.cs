using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace RentalHousingManagementConsole.Converters;

public sealed class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value is null ? null : Avalonia.Data.BindingOperations.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value is null ? null : Avalonia.Data.BindingOperations.DoNothing;
    }
}