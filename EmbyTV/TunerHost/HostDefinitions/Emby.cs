using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbyTV.Configuration;
using EmbyTV.GeneralHelpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyTV.TunerHost.HostDefinitions
{
    class Emby:ITunerHost
    {
        private List<LiveTvTunerInfo> tuners;
        private ILogger _logger;
        private IJsonSerializer _jsonSerializer;
        private IHttpClient _httpClient;
        private List<ChannelInfoDto> channels;
        public string Url { get; set; }
        public string ApiKey { get; set; }

         public Emby(ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            tuners = new List<LiveTvTunerInfo>();
           _logger = logger;
            _jsonSerializer = jsonSerializer;
           _httpClient = httpClient;
            channels = new List<ChannelInfoDto>();
             ApiKey = "";
             Url = "";
        }

        public Emby()
        {
            
        }
        public Task GetDeviceInfo(System.Threading.CancellationToken cancellationToken)
        {
           return Task.FromResult(0);
        }

        public string HostId
        {
            get { return "Emby Server"; }
            set
            {
               
            }
        }

        public bool Enabled{get;set ;}

        public async Task<IEnumerable<MediaBrowser.Controller.LiveTv.ChannelInfo>> GetChannels(System.Threading.CancellationToken cancellationToken)
        {
            var ChannelList = new List<ChannelInfo>();
            var options = new HttpRequestOptions()
            {
                Url = string.Format("http://{0}:8096/LiveTv/Channels?api_key={1}", Url,ApiKey),
                CancellationToken = cancellationToken,
                AcceptHeader = "application/json"
            };
           
            using (var stream = await _httpClient.Get(options))
            {

                var root = _jsonSerializer.DeserializeFromStream<ChannelResponse>(stream);
                channels = root.Items;
                _logger.Info("Found " + root.Items.Count() + "channels on host: " );
                if (root.Items != null)
                {
                    ChannelList = root.Items.Select(i => new ChannelInfo
                    {
                        Name = i.Name,
                        Number = i.Number,
                        Id = i.Number
                    }).ToList();

                }
                return ChannelList;
            }
        }

        public Task<List<LiveTvTunerInfo>> GetTunersInfo(System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(tuners);
        }

        public IEnumerable<Configuration.ConfigurationField> GetFieldBuilder()
        {
            List<ConfigurationField> userFields = new List<ConfigurationField>()
            {
                new ConfigurationField()
                {
                    Name = "Url",
                    Type = FieldType.Text,
                    defaultValue = "",
                    Description = "Emby URL",
                    Label = "Hostname"
                },
                new ConfigurationField()
                {
                    Name = "ApiKey",
                    Type = FieldType.Text,
                    defaultValue = "",
                    Description = "Api Key",
                    Label = "Api Key"
                }
            };
          return userFields;
        }

        public string getWebUrl()
        {
            return Url;
        }

        public MediaBrowser.Model.Dto.MediaSourceInfo GetChannelStreamInfo(string channelId)
        {
            throw new NotImplementedException();
        }
    }

    public class ChannelResponse
    {
        public List<ChannelInfoDto>  Items { get; set; }
    }
}
