using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using Trakt.Api.DataContracts;
using Trakt.Api.DataContracts.Users.Collection;

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

        public static int? ConvertToInt(this string input)
        {
            int result;
            if (int.TryParse(input, out result))
            {
                return result;
            }
            return null;
        }

        public static bool IsEmpty(this TraktMetadata metadata)
        {
            return string.IsNullOrEmpty(metadata.MediaType) &&
                   string.IsNullOrEmpty(metadata.Resolution) &&
                   string.IsNullOrEmpty(metadata.Audio) &&
                   string.IsNullOrEmpty(metadata.AudioChannels);
        }

        public static string GetResolution(this MediaStream videoStream)
        {
            if (videoStream == null)
            {
                return null;
            }
            if (!videoStream.Width.HasValue)
            {
                return null;
            }
            if (videoStream.Width.Value >= 3800)
            {
                return "uhd_4k";
            }
            if (videoStream.Width.Value >= 1900)
            {
                return "hd_1080p";
            }
            if (videoStream.Width.Value >= 1270)
            {
                return "hd_720p";
            }
            if (videoStream.Width.Value >= 700)
            {
                return "sd_480p";
            }
            return null;
        }

        public static string ToJSON(this object obj)
        {
            if (obj == null) return string.Empty;
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(obj.GetType());
                ser.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string ToISO8601(this DateTime dt, double hourShift = 0)
        {
            return dt.AddHours(hourShift).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }


        public static int GetSeasonNumber(this Episode episode)
        {
            return (episode.ParentIndexNumber != 0 ? episode.ParentIndexNumber ?? 1 + (episode.Series.AnimeSeriesIndex ?? 1) - 1 : episode.ParentIndexNumber).Value;
        }

        public static string GetAudioChannels(this MediaStream audioStream)
        {
            if (audioStream == null)
            {
                return null;
            }
            var channels = audioStream.ChannelLayout.Split('(')[0];
            return channels.Replace("stereo", "2.0");
        }

        public static IEnumerable<IEnumerable<T>> ToChunks<T>(this IEnumerable<T> enumerable, int chunkSize)
        {
            var itemsReturned = 0;
            var list = enumerable.ToList(); // Prevent multiple execution of IEnumerable.
            var count = list.Count;
            while (itemsReturned < count)
            {
                var currentChunkSize = Math.Min(chunkSize, count - itemsReturned);
                yield return list.GetRange(itemsReturned, currentChunkSize);
                itemsReturned += currentChunkSize;
            }
        }
    }
}
