using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace TravellerTales.Views;

public sealed partial class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null
            ? string.Empty
            : LowerUpperBoundaryRegex().Replace(value.ToString() ?? string.Empty, "$1 $2");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex LowerUpperBoundaryRegex();
}
