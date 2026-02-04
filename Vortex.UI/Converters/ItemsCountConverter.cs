using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

// Value converters for XAML bindings
namespace Vortex.UI.Converters
{
    // Converts count to formatted items text
    public class ItemsCountConverter : IValueConverter
    {
        // Converts integer count to formatted string
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                var formatString = Application.Current.TryFindResource("ItemsFormat") as string ?? "{0} Items";
                return string.Format(formatString, count);
            }
            return "0 Items";
        }

        // Not supported for this converter
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ItemsCountConverter does not support two-way binding");
        }
    }
}
