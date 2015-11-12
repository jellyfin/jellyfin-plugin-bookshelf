using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
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

        public static string GetCodecRepresetation(this MediaStream audioStream)
        {
            var audio = audioStream != null && !string.IsNullOrEmpty(audioStream.Codec)
                ? audioStream.Codec.ToLower().Replace(" ", "_")
                : null;
            switch (audio)
            {
                case "truehd":
                    return TraktAudio.dolby_truehd.ToString();
                case "dts":
                case "dca":
                    return TraktAudio.dts.ToString();
                case "dtshd":
                    return TraktAudio.dts_ma.ToString();
                case "ac3":
                    return TraktAudio.dolby_digital.ToString();
                case "aac":
                    return TraktAudio.aac.ToString();
                case "mp2":
                    return TraktAudio.mp3.ToString();
                case "pcm":
                    return TraktAudio.lpcm.ToString();
                case "ogg":
                    return TraktAudio.ogg.ToString();
                case "wma":
                    return TraktAudio.wma.ToString();
                case "flac":
                    return TraktAudio.flac.ToString();
                default:
                    return null;
            }
        }

        public static bool MetadataIsDifferent(this TraktMovieCollected collectedMovie, Movie movie)
        {
            var audioStream = movie.GetMediaStreams().FirstOrDefault(x => x.Type == MediaStreamType.Audio);

            var resolution = movie.GetDefaultVideoStream().GetResolution();
            var audio = GetCodecRepresetation(audioStream);
            var audioChannels = audioStream.GetAudioChannels();

            if (collectedMovie.Metadata == null || collectedMovie.Metadata.IsEmpty())
            {
                return !string.IsNullOrEmpty(resolution) || !string.IsNullOrEmpty(audio) || !string.IsNullOrEmpty(audioChannels);
            }
            return collectedMovie.Metadata.Audio != audio ||
                   collectedMovie.Metadata.AudioChannels != audioChannels ||
                   collectedMovie.Metadata.Resolution != resolution;
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
            if (audioStream == null || string.IsNullOrEmpty(audioStream.ChannelLayout))
            {
                return null;
            }
            var channels = audioStream.ChannelLayout.Split('(')[0];
            switch (channels)
            {
                case "7":
                    return "6.1";
                case "6":
                    return "5.1";
                case "5":
                    return "5.0";
                case "4":
                    return "4.0";
                case "3":
                    return "2.1";
                case "stereo":
                    return "2.0";
                case "mono":
                    return "1.0";
                default:
                    return channels;
            }
        }

        public static IList<IEnumerable<T>> ToChunks<T>(this IEnumerable<T> enumerable, int chunkSize)
        {
            var itemsReturned = 0;
            var list = enumerable.ToList(); // Prevent multiple execution of IEnumerable.
            var count = list.Count;
            var chunks = new List<IEnumerable<T>>();
            while (itemsReturned < count)
            {
                chunks.Add(list.Take(chunkSize).ToList());
                list = list.Skip(chunkSize).ToList();
                itemsReturned += chunkSize;
            }
            return chunks;
        }

        public enum TraktAudio
        {
            lpcm,
            mp3,
            aac,
            dts,
            dts_ma,
            flac,
            ogg,
            wma,
            dolby_prologic,
            dolby_digital,
            dolby_digital_plus,
            dolby_truehd
        }
    }
}
