using System;
using System.Globalization;
using System.Windows.Data;

namespace krusach.Converters
{
    public class RatingToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rating = 0;
            
            if (value is int r) 
            {
                rating = r;
            }
            else if (value != null && int.TryParse(value.ToString(), out int rParsed))
            {
                rating = rParsed;
            }
            else 
            {
                return "☆☆☆☆☆";
            }

            return new string('⭐', Math.Min(5, Math.Max(0, rating)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value?.ToString() ?? "0";
            
            // Если ввели число (например "5")
            if (int.TryParse(input, out int result))
            {
                return result;
            }
            
            // Если ввели звезды (например "⭐⭐")
            int starsCount = 0;
            foreach (char c in input)
            {
                if (c == '⭐') starsCount++;
            }
            
            if (starsCount > 0) return starsCount;

            return 0;
        }
    }
}
