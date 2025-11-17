using System;
using System.Globalization;
using Avalonia.Data.Converters;
using RentalHousingManagementConsole.ViewModels;

namespace RentalHousingManagementConsole.Converters;

public class PageTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PageType currentPage && parameter is string targetPageString)
        {
            if (Enum.TryParse<PageType>(targetPageString, out var targetPage))
            {
                return currentPage == targetPage;
            }
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
