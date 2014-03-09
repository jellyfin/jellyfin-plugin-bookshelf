using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    class DetailsServiceResponse
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly string _baseUrl;

        public DetailsServiceResponse(string baseUrl)
        {
            _baseUrl = baseUrl;
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
