using EmbyTV.GeneralHelpers;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmbyTV.DVR
{
    internal class RecordingHelper
    {
        public static List<TimerInfo> GetTimersForSeries(SeriesTimerInfo seriesTimer, IEnumerable<ProgramInfo> epgData, IReadOnlyList<RecordingInfo> currentRecordings, ILogger logger)
        {
            List<TimerInfo> timers = new List<TimerInfo>();
            var filteredEpg = epgData.Where(epg => epg.Id.Substring(0, 10) == seriesTimer.Id); //Filtered Per Show
            logger.Debug(String.Format("Found {0} episode for show {1}", filteredEpg.Count(), seriesTimer.Id));
            filteredEpg = filteredEpg.Where(epg => seriesTimer.RecordAnyTime || (seriesTimer.StartDate.TimeOfDay == epg.StartDate.TimeOfDay)); //Filtered by Hour
            logger.Debug(String.Format("Found {0} episode for show {1} that meet timer constraint", filteredEpg.Count(), seriesTimer.Id));
            filteredEpg = filteredEpg.Where(epg => !seriesTimer.RecordNewOnly || !epg.IsRepeat); //Filtered by New only
            logger.Debug(String.Format("Found {0} episode for show {1} that meet is new constraint", filteredEpg.Count(), seriesTimer.Id));
            filteredEpg.ToList().ForEach(epg => logger.Debug(String.Format("Day {0} is avaliable", epg.StartDate.DayOfWeek)));
            filteredEpg = filteredEpg.Where(epg => seriesTimer.Days.Contains(epg.StartDate.DayOfWeek)); //Filtered by day of week
            seriesTimer.Days.ForEach(d => logger.Debug(String.Format("Day {0} is included", d)));
            //logger.Debug(String.Format("Day {0} is included", d)
            logger.Debug(String.Format("Found {0} episode for show {1} that meet day constraint", filteredEpg.Count(), seriesTimer.Id));
            filteredEpg = filteredEpg.Where(epg => epg.ChannelId == seriesTimer.ChannelId || seriesTimer.RecordAnyChannel); //Filtered by Channel
            logger.Debug(String.Format("Found {0} episode for show {1} that meet channel constraint", filteredEpg.Count(), seriesTimer.Id));
            filteredEpg = filteredEpg.Where(epg => !currentRecordings.Any(r => r.Id.Substring(0, 14) == epg.Id.Substring(0, 14))); //filtered recordings already running
            logger.Debug(String.Format("Found {0} episode for show {1} that are not already scheduled", filteredEpg.Count(), seriesTimer.Id));
            filteredEpg = filteredEpg.GroupBy(epg => epg.Id.Substring(0, 14)).Select(g => g.First()).ToList();
            logger.Debug(String.Format("Found {0} episode for show {1} that are not duplicates", filteredEpg.Count(), seriesTimer.Id));
            filteredEpg.ToList().ForEach(epg => timers.Add(CreateTimer(epg, seriesTimer)));
            return timers;
        }

        public static DateTime GetStartTime(TimerInfo timer)
        {
            if (timer.StartDate.AddSeconds(-timer.PrePaddingSeconds + 1) < DateTime.UtcNow)
            {
                return DateTime.UtcNow.AddSeconds(1);
            }
            return timer.StartDate.AddSeconds(-timer.PrePaddingSeconds);
        }

        public static TimerInfo CreateTimer(ProgramInfo parent, SeriesTimerInfo series)
        {
            var timer = new TimerInfo();

            timer.ChannelId = parent.ChannelId;
            timer.Id = parent.Id;
            timer.StartDate = parent.StartDate;
            timer.EndDate = parent.EndDate;
            timer.ProgramId = parent.Id;
            timer.PrePaddingSeconds = series.PrePaddingSeconds;
            timer.PostPaddingSeconds = series.PostPaddingSeconds;
            timer.IsPostPaddingRequired = series.IsPostPaddingRequired;
            timer.IsPrePaddingRequired = series.IsPrePaddingRequired;
            timer.Priority = series.Priority;
            timer.Name = parent.Name;
            timer.Overview = parent.Overview;
            timer.SeriesTimerId = series.Id;

            return timer;
        }

        public static string GetRecordingName(TimerInfo timer, ProgramInfo info)
        {
            if (info == null)
            {
                return (timer.ProgramId + ".ts");
            }
            var fancyName = info.Name;
            if (info.ProductionYear != null)
            {
                fancyName += "_(" + info.ProductionYear + ")";
            }
            if (info.IsSeries)
            {
                fancyName += "_" + info.EpisodeTitle.Replace("Season: ", "S").Replace(" Episode: ", "E");
            }
            if (info.IsHD ?? false)
            {
                fancyName += "_HD";
            }
            if (info.OriginalAirDate != null)
            {
                fancyName += "_" + info.OriginalAirDate.Value.ToString("yyyy-MM-dd");
            }
            return StringHelper.RemoveSpecialCharacters(fancyName) + ".ts";
        }
    }
}