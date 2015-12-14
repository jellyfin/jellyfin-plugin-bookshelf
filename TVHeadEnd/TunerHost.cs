using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TVHeadEnd.HTSP;
using TVHeadEnd.HTSP_Responses;
using TVHeadEnd.TimeoutHelper;


namespace TVHeadEnd
{
    /*
    class TunerHost : ITunerHost
    {
        private readonly ILogger _logger;
        private volatile int _subscriptionId = 0;
        private HTSConnectionHandler _htsConnectionHandler;

        public TunerHost(ILogger logger)
        {
            logger.Info("[TVHclient] TunerHost()");
            _logger = logger;
            _htsConnectionHandler = HTSConnectionHandler.GetInstance(_logger);
        }

        public string Name { get { return "TVHclient-TunerHost"; } }

        public string Type { get { return "Live-TV"; } }

        public Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken)
        {
            int timeOut = _htsConnectionHandler.WaitForInitialLoad(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] TunerHost.GetChannels, call canceled or timed out - returning empty list.");
                return Task.Factory.StartNew<IEnumerable<ChannelInfo>>(() =>
                {
                    return new List<ChannelInfo>();
                });
            }

            return _htsConnectionHandler.BuildChannelInfos(cancellationToken);
        }

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            HTSMessage getTicketMessage = new HTSMessage();
            getTicketMessage.Method = "getTicket";
            getTicketMessage.putField("channelId", channelId);

            LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
            _htsConnectionHandler.SendMessage(getTicketMessage, lbrh);
            HTSMessage getTicketResponse = lbrh.getResponse();
            

                if (_subscriptionId == int.MaxValue)
                {
                    _subscriptionId = 0;
                }
                int currSubscriptionId = _subscriptionId++;

            return Task.Factory.StartNew<MediaSourceInfo>(() =>
            {
                return new MediaSourceInfo
                {
                    Id = "" + currSubscriptionId,
                    Path = _htsConnectionHandler.GetHttpBaseUrl() + getTicketResponse.getString("path") + "?ticket=" + getTicketResponse.getString("ticket"),
                    Protocol = MediaProtocol.Http,
                    MediaStreams = new List<MediaStream>
                        {
                            new MediaStream
                            {
                                Type = MediaStreamType.Video,
                                // Set the index to -1 because we don't know the exact index of the video stream within the container
                                Index = -1,
                                // Set to true if unknown to enable deinterlacing
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
            });

            throw new TimeoutException("");
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            _logger.Fatal("[TVHclient] TunerHost.GetChannelStreamMediaSources called for channelID '" + channelId + "'");
            throw new NotImplementedException();
        }

        public Task<List<LiveTvTunerInfo>> GetTunerInfos(CancellationToken cancellationToken)
        {
            // TVHeadend can't deliver TunerInfo data
            throw new NotImplementedException();
        }

        public Task Validate(TunerHostInfo info)
        {
            // TVHeadend can't deliver TunerInfo data
            throw new NotImplementedException();
        }
    }
    */
}
