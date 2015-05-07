using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using System.Timers;
using Timer = System.Timers.Timer;
using EmbyTV.GeneralHelpers;

namespace EmbyTV.DVR
{
    internal class RecordingHelper
    {
        public static async Task DownloadVideo(IHttpClient httpClient, HttpRequestOptions httpRequestOptions, ILogger logger, string filePath, CancellationToken cancellationToken)
        {

            //string filePath = Path.GetTempPath()+"/test.ts";
            httpRequestOptions.BufferContent = false;
            httpRequestOptions.CancellationToken = cancellationToken;
            logger.Info("Writing file to path: " + filePath);
            using (var response = await httpClient.SendAsync(httpRequestOptions, "GET"))
            {
                using (var output = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await response.Content.CopyToAsync(output, 4096, cancellationToken);
                }
            }
        }
    }

    public class SingleTimer : TimerInfo
    {
        private Timer countDown;
        public event EventHandler StartRecording;
        public CancellationTokenSource Cts = new CancellationTokenSource();


        public SingleTimer()
        {

        }
        public SingleTimer(TimerInfo parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
            {
                GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(parent, null), null);
            }
            this.Id = parent.ProgramId;
        }
        public SingleTimer(ProgramInfo parent, SeriesTimerInfo series)
        {
            ChannelId = parent.ChannelId;
            Id = parent.Id;
            StartDate = parent.StartDate;
            EndDate = parent.EndDate;
            ProgramId = parent.Id;
            PrePaddingSeconds = series.PrePaddingSeconds;
            PostPaddingSeconds = series.PostPaddingSeconds;
            IsPostPaddingRequired = series.IsPostPaddingRequired;
            IsPrePaddingRequired = series.IsPrePaddingRequired;
            Priority = series.Priority;
            Name = parent.Name;
            Overview = parent.Overview;
            SeriesTimerId = series.Id;
        }

        public void CopyTimer(TimerInfo parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
            {
                GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(parent, null), null);
            }
            this.Id = parent.ProgramId;
        }

        public void GenerateEvent()
        {
            if (StartRecording != null)
            {
                countDown = new Timer((StartTime() - DateTime.UtcNow).TotalMilliseconds);
                countDown.Elapsed += sendSignal;
                countDown.AutoReset = false;
                countDown.Start();
            }
        }

        private void sendSignal(object obj, ElapsedEventArgs arg)
        {
            StartRecording(this, null);
        }

        public DateTime StartTime()
        {
            if (StartDate.AddSeconds(-PrePaddingSeconds + 1) < DateTime.UtcNow)
            {
                return DateTime.UtcNow.AddSeconds(1);
            }
            return StartDate.AddSeconds(-PrePaddingSeconds);
        }

        public double Duration()
        {
            return (EndDate - StartTime()).TotalSeconds + PrePaddingSeconds;
        }

        public string GetRecordingName(ProgramInfo info)
        {
            if (info == null)
            {
                return (ProgramId + ".ts");
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

    public class SeriesTimer : SeriesTimerInfo
    {
        public SeriesTimer()
        {
        }
        public SeriesTimer(SeriesTimerInfo parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
            {
                GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(parent, null), null);
            }
            Id = parent.ProgramId.Substring(0, 10);
        }
        public void CopyTimer(SeriesTimerInfo parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
            {
                GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(parent, null), null);
            }
        }

        public List<SingleTimer> GetTimersForSeries(IEnumerable<ProgramInfo> epgData, List<RecordingInfo> currentRecordings, ILogger logger )
        {
            List<SingleTimer> timers = new List<SingleTimer>();
            var filteredEpg = epgData.Where(epg => epg.Id.Substring(0, 10) == Id); //Filtered Per Show
            logger.Debug(String.Format("Found {0} episode for show {1}",filteredEpg.Count(),Id));
            filteredEpg = filteredEpg.Where(epg => RecordAnyTime || (StartDate.TimeOfDay == epg.StartDate.TimeOfDay)); //Filtered by Hour
            logger.Debug(String.Format("Found {0} episode for show {1} that meet timer constraint",filteredEpg.Count(),Id));
            filteredEpg = filteredEpg.Where(epg => !RecordNewOnly || !epg.IsRepeat); //Filtered by New only
            logger.Debug(String.Format("Found {0} episode for show {1} that meet is new constraint",filteredEpg.Count(),Id));
            filteredEpg.ToList().ForEach(epg => logger.Debug(String.Format("Day {0} is avaliable", epg.StartDate.DayOfWeek)));
            filteredEpg = filteredEpg.Where(epg => Days.Contains(epg.StartDate.DayOfWeek)); //Filtered by day of week
            Days.ForEach(d => logger.Debug(String.Format("Day {0} is included", d)));
            //logger.Debug(String.Format("Day {0} is included", d)
            logger.Debug(String.Format("Found {0} episode for show {1} that meet day constraint",filteredEpg.Count(),Id));
            filteredEpg = filteredEpg.Where(epg => epg.ChannelId == ChannelId ||RecordAnyChannel); //Filtered by Channel
            logger.Debug(String.Format("Found {0} episode for show {1} that meet channel constraint",filteredEpg.Count(),Id));
            filteredEpg = filteredEpg.Where(epg => !currentRecordings.Any(r => r.Id.Substring(0, 14) == epg.Id.Substring(0, 14))); //filtered recordings already running
            logger.Debug(String.Format("Found {0} episode for show {1} that are not already scheduled",filteredEpg.Count(),Id));
            filteredEpg = filteredEpg.GroupBy(epg => epg.Id.Substring(0,14)).Select(g => g.First()).ToList();
            logger.Debug(String.Format("Found {0} episode for show {1} that are not duplicates",filteredEpg.Count(),Id));
            filteredEpg.ToList().ForEach(epg => timers.Add(new SingleTimer(epg,this)));
            return timers;
        }




    }

    public enum RecordingMethod
    {
        HttpStream = 1
    }
}