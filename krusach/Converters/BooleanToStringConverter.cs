using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace krusach.Converters
{
    public class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && parameter is string p)
            {
                var parts = p.Split(':');
                if (parts.Length == 2) return b ? parts[1] : parts[0];
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
