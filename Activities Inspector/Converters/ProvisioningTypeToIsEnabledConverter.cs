using Activities_Inspector.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Activities_Inspector.Converters
{
    public class ProvisioningTypeToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = (ProvisioningType)value;

            return type == ProvisioningType.Other ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
