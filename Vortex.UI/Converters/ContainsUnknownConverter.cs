using System;
using System.Globalization;
using System.Windows.Data;

// Value converters for XAML bindings
namespace Vortex.UI.Converters
{
    // Checks if value contains unknown text
    public class ContainsUnknownConverter : IValueConverter
    {
        // Checks for unknown text in value
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            string text = value.ToString();
            return !string.IsNullOrEmpty(text) &&
                              text.IndexOf("Unknown", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Not supported for this converter
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
