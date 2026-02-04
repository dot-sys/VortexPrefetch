using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

// Value converters for XAML bindings
namespace Vortex.UI.Converters
{
    // Converts multiple booleans to single visibility
    public class BooleanOrToVisibilityConverter : IMultiValueConverter
    {
        // Converts boolean array using OR logic
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || !values.Any())
                return Visibility.Collapsed;

            bool anyTrue = values.OfType<bool>().Any(b => b);
            return anyTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        // Not supported for this converter
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanOrToVisibilityConverter does not support two-way binding");
        }
    }
}
