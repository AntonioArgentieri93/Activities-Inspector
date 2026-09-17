using Activities_Inspector.Utils;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Activities_Inspector.Converters
{
    public class DateTimeOffsetFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTimeOffset?)value;

            return DateBuilder.BuildFromDateTimeOffset(date);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
