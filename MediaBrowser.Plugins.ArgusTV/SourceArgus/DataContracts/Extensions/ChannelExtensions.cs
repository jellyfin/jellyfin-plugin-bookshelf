using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgusTV.DataContracts.Extensions
{
    /// <summary>
    /// Channel extensions.
    /// </summary>
    public static class ChannelExtensions
    {
        /// <summary>
        /// Get the broadcast start time of a channel as a TimeSpan.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A TimeSpan holding the time or null.</returns>
        public static TimeSpan? GetBroadcastStartTime(this Channel channel)
        {
            TimeSpan time;
            if (channel.BroadcastStart == null
                || !TimeSpan.TryParseExact(channel.BroadcastStart, @"h\:mm", CultureInfo.InvariantCulture, out time))
            {
                return null;
            }
            return time;
        }

        /// <summary>
        /// Get the broadcast stop time of a channel as a TimeSpan.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A TimeSpan holding the time or null.</returns>
        public static TimeSpan? GetBroadcastStopTime(this Channel channel)
        {
            TimeSpan time;
            if (channel.BroadcastStop == null
                || !TimeSpan.TryParseExact(channel.BroadcastStop, @"h\:mm", CultureInfo.InvariantCulture, out time))
            {
                return null;
            }
            return time;
        }
    }
}
