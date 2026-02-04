using System;
using System.Globalization;
using System.Windows.Data;

// Value converters for XAML bindings
namespace Vortex.UI.Converters
{
    // Inverts boolean value
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        // Inverts boolean to opposite value
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : value;
        }

        // Inverts boolean to opposite value
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : value;
        }
    }
}