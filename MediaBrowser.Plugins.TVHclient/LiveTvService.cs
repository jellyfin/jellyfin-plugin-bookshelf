using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.TVHclient.DataHelper;
using MediaBrowser.Plugins.TVHclient.HTSP;
using MediaBrowser.Plugins.TVHclient.HTSP_Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Plugins.TVHclient.Helper;

namespace MediaBrowser.Plugins.TVHclient
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService, HTSConnectionListener
    {
        private volatile Boolean _connected = false;
        private volatile Boolean _initialLoadFinished = false;

        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;
        private readonly HTSConnectionAsync _htsConnection;

        // Data helpers
        private readonly ChannelDataHelper _channelDataHelper;
        private readonly TunerDataHelper _tunerDataHelper;
        private readonly DvrDataHelper _dvrDataHelper;

        //private readonly CultureInfo _deCulture = new CultureInfo("de-DE");
        //private int _liveStreams;
        //private readonly Dictionary<int, int> _heartBeat = new Dictionary<int, int>();
        //private string Sid { get; set; }

        public LiveTvService(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;

            _tunerDataHelper = new TunerDataHelper(logger);
            _channelDataHelper = new ChannelDataHelper(logger, _tunerDataHelper);
            _dvrDataHelper = new DvrDataHelper(logger);

            Version version = Assembly.GetEntryAssembly().GetName().Version;
            _htsConnection = new HTSConnectionAsync(this, "TVHclient", version.ToString(), logger);
        }

        private void ensureConnection()
        {
            if (_htsConnection == null)
            {
                return;
            }

            lock (_htsConnection)
            {
                if (!_connected)
                {
                    var config = Plugin.Instance.Configuration;

                    if (string.IsNullOrEmpty(config.TVH_ServerName))
                    {
                        string message = "[TVHclient] TVH-Server name must be configured.";
                        _logger.Error(message);
                        throw new InvalidOperationException(message);
                    }

                    if (string.IsNullOrEmpty(config.Username))
                    {
                        string message = "[TVHclient] Username must be configured.";
                        _logger.Error(message);
                        throw new InvalidOperationException(message);
                    }

                    if (string.IsNullOrEmpty(config.Password))
                    {
                        string message = "[TVHclient] Password must be configured.";
                        _logger.Error(message);
                        throw new InvalidOperationException(message);
                    }

                    _htsConnection.open(config.TVH_ServerName, config.HTSP_Port);
                    _connected = _htsConnection.authenticate(config.Username, config.Password);

                    _channelDataHelper.clean();
                    _dvrDataHelper.clean();
                }
            }
        }


        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] Start GetChannels Async, call canceled - returning empty list.");
                return new List<ChannelInfo>();
            }

            IEnumerable<ChannelInfo> data = await _channelDataHelper.buildChannelInfos(cancellationToken);
            return data;
        }

        private Task<int> WaitForInitialLoadTask(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew<int>(() =>
            {
                while (!_initialLoadFinished || cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(500);
                }
                return 0;
            });
        }

        /// <summary>
        /// Gets the Recordings async
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}</returns>
        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            // retrieve all 'Pending', 'Inprogress' and 'Completed' recordings
            // we don't deliver the 'Pending' recordings

            ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] Start GetRecordingsAsync Async, call canceled - returning empty list.");
                return new List<RecordingInfo>();
            }

            IEnumerable<RecordingInfo> data = await _dvrDataHelper.buildDvrInfos(cancellationToken);
            return data;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "TVHclient"; }
        }

        /// <summary>
        /// Delete the Recording async from the disk
        /// </summary>
        /// <param name="recordingId">The recordingId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
           ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);

            HTSMessage deleteRecordingMessage = new HTSMessage();
            deleteRecordingMessage.Method = "deleteDvrEntry";
            deleteRecordingMessage.putField("id", recordingId);

            HTSMessage deleteRecordingResponse = await Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnection.sendMessage(deleteRecordingMessage, lbrh);
                return lbrh.getResponse();
            });

            Boolean success = deleteRecordingResponse.getInt("success", 0) == 1;
            if (!success)
            {
                _logger.Error("[TVHclient] Can't delete recording: '" + deleteRecordingResponse.getString("error") + "'");
            }
        }

        /// <summary>
        /// Cancel pending scheduled Recording 
        /// </summary>
        /// <param name="timerId">The timerId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
           ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);

            HTSMessage cancelTimerMessage = new HTSMessage();
            cancelTimerMessage.Method = "cancelDvrEntry";
            cancelTimerMessage.putField("id", timerId);

            HTSMessage cancelTimerResponse = await Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnection.sendMessage(cancelTimerMessage, lbrh);
                return lbrh.getResponse();
            });

            Boolean success = cancelTimerResponse.getInt("success", 0) == 1;
            if (!success)
            {
                _logger.Error("[TVHclient] Can't cancel timer: '" + cancelTimerResponse.getString("error") + "'");
            }
        }

        /// <summary>
        /// Create a new recording
        /// </summary>
        /// <param name="info">The TimerInfo</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);

            HTSMessage createTimerMessage = new HTSMessage();
            createTimerMessage.Method = "addDvrEntry";
            createTimerMessage.putField("channelId", info.ChannelId);
            createTimerMessage.putField("start", DateTimeHelper.getUnixUTCTimeFromUtcDateTime(info.StartDate));
            createTimerMessage.putField("stop", DateTimeHelper.getUnixUTCTimeFromUtcDateTime(info.EndDate));
            createTimerMessage.putField("startExtra", (long)(info.PrePaddingSeconds / 60));
            createTimerMessage.putField("stopExtra", (long)(info.PostPaddingSeconds / 60));
            createTimerMessage.putField("priority", info.Priority);
            createTimerMessage.putField("description", info.Overview);
            createTimerMessage.putField("title", info.Name);
            createTimerMessage.putField("creator", Plugin.Instance.Configuration.Username);

            HTSMessage createTimerResponse = await Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnection.sendMessage(createTimerMessage, lbrh);
                return lbrh.getResponse();
            });

            Boolean success = createTimerResponse.getInt("success", 0) == 1;
            if (!success)
            {
                _logger.Error("[TVHclient] Can't create timer: '" + createTimerResponse.getString("error") + "'");
            }
        }

        /// <summary>
        /// Update a single Timer
        /// </summary>
        /// <param name="info">The program info</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
           ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);

            HTSMessage updateTimerMessage = new HTSMessage();
            updateTimerMessage.Method = "updateDvrEntry";
            updateTimerMessage.putField("id", info.Id);
            updateTimerMessage.putField("startExtra", (long)(info.PrePaddingSeconds / 60));
            updateTimerMessage.putField("stopExtra", (long)(info.PostPaddingSeconds / 60));

            HTSMessage updateTimerResponse = await Task.Factory.StartNew<HTSMessage>(() =>
            {
                LoopBackResponseHandler lbrh = new LoopBackResponseHandler();
                _htsConnection.sendMessage(updateTimerMessage, lbrh);
                return lbrh.getResponse();
            });

            Boolean success = updateTimerResponse.getInt("success", 0) == 1;
            if (!success)
            {
                _logger.Error("[TVHclient] Can't update timer: '" + updateTimerResponse.getString("error") + "'");
            }
        }

        /// <summary>
        /// Get the pending Recordings.
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            //  retrieve the 'Pending' recordings");

            ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] Start GetTimersAsync Async, call canceled - returning empty list.");
                return new List<TimerInfo>();
            }

            IEnumerable<TimerInfo> data = await _dvrDataHelper.buildPendingTimersInfos(cancellationToken);
            return data;
        }

        /// <summary>
        /// Get the recurrent recordings
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Info("[TVHclient] Start GetSeriesTimer Async, retrieve the recurring recordings");

            //ensureConnection();

            return new List<SeriesTimerInfo>();
        }

        /// <summary>
        /// Create a recurrent recording
        /// </summary>
        /// <param name="info">The recurrend program info</param>
        /// <param name="cancellationToken">The CancelationToken</param>
        /// <returns></returns>
        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[TVHclient] Start CreateSeriesTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));

            //ensureConnection();

            _logger.Info("[TVHclient] Implement CreateSeriesTimerAsync");
        }

        /// <summary>
        /// Update the series Timer
        /// </summary>
        /// <param name="info">The series program info</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[TVHclient] Start UpdateSeriesTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));

            //ensureConnection();

            _logger.Info("[TVHclient] Implement UpdateSeriesTimerAsync");
        }

        /// <summary>
        /// Cancel the Series Timer
        /// </summary>
        /// <param name="timerId">The Timer Id</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[TVHclient] Start Cancel SeriesRecording Async for recordingId: {0}", timerId));

            //ensureConnection();

            _logger.Info("[TVHclient] Implement CancelSeriesTimerAsync");
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MediaSourceInfo> GetChannelStream(string channelOid, string mediaSourceId, CancellationToken cancellationToken)
        {
            _logger.Info("[TVHclient] Start ChannelStream");

            ensureConnection();

            return new MediaSourceInfo
            {
                Id = "Implement me please",
                Path = null,
                Protocol = MediaProtocol.Http,
                MediaStreams = new List<MediaStream>
                        {
                            new MediaStream
                            {
                                Type = MediaStreamType.Video,
                                // Set the index to -1 because we don't know the exact index of the video stream within the container
                                Index = -1
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



        public async Task<MediaSourceInfo> GetRecordingStream(string recordingId, string mediaSourceId, CancellationToken cancellationToken)
        {
            _logger.Info("[TVHclient] Start GetRecordingStream");

            ensureConnection();

            _logger.Info("[TVHclient] Implement GetRecordingStream please");
            return new MediaSourceInfo
            {
                Path = null,
                Protocol = MediaProtocol.Http,
                MediaStreams = new List<MediaStream>
                        {
                            new MediaStream
                            {
                                Type = MediaStreamType.Video,
                                // Set the index to -1 because we don't know the exact index of the video stream within the container
                                Index = -1
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

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info("[TVHclient] Closing " + id);
            _logger.Info("[TVHclient] Implement CloseLiveStream");

            //ensureConnection();

        }

        public async Task CopyFilesAsync(StreamReader source, StreamWriter destination)
        {
            _logger.Info("[TVHclient] Start CopyFiles Async");
            char[] buffer = new char[0x1000];
            int numRead;
            while ((numRead = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await destination.WriteAsync(buffer, 0, numRead);
            }
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

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("[TVHclient] Start GetChannels Async, call canceled - returning empty list.");
                return new List<ProgramInfo>();
            }

            GetEventsResponseHandler currGetEventsResponseHandler = new GetEventsResponseHandler(startDateUtc, endDateUtc, _logger, cancellationToken);

            HTSMessage queryEvents = new HTSMessage();
            queryEvents.Method = "getEvents";
            queryEvents.putField("channelId", Convert.ToInt32(channelId));
            _htsConnection.sendMessage(queryEvents, currGetEventsResponseHandler);

            IEnumerable<ProgramInfo> pi = await currGetEventsResponseHandler.GetEvents(cancellationToken);
            return pi;
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info("[TVHclient] RecordLiveStream " + id);

            throw new NotImplementedException();
        }

        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            // retrieve status info of the TVHserver");

            ensureConnection();

            await WaitForInitialLoadTask(cancellationToken);

            string serverName = _htsConnection.getServername();
            string serverVersion = _htsConnection.getServerversion();
            int serverProtokollVersion = _htsConnection.getServerProtocolVersion();
            string diskSpace = _htsConnection.getDiskspace();

            bool upgradeAvailable = false;
            string serverVersionMessage = serverName + " " + serverVersion + " // HTSP protokoll version: " + serverProtokollVersion;
            string statusMessage = "Diskspace: " + diskSpace;

            List<LiveTvTunerInfo> tvTunerInfos = await _tunerDataHelper.buildTunerInfos(cancellationToken);

            return new LiveTvServiceStatusInfo
            {
                HasUpdateAvailable = upgradeAvailable,
                Version = serverVersionMessage,
                //Tuners = tvTunerInfos,
                Status = LiveTvServiceStatus.Ok,
                StatusMessage = statusMessage
            };
        }

        public string HomePageUrl
        {
            get { return "http://tvheadend.org/"; }
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ChannelInfo
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ProgramInfo
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to RecordingInfo
            throw new NotImplementedException();
        }





        public Task<ChannelMediaInfo> GetChannelStream(string channelId, CancellationToken cancellationToken)
        {
            _logger.Fatal("[TVHclient] LiveTvService.GetChannelStream called for channelID '" + channelId + "'");

            throw new NotImplementedException();
        }

        public Task<ChannelMediaInfo> GetRecordingStream(string recordingId, CancellationToken cancellationToken)
        {
            _logger.Fatal("[TVHclient] LiveTvService.GetRecordingStream called for recordingId '" + recordingId + "'");

            throw new NotImplementedException();
        }


        public event EventHandler DataSourceChanged;

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;


        private void sendRecordingStatusChanged()
        {
            EventHandler<RecordingStatusChangedEventArgs> handler = RecordingStatusChanged;
            if (handler != null)
            {
                handler(this, new RecordingStatusChangedEventArgs());
            }
        }


        public void onMessage(HTSMessage response)
        {
            //_logger.Fatal("[TVHclient]" + response);


            if (response != null)
            {
                switch (response.Method)
                {
                    case "tagAdd":
                    case "tagUpdate":
                    case "tagDelete":
                        //_logger.Fatal("[TVHclient] tad add/update/delete" + response.ToString());
                        break;

                    case "channelAdd":
                    case "channelUpdate":
                        _channelDataHelper.add(response);
                        break;

                    case "dvrEntryAdd":
                        _dvrDataHelper.dvrEntryAdd(response);
                        sendRecordingStatusChanged();
                        break;
                    case "dvrEntryUpdate":
                        _dvrDataHelper.dvrEntryUpdate(response);
                        sendRecordingStatusChanged();
                        break;
                    case "dvrEntryDelete":
                        _dvrDataHelper.dvrEntryDelete(response);
                        sendRecordingStatusChanged();
                        break;

                    case "autorecEntryAdd":
                    case "autorecEntryUpdate":
                    case "autorecEntryDelete":
                        sendRecordingStatusChanged();
                        _logger.Fatal("[TVHclient] autorecEntry Add/Update/Delete " + response.ToString());
                        break;

                    case "eventAdd":
                    case "eventUpdate":
                    case "eventDelete":
                        // should not happen as we don't subscribe for this events.
                        break;

                    case "subscriptionStart":
                    case "subscriptionGrace":
                    case "subscriptionStop":
                    case "subscriptionSkip":
                    case "subscriptionSpeed":
                    case "subscriptionStatus":
                        _logger.Fatal("[TVHclient] subscription events " + response.ToString());
                        break;

                    case "queueStatus":
                        _logger.Fatal("[TVHclient] queueStatus event " + response.ToString());
                        break;

                    case "signalStatus":
                        _logger.Fatal("[TVHclient] signalStatus event " + response.ToString());
                        break;

                    case "timeshiftStatus":
                        _logger.Fatal("[TVHclient] timeshiftStatus event " + response.ToString());
                        break;

                    case "muxpkt": // streaming data
                        _logger.Fatal("[TVHclient] muxpkt event " + response.ToString());
                        break;

                    case "initialSyncCompleted":
                        _initialLoadFinished = true;
                        break;



                    default:
                        _logger.Fatal("[TVHclient] Method '" + response.Method + "' not handled in LiveTvService.cs");
                        break;
                }
            }
        }

        public void onError(Exception ex)
        {
            _logger.Fatal("[TVHclient] HTSP error: " + ex);
            _htsConnection.stop();
            _connected = false;

            EventHandler handler = DataSourceChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }


    }

}
