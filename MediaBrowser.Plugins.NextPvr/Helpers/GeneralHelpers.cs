using MediaBrowser.Model.LiveTv;
using System;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.NextPvr.Helpers
{
    public static class ChannelHelper
    {
        public static ChannelType GetChannelType(string channelType)
        {
            ChannelType type = new ChannelType();

            if (channelType == "0x1")
            {
                type = ChannelType.TV;
            }
            else if (channelType == "0xa")
            {
                type = ChannelType.Radio;
            }

            return type;
        }
    }

    public static class UtilsHelper
    {
        public static void DebugInformation(ILogger logger, string message)
        {
            var config = Plugin.Instance.Configuration;
            bool enableDebugLogging = config.EnableDebugLogging;

            if (enableDebugLogging)
            {
                logger.Debug(message);
            }
        }
   
    }

    public static class RecordingHelper
    {

    }

    public static class ApiHelper
    {
        private static readonly DateTime UnixEpoch =
    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetCurrentUnixTimestampMillis()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }

        public static DateTime DateTimeFromUnixTimestampMillis(long millis)
        {
            return UnixEpoch.AddMilliseconds(millis);
        }

        public static long GetCurrentUnixTimestampSeconds(DateTime date)
        {
            return (long)(date - UnixEpoch).TotalSeconds;
        }

        public static DateTime DateTimeFromUnixTimestampSeconds(long seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }
    }
}
