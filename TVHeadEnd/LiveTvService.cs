using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TVHeadEnd.Helper;
using TVHeadEnd.HTSP;
using TVHeadEnd.HTSP_Responses;
using TVHeadEnd.TimeoutHelper;

namespace TVHeadEnd
{
    public class LiveTvService : ILiveTvService
    {
        public event EventHandler DataSourceChanged;
        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        //Added for stream probing
        private readonly IMediaEncoder _mediaEncoder;
        
        private readonly TimeSpan TIMEOUT = TimeSpan.FromMinutes(5);

        private HTSConnectionHandler _htsConnectionHandler;
        private volatile int _subscriptionId = 0;

        private readonly ILogger _logger;

        public LiveTvService(ILogger logger, IMediaEncoder mediaEncoder)
        {
            //System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            logger.Info("[TVHclient] LiveTvService()");

            _logger = logger;

            _htsConnectionHandler = HTSConnectionHandler.GetInstance(_logger);
            _htsConnectionHandler.setLiveTvService(this);

            //Added for stream probing
            _mediaEncoder = mediaEncoder;
        }

        public string HomePageUrl { get { return "http://tvheadend.org/"; } }

        public string Name { get { return "TVHclient LiveTvService"; } }

        public void sendDataSourceChanged()
        {
            try
            {
                _logger.Fatal("[TVHclient] sendDataSourceChanged 1");
                if (DataSourceChanged != null)
                {
                    _logger.Fatal("[TVHclient] sendDataSourceChanged 2");
                    DataSourceChanged(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("[TVHclient] LiveTvService.sendDataSourceChanged caught exception: " + ex.Message);
            }
        }

        public void sendRecordingStatusChanged(RecordingStatusChangedEventArgs recordingStatusChangedEventArgs)
        {
            try
            {
                _logger.Fatal("[TVHclient] sendRecordingStatusChanged 1");
                if (RecordingStatusChanged != null)
                {
                    _logger.Fatal("[TVHclient] sendRecordingStatusChanged 2");
                    RecordingStatusChanged(this, recordingStatusChangedEventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("[TVHclient] LiveTvService.sendRecordingStatusChanged caught exception: " + ex.Message);
            }
        }

        public async Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] CancelSeriesTimerAsync, call canceled or timed out.");
                return;
            }

            HTSMessage deleteAutorecMessage = new HTSMessage();
            deleteAutorecMessage.Method = "deleteAutorecEntry";
            deleteAutorecMessage.putField("id", timerId);

            TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            TaskWithTimeoutResult<HTSMessage> twtRes = await twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnectionHandler.SendMessage(deleteAutorecMessage, lbrh);
                return lbrh.getResponse();
            }));

            if (twtRes.HasTimeout)
            {
                _logger.Error("[TVHclient] Can't delete recording because of timeout");
            }
            else
            {
                HTSMessage deleteAutorecResponse = twtRes.Result;
                Boolean success = deleteAutorecResponse.getInt("success", 0) == 1;
                if (!success)
                {
                    _logger.Error("[TVHclient] Can't cancel timer: '" + deleteAutorecResponse.getString("error") + "'");
                }
            }
        }

        public async Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] CancelTimerAsync, call canceled or timed out.");
                return;
            }

            HTSMessage cancelTimerMessage = new HTSMessage();
            cancelTimerMessage.Method = "cancelDvrEntry";
            cancelTimerMessage.putField("id", timerId);

            TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            TaskWithTimeoutResult<HTSMessage> twtRes = await twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnectionHandler.SendMessage(cancelTimerMessage, lbrh);
                return lbrh.getResponse();
            }));

            if (twtRes.HasTimeout)
            {
                _logger.Error("[TVHclient] Can't cancel timer because of timeout");
            }
            else
            {
                HTSMessage cancelTimerResponse = twtRes.Result;
                Boolean success = cancelTimerResponse.getInt("success", 0) == 1;
                if (!success)
                {
                    _logger.Error("[TVHclient] Can't cancel timer: '" + cancelTimerResponse.getString("error") + "'");
                }
            }
        }

        public async Task CloseLiveStream(string subscriptionId, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew<string>(() =>
            {
                //_logger.Info("[TVHclient] CloseLiveStream for subscriptionId = " + subscriptionId);
                return subscriptionId;
            });
        }

        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            // Dummy method to avoid warnings
            await Task.Factory.StartNew<int>(() => { return 0; });

            throw new NotImplementedException();


            //int timeOut = await WaitForInitialLoadTask(cancellationToken);
            //if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            //{
            //    _logger.Info("[TVHclient] CreateSeriesTimerAsync, call canceled or timed out - returning empty list.");
            //    return;
            //}

            ////_logger.Info("[TVHclient] CreateSeriesTimerAsync: got SeriesTimerInfo: " + dump(info));

            //HTSMessage createSeriesTimerMessage = new HTSMessage();
            //createSeriesTimerMessage.Method = "addAutorecEntry";
            //createSeriesTimerMessage.putField("title", info.Name);
            //if (!info.RecordAnyChannel)
            //{
            //    createSeriesTimerMessage.putField("channelId", info.ChannelId);
            //}
            //createSeriesTimerMessage.putField("minDuration", 0);
            //createSeriesTimerMessage.putField("maxDuration", 0);

            //int tempPriority = info.Priority;
            //if (tempPriority == 0)
            //{
            //    tempPriority = _priority; // info.Priority delivers 0 if timers is newly created - no GUI
            //}
            //createSeriesTimerMessage.putField("priority", tempPriority);
            //createSeriesTimerMessage.putField("configName", _profile);
            //createSeriesTimerMessage.putField("daysOfWeek", AutorecDataHelper.getDaysOfWeekFromList(info.Days));

            //if (!info.RecordAnyTime)
            //{
            //    createSeriesTimerMessage.putField("approxTime", AutorecDataHelper.getMinutesFromMidnight(info.StartDate));
            //}
            //createSeriesTimerMessage.putField("startExtra", (long)(info.PrePaddingSeconds / 60L));
            //createSeriesTimerMessage.putField("stopExtra", (long)(info.PostPaddingSeconds / 60L));
            //createSeriesTimerMessage.putField("comment", info.Overview);


            ////_logger.Info("[TVHclient] CreateSeriesTimerAsync: created HTSP message: " + createSeriesTimerMessage.ToString());


            ///*
            //        public DateTime EndDate { get; set; }
            //        public string ProgramId { get; set; }
            //        public bool RecordNewOnly { get; set; } 
            // */

            ////HTSMessage createSeriesTimerResponse = await Task.Factory.StartNew<HTSMessage>(() =>
            ////{
            ////    LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
            ////    _htsConnection.sendMessage(createSeriesTimerMessage, lbrh);
            ////    return lbrh.getResponse();
            ////});

            //TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            //TaskWithTimeoutResult<HTSMessage> twtRes = await  twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            //{
            //    LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
            //    _htsConnection.sendMessage(createSeriesTimerMessage, lbrh);
            //    return lbrh.getResponse();
            //}));

            //if (twtRes.HasTimeout)
            //{
            //    _logger.Error("[TVHclient] Can't create series because of timeout");
            //}
            //else
            //{
            //    HTSMessage createSeriesTimerResponse = twtRes.Result;
            //    Boolean success = createSeriesTimerResponse.getInt("success", 0) == 1;
            //    if (!success)
            //    {
            //        _logger.Error("[TVHclient] Can't create series timer: '" + createSeriesTimerResponse.getString("error") + "'");
            //    }
            //}
        }

        public async Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] CreateTimerAsync, call canceled or timed out.");
                return;
            }

            HTSMessage createTimerMessage = new HTSMessage();
            createTimerMessage.Method = "addDvrEntry";
            createTimerMessage.putField("channelId", info.ChannelId);
            createTimerMessage.putField("start", DateTimeHelper.getUnixUTCTimeFromUtcDateTime(info.StartDate));
            createTimerMessage.putField("stop", DateTimeHelper.getUnixUTCTimeFromUtcDateTime(info.EndDate));
            createTimerMessage.putField("startExtra", (long)(info.PrePaddingSeconds / 60));
            createTimerMessage.putField("stopExtra", (long)(info.PostPaddingSeconds / 60));
            createTimerMessage.putField("priority", _htsConnectionHandler.GetPriority()); // info.Priority delivers always 0 - no GUI
            createTimerMessage.putField("configName", _htsConnectionHandler.GetProfile());
            createTimerMessage.putField("description", info.Overview);
            createTimerMessage.putField("title", info.Name);
            createTimerMessage.putField("creator", Plugin.Instance.Configuration.Username);

            TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            TaskWithTimeoutResult<HTSMessage> twtRes = await twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnectionHandler.SendMessage(createTimerMessage, lbrh);
                return lbrh.getResponse();
            }));

            if (twtRes.HasTimeout)
            {
                _logger.Error("[TVHclient] Can't create timer because of timeout");
            }
            else
            {
                HTSMessage createTimerResponse = twtRes.Result;
                Boolean success = createTimerResponse.getInt("success", 0) == 1;
                if (!success)
                {
                    _logger.Error("[TVHclient] Can't create timer: '" + createTimerResponse.getString("error") + "'");
                }
            }
        }

        public async Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] DeleteRecordingAsync, call canceled or timed out.");
                return;
            }

            HTSMessage deleteRecordingMessage = new HTSMessage();
            deleteRecordingMessage.Method = "deleteDvrEntry";
            deleteRecordingMessage.putField("id", recordingId);

            TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            TaskWithTimeoutResult<HTSMessage> twtRes = await twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnectionHandler.SendMessage(deleteRecordingMessage, lbrh);
                return lbrh.getResponse();
            }));

            if (twtRes.HasTimeout)
            {
                _logger.Error("[TVHclient] Can't delete recording because of timeout");
            }
            else
            {
                HTSMessage deleteRecordingResponse = twtRes.Result;
                Boolean success = deleteRecordingResponse.getInt("success", 0) == 1;
                if (!success)
                {
                    _logger.Error("[TVHclient] Can't delete recording: '" + deleteRecordingResponse.getString("error") + "'");
                }
            }
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            string piconData = _htsConnectionHandler.GetPiconData(channelId);
            if (piconData == null)
            {
                // Leave as is. This is handled by supplying image url to ChannelInfo
                throw new NotImplementedException();
            }

            _logger.Info("[TVHclient] LiveTvService.GetChannelImageAsync called for channelID '" + channelId + "'  piconData '" + piconData + "'");

            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] GetChannelsAsync, call canceled or timed out - returning empty list.");
                return new List<ChannelInfo>();
            }

            TaskWithTimeoutRunner<IEnumerable<ChannelInfo>> twtr = new TaskWithTimeoutRunner<IEnumerable<ChannelInfo>>(TIMEOUT);
            TaskWithTimeoutResult<IEnumerable<ChannelInfo>> twtRes = await
                twtr.RunWithTimeout(_htsConnectionHandler.BuildChannelInfos(cancellationToken));

            if (twtRes.HasTimeout)
            {
                return new List<ChannelInfo>();
            }

            return twtRes.Result;
        }

        public async Task<MediaSourceInfo> GetChannelStream(string channelId, string mediaSourceId, CancellationToken cancellationToken)
        {
            HTSMessage getTicketMessage = new HTSMessage();
            getTicketMessage.Method = "getTicket";
            getTicketMessage.putField("channelId", channelId);

            TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            TaskWithTimeoutResult<HTSMessage> twtRes = await twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnectionHandler.SendMessage(getTicketMessage, lbrh);
                return lbrh.getResponse();
            }));

            if (twtRes.HasTimeout)
            {
                _logger.Error("[TVHclient] Timeout obtaining playback authentication ticket from TVH");
            }
            else
            {
                HTSMessage getTicketResponse = twtRes.Result;

                if (_subscriptionId == int.MaxValue)
                {
                    _subscriptionId = 0;
                }
                int currSubscriptionId = _subscriptionId++;

                if (_htsConnectionHandler.GetEnableSubsMaudios())
                {

                    _logger.Info("[TVHclient] Support for live TV subtitles and multiple audio tracks is enabled.");

                    MediaSourceInfo livetvasset = new MediaSourceInfo();

                    livetvasset.Id = "" + currSubscriptionId;

                    // Use HTTP basic auth instead of TVH ticketing system for authentication to allow the users to switch subs or audio tracks at any time
                    livetvasset.Path = _htsConnectionHandler.GetHttpBaseUrl() + getTicketResponse.getString("path");
                    livetvasset.Protocol = MediaProtocol.Http;

                    // Probe the asset stream to determine available sub-streams
                    string livetvasset_probeUrl = "" + livetvasset.Path;
                    string livetvasset_source = "LiveTV";

                    // Probe the asset stream to determine available sub-streams
                    await ProbeStream(livetvasset, livetvasset_probeUrl, livetvasset_source, cancellationToken);

                    return livetvasset;

                }
                else
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

                }
            }

            throw new TimeoutException("");
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            return await Task.Factory.StartNew<SeriesTimerInfo>(() =>
            {
                return new SeriesTimerInfo
                {
                    PostPaddingSeconds = 0,
                    PrePaddingSeconds = 0,
                    RecordAnyChannel = true,
                    RecordAnyTime = true,
                    RecordNewOnly = false
                };
            });
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ProgramInfo
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] GetProgramsAsync, call canceled or timed out - returning empty list.");
                return new List<ProgramInfo>();
            }

            GetEventsResponseHandler currGetEventsResponseHandler = new GetEventsResponseHandler(startDateUtc, endDateUtc, _logger, cancellationToken);

            HTSMessage queryEvents = new HTSMessage();
            queryEvents.Method = "getEvents";
            queryEvents.putField("channelId", Convert.ToInt32(channelId));
            _htsConnectionHandler.SendMessage(queryEvents, currGetEventsResponseHandler);

            _logger.Info("[TVHclient] GetProgramsAsync, ask TVH for events of channel '" + channelId + "'.");

            TaskWithTimeoutRunner<IEnumerable<ProgramInfo>> twtr = new TaskWithTimeoutRunner<IEnumerable<ProgramInfo>>(TIMEOUT);
            TaskWithTimeoutResult<IEnumerable<ProgramInfo>> twtRes = await
                twtr.RunWithTimeout(currGetEventsResponseHandler.GetEvents(cancellationToken, channelId));

            if (twtRes.HasTimeout)
            {
                _logger.Info("[TVHclient] GetProgramsAsync, timeout during call for events of channel '" + channelId + "'.");
                return new List<ProgramInfo>();
            }

            return twtRes.Result;
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to RecordingInfo
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            // retrieve all 'Pending', 'Inprogress' and 'Completed' recordings
            // we don't deliver the 'Pending' recordings

            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] GetRecordingsAsync, call canceled or timed out - returning empty list.");
                return new List<RecordingInfo>();
            }

            TaskWithTimeoutRunner<IEnumerable<RecordingInfo>> twtr = new TaskWithTimeoutRunner<IEnumerable<RecordingInfo>>(TIMEOUT);
            TaskWithTimeoutResult<IEnumerable<RecordingInfo>> twtRes = await
                twtr.RunWithTimeout(_htsConnectionHandler.BuildDvrInfos(cancellationToken));

            if (twtRes.HasTimeout)
            {
                return new List<RecordingInfo>();
            }

            return twtRes.Result;
        }

        public async Task ProbeStream(MediaSourceInfo mediaSourceInfo, string probeUrl, string source, CancellationToken cancellationToken)
        {

            _logger.Info("[TVHclient] Probe stream for {0}", source);
            _logger.Info("[TVHclient] Probe URL: {0}", probeUrl);
            
            MediaInfoRequest req = new MediaInfoRequest

            {

                MediaType = MediaBrowser.Model.Dlna.DlnaProfileType.Video,

                InputPath = probeUrl,

                Protocol = MediaProtocol.Http,

                ExtractChapters = false,

                VideoType = VideoType.VideoFile,

            };



            var originalRuntime = mediaSourceInfo.RunTimeTicks;

            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();

            MediaInfo info = await _mediaEncoder.GetMediaInfo(req, cancellationToken).ConfigureAwait(false);

            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;

            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

            _logger.Info("[TVHclient] Probe RunTime " + elapsedTime);



            if (info != null)
            {

                mediaSourceInfo.Bitrate = info.Bitrate;

                mediaSourceInfo.Container = info.Container;

                mediaSourceInfo.Formats = info.Formats;

                mediaSourceInfo.MediaStreams = info.MediaStreams;

                mediaSourceInfo.RunTimeTicks = info.RunTimeTicks;

                mediaSourceInfo.Size = info.Size;

                mediaSourceInfo.Timestamp = info.Timestamp;

                mediaSourceInfo.Video3DFormat = info.Video3DFormat;

                mediaSourceInfo.VideoType = info.VideoType;

                mediaSourceInfo.RequiresClosing = true;

                mediaSourceInfo.RequiresOpening = true;

                mediaSourceInfo.SupportsDirectPlay = true;

                mediaSourceInfo.SupportsDirectStream = true;

                mediaSourceInfo.SupportsTranscoding = true;





                mediaSourceInfo.DefaultSubtitleStreamIndex = null;



                if (!originalRuntime.HasValue)
                {

                    mediaSourceInfo.RunTimeTicks = null;

                }

                var audioStream = mediaSourceInfo.MediaStreams.FirstOrDefault(i => i.Type == MediaBrowser.Model.Entities.MediaStreamType.Audio);



                if (audioStream == null || audioStream.Index == -1)
                {

                    mediaSourceInfo.DefaultAudioStreamIndex = null;

                }

                else
                {

                    mediaSourceInfo.DefaultAudioStreamIndex = audioStream.Index;

                }

            }

            else
            {

                _logger.Error("[TVHclient] Cannot probe {0} stream", source);

            }

        }

        public async Task<MediaSourceInfo> GetRecordingStream(string recordingId, string mediaSourceId, CancellationToken cancellationToken)
        {
            HTSMessage getTicketMessage = new HTSMessage();
            getTicketMessage.Method = "getTicket";
            getTicketMessage.putField("dvrId", recordingId);

            TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            TaskWithTimeoutResult<HTSMessage> twtRes = await twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnectionHandler.SendMessage(getTicketMessage, lbrh);
                return lbrh.getResponse();
            }));

            if (twtRes.HasTimeout)
            {
                _logger.Error("[TVHclient] Timeout obtaining playback authentication ticket from TVH");
            }
            else
            {
                HTSMessage getTicketResponse = twtRes.Result;

                if (_subscriptionId == int.MaxValue)
                {
                    _subscriptionId = 0;
                }
                int currSubscriptionId = _subscriptionId++;

                if (_htsConnectionHandler.GetEnableSubsMaudios())
                {

                    _logger.Info("[TVHclient] Support for live TV subtitles and multiple audio tracks is enabled.");

                    MediaSourceInfo recordingasset = new MediaSourceInfo();

                    recordingasset.Id = "" + currSubscriptionId;

                    // Use HTTP basic auth instead of TVH ticketing system for authentication to allow the users to switch subs or audio tracks at any time
                    recordingasset.Path = _htsConnectionHandler.GetHttpBaseUrl() + getTicketResponse.getString("path");
                    recordingasset.Protocol = MediaProtocol.Http;

                    // Set asset source and type for stream probing and logging
                    string recordingasset_probeUrl = "" + recordingasset.Path;
                    string recordingasset_source = "Recording";

                    // Probe the asset stream to determine available sub-streams
                    await ProbeStream(recordingasset, recordingasset_probeUrl, recordingasset_source, cancellationToken);

                    return recordingasset;

                }
                else
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

                }
            }

            throw new TimeoutException();
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] GetSeriesTimersAsync, call canceled ot timed out - returning empty list.");
                return new List<SeriesTimerInfo>();
            }

            TaskWithTimeoutRunner<IEnumerable<SeriesTimerInfo>> twtr = new TaskWithTimeoutRunner<IEnumerable<SeriesTimerInfo>>(TIMEOUT);
            TaskWithTimeoutResult<IEnumerable<SeriesTimerInfo>> twtRes = await
                twtr.RunWithTimeout(_htsConnectionHandler.BuildAutorecInfos(cancellationToken));

            if (twtRes.HasTimeout)
            {
                return new List<SeriesTimerInfo>();
            }

            return twtRes.Result;
        }

        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] GetStatusInfoAsync, call canceled or timed out.");
                return new LiveTvServiceStatusInfo
                {
                    Status = LiveTvServiceStatus.Unavailable
                };
            }

            string serverName = _htsConnectionHandler.GetServername();
            string serverVersion = _htsConnectionHandler.GetServerVersion();
            int serverProtocolVersion = _htsConnectionHandler.GetServerProtocolVersion();
            string diskSpace = _htsConnectionHandler.GetDiskSpace();

            int usedHTSPversion = (serverProtocolVersion < (int)HTSMessage.HTSP_VERSION) ? serverProtocolVersion : (int)HTSMessage.HTSP_VERSION;

            string serverVersionMessage = "<p>" + serverName + " " + serverVersion + "</p>"
                + "<p>HTSP protocol version: " + usedHTSPversion + "</p>"
                + "<p>Free diskspace: " + diskSpace + "</p>";

            //TaskWithTimeoutRunner<List<LiveTvTunerInfo>> twtr = new TaskWithTimeoutRunner<List<LiveTvTunerInfo>>(TIMEOUT);
            //TaskWithTimeoutResult<List<LiveTvTunerInfo>> twtRes = await
            //    twtr.RunWithTimeout(_tunerDataHelper.buildTunerInfos(cancellationToken));

            List<LiveTvTunerInfo> tvTunerInfos;
            //if (twtRes.HasTimeout)
            //{
            tvTunerInfos = new List<LiveTvTunerInfo>();
            //} else
            //{
            //    tvTunerInfos = twtRes.Result;
            //}

            return new LiveTvServiceStatusInfo
            {
                Version = serverVersionMessage,
                Tuners = tvTunerInfos,
                Status = LiveTvServiceStatus.Ok,
            };
        }

        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            //  retrieve the 'Pending' recordings");

            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] GetTimersAsync, call canceled or timed out - returning empty list.");
                return new List<TimerInfo>();
            }

            TaskWithTimeoutRunner<IEnumerable<TimerInfo>> twtr = new TaskWithTimeoutRunner<IEnumerable<TimerInfo>>(TIMEOUT);
            TaskWithTimeoutResult<IEnumerable<TimerInfo>> twtRes = await
                twtr.RunWithTimeout(_htsConnectionHandler.BuildPendingTimersInfos(cancellationToken));

            if (twtRes.HasTimeout)
            {
                return new List<TimerInfo>();
            }

            return twtRes.Result;
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info("[TVHclient] RecordLiveStream " + id);

            throw new NotImplementedException();
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            await CancelSeriesTimerAsync(info.Id, cancellationToken);
            // TODO add if method is implemented 
            // await CreateSeriesTimerAsync(info, cancellationToken);
        }

        public async Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            int timeOut = await WaitForInitialLoadTask(cancellationToken);
            if (timeOut == -1 || cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] UpdateTimerAsync, call canceled or timed out.");
                return;
            }

            HTSMessage updateTimerMessage = new HTSMessage();
            updateTimerMessage.Method = "updateDvrEntry";
            updateTimerMessage.putField("id", info.Id);
            updateTimerMessage.putField("startExtra", (long)(info.PrePaddingSeconds / 60));
            updateTimerMessage.putField("stopExtra", (long)(info.PostPaddingSeconds / 60));

            TaskWithTimeoutRunner<HTSMessage> twtr = new TaskWithTimeoutRunner<HTSMessage>(TIMEOUT);
            TaskWithTimeoutResult<HTSMessage> twtRes = await twtr.RunWithTimeout(Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnectionHandler.SendMessage(updateTimerMessage, lbrh);
                return lbrh.getResponse();
            }));

            if (twtRes.HasTimeout)
            {
                _logger.Error("[TVHclient] Can't update timer because of timeout");
            }
            else
            {
                HTSMessage updateTimerResponse = twtRes.Result;
                Boolean success = updateTimerResponse.getInt("success", 0) == 1;
                if (!success)
                {
                    _logger.Error("[TVHclient] Can't update timer: '" + updateTimerResponse.getString("error") + "'");
                }
            }
        }

        /***********/
        /* Helpers */
        /***********/

        private Task<int> WaitForInitialLoadTask(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew<int>(() => _htsConnectionHandler.WaitForInitialLoad(cancellationToken));
        }

        private string dump(SeriesTimerInfo sti)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n<SeriesTimerInfo>\n");
            sb.Append("  Id:                    " + sti.Id + "\n");
            sb.Append("  Name:                  " + sti.Name + "\n");
            sb.Append("  Overview:              " + sti.Overview + "\n");
            sb.Append("  Priority:              " + sti.Priority + "\n");
            sb.Append("  ChannelId:             " + sti.ChannelId + "\n");
            sb.Append("  ProgramId:             " + sti.ProgramId + "\n");
            sb.Append("  Days:                  " + dump(sti.Days) + "\n");
            sb.Append("  StartDate:             " + sti.StartDate + "\n");
            sb.Append("  EndDate:               " + sti.EndDate + "\n");
            sb.Append("  IsPrePaddingRequired:  " + sti.IsPrePaddingRequired + "\n");
            sb.Append("  PrePaddingSeconds:     " + sti.PrePaddingSeconds + "\n");
            sb.Append("  IsPostPaddingRequired: " + sti.IsPrePaddingRequired + "\n");
            sb.Append("  PostPaddingSeconds:    " + sti.PostPaddingSeconds + "\n");
            sb.Append("  RecordAnyChannel:      " + sti.RecordAnyChannel + "\n");
            sb.Append("  RecordAnyTime:         " + sti.RecordAnyTime + "\n");
            sb.Append("  RecordNewOnly:         " + sti.RecordNewOnly + "\n");
            sb.Append("</SeriesTimerInfo>\n");
            return sb.ToString();
        }

        private string dump(List<DayOfWeek> days)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DayOfWeek dow in days)
            {
                sb.Append(dow + ", ");
            }
            string tmpResult = sb.ToString();
            if (tmpResult.EndsWith(","))
            {
                tmpResult = tmpResult.Substring(0, tmpResult.Length - 2);
            }
            return tmpResult;
        }

        /*
        public async Task CopyFilesAsync(StreamReader source, StreamWriter destination)
        {
            char[] buffer = new char[0x1000];
            int numRead;
            while ((numRead = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await destination.WriteAsync(buffer, 0, numRead);
            }
        }
        */
    }
}
