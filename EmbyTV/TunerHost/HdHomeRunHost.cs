using EmbyTV.General_Helper;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbyTV.Configuration;
using  System.Reflection;
using System.Runtime.CompilerServices;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace EmbyTV.TunerHost
{
    public class HdHomeRunHost:ITunerHost
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        public string deviceType { get; set; }
        public string model { get; set; }
        public string deviceID { get; set; }
        public string firmware { get; set; }
        public string port { get; set; }
        public string Url { get; set; }
        private bool _onlyFavorites { get; set; }
        public string OnlyFavorites { get { return this._onlyFavorites.ToString(); }set { this._onlyFavorites = Convert.ToBoolean(value); } }
        public List<LiveTvTunerInfo> tuners;
        public bool Enabled { get; set; }
        public string HostId {get
        {
            var hostId= model + "-" + deviceID;
            if (hostId == "-")
            {
                hostId = "";
            }
            return hostId; }
            set {  } 
        }

        public HdHomeRunHost(ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            model = "";
            deviceID = "";
            firmware = "";
            port = "5004";
            _onlyFavorites = false;
            tuners = new List<LiveTvTunerInfo>();
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public string getWebUrl()
        {
            return "http://" +Url;
        }
        public string getApiUrl()
        {
            return getWebUrl() + ":" + port;
        }
        
        public async Task GetDeviceInfo(CancellationToken cancellationToken)
        {
            var httpOptions = new HttpRequestOptions()
            {
                Url = string.Format("{0}/", getWebUrl()),
                CancellationToken = cancellationToken
            };
            using (var stream = await _httpClient.Get(httpOptions))
            {
                using (var sr = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = Xml.StripXML(sr.ReadLine());
                        if (line.StartsWith("Model:")) { model = line.Replace("Model: ", ""); }
                        if (line.StartsWith("Device ID:")) { deviceID = line.Replace("Device ID: ", ""); }
                        if (line.StartsWith("Firmware:")) { firmware = line.Replace("Firmware: ", ""); }
                    }
                    if (String.IsNullOrWhiteSpace(model))
                    {
                        throw new ApplicationException("Failed to locate the tuner host.");
                    }
                }
            }
        }
        public async Task<List<LiveTvTunerInfo>> GetTunersInfo(CancellationToken cancellationToken)
        {
            var httpOptions = new HttpRequestOptions()
            {
                Url = string.Format("{0}/tuners.html", getWebUrl()),
                CancellationToken = cancellationToken
            };
            using (var stream = await _httpClient.Get(httpOptions))
            {
                tuners = new List<LiveTvTunerInfo>();
                using (var sr = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = Xml.StripXML(sr.ReadLine());
                        if (line.Contains("Channel"))
                        {
                            LiveTvTunerStatus status;
                            var index = line.IndexOf("Channel");
                            var name = line.Substring(0, index - 1);
                            var currentChannel = line.Substring(index + 7);
                            if (currentChannel != "none") { status = LiveTvTunerStatus.LiveTv; } else { status = LiveTvTunerStatus.Available; }
                            tuners.Add(new LiveTvTunerInfo() { Name = name, SourceType = model, ProgramName = currentChannel, Status = status });
                        }
                    }
                    if (String.IsNullOrWhiteSpace(model))
                    {
                        _logger.Error("Failed to load tuner info");
                    }
                }
                return tuners;
            }
        }


        public async Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken)
        {
            List<ChannelInfo> ChannelList;
            var options = new HttpRequestOptions
            {
                Url = string.Format("{0}/lineup.json", getWebUrl()),
                CancellationToken = cancellationToken
            };
            using (var stream = await _httpClient.Get(options))
            {
                var root = _jsonSerializer.DeserializeFromStream<List<Channels>>(stream);
                _logger.Info("Found " + root.Count() + "channels on host: " + Url);
                _logger.Info("Only Favorites?" + OnlyFavorites);
                if (Convert.ToBoolean(_onlyFavorites)) { root.RemoveAll(x => x.Favorite == false); }
                if (root != null)
                {
                    ChannelList = root.Select(i => new ChannelInfo
                    {
                        Name = i.GuideName,
                        Number = i.GuideNumber.ToString(),
                        Id = i.GuideNumber.ToString(),
                    }).ToList();

                }
                else
                {
                    ChannelList = new List<ChannelInfo>();
                }
                return ChannelList;
            }
        }

        public MediaSourceInfo GetChannelStreamInfo(string ChannelNumber)
        {
            var tunerInfo = GetTunersInfo(CancellationToken.None);
            tunerInfo.Wait();
            if (tuners.FindIndex(t => t.Status == LiveTvTunerStatus.Available) >= 0)
            {
                return new MediaSourceInfo
                    {
                        Path = getApiUrl() + "/auto/v" + ChannelNumber,
                        Protocol = MediaProtocol.Http,
                        MediaStreams = new List<MediaStream>
                        {
                            new MediaStream
                            {
                                Type = MediaStreamType.Video,
                                // Set the index to -1 because we don't know the exact index of the video stream within the container
                                Index = -1,
                                IsInterlaced = true
                            },
                            new MediaStream
                            {
                                Type = MediaStreamType.Audio,
                                // Set the index to -1 because we don't know the exact index of the audio stream within the container
                                Index = -1

                            }
                        }
                    };
            }
            throw new ApplicationException("Host: " + deviceID + " has no tuners avaliable.");
        }
        public class Channels
        {
            public string GuideNumber { get; set; }
            public string GuideName { get; set; }
            public string URL { get; set; }
            public bool Favorite { get; set; }
            public bool DRM { get; set; }
        }

        public void RefreshConfiguration()
        {
            throw new NotImplementedException();
        }
    


}
}
