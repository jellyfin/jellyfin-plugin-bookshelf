using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
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
        }
    }

    public class Recording
    {
        public TimerInfo TimerInfo;
        DateTime start { get; set; }
        double duration { get; set; }
        string programID { get; set; }
        string Url { get; set; }
        public event EventHandler StartRecording;
        public Recording(TimerInfo info)
        {
            start = info.StartDate.AddSeconds(info.PrePaddingSeconds);
            duration = (info.EndDate - info.StartDate).TotalSeconds + info.PrePaddingSeconds;
            programID = info.Id;
            TimerInfo = info;
        }
    }

    public enum RecordingMethod
    {
        HttpStream = 1
    }
}