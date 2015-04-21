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
using EmbyTV.GeneralHelpers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace EmbyTV.TunerHost.HostDefinitions
{
    public class M3UPlaylist : ITunerHost
    {
        public string PlaylistPath { get; set; }
        private IJsonSerializer jsonSerializer;
        private IHttpClient httpClient;
        private List<LiveTvTunerInfo> tuners;
        private ILogger logger;
        public bool Enabled { get; set; }
        private List<M3UChannel> channels;
        private string _id; 


        public M3UPlaylist(ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            tuners = new List<LiveTvTunerInfo>();
            this.logger = logger;
            this.jsonSerializer = jsonSerializer;
            this.httpClient = httpClient;
            _id = "none";
            channels = new List<M3UChannel>();
        }

        public M3UPlaylist()
        {
            
        }
        public Task GetDeviceInfo(CancellationToken cancellationToken)
        {
            GetChannels(cancellationToken);
            return Task.FromResult(0);
        }

        public string HostId
        {
            get { return _id; }
            set { }
        }



        public Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken)
        {
            int position = 0;
            string line;
            // Read the file and display it line by line.
            System.IO.StreamReader file = new StreamReader(PlaylistPath);
            channels = new List<M3UChannel>();
            while ((line = file.ReadLine()) != null)
            {
                line = line.Trim();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    if (position == 0 && !line.StartsWith("#EXTM3U"))
                    {
                        throw new ApplicationException("wrong file");
                    }
                    if (position % 2 == 0)
                    {
                        if (position != 0)
                        {
                            channels.Last().Path = line;
                        }
                        else
                        {
                            line = line.Replace("#EXTM3U", "");
                            line = line.Trim();
                            var vars = line.Split(' ').ToList();
                            foreach (var variable in vars)
                            {
                                var list = variable.Replace('"', ' ').Split('=');
                                switch (list[0])
                                {
                                    case ("id"):
                                        _id = list[1];
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!line.StartsWith("#EXTINF:")) { throw new ApplicationException("Bad file"); }
                        line = line.Replace("#EXTINF:", "");
                        var nameStart = line.LastIndexOf(',');
                        line = line.Substring(0, nameStart);
                        var vars = line.Split(' ').ToList();
                        vars.RemoveAt(0);
                        channels.Add(new M3UChannel());
                        foreach (var variable in vars)
                        {
                            var list = variable.Replace('"', ' ').Split('=');
                            switch (list[0])
                            {
                                case "tvg-id":
                                    channels.Last().Id = list[1];
                                    channels.Last().Number = list[1];
                                    break;
                                case "tvg-name":
                                    channels.Last().Name = list[1];
                                    break;
                            }
                        }
                    }
                    position++;
                }
            }
            file.Close();
            return Task.FromResult((IEnumerable<ChannelInfo>)channels);
        }



        public Task<List<LiveTvTunerInfo>> GetTunersInfo(CancellationToken cancellationToken)
        {
            tuners = new List<LiveTvTunerInfo>();
            tuners.Add(new LiveTvTunerInfo()
            {
                Name = "VirtualTuner",
                SourceType = "IPTV",
                ProgramName = "TEST",
                Status = LiveTvTunerStatus.Available
            });
            return Task.FromResult(tuners);
        }

        public string getWebUrl()
        {
            return "localhost";
        }

        public MediaBrowser.Model.Dto.MediaSourceInfo GetChannelStreamInfo(string channelId)
        {
            var channel = channels.FirstOrDefault(c => c.Id == channelId);
            if (channel != null)
            {
                var path = channel.Path;
                MediaProtocol protocol = MediaProtocol.File;
                if (path.StartsWith("http"))
                {
                    protocol = MediaProtocol.Http;
                }
                else if (path.StartsWith("rtmp"))
                {
                    protocol = MediaProtocol.Rtmp;
                }
                else if (path.StartsWith("rtsp"))
                {
                    protocol = MediaProtocol.Rtsp;
                }

                return new MediaSourceInfo
                {
                    Path = channel.Path,
                    Protocol = protocol,
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
            throw new ApplicationException("Host doesnt provide this channel");
        }
        public IEnumerable<ConfigurationField> GetFieldBuilder()
        {
            List<ConfigurationField> userFields = new List<ConfigurationField>()
            {
                new ConfigurationField()
                {
                    Name = "PlaylistPath",
                    Type = FieldType.Text,
                    defaultValue = "",
                    Description = "File Path for M3U file",
                    Label = "Filepath"
                }
            };
          return userFields;
        }
    }

    class M3UChannel : ChannelInfo
    {
        public string Path { get; set; }

        public M3UChannel()
        {
        }
    }

}

