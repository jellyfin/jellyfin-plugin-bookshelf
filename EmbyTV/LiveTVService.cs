﻿using EmbyTV.TunerHost;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
﻿using System.IO;
﻿using System.Threading;
using System.Threading.Tasks;
﻿using EmbyTV.Configuration;

namespace EmbyTV
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService
    {
        private int _liveStreams;
        private List<ITunerHost> _tunerServer;
        private EPGProvider.SchedulesDirect _tvGuide;
        private readonly ILogger _logger;
        private DateTime _configLastModified;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private bool FirstRun;

       public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _liveStreams = 0;
            _logger = logManager.GetLogger(Name);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _tunerServer = new List<TunerHost.ITunerHost>();
           FirstRun = true;
            
           CheckForUpdates();
        }

        private void CheckForUpdates()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    //Helper.LogInfo("Last Time config modified:" + configLastModified);
                    if ((Plugin.Instance.ConfigurationDateLastModified != _configLastModified) || FirstRun)
                    {
                        RefreshConfigData(CancellationToken.None);
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        /// <summary>
        /// Ensure that we are connected to the HomeRunTV server
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Plugin.Instance.Configuration.apiURL))
            {
                throw new ApplicationException("Tunner hostname/ip missing.");
            }
            await _tunerServer[0].GetDeviceInfo(cancellationToken);
            if (_tunerServer[0].model == "")
            {
                throw new ApplicationException("No tuner found at address.");
            }
        }

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Start GetChannels Async, retrieve all channels for " + _tunerServer[0].model);
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var response = await _tunerServer[0].GetChannels(cancellationToken);
            return await _tvGuide.getChannelInfo(response, cancellationToken);
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info("Closing " + id);
            return Task.FromResult(0);
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public event EventHandler DataSourceChanged;

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async void RefreshConfigData(CancellationToken cancellationToken)
        {
            
            TunerUserConfiguration tunerUserConfiguration = new TunerUserConfiguration()
            {
                ServerType = TunerServerType.HdHomerun,
                ServerId = "1",
                ConfigurationFields = new List<ConfigurationField>()
            };
            tunerUserConfiguration.ConfigurationFields.Add(new ConfigurationField() { Name = "Url", Value = Plugin.Instance.Configuration.apiURL });
            tunerUserConfiguration.ConfigurationFields.Add(new ConfigurationField() { Name = "OnlyFavorites", Value = Convert.ToString(Plugin.Instance.Configuration.loadOnlyFavorites) });
            if (FirstRun)
            {
                _tunerServer.Add(TunerHostFactory.CreateTunerHost(tunerUserConfiguration, _logger, _jsonSerializer,_httpClient));
                FirstRun = false;
            }
            else
            {
                _tunerServer[0] = TunerHostFactory.CreateTunerHost(tunerUserConfiguration, _logger, _jsonSerializer, _httpClient);   
            }
           
            _tvGuide = new EPGProvider.SchedulesDirect(Plugin.Instance.Configuration.username, Plugin.Instance.Configuration.hashPassword, Plugin.Instance.Configuration.tvLineUp, _logger, _jsonSerializer, _httpClient);
            var config = Plugin.Instance.Configuration;
            config.avaliableLineups = await _tvGuide.getLineups(cancellationToken);
            var dict = await _tvGuide.getHeadends(config.zipCode, cancellationToken);
            var names = "";
            var values = "";
            foreach (KeyValuePair<string, string> entry in dict)
            {
                names = names + "," + entry.Key;
                values = values + "," + entry.Value;
            }
            if (!String.IsNullOrWhiteSpace(names)) { names = names.Substring(1); values = values.Substring(1); }
            config.headendName = names;
            config.headendValue = values;
            Plugin.Instance.SaveConfiguration();
            _configLastModified = Plugin.Instance.ConfigurationDateLastModified;
        }

        public Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            return await _tvGuide.getTvGuideForChannel(channelId, startDateUtc, endDateUtc, cancellationToken);
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ChannelMediaInfo> GetRecordingStream(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<RecordingInfo> result = new List<RecordingInfo>();
            return Task.FromResult(result);
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            IEnumerable<SeriesTimerInfo> result = new List<SeriesTimerInfo>();
            return Task.FromResult(result);
        }

        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            //Version Check
            bool upgradeAvailable;
            string serverVersion;
            upgradeAvailable = false;
            serverVersion = _tunerServer[0].firmware;
            //Tuner information
            List<LiveTvTunerInfo> tvTunerInfos = await _tunerServer[0].GetTunersInfo(cancellationToken);
            return new LiveTvServiceStatusInfo
            {
                HasUpdateAvailable = upgradeAvailable,
                Version = serverVersion,
                Tuners = tvTunerInfos
            };
        }

        public Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            IEnumerable<TimerInfo> result = new List<TimerInfo>();
            return Task.FromResult(result);
        }

        public string HomePageUrl
        {
            get { return _tunerServer[0].getWebUrl(); }
        }

        public string Name
        {
            get { return "EmbyTV"; }
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            _logger.Info("Streaming Channel Not implemented");
            throw new NotImplementedException();
        }

        public Task<MediaSourceInfo> GetRecordingStream(string recordingId, string streamId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _liveStreams++;
            _logger.Info("Streaming Channel");
            string streamUrl = _tunerServer[0].getChannelStreamInfo(channelId);
            _logger.Info("Streaming Channel" + channelId + "from: " + streamUrl);
            return Task.FromResult(new MediaSourceInfo
            {
                Id = _liveStreams.ToString(CultureInfo.InvariantCulture),
                Path = streamUrl,
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
            });
        }
    }
}
