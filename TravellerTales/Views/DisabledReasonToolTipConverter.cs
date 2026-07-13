using System.Globalization;
using System.Windows.Data;

namespace TravellerTales.Views;

public sealed class DisabledReasonToolTipConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var reason = value?.ToString();
        return string.IsNullOrWhiteSpace(reason) ? null : reason;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
