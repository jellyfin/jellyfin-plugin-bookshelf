using System;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Plugins.NextPvr
{
    public class ChannelResponse
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

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
                    Id = i.channel.channelOID.ToString(_usCulture)
                });
            }

            return new List<ChannelInfo>();
        }

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
