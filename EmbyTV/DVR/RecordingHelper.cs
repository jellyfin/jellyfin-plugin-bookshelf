using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using System.Timers;
using System.Xml;

namespace EmbyTV.DVR
{
    internal class RecordingHelper
    {
        public static async Task DownloadVideo(IHttpClient httpClient, HttpRequestOptions httpRequestOptions, ILogger logger,string filePath )
        {
            
            //string filePath = Path.GetTempPath()+"/test.ts";
            httpRequestOptions.BufferContent = false;
          
         
            logger.Info("Writing file to path: "+filePath);
            using (var request = httpClient.SendAsync(httpRequestOptions,"GET"))
            {
                using (var output = File.Open(filePath, FileMode.Create,FileAccess.Read))
                {
                    await request.Result.Content.CopyToAsync(output);
                }
            }
        }
    }

    public class SingleTimer:TimerInfo
    {
        private Timer countDown;
        public event EventHandler StartRecording;

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
                var wait = (StartTime() - DateTime.UtcNow).TotalMilliseconds;
                if (wait < 0)
                {wait = 1;}
                countDown = new Timer(wait);
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
            return StartDate.AddSeconds(-PrePaddingSeconds);
        }

        public double Duration()
        {
            return (EndDate - StartDate).TotalSeconds + PrePaddingSeconds;
        }

        public string GetRecordingName()
        {
            return (ProgramId + ".ts");
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