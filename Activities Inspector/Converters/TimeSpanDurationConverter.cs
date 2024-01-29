using System;
using System.Globalization;
using System.Windows.Data;

namespace ProgettoInformaticaForense_Argentieri.Converters
{
    public class TimeSpanDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var duration = (TimeSpan)value;

            return $"{duration.Days} giorno/i - {duration.Hours} ora/e - {duration.Minutes} minuti - " +
                $"{duration.Seconds} secondi.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
