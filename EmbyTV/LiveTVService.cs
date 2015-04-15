﻿using EmbyTV.TunerHost;
﻿using MediaBrowser.Common.Configuration;
﻿using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
﻿using System.CodeDom;
﻿using System.Collections.Generic;
using System.Globalization;
﻿using System.IO;
﻿using System.Linq;
﻿using System.Threading;
using System.Threading.Tasks;
﻿using System.Timers;
﻿using EmbyTV.Configuration;
﻿using EmbyTV.DVR;
﻿using EmbyTV.GeneralHelpers;
﻿using MediaBrowser.Model.Connect;
﻿using MediaBrowser.Model.Net;


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
        private List<SingleTimer> timers;
        private List<SeriesTimer> seriesTimers;
        private readonly IXmlSerializer _xmlSerializer;
        private Dictionary<int, MediaSourceInfo> streams;
        private readonly IApplicationPaths _appPaths;

        public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager, IXmlSerializer xmlSerializer, IApplicationPaths appPaths)
        {
            _liveStreams = 0;
            _logger = logManager.GetLogger(Name);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            FirstRun = true;
            streams = new Dictionary<int, MediaSourceInfo>();
            _xmlSerializer = xmlSerializer;
            _appPaths = appPaths;
            _logger.Info("Directory is: " + DataPath);
            timers = new List<SingleTimer>();
            RefreshConfigData(CancellationToken.None);
            Plugin.Instance.Configuration.TunerDefaultConfigurationsFields = TunerHostConfig.BuildDefaultForTunerHostsBuilders();
            Plugin.Instance.ConfigurationUpdated += (sender, args) => RefreshConfigData(CancellationToken.None);
        }

        public string DataPath
        {
            get { return Plugin.Instance.DataFolderPath; }
        }

        public string RecordingPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Plugin.Instance.Configuration.RecordingPath))
                {
                    return Plugin.Instance.Configuration.RecordingPath;
                }

                return Path.Combine(_appPaths.TempDirectory, "embytv", "recordings");
            }
        }

        private void InitializeTimer(List<SingleTimer> timers)
        {
            foreach (var timer in timers)
            {
                CreateTimerAsync(timer, CancellationToken.None);
            }
        }

        private static List<SeriesTimer> GetSeriesTimerData(string dataPath, IXmlSerializer xmlSerializer)
        {
            List<SeriesTimer> dummy = new List<SeriesTimer>();
            var timerPath = Path.Combine(dataPath, "seriesTimers.xml");
            try
            {
                return (List<SeriesTimer>)xmlSerializer.DeserializeFromFile(dummy.GetType(), timerPath);
            }
            catch (FileNotFoundException)
            {
                return dummy;
            }
        }
        private static List<SingleTimer> GetTimerData(string dataPath, IXmlSerializer xmlSerializer)
        {
            List<SingleTimer> dummy = new List<SingleTimer>();
            var timerPath = Path.Combine(dataPath, "timers.xml");

            try
            {
                return (List<SingleTimer>)xmlSerializer.DeserializeFromFile(dummy.GetType(), timerPath);
            }
            catch (FileNotFoundException)
            {
                return dummy;
            }
        }

        private void UpdateSeriesTimerData()
        {
            var timerPath = Path.Combine(DataPath, "seriesTimers.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(timerPath));

            _xmlSerializer.SerializeToFile(seriesTimers, timerPath);
        }
        private void UpdateTimerData()
        {
            var timerPath = Path.Combine(DataPath, "timers.xml");

            if (timers != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(timerPath));
                _xmlSerializer.SerializeToFile(timers, timerPath);
            }
        }

        public async Task RecordStream(SingleTimer timer)
        {
            var mediaStreamInfo = await GetChannelStream(timer.ChannelId, "none", CancellationToken.None);
            HttpRequestOptions options = new HttpRequestOptionsMod()
            {
                Url = mediaStreamInfo.Path + "?duration=" + timer.Duration()
            };
            await RecordingHelper.DownloadVideo(_httpClient, options, _logger, Path.Combine(RecordingPath, timer.GetRecordingName()), timer.Cts.Token);
            _logger.Info("Recording was a success");
        }
        public async Task RecordStream(MediaSourceInfo mediaSourceInfo, CancellationToken cancellationToken)
        {

            HttpRequestOptions options = new HttpRequestOptionsMod()
            {
                Url = mediaSourceInfo.Path
            };
            await RecordingHelper.DownloadVideo(_httpClient, options, _logger, Path.Combine(RecordingPath, mediaSourceInfo.Name), cancellationToken);
            _logger.Info("Recording was a success");
        }


        /// <summary>
        /// Ensure that we are connected to the HomeRunTV server
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            foreach (var host in _tunerServer)
            {
                try
                {
                    if (string.IsNullOrEmpty(host.getWebUrl()))
                    {
                        throw new ApplicationException("Tunner hostname/ip missing.");
                    }
                    await host.GetDeviceInfo(cancellationToken);
                    host.Enabled = true;
                }
                catch (HttpException)
                {
                    host.Enabled = false;
                }
                catch (ApplicationException)
                {
                    host.Enabled = false;
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
            List<ChannelInfo> channels = new List<ChannelInfo>();
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            foreach (var host in _tunerServer)
            {
                _logger.Info("Start GetChannels Async, retrieve all channels for " + host.HostId);
                channels.AddRange(await host.GetChannels(cancellationToken));
            }
            channels = channels.GroupBy(x => x.Id).Select(x => x.First()).ToList();
            return await _tvGuide.getChannelInfo(channels, cancellationToken);
        }



        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info("Closing " + id);
            streams.Remove(Convert.ToInt16(id));
            return Task.FromResult(0);
        }



        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            var lastTimer = new SingleTimer(info);
            if (lastTimer.Duration() > 5)
            {
                timers.Add(lastTimer);
                UpdateTimerData();
                timers.Last().StartRecording += (sender, args) => { RecordStream(lastTimer); };
                timers.Last().GenerateEvent();
                _logger.Info("Added timer for: " + info.ProgramId);
            }
            else
            {
                _logger.Error("Timer not created the show is about to end or has already ended");
            }
            return Task.FromResult(0);
        }

        public event EventHandler DataSourceChanged;

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            var remove = timers.SingleOrDefault(r => r.Id == recordingId);
            if (remove != null) { timers.Remove(remove); }
            return Task.FromResult(true);
        }



        public async void RefreshConfigData(CancellationToken cancellationToken)
        {
            var config = Plugin.Instance.Configuration;
            if (config.TunerHostsConfiguration != null)
            {
                _tunerServer = TunerHostFactory.CreateTunerHosts(config.TunerHostsConfiguration, _logger, _jsonSerializer, _httpClient);
                for (var i = 0; i < _tunerServer.Count(); i++)
                {
                    await _tunerServer[i].GetDeviceInfo(cancellationToken);
                    config.TunerHostsConfiguration[i].ServerId = _tunerServer[i].HostId;
                }
            }
            if (FirstRun)
            {
                seriesTimers = GetSeriesTimerData(DataPath, _xmlSerializer);
                InitializeTimer(GetTimerData(DataPath, _xmlSerializer));
            }
            FirstRun = false;
            _tvGuide = new EPGProvider.SchedulesDirect(config.username, config.hashPassword, config.lineup, _logger, _jsonSerializer, _httpClient);
            config.avaliableLineups = await _tvGuide.getLineups(cancellationToken);
            if (_tvGuide.badPassword)
            {
                config.hashPassword = "";
            }
            config.headends = await _tvGuide.getHeadends(config.zipCode, cancellationToken);
            Plugin.Instance.SaveConfiguration();
            _configLastModified = Plugin.Instance.ConfigurationDateLastModified;
        }



        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            return await _tvGuide.getTvGuideForChannel(channelId, startDateUtc, endDateUtc, cancellationToken);
        }


        public Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<RecordingInfo> result = new List<RecordingInfo>();

            return Task.FromResult(result);
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {

            return Task.FromResult((IEnumerable<SeriesTimerInfo>)seriesTimers);
        }

        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            //Version Check
            bool upgradeAvailable;
            string serverVersion;

            upgradeAvailable = false;
            serverVersion = Plugin.Instance.Version.ToString();
            //Tuner information
            List<LiveTvTunerInfo> tvTunerInfos = new List<LiveTvTunerInfo>();
            foreach (var host in _tunerServer)
            {
                tvTunerInfos.AddRange(await host.GetTunersInfo(cancellationToken));
            }

            return new LiveTvServiceStatusInfo
            {
                HasUpdateAvailable = upgradeAvailable,
                Version = serverVersion,
                Tuners = tvTunerInfos
            };
        }

        public Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<TimerInfo>)timers);
        }

        public string HomePageUrl
        {
            get { return "http://localhost:8096/web/ConfigurationPage?name=EmbyTV"; }
        }

        public string Name
        {
            get { return "EmbyTV"; }
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            var timer = new SingleTimer()
            {
                Name = DateTime.UtcNow.ToString(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddHours(5),
                PostPaddingSeconds = 0,
                PrePaddingSeconds = 0,

            };

            // RecordStream(timer);
            return Task.FromResult(0);
        }

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }





        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _liveStreams++;
            _logger.Info("Streaming Channel");
            MediaSourceInfo mediaSourceInfo = null;
            foreach (var host in _tunerServer)
            {
                try
                {
                    mediaSourceInfo = host.GetChannelStreamInfo(channelId);
                    break;
                }
                catch (ApplicationException e)
                {
                    _logger.Info(e.Message);
                }
            }
            if ((mediaSourceInfo == null)) { throw new ApplicationException("No tuners Avaliable"); }
            mediaSourceInfo.Id = _liveStreams.ToString();
            streams.Add(_liveStreams, mediaSourceInfo);
            return Task.FromResult(mediaSourceInfo);
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var remove = seriesTimers.SingleOrDefault(r => r.Id == timerId);
            if (remove != null) { seriesTimers.Remove(remove); }
            UpdateSeriesTimerData();
            return Task.FromResult(true);
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            int index = timers.FindIndex(r => r.Id == timerId);
            if (index >= 0)
            {
                timers[index].Cts.Cancel();
                timers.RemoveAt(index);
            }
            UpdateTimerData();
            return Task.FromResult(true);
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            seriesTimers.Add(new SeriesTimer(info));
            UpdateSeriesTimerData();
            return Task.FromResult(true);
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            var defaults = new SeriesTimerInfo()
            {
                PostPaddingSeconds = 60,
                PrePaddingSeconds = 60,
                RecordAnyChannel = false,
                RecordAnyTime = false,
                RecordNewOnly = false
            };
            return Task.FromResult(defaults);
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
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

        public Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            var recording = seriesTimers.FindIndex(r => r.Id == info.Id);
            if (recording >= 0) { seriesTimers[recording] = new SeriesTimer(info); }
            UpdateSeriesTimerData();
            return Task.FromResult(true);
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            int index = timers.FindIndex(r => r.Id == info.Id);
            if (index >= 0)
            {
                timers[index] = new SingleTimer(info);
            }
            UpdateTimerData();
            return Task.FromResult(true);
        }
    }
}
