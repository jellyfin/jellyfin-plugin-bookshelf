using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class ChannelResponse
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly string _baseUrl;

        public ChannelResponse(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerable<ChannelInfo> GetChannels(Stream stream, IJsonSerializer json)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.channelsJSONObject.rtn != null && root.channelsJSONObject.rtn.Error)
            {
                throw new ApplicationException(root.channelsJSONObject.rtn.Message ?? "Failed to download channel information.");
            }

            if (root.channelsJSONObject != null && root.channelsJSONObject.Channels != null)
            {
                return root.channelsJSONObject.Channels.Select(i => new ChannelInfo
                {
                    Name = i.channel.channelName,
                    Number = i.channel.channelNum.ToString(_usCulture),
                    Id = i.channel.channelOID.ToString(_usCulture),
                    ImageUrl = string.IsNullOrEmpty(i.channel.channelIcon) ? null : (_baseUrl + "/" + i.channel.channelIcon)
                });
            }

            return new List<ChannelInfo>();
        }

        // Classes created with http://json2csharp.com/

        private class Channel2
        {
            public int channelNum { get; set; }
            public string channelName { get; set; }
            public int channelOID { get; set; }
            public string channelIcon { get; set; }
        }

        private class Channel
        {
            public Channel2 channel { get; set; }
        }

        private class Rtn
        {
            public bool Error { get; set; }
            public string Message { get; set; }
        }

        private class ChannelsJSONObject
        {
            public List<Channel> Channels { get; set; }
            public Rtn rtn { get; set; }
        }

        private class RootObject
        {
            public ChannelsJSONObject channelsJSONObject { get; set; }
        }
    }
}
