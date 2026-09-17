using System;
using System.Globalization;
using System.Windows.Data;

namespace Activities_Inspector.Converters
{
    public class AccessTypeTooltipValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var accessType = (string)value;

            switch (accessType)
            {
                case "2":
                    return Activities_Inspector.Resources.Sessions_AccessType_Interactive_Tooltip;
                case "4":
                    return Activities_Inspector.Resources.Sessions_AccessType_Batch_Tooltip;
                case "8":
                    return Activities_Inspector.Resources.Sessions_AccessType_NetworkClearText_Tooltip;
                case "7":
                    return Activities_Inspector.Resources.Sessions_AccessType_Unlock_Tooltip;
                case "9":
                    return Activities_Inspector.Resources.Sessions_AccessType_NewCredentials_Tooltip;
                case "10":
                    return Activities_Inspector.Resources.Sessions_AccessType_RemoteInteractive_Tooltip;
                case "11":
                    return Activities_Inspector.Resources.Sessions_AccessType_CachedInteractive_Tooltip;
                case "12":
                    return Activities_Inspector.Resources.Sessions_AccessType_CachedRemoteInteractive_Tooltip;
                case "13":
                    return Activities_Inspector.Resources.Sessions_AccessType_CachedUnlock_Tooltip;

                default: return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
