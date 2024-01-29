using System;
using System.Globalization;

namespace ProgettoInformaticaForense_Argentieri.Utils
{
    public static class DateBuilder
    {
        public static string BuildFromString(string strDate)
        {
            if (string.IsNullOrEmpty(strDate)) return string.Empty;

            DateTime dDate;

            if (DateTime.TryParseExact(strDate, "dd/M/yyyy", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dDate))
            {
                return dDate.ToString("dd/M/yyyy");
            }

            if(DateTime.TryParseExact(strDate, "dd/M/yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dDate))
            {
                var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                var strOffset = $"GMT+{offset.Hours}";

                return $"{strDate} {strOffset}";
            }
            else
            {
                var year = strDate.Substring(0, 4);
                var month = strDate.Substring(4, 2);
                var day = strDate.Substring(6, 2);

                var date = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month),
                    Convert.ToInt32(day));

                return date.ToString("dd/M/yyyy");
            }
        }

        public static string BuildFromDateTime(DateTime dateTime)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);

            var strDate = dateTime.ToString("dd/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var strOffset = $"GMT+{offset.Hours}";

            return $"{strDate} {strOffset}";
        }

        public static string BuildFromDateTimeOffset(DateTimeOffset? dateTimeOffset)
            => dateTimeOffset.HasValue == false ? string.Empty : BuildFromDateTime(dateTimeOffset.Value.LocalDateTime);

        public static DateTime? BuildDateTimeFromString(string strDate)
        {
            if (string.IsNullOrEmpty(strDate) == false)
            {
                DateTime dDate;

                if (DateTime.TryParseExact(strDate, "dd/M/yyyy", DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out dDate))
                {
                    return dDate;
                }
                else
                {
                    var year = strDate.Substring(0, 4);
                    var month = strDate.Substring(4, 2);
                    var day = strDate.Substring(6, 2);

                    dDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month),
                        Convert.ToInt32(day));

                    return dDate;
                }
            }

            return null;
        }

        public static DateTime ConvertToLocalDate(string utcDate)
        {
            try
            {
                DateTime dDate;

                if (DateTime.TryParseExact(utcDate, "dd/M/yyyy HH:mm:ss", DateTimeFormatInfo.CurrentInfo,
                    DateTimeStyles.None, out dDate))
                {
                    var localDateTime = dDate.ToLocalTime();
                    return localDateTime;
                }

                return DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
    }
}
