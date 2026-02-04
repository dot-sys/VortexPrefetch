using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

// Value converters for XAML bindings
namespace Vortex.UI.Converters
{
    // Converts string to visibility based on content
    public class ContainsDeletedConverter : IValueConverter
    {
        // Converts value to visibility or boolean
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return targetType == typeof(Visibility) ? Visibility.Collapsed : (object)false;
            }

            string text = value.ToString();

            if (targetType == typeof(Visibility))
            {
                bool isValidUrl = !string.IsNullOrEmpty(text) &&
                                  text.StartsWith("http", StringComparison.OrdinalIgnoreCase);
                return isValidUrl ? Visibility.Visible : Visibility.Collapsed;
            }

            return ContainsDeletedOrReplaced(text);
        }

        // Not supported for this converter
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ContainsDeletedConverter does not support two-way binding");
        }

        // Checks if text contains deleted keywords
        private static bool ContainsDeletedOrReplaced(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                   (text.IndexOf("Deleted", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Replaced", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
