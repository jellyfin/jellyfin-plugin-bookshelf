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


namespace EmbyTV.DVR
{
    internal class RecordingHelper
    {
        public static async Task DownloadVideo(IHttpClient httpClient, HttpRequestOptions httpRequestOptions, ILogger logger,string filePath )
        {
            
            //string filePath = Path.GetTempPath()+"/test.ts";
            logger.Info("Writing file to path: "+filePath);
            using (var request = httpClient.SendAsync(httpRequestOptions, "GET"))
            {
                using (var output = File.Open(filePath, FileMode.Create))
                {
                    await request.Result.Content.CopyToAsync(output);
                }
            }
            TimerInfo test = new Recording();
        }
    }

    public class Recording:TimerInfo
    {
        public event EventHandler StartRecording;

        public Recording() 
        {
            
        }
        public Recording(TimerInfo parent)
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

        public DateTime StartTime()
        {
            return StartDate.AddSeconds(PrePaddingSeconds);
        }

        public double Duration()
        {
            return (EndDate - StartDate).TotalSeconds + PrePaddingSeconds;
        }

    }

    public class RecordingSeries : SeriesTimerInfo
    {
        public RecordingSeries()
        {
        }
        public RecordingSeries(SeriesTimerInfo parent)
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