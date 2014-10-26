using System.Globalization;
using System.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.NextPvr.Helpers;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class TimerDefaultsResponse
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public SeriesTimerInfo GetDefaultTimerInfo(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = GetScheduleSettings(stream, json);
            UtilsHelper.DebugInformation(logger,string.Format("[NextPvr] GetDefaultTimerInfo Response: {0}", json.SerializeToString(root)));

            return new SeriesTimerInfo
            {
                PostPaddingSeconds = root.post_padding_min * 60,
                PrePaddingSeconds = root.pre_padding_min * 60,
                RecordAnyChannel = root.allChannels,
                RecordAnyTime = root.recordAnyTimeslot,
                RecordNewOnly = root.onlyNew
            };
        }

        public ScheduleSettings GetScheduleSettings(Stream stream, IJsonSerializer json)
        {
            return json.DeserializeFromStream<ScheduleSettings>(stream);
        }
    }

    // Classes created with http://json2csharp.com/

    public class ScheduleSettings
    {
        public int ChannelOID { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public object manualRecTitle { get; set; }
        public object epgeventOID { get; set; }
        public object scheduleOID { get; set; }
        public object recurrOID { get; set; }
        public object recDirId { get; set; }
        public object rules { get; set; }
        public object recurringName { get; set; }
        public bool qualityDefault { get; set; }
        public bool qualityGood { get; set; }
        public bool qualityBetter { get; set; }
        public bool qualityBest { get; set; }
        public int pre_padding_min { get; set; }
        public int post_padding_min { get; set; }
        public int extend_end_time_min { get; set; }
        public bool keep_all_days { get; set; }
        public int days_to_keep { get; set; }
        public bool onlyNew { get; set; }
        public bool recordOnce { get; set; }
        public bool recordThisTimeslot { get; set; }
        public bool recordAnyTimeslot { get; set; }
        public bool recordThisDay { get; set; }
        public bool recordAnyDay { get; set; }
        public bool recordSpecificdays { get; set; }
        public bool dayMonday { get; set; }
        public bool dayTuesday { get; set; }
        public bool dayWednesday { get; set; }
        public bool dayThursday { get; set; }
        public bool dayFriday { get; set; }
        public bool daySaturday { get; set; }
        public bool daySunday { get; set; }
        public bool allChannels { get; set; }
        public bool recColorRed { get; set; }
        public bool recColorYellow { get; set; }
        public bool recColorGreen { get; set; }
        public bool recColorBlue { get; set; }
    }
}
