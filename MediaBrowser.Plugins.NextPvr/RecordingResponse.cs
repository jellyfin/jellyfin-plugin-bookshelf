using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Plugins.NextPvr
{
    public class RecordingResponse
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public IEnumerable<RecordingInfo> GetRecordings(Stream stream, IJsonSerializer json)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var root = json.DeserializeFromStream<RootObject>(stream);

            return root.ManageResults
                .EPGEvents
                .Select(i => i.epgEventJSONObject)

                // Seeing recurring parents coming back with these reponses, for some reason
                .Where(i => i.schd != null)
                .Select(GetRecordingInfo);
        }

        private RecordingInfo GetRecordingInfo(EpgEventJSONObject i)
        {
            var info = new RecordingInfo();

            var recurr = i.recurr;
            if (recurr != null)
            {
                info.ChannelName = recurr.RulesXmlDoc.Rules.ChannelName;
            }

            var schd = i.schd;

            if (schd != null)
            {
                info.ChannelId = schd.ChannelOid.ToString(_usCulture);
                info.Id = schd.OID.ToString(_usCulture);
                info.Path = schd.RecordingFileName;
                info.Url = schd.DownloadURL;
                info.Status = ParseStatus(schd.Status);
                info.StartDate = DateTime.Parse(schd.StartTime);
                info.EndDate = DateTime.Parse(schd.EndTime);
            }

            var epg = i.epgEvent;

            if (epg != null)
            {
                info.Audio = ListingsResponse.ParseAudio(epg.Audio);
                info.ProgramId = epg.OID.ToString(_usCulture);
                info.OfficialRating = epg.Rating;
                info.EpisodeTitle = epg.Subtitle;
                info.Name = epg.Title;
                info.Overview = epg.Desc;
                info.Genres = epg.Genres;
                info.IsRepeat = !epg.FirstRun;
                info.CommunityRating = ListingsResponse.ParseCommunityRating(epg.StarRating);
                info.IsHD = string.Equals(epg.Quality, "hdtv", StringComparison.OrdinalIgnoreCase);
            }

            return info;
        }

        private RecordingStatus ParseStatus(string value)
        {
            if (string.Equals(value, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return RecordingStatus.Completed;
            }

            if (string.Equals(value, "In-Progress", StringComparison.OrdinalIgnoreCase))
            {
                return RecordingStatus.InProgress;
            }

            return RecordingStatus.Scheduled;
        }

        private class Rules
        {
            public string ChannelOID { get; set; }
            public string ChannelName { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string PrePadding { get; set; }
            public string PostPadding { get; set; }
            public string Quality { get; set; }
            public string Keep { get; set; }
            public string Days { get; set; }
            public string EPGTitle { get; set; }
        }

        private class RulesXmlDoc
        {
            public Rules Rules { get; set; }
        }

        private class Recurr
        {
            public string Type { get; set; }
            public int OID { get; set; }
            public string RecurringName { get; set; }
            public string PeriodDescription { get; set; }
            public string EPGTitle { get; set; }
            public int ChannelOid { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public object RecordingDirectoryID { get; set; }
            public int Priority { get; set; }
            public string Quality { get; set; }
            public string PrePadding { get; set; }
            public string PostPadding { get; set; }
            public string MaxRecordings { get; set; }
            public bool allChannels { get; set; }
            public bool OnlyNew { get; set; }
            public string Day { get; set; }
            public object AdvancedRules { get; set; }
            public RulesXmlDoc RulesXmlDoc { get; set; }
        }

        private class Rtn
        {
            public bool Error { get; set; }
            public string Message { get; set; }
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

        private class Schd
        {
            public int OID { get; set; }
            public int ChannelOid { get; set; }
            public int Priority { get; set; }
            public string Name { get; set; }
            public string Quality { get; set; }
            public string Type { get; set; }
            public string Day { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Status { get; set; }
            public string FailureReason { get; set; }
            public string PrePadding { get; set; }
            public string PostPadding { get; set; }
            public string MaxRecordings { get; set; }
            public string DownloadURL { get; set; }
            public string RecordingFileName { get; set; }
            public int PlaybackPosition { get; set; }
            public int PlaybackDuration { get; set; }
            public string LastWatched { get; set; }
            public bool OnlyNew { get; set; }
            public bool Blue { get; set; }
            public bool Green { get; set; }
            public bool Red { get; set; }
            public bool Yellow { get; set; }
            public string FanArt { get; set; }
        }

        private class EpgEventJSONObject
        {
            public Recurr recurr { get; set; }
            public Rtn rtn { get; set; }
            public EpgEvent2 epgEvent { get; set; }
            public Schd schd { get; set; }
        }

        private class EPGEvent
        {
            public EpgEventJSONObject epgEventJSONObject { get; set; }
        }

        private class ManageResults
        {
            public List<EPGEvent> EPGEvents { get; set; }
        }

        private class RootObject
        {
            public ManageResults ManageResults { get; set; }
        }
    }
}
