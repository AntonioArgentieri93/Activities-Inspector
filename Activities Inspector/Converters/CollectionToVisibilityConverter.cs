using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Activities_Inspector.Converters
{
    public class CollectionToVisibilityConverter : IValueConverter
    {
        public Visibility Visible { get; set; }
        public Visibility Collapsed { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICollection collection)
            {
                return collection.Count > 0 ? Visible : Collapsed;
            }

            return Collapsed; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
