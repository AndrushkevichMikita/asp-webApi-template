using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HelpersCommon.PrimitivesExtensions
{
    public static class DateTimeExtension
    {
        static readonly TimeZoneInfo tz = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        public static int TotalYears(this DateTime start, DateTime end) => (end.Year - start.Year - 1) +
                                                                           (((end.Month > start.Month) ||
                                                                           ((end.Month == start.Month) && (end.Day >= start.Day))) ? 1 : 0);
        public static int TotalYears(this DateTime start)
        {
            var now = DateTime.UtcNow;
            return (now.Year - start.Year - 1) +
                (((now.Month > start.Month) ||
                ((now.Month == start.Month) && (now.Day >= start.Day))) ? 1 : 0);
        }

        public static int TotalMonth(this DateTime start)
        {
            var now = DateTime.UtcNow;
            return ((now.Year - start.Year) * 12) + (now.Month - (start.Month == 0 ? 1 : start.Month));
        }

        public static DateTime AdjustUtcDate(this DateTime toAdjust, int? year = null, int? month = null, int? day = null, int? hour = null)
             => DateTime.SpecifyKind(toAdjust.AdjustDate(year, month, day, hour), DateTimeKind.Utc);

        /// <summary>
        /// Minutes & seconds sets to 0
        /// </summary>
        public static DateTime AdjustDate(this DateTime toAdjust, int? year = null, int? month = null, int? day = null, int? hour = null)
            => new(year ?? toAdjust.Year,
                   month ?? toAdjust.Month,
                   day ?? toAdjust.Day,
                   hour ?? toAdjust.Hour,
                   0,
                   0,
                   kind: DateTimeKind.Unspecified);

        public static int TotalMonth(this DateTime start, DateTime end) => ((end.Year - start.Year) * 12) + (end.Month - (start.Month == 0 ? 1 : start.Month));

        public static IEnumerable<DateTime> AllDatesInMonth(int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= days; day++)
            {
                yield return new DateTime(year, month, day);
            }
        }

        public static DateTime GetNextWeekday(this DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        /// <summary>
        /// Convert Utc Time to Est time, then change kind to Utc
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime ConvertToEasternTimeAsUtc(this DateTime value)
            => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(value, tz), DateTimeKind.Utc);

        public static DateTime ConvertEstToUtc(this DateTime value)
          => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(value, tz), DateTimeKind.Utc);

        public static DateTime ConvertLocalToUtc(this DateTime value)
            => TimeZoneInfo.ConvertTimeToUtc(value, TimeZoneInfo.Local);

        public static DateTime ConvertUtcToEst(this DateTime value)
            => TimeZoneInfo.ConvertTimeFromUtc(value, tz);

        /// <summary>
        /// Convert Utc time to Est time. Adjust Est time if necessary & convert to Utc 
        /// </summary>
        /// <returns></returns>
        public static DateTime SetUtcTimeAsEstToUtc(this DateTime value, int? year = null, int? month = null, int? day = null, int? hour = null)
        {
            if (value.Kind == DateTimeKind.Local)
                value = value.ConvertLocalToUtc();

            var asEST = value.ConvertUtcToEst();
            if (hour.HasValue)
                asEST = asEST.AdjustDate(year, month, day, hour);

            return asEST.ConvertEstToUtc();
        }

        public static int NumberOfSaturdaysBetween(this DateTime start, DateTime end)
        {
            var difinDays = end.Subtract(start).Days;
            var ad = (6 + (int)start.DayOfWeek) % 7;
            return (2 + difinDays + ad) / 7;
        }
    }
}
