using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProgettoInformaticaForense_Argentieri.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility Visible { get; set; }
        public Visibility NotVisible { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (bool)value;
            return val ? Visible : NotVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
