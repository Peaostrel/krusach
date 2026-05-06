using System;
using System.Globalization;
using System.Windows.Data;

namespace krusach.Converters
{
    public class RoleToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            string userRole = value.ToString();
            string requiredRole = parameter.ToString();
            
            // Если роль Admin - разрешено всё. Если роль совпадает с требуемой - разрешено.
            return userRole == "Admin" || userRole == requiredRole;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
