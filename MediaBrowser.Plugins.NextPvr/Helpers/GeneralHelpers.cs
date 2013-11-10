using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Plugins.NextPvr.Helpers
{
    public static class ChannelHelper
    {
        public static ChannelType GetChannelType(string channelType)
        {
            ChannelType type = new ChannelType(); 

            if (channelType == "0x1")
            {
                type = ChannelType.TV;
            } 
            else if (channelType == "0xa")
            {
               type = ChannelType.Radio; 
            }

            return type;
        }
    }


    public static class RecordingHelper
    {

    }
}
