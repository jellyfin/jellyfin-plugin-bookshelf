﻿using EmbyTV.Configuration;
using EmbyTV.DVR;
using EmbyTV.GeneralHelpers;
using EmbyTV.TunerHost;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmbyTV
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService, IDisposable
    {
        private List<ITunerHost> _tunerServer;
        private readonly EPGProvider.SchedulesDirect _tvGuide;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly Dictionary<string, MediaSourceInfo> _streams;
        private readonly IApplicationPaths _appPaths;
        private readonly ItemDataProvider<RecordingInfo> _recordingProvider;
        private readonly ItemDataProvider<SeriesTimerInfo> _seriesTimerProvider;
        private readonly TimerManager _timerProvider;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeRecordings =
            new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

        public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager, IXmlSerializer xmlSerializer, IApplicationPaths appPaths)
        {
            _logger = logManager.GetLogger(Name);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _streams = new Dictionary<string, MediaSourceInfo>();
            _appPaths = appPaths;
            _logger.Info("Directory is: " + DataPath);

            _tvGuide = new EPGProvider.SchedulesDirect(_logger, _jsonSerializer, _httpClient);

            _recordingProvider = new ItemDataProvider<RecordingInfo>(xmlSerializer, _logger, Path.Combine(DataPath, "recordings.xml"), (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase));
            _seriesTimerProvider = new SeriesTimerManager(xmlSerializer, _logger, Path.Combine(DataPath, "seriestimers.xml"));
            _timerProvider = new TimerManager(xmlSerializer, _logger, Path.Combine(DataPath, "timers.xml"));
            _timerProvider.TimerFired += _timerProvider_TimerFired;

            Initialize();
        }

        public void Initialize()
        {
            RefreshConfigData(false, CancellationToken.None);
            Plugin.Instance.ConfigurationUpdated += (sender, args) => RefreshConfigData(true, CancellationToken.None);
            _timerProvider.RestartTimers();
        }

        public string DataPath
        {
            get { return Plugin.Instance.DataFolderPath; }
        }

        private string GetChannelEpgCachePath(string channelId)
        {
            return Path.Combine(DataPath, "epg", channelId + ".json");
        }

        private readonly object _epgLock = new object();
        private void SaveEpgDataForChannel(string channelId, List<ProgramInfo> epgData)
        {
            var path = GetChannelEpgCachePath(channelId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            lock (_epgLock)
            {
                _jsonSerializer.SerializeToFile(epgData, path);
            }
        }
        private List<ProgramInfo> GetEpgDataForChannel(string channelId)
        {
            try
            {
                lock (_epgLock)
                {
                    return _jsonSerializer.DeserializeFromFile<List<ProgramInfo>>(GetChannelEpgCachePath(channelId));
                }
            }
            catch
            {
                return new List<ProgramInfo>();
            }
        }
        private List<ProgramInfo> GetEpgDataForAllChannels()
        {
            List<ProgramInfo> channelEpg = new List<ProgramInfo>();
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(DataPath, "epg"));
            List<string> channels = dir.GetFiles("*").Where(i => string.Equals(i.Extension, ".json", StringComparison.OrdinalIgnoreCase)).Select(f => f.Name).ToList();
            foreach (var channel in channels)
            {
                channelEpg.AddRange(GetEpgDataForChannel(channel));
            }
            return channelEpg;
        }
        private ProgramInfo GetProgramInfoFromCache(string channelId, string programId)
        {
            var epgData = GetEpgDataForChannel(channelId);
            if (epgData.Any())
            {
                return epgData.FirstOrDefault(p => p.Id == programId);
            }
            return null;
        }

        async void _timerProvider_TimerFired(object sender, GenericEventArgs<TimerInfo> e)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();

                if (_activeRecordings.TryAdd(e.Argument.Id, cancellationTokenSource))
                {
                    await RecordStream(e.Argument, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error recording stream", ex);
            }
        }

        private async Task RecordStream(TimerInfo timer, CancellationToken cancellationToken)
        {
            var mediaStreamInfo = await GetChannelStream(timer.ChannelId, "none", CancellationToken.None);
            var duration = (timer.EndDate - RecordingHelper.GetStartTime(timer)).TotalSeconds + timer.PrePaddingSeconds;

            HttpRequestOptions httpRequestOptions = new HttpRequestOptionsMod()
            {
                Url = mediaStreamInfo.Path + "?duration=" + duration
            };

            var info = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);
            var recordPath = RecordingPath;
            if (info.IsMovie)
            {
                recordPath = Path.Combine(recordPath, "Movies", StringHelper.RemoveSpecialCharacters(info.Name));
            }
            else
            {
                recordPath = Path.Combine(recordPath, "TV", StringHelper.RemoveSpecialCharacters(info.Name));
            }

            recordPath = Path.Combine(recordPath, RecordingHelper.GetRecordingName(timer, info));
            Directory.CreateDirectory(recordPath);

            var recording = _recordingProvider.GetAll().First(x => x.Id == info.Id);

            recording.Path = recordPath;
            recording.Status = RecordingStatus.InProgress;
            _recordingProvider.Update(recording);

            try
            {
                httpRequestOptions.BufferContent = false;
                httpRequestOptions.CancellationToken = cancellationToken;
                _logger.Info("Writing file to path: " + recordPath);
                using (var response = await _httpClient.SendAsync(httpRequestOptions, "GET"))
                {
                    using (var output = File.Open(recordPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await response.Content.CopyToAsync(output, 4096, cancellationToken);
                    }
                }

                recording.Status = RecordingStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                recording.Status = RecordingStatus.Cancelled;
            }
            catch
            {
                recording.Status = RecordingStatus.Error;
            }

            _recordingProvider.Update(recording);
            _timerProvider.Delete(timer);
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
            _streams.Remove(id);
            return Task.FromResult(0);
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            _timerProvider.Add(info);
            return Task.FromResult(0);
        }

        public event EventHandler DataSourceChanged;

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            var remove = _recordingProvider.GetAll().FirstOrDefault(i => string.Equals(i.Id, recordingId, StringComparison.OrdinalIgnoreCase));
            if (remove != null)
            {
                try
                {
                    File.Delete(remove.Path);
                }
                catch (DirectoryNotFoundException)
                {

                }
                catch (FileNotFoundException)
                {
                    
                }
                _recordingProvider.Delete(remove);
            }
            return Task.FromResult(true);
        }

        public async void RefreshConfigData(bool isConfigChange, CancellationToken cancellationToken)
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

            Plugin.Instance.SaveConfiguration();
            if (isConfigChange)
            {
                await AddLineupIfNeeded(config, cancellationToken).ConfigureAwait(false);
            }
        }

        private string RecordingPath
        {
            get
            {
                var path = Plugin.Instance.Configuration.RecordingPath;

                return string.IsNullOrWhiteSpace(path)
                    ? Path.Combine(DataPath, "recordings")
                    : path;
            }
        }

        private async Task AddLineupIfNeeded(PluginConfiguration config, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(config.lineup.Id))
            {
                return;
            }

            var lineups = await _tvGuide.getLineups(cancellationToken).ConfigureAwait(false);

            if (!lineups.Any(i => string.Equals(i, config.lineup.Id, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    await _tvGuide.addHeadEnd(config.lineup.Id, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.Debug("Error adding headend", e);
                }
            }
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var epgData = await _tvGuide.getTvGuideForChannel(channelId, startDateUtc, endDateUtc, cancellationToken);
            var programInfos = epgData.ToList();
            if (!programInfos.Any())
            {
                _logger.Debug("Couldnt find any data on the epg provider looking through the local cache");
                programInfos = GetEpgDataForChannel(channelId);
            }
            else
            {
                _logger.Debug("Found data on the epg provider saving it to the local cache");
                SaveEpgDataForChannel(channelId, programInfos);
            }
            foreach (var seriesTimer in _seriesTimerProvider.GetAll())
            {
                if (seriesTimer.ChannelId == channelId || seriesTimer.RecordAnyChannel)
                {
                    _logger.Info("Proccesing series timers for show" + seriesTimer.Id + " on channel " + channelId);
                    UpdateTimersForSeriesTimer(programInfos, seriesTimer);
                }
            }
            return programInfos;
        }

        private void UpdateTimersForSeriesTimer(IEnumerable<ProgramInfo> epgData, SeriesTimerInfo seriesTimer)
        {
            var tempTimers = RecordingHelper.GetTimersForSeries(seriesTimer, epgData, _recordingProvider.GetAll(), _logger);
            _logger.Info("Creating " + tempTimers.Count + " timers for series timer " + seriesTimer.Id);
            foreach (var timer in tempTimers)
            {
                _timerProvider.AddOrUpdate(timer);
            }
        }

        public Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<RecordingInfo>)_recordingProvider.GetAll());
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<SeriesTimerInfo>)_seriesTimerProvider.GetAll());
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
            return Task.FromResult((IEnumerable<TimerInfo>)_timerProvider.GetAll());
        }

        public string HomePageUrl
        {
            get { return "http://emby.media"; }
        }

        public string Name
        {
            get { return "EmbyTV"; }
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _logger.Info("Streaming Channel " + channelId);
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
            if ((mediaSourceInfo == null))
            {
                throw new ApplicationException("No tuners Avaliable");
            }
            mediaSourceInfo.Id = Guid.NewGuid().ToString("N");
            _streams.Add(mediaSourceInfo.Id, mediaSourceInfo);
            return Task.FromResult(mediaSourceInfo);
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var remove = _seriesTimerProvider.GetAll().SingleOrDefault(r => r.Id == timerId);
            if (remove != null)
            {
                _seriesTimerProvider.Delete(remove);
            }
            return Task.FromResult(true);
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var remove = _timerProvider.GetAll().SingleOrDefault(r => r.Id == timerId);
            if (remove != null)
            {
                _timerProvider.Delete(remove);
            }
            CancellationTokenSource cancellationTokenSource;

            if (_activeRecordings.TryGetValue(timerId, out cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }

            return Task.FromResult(true);
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            List<ProgramInfo> pInfo;
            if (info.RecordAnyChannel)
            {
                pInfo = GetEpgDataForAllChannels();
            }
            else
            {
                pInfo = GetEpgDataForChannel(info.ChannelId);
            }
            UpdateTimersForSeriesTimer(pInfo, info);
            _seriesTimerProvider.Add(info);
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
            List<ProgramInfo> pInfo;
            if (info.RecordAnyChannel)
            {
                pInfo = GetEpgDataForAllChannels();
            }
            else
            {
                pInfo = GetEpgDataForChannel(info.ChannelId);
            }
            UpdateTimersForSeriesTimer(pInfo, info);
            _seriesTimerProvider.Update(info);
            return Task.FromResult(true);
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            _timerProvider.Update(info);
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            foreach (var pair in _activeRecordings.ToList())
            {
                pair.Value.Cancel();
            }
        }
    }
}
