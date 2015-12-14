using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.NextPvr.Helpers;

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

        public IEnumerable<ChannelInfo> GetChannels(Stream stream, IJsonSerializer json,ILogger logger)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.channelsJSONObject.rtn != null && root.channelsJSONObject.rtn.Error)
            {
                logger.Error(root.channelsJSONObject.rtn.Message ?? "Failed to download channel information.");
                throw new ApplicationException(root.channelsJSONObject.rtn.Message ?? "Failed to download channel information.");
            }

            if (root.channelsJSONObject != null && root.channelsJSONObject.Channels != null)
            {
                UtilsHelper.DebugInformation(logger,string.Format("[NextPvr] ChannelResponse: {0}", json.SerializeToString(root)));
                return root.channelsJSONObject.Channels.Select(i => new ChannelInfo
                {
                    Name = i.channel.channelName,
                    Number = i.channel.channelFormattedNumber.ToString(_usCulture),
                    Id = i.channel.channelOID.ToString(_usCulture),
                    ImageUrl = string.IsNullOrEmpty(i.channel.channelIcon) ? null : (_baseUrl + "/" + i.channel.channelIcon),
                    HasImage = !string.IsNullOrEmpty(i.channel.channelIcon)
                });
            }

            return new List<ChannelInfo>();
        }

        // Classes created with http://json2csharp.com/
        public class Channel2
        {
            public int channelNum { get; set; }
            public int channelMinor { get; set; }
            public string channelFormattedNumber { get; set; }
            public string channelName { get; set; }
            public int channelOID { get; set; }
            public string channelIcon { get; set; }
        }

        public class Channel
        {
            public Channel2 channel { get; set; }
        }

        public class Rtn
        {
            public bool Error { get; set; }
            public int HTTPStatus { get; set; }
            public string Message { get; set; }
        }

        public class ChannelsJSONObject
        {
            public List<Channel> Channels { get; set; }
            public Rtn rtn { get; set; }
        }

        public class RootObject
        {
            public ChannelsJSONObject channelsJSONObject { get; set; }
        }



    }
}
