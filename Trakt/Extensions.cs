using System;

namespace Trakt
{
    public static class Extensions
    {
        // Trakt.tv uses Unix timestamps, which are seconds past epoch.
        public static DateTime ConvertEpochToDateTime(this long unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();

            return dtDateTime;
        }



        public static long ConvertToUnixTimeStamp(this DateTime dateTime)
        {
            try
            {
                var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

                var ts = dateTime.Subtract(dtDateTime);

                return Convert.ToInt64(ts.TotalSeconds);
            }
            catch
            {
                return 0;
            }
        }
    }
}
