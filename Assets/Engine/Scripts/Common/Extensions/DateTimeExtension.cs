using System;

namespace Assets.Engine.Scripts.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static uint ToUnixTime(this DateTime time)
        {
            return (uint)((time.ToUniversalTime().Ticks - 621355968000000000L) / 10000000L);
        }

        public static ulong ToExtendedEpoch(this DateTime time)
        {
            return (ulong)((time.ToUniversalTime().Ticks - 621355968000000000L) / 10L);
        }
    }
}
