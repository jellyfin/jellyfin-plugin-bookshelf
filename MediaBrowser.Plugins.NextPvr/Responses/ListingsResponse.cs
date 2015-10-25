using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.NextPvr.Helpers;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    class ListingsResponse
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly string _baseUrl;

        public ListingsResponse(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerable<ProgramInfo> GetPrograms(Stream stream, IJsonSerializer json, string channelId, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);
            UtilsHelper.DebugInformation(logger,string.Format("[NextPvr] GetPrograms Response: {0}",json.SerializeToString(root)));

            var listings = root.Guide.Listings;

            return listings.Where(i => string.Equals(i.Channel.channelOID.ToString(_usCulture), channelId, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.EPGEvents.Select(e => GetProgram(i.Channel, e.epgEventJSONObject.epgEvent)));
        }

        private ProgramInfo GetProgram(Channel channel, EpgEvent2 epg)
        {
            var info = new ProgramInfo
            {
                ChannelId = channel.channelOID.ToString(_usCulture),
                Id = epg.OID.ToString(_usCulture),
                Overview = epg.Desc,
                StartDate = DateTime.Parse(epg.StartTime).ToUniversalTime(),
                EndDate = DateTime.Parse(epg.EndTime).ToUniversalTime(),
                Genres = epg.Genres.Where(g => !string.IsNullOrWhiteSpace(g)).ToList(),
                OriginalAirDate = DateTime.Parse(epg.OriginalAirdate).ToUniversalTime(),
                Name = epg.Title,
                OfficialRating = epg.Rating,
                CommunityRating = ParseCommunityRating(epg.StarRating),
                EpisodeTitle = epg.Subtitle,
                Audio = ParseAudio(epg.Audio),
                IsHD = string.Equals(epg.Quality, "hdtv", StringComparison.OrdinalIgnoreCase),
                IsRepeat = !epg.FirstRun,
                IsSeries = true, //!string.IsNullOrEmpty(epg.Subtitle),  http://emby.media/community/index.php?/topic/21264-series-record-ability-missing-in-emby-epg/#entry239633
                ImageUrl = string.IsNullOrEmpty(epg.FanArt) ? null : (_baseUrl + "/" + epg.FanArt),
                HasImage = !string.IsNullOrEmpty(epg.FanArt),
                IsNews = epg.Genres.Contains("news", StringComparer.OrdinalIgnoreCase),
                IsMovie = epg.Genres.Contains("movie", StringComparer.OrdinalIgnoreCase),
                IsKids = epg.Genres.Contains("kids", StringComparer.OrdinalIgnoreCase),

                IsSports = epg.Genres.Contains("sports", StringComparer.OrdinalIgnoreCase) ||
                    epg.Genres.Contains("Sports non-event", StringComparer.OrdinalIgnoreCase) ||
                    epg.Genres.Contains("Sports event", StringComparer.OrdinalIgnoreCase) ||
                    epg.Genres.Contains("Sports talk", StringComparer.OrdinalIgnoreCase) ||
                    epg.Genres.Contains("Sports news", StringComparer.OrdinalIgnoreCase)
            };

            return info;
        }

        public static float? ParseCommunityRating(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var hasPlus = value.IndexOf('+') != -1;

                var rating = value.Replace("+", string.Empty).Length + (hasPlus ? .5 : 0);

                return (float)rating;
            }

            return null;
        }

        public static ProgramAudio? ParseAudio(string value)
        {
            if (string.Equals(value, "stereo", StringComparison.OrdinalIgnoreCase))
            {
                return ProgramAudio.Stereo;
            }

            return null;
        }

        // Classes created with http://json2csharp.com/

        private class Channel
        {
            public int channelOID { get; set; }
            public int channelNumber { get; set; }
            public string channelName { get; set; }
        }

        private class EpgEvent2
        {
            public int OID { get; set; }
            public string UniqueId { get; set; }
            public int ChannelOid { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public string Desc { get; set; }
            public string Rating { get; set; }
            public string Quality { get; set; }
            public string StarRating { get; set; }
            public string Aspect { get; set; }
            public string Audio { get; set; }
            public string OriginalAirdate { get; set; }
            public string FanArt { get; set; }
            public List<string> Genres { get; set; }
            public bool FirstRun { get; set; }
            public bool HasSchedule { get; set; }
            public bool ScheduleIsRecurring { get; set; }
        }

        private class Rtn
        {
            public bool Error { get; set; }
            public string Message { get; set; }
        }

        private class EpgEventJSONObject
        {
            public EpgEvent2 epgEvent { get; set; }
            public Rtn rtn { get; set; }
        }

        private class EPGEvent
        {
            public EpgEventJSONObject epgEventJSONObject { get; set; }
        }

        private class Listing
        {
            public Channel Channel { get; set; }
            public List<EPGEvent> EPGEvents { get; set; }
        }

        private class Guide
        {
            public List<Listing> Listings { get; set; }
        }

        private class RootObject
        {
            public Guide Guide { get; set; }
        }
    }
}
