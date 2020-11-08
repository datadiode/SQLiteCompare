using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLiteTurbo
{
    public static class DateTimeOffsetExtensions
    {
        public static class DateTimeExtensions
        {
            // Number of 100ns ticks per time unit
            private const long TicksPerMillisecond = 10000;
            private const long TicksPerSecond = TicksPerMillisecond * 1000;
            private const long TicksPerMinute = TicksPerSecond * 60;
            private const long TicksPerHour = TicksPerMinute * 60;
            private const long TicksPerDay = TicksPerHour * 24;

            // Number of days in a non-leap year
            private const int DaysPerYear = 365;
            // Number of days in 4 years
            private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
                                                                         // Number of days in 100 years
            private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
                                                                         // Number of days in 400 years
            private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

            // Number of days from 1/1/0001 to 12/31/1969
            internal const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear; // 719,162
                                                                                                                          // Number of days from 1/1/0001 to 12/31/9999
            private const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059

            internal const long MinTicks = 0;
            internal const long MaxTicks = DaysTo10000 * TicksPerDay - 1;

        }

        // Constants
        internal const Int64 MaxOffset = TimeSpan.TicksPerHour * 14;
        internal const Int64 MinOffset = -MaxOffset;

        private const long UnixEpochTicks = TimeSpan.TicksPerDay * DateTimeExtensions.DaysTo1970; // 621,355,968,000,000,000
        private const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond; // 62,135,596,800
        private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond; // 62,135,596,800,000

        public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds)
        {
            const long MinMilliseconds = DateTimeExtensions.MinTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;
            const long MaxMilliseconds = DateTimeExtensions.MaxTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;

            if (milliseconds < MinMilliseconds || milliseconds > MaxMilliseconds)
            {
                throw new ArgumentOutOfRangeException("milliseconds",
                    string.Format("Valid values are between {0} and {1}, inclusive.", MinMilliseconds, MaxMilliseconds));
            }

            long ticks = milliseconds * TimeSpan.TicksPerMillisecond + UnixEpochTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        public static long ToUnixTimeMilliseconds(this DateTimeOffset dateTime)
        {
            var utcDateTime = DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Utc);
            // Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times
            long milliseconds = utcDateTime.Ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UnixEpochMilliseconds;
        }

    }
}
