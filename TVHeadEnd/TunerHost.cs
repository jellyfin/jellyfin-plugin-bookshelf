using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TVHeadEnd
{
    class TunerHost : ITunerHost
    {
        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<LiveTvTunerInfo>> GetTunerInfos(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task Validate(TunerHostInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
