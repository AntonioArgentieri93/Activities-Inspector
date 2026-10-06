using System;
using System.Globalization;

namespace Activities_Inspector.Utils
{
    public static class DateBuilder
    {
        /// <summary>Formato di una data: sempre due cifre per giorno e mese (04/10/2026, 20/09/2026).</summary>
        public const string DateFormat = "dd/MM/yyyy";

        /// <summary>Formato di data e ora (senza offset): 04/10/2026 14:36:51.</summary>
        public const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";

        // Formati accettati in lettura: quello attuale e quelli usati dalle versioni precedenti (mese senza zero)
        private static readonly string[] AcceptedDateFormats = { "dd/MM/yyyy", "dd/M/yyyy", "d/M/yyyy" };
        private static readonly string[] AcceptedDateTimeFormats = { "dd/MM/yyyy HH:mm:ss", "dd/M/yyyy HH:mm:ss", "d/M/yyyy HH:mm:ss" };

        // CultureInfo.InvariantCulture: il "/" di un formato personalizzato e' il separatore della cultura corrente
        // (con de-DE "dd/MM/yyyy" darebbe "04.10.2026"), e il report deve essere identico su ogni PC
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string BuildFromString(string strDate)
        {
            if (string.IsNullOrEmpty(strDate)) return string.Empty;

            DateTime dDate;

            if (DateTime.TryParseExact(strDate, AcceptedDateFormats, Invariant, DateTimeStyles.None, out dDate))
            {
                return dDate.ToString(DateFormat, Invariant);
            }

            if (DateTime.TryParseExact(strDate, AcceptedDateTimeFormats, Invariant, DateTimeStyles.None, out dDate))
            {
                return $"{dDate.ToString(DateTimeFormat, Invariant)} {FormatOffset(TimeZoneInfo.Local.GetUtcOffset(dDate))}";
            }
            else
            {
                var year = strDate.Substring(0, 4);
                var month = strDate.Substring(4, 2);
                var day = strDate.Substring(6, 2);

                var date = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month),
                    Convert.ToInt32(day));

                return date.ToString(DateFormat, Invariant);
            }
        }

        /// <summary>Testo per date non documentate dalla sorgente (equivalente al "N / A" di NirSoft).</summary>
        public const string NotAvailable = "N/D";

        public static string BuildFromDateTime(DateTime dateTime)
        {
            // DateTime.MinValue e' il segnaposto interno di "data assente" (es. uscita senza accesso nel log):
            // non va mai mostrato come "01/01/0001 00:00:00"
            if (dateTime == DateTime.MinValue) return NotAvailable;

            var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);

            return $"{dateTime.ToString(DateTimeFormat, Invariant)} {FormatOffset(offset)}";
        }

        /// <summary>
        /// Offset rispetto a GMT: "GMT+2", "GMT-5", "GMT+5:30". Prima era "GMT+" + ore, quindi un fuso negativo
        /// dava "GMT+-5" e uno a mezz'ora (India) perdeva i minuti ("GMT+5").
        /// </summary>
        public static string FormatOffset(TimeSpan offset)
        {
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var absolute = offset.Duration();

            return absolute.Minutes == 0
                ? $"GMT{sign}{absolute.Hours}"
                : $"GMT{sign}{absolute.Hours}:{absolute.Minutes:00}";
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

        public static DateTime? BuildDateTimeFromString(string strDate)
        {
            if (string.IsNullOrEmpty(strDate) == false)
            {
                DateTime dDate;

                if (DateTime.TryParseExact(strDate, AcceptedDateFormats, Invariant, DateTimeStyles.None, out dDate))
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

                if (DateTime.TryParseExact(utcDate, AcceptedDateTimeFormats, Invariant,
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
