using Activities_Inspector.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Activities_Inspector.Converters
{
    public class ViewerModeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = (LeftNavbarItem)value;
            var name = item.ViewerMode;

            switch (name)
            {
                case ViewerMode.InstalledPrograms:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_InstalledPrograms;
                case ViewerMode.Prefetch:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_Prefetch;
                case ViewerMode.Recents:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_Recents;
                case ViewerMode.ShellBags:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_ShellBags;
                case ViewerMode.TimeIntervals:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_TimeIntervals;
                case ViewerMode.Sessions:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_Sessions;
                case ViewerMode.SystemTimeChanged:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_SystemTimeChanged;
                case ViewerMode.Usb:
                    return Activities_Inspector.Resources.MainWindow_ViewerMode_Usb;

                default: throw new ArgumentException($"Valore {nameof(name)} non previsto");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
