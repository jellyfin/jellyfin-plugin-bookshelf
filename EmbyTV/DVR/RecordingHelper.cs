using System;
using System.IO;
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
            using (var request = httpClient.SendAsync(httpRequestOptions, "GET"))
            {
                using (var output = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await request.Result.Content.CopyToAsync(output, 4096, cancellationToken);
                    output.Dispose();
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
        }

        public void CopyTimer(TimerInfo parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
            {
                GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(parent, null), null);
            }
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
        }
        public void CopyTimer(SeriesTimerInfo parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
            {
                GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(parent, null), null);
            }
        }




    }

    public enum RecordingMethod
    {
        HttpStream = 1
    }
}