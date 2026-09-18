using System;
using System.Globalization;

namespace Activities_Inspector.Utils
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
                var offset = TimeZoneInfo.Local.GetUtcOffset(dDate);
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
            var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);

            var strDate = dateTime.ToString("dd/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var strOffset = $"GMT+{offset.Hours}";

            return $"{strDate} {strOffset}";
        }

        /// <summary>
        /// Unico punto di conversione verso l'ora locale (stessa semantica
        /// di DateTime.ToLocalTime/DateTimeOffset.ToLocalTime per ogni Kind).
        /// </summary>
        public static DateTime ToLocal(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local) return dateTime;

            var utc = dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
        }

        public static DateTimeOffset ToLocal(DateTimeOffset dateTimeOffset)
            => dateTimeOffset.ToLocalTime();

        public static string BuildFromDateTimeOffset(DateTimeOffset? dateTimeOffset)
            => dateTimeOffset.HasValue == false ? string.Empty : BuildFromDateTime(dateTimeOffset.Value.LocalDateTime);

        public static string BuildFromDateTimeUtc(DateTime dateTime)
        {
            var strDate = dateTime.ToString("dd/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            return $"{strDate} UTC";
        }

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
                    return ToLocal(dDate);
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
