using EmbyTV.General_Helper;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbyTV.Configuration;
using  System.Reflection;
using System.Runtime.CompilerServices;

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
                CreateTuners(stream);
                return tuners;
            }
        }
        private void CreateTuners(Stream tunersXML)
        {
            int numberOfTuners = 3;
            while (tuners.Count() < numberOfTuners)
            {
                tuners.Add(new LiveTvTunerInfo() { Name = "Tunner " + tuners.Count, SourceType = model });
            }
            using (var sr = new StreamReader(tunersXML, System.Text.Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    string line = Xml.StripXML(sr.ReadLine());
                    if (line.StartsWith("Tuner 0 Channel")) { CheckTuner(0, line); }
                    if (line.StartsWith("Tuner 1 Channel")) { CheckTuner(1, line); }
                    if (line.StartsWith("Tuner 2 Channel")) { CheckTuner(2, line); }
                }
                if (String.IsNullOrWhiteSpace(model))
                {
                    _logger.Error("Failed to load tuner info");
                    throw new ApplicationException("Failed to load tuner info.");
                }
            }
        }
        private void CheckTuner(int tunerPos, string tunerInfo)
        {
            string currentChannel;
            LiveTvTunerStatus status;
            currentChannel = tunerInfo.Replace("Tuner " + tunerPos + " Channel", "");
            if (currentChannel != "none") { status = LiveTvTunerStatus.LiveTv; } else { status = LiveTvTunerStatus.Available; }
            tuners[tunerPos].ProgramName = currentChannel;
            tuners[tunerPos].Status = status;
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
                        ImageUrl = null,
                        HasImage = false
                    }).ToList();

                }
                else
                {
                    ChannelList = new List<ChannelInfo>();
                }
                return ChannelList;
            }
        }

        public string getChannelStreamInfo(string ChannelNumber)
        {
            return getApiUrl() + "/auto/v" + ChannelNumber;
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
