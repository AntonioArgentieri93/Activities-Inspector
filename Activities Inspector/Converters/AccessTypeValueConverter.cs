using Activities_Inspector.Utils;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Activities_Inspector.Converters
{
    public class AccessTypeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var accessType = (string)value;

            return AccessTypeBuilder.BuildStringSessionType(accessType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
