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
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;

namespace EmbyTV.TunerHost.HostDefinitions
{
    public class Emby:ITunerHost
    {
        private List<LiveTvTunerInfo> tuners;
        private ILogger _logger;
        private IJsonSerializer _jsonSerializer;
        private IHttpClient _httpClient;
        private List<ChannelInfoDto> channels;
        public string Url { get; set; }
        public string ApiKey { get; set; }
        public List<ChannelInfo> ChannelList;

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
        public  Task GetDeviceInfo(System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public string HostId
        {
            get { return Url; }
            set
            {
               
            }
        }

        public bool Enabled{get;set ;}

        public async Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken)
        {
            ChannelList = new List<ChannelInfo>();
           
            var options = new HttpRequestOptions()
            {
                Url = string.Format("http://{0}/LiveTv/Channels?api_key={1}", Url,ApiKey),
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

        public async Task<List<LiveTvTunerInfo>> GetTunersInfo(System.Threading.CancellationToken cancellationToken)
        {
            tuners = new List<LiveTvTunerInfo>();
            var httpOptions = new HttpRequestOptions()
            {
                Url = string.Format("http://{0}/LiveTv/Info?api_key={1}", Url, ApiKey),
                CancellationToken = cancellationToken,
                AcceptHeader = "application/json"
            };
            using (var stream = await _httpClient.Get(httpOptions))
            {

                var root = _jsonSerializer.DeserializeFromStream<DeviceInfoResponse>(stream);
                var services = root.Services;
                var allTuners = services.SelectMany(s => s.Tuners);
                _logger.Info("Found " + services.Count() + " services on host: ");
                _logger.Info("Found " + allTuners.Count() + " tuners on host: ");
                if (allTuners.Any())
                {
                    tuners = allTuners.Select(i => new LiveTvTunerInfo()
                    {
                        Name = i.Name,
                        SourceType = HostId,
                        Id = i.Id,
                        Clients = i.Clients,
                        ChannelId = i.ChannelId,
                        ProgramName = i.ProgramName,
                        Status = i.Status,
                        
                    }).ToList();

                }
            }
           return tuners;
           
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

        public MediaSourceInfo GetChannelStreamInfo(string channelId)
        {
            var tunerInfo = GetTunersInfo(CancellationToken.None);
            tunerInfo.Wait();
            var channel = channels.FirstOrDefault(c => c.Number == channelId);
          
            if (channel != null)
            {
                if (tuners.FindIndex(t => t.Status == LiveTvTunerStatus.Available) >= 0)
                {
                    var Key = channel.Id;
                    return new MediaSourceInfo
                    {
                        Path = string.Format("http://{0}/Videos/{2}/stream.mkv?api_key={1}&MediaSourceId={2}", Url, ApiKey,Key),
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
                throw new ApplicationException("Host: " +HostId + " has no tuners avaliable.");
            } throw new ApplicationException("Host: " + HostId + " doesnt provide this channel");
        }
    }

    public class ChannelResponse
    {
        public List<ChannelInfoDto>  Items { get; set; }
    }
    public class DeviceInfoResponse
    {
        public List<LiveTvServiceInfo> Services { get; set; }
    }
}
