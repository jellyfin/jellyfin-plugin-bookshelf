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
using System.Threading.Tasks;

namespace EmbyTV.TunerHost
{
    public class TunerServer
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        public string model { get; set; }
        public string deviceID { get; set; }
        public string firmware { get; set; }
        public string hostname { get; set; }
        public string port { get; set; }
        public List<LiveTvTunerInfo> tuners;
        public bool onlyLoadFavorites { get; set; }

        public TunerServer(string hostname, ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            model = "";
            deviceID = "";
            firmware = "";
            port = "5004";
            tuners = new List<LiveTvTunerInfo>();
            this.hostname = hostname;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }
        public string getWebUrl()
        {
            return "http://" + hostname;
        }
        public string getApiUrl()
        {
            return getWebUrl() + ":" + port;
        }
        public async Task GetDeviceInfo()
        {
            var httpOptions = new HttpRequestOptions() { Url = string.Format("{0}/", getWebUrl()) };
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
        public async Task<List<LiveTvTunerInfo>> GetTunersInfo()
        {
            var httpOptions = new HttpRequestOptions() { Url = string.Format("{0}/tuners.html", getWebUrl()) };
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

        public async Task<IEnumerable<ChannelInfo>> GetChannels()
        {
            List<ChannelInfo> ChannelList;
            var options = new HttpRequestOptions { Url = string.Format("{0}/lineup.json", getWebUrl()) };
            using (var stream = await _httpClient.Get(options))
            {
                var root = _jsonSerializer.DeserializeFromStream<List<Channels>>(stream);
                _logger.Info("Found " + root.Count() + "channels on host: " + hostname);
                if (onlyLoadFavorites) { root.RemoveAll(x => x.Favorite == false); }
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
    }
}
