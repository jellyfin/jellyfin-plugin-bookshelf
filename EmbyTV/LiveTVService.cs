﻿using EmbyTV.Configuration;
﻿using EmbyTV.DVR;
using EmbyTV.GeneralHelpers;
using EmbyTV.TunerHost;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
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
    public class LiveTvService : ILiveTvService
    {
        private List<ITunerHost> _tunerServer;
        private readonly EPGProvider.SchedulesDirect _tvGuide;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private bool FirstRun;
        private List<SingleTimer> timers;
        private List<SeriesTimer> seriesTimers;
        private readonly IXmlSerializer _xmlSerializer;
        private Dictionary<string, MediaSourceInfo> streams;
        private readonly IApplicationPaths _appPaths;
        private List<RecordingInfo> recordings;      

        public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager, IXmlSerializer xmlSerializer, IApplicationPaths appPaths)
        {
            _logger = logManager.GetLogger(Name);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            FirstRun = true;
            streams = new Dictionary<string, MediaSourceInfo>();
            _xmlSerializer = xmlSerializer;
            _appPaths = appPaths;
            _logger.Info("Directory is: " + DataPath);

            _tvGuide = new EPGProvider.SchedulesDirect(_logger, _jsonSerializer, _httpClient);
            
            RefreshConfigData(false, CancellationToken.None);
            Plugin.Instance.ConfigurationUpdated += (sender, args) => RefreshConfigData(true, CancellationToken.None);
        }

        public string DataPath
        {
            get { return Plugin.Instance.DataFolderPath; }
        }


        public string RecordingPath{get{return Plugin.Instance.Configuration.RecordingPath;}}
   
        private void InitializeTimer()
        {
            var timersClone = new List<SingleTimer>(timers);
            timers = new List<SingleTimer>();
            foreach (var timer in timersClone)
            {
                CreateTimerAsync(timer, CancellationToken.None);
            }
        }

       private void GetSeriesTimerData()
       {
           GetFileCopy(ref seriesTimers, "seriesTimers.xml");
       }

        private void GetTimerData()
        {
            GetFileCopy(ref timers,"timers.xml");
            InitializeTimer();
        }

       public void GetFileCopy<T>(ref T obj, string filename)
        {
            var path = DataPath + @"\"+filename;
            if (File.Exists(path))
            {
                obj = (T) _xmlSerializer.DeserializeFromFile(typeof(T), path);
            }
            else
            {
                
                obj = (T)Activator.CreateInstance<T>();
            }
        }

        private void UpdateSeriesTimerData() { CreateFileCopy(seriesTimers,DataPath+ @"\seriesTimers.xml"); }
        private void UpdateTimerData() { CreateFileCopy(timers,DataPath+ @"\timers.xml"); }
        private void UpdateRecordingsData(){ CreateFileCopy(recordings,DataPath+ @"\recordings.xml"); }

        public void CreateFileCopy(object obj, string filePath)
        {
                Helpers.CreateFileCopy(obj,filePath,_xmlSerializer);
        }


        private void GetRecordingData(){
            GetFileCopy(ref recordings, "recordings.xml");
            recordings = recordings.GroupBy(x => x.Id).Select(g => g.First()).ToList();
        }
        private void SaveEpgDataForChannel(string channelId, IEnumerable<ProgramInfo> epgData)
        {
            CreateFileCopy(epgData, DataPath+@"\EPG\" + channelId + ".xml");
        }
        private List<ProgramInfo> GetEpgDataForChannel(string channelId)
        {
            List<ProgramInfo> channelEpg = new List<ProgramInfo>();
            GetFileCopy<List<ProgramInfo>>(ref channelEpg, @"EPG\" + channelId + ".xml");
            return channelEpg;
        }
        private List<ProgramInfo> GetEpgDataForAllChannels()
        {
            List<ProgramInfo> channelEpg = new List<ProgramInfo>();
            DirectoryInfo dir = new DirectoryInfo(DataPath + @"\EPG\");
            List<FileInfo> Files = dir.GetFiles("*.xml").ToList();
            List<string> channels = Files.Select(f => f.Name).ToList();
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

        public async Task RecordStream(SingleTimer timer)
        {
            var mediaStreamInfo = await GetChannelStream(timer.ChannelId, "none", CancellationToken.None);
            HttpRequestOptions options = new HttpRequestOptionsMod()
            {
                Url = mediaStreamInfo.Path + "?duration=" + timer.Duration()
            };
            var info = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);
            var recordPath = RecordingPath;
            if (info.IsMovie)
            {
                recordPath += @"\Movies"+StringHelper.RemoveSpecialCharacters(info.Name);
            }
            else
            {
                recordPath += @"\TV\"+StringHelper.RemoveSpecialCharacters(info.Name);
            }
            Directory.CreateDirectory(recordPath);
            recordPath +=  @"\" + timer.GetRecordingName(info);
            recordings.First(x => x.Id == info.Id).Path = recordPath;
            recordings.First(x => x.Id == info.Id).Status = RecordingStatus.InProgress;
            UpdateRecordingsData();
            try
            {
                await RecordingHelper.DownloadVideo(_httpClient, options, _logger, recordPath, timer.Cts.Token);
                recordings.First(x => x.Id == info.Id).Status = RecordingStatus.Completed;
            }
            catch 
            {
                recordings.First(x => x.Id == info.Id).Status = RecordingStatus.Error;
            }
            UpdateRecordingsData();
            timers.RemoveAll(x => x.Id == timer.Id);
            UpdateTimerData();
            _logger.Info("Recording was a success");
        }


        private void ScheduleRecording(TimerInfo timer)
        {
            var info = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);
            if (recordings.FindIndex(x => x.Id == timer.ProgramId) == -1)
            {
                recordings.Add(new RecordingInfo()
                {
                    ChannelId = info.ChannelId,
                    Id = info.Id,
                    StartDate = info.StartDate,
                    EndDate = info.EndDate,
                    Genres = info.Genres ?? null,
                    IsKids = info.IsKids,
                    IsLive = info.IsLive,
                    IsMovie = info.IsMovie,
                    IsHD = info.IsHD,
                    IsNews = info.IsNews,
                    IsPremiere = info.IsPremiere,
                    IsSeries = info.IsSeries,
                    IsSports = info.IsSports,
                    IsRepeat = !info.IsPremiere,
                    Name = info.Name,
                    EpisodeTitle = info.EpisodeTitle ?? "",
                    ProgramId = info.Id,
                    HasImage = info.HasImage ?? false,
                    ImagePath = info.ImagePath ?? null,
                    ImageUrl = info.ImageUrl,
                    OriginalAirDate = info.OriginalAirDate,
                    Status = RecordingStatus.Scheduled,
                    Overview = info.Overview,
                    SeriesTimerId = info.Id.Substring(0,10)
                });
            }
            UpdateRecordingsData();
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
            streams.Remove(id);
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
                ScheduleRecording(info);
            }
            else
            {
                _logger.Info("Timer not created the show is about to end or has already ended");
            }
            return Task.FromResult(0);
        }

        public event EventHandler DataSourceChanged;

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            var remove = recordings.FindIndex(r => r.Id == recordingId);
            if(remove != -1)
            {
                Helpers.DeleteFile(recordings[remove].Path);
                recordings.RemoveAt(remove);
            }
            UpdateRecordingsData();
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
            if (FirstRun)
            {
                GetRecordingData();
                GetSeriesTimerData();
                GetTimerData();               
            }
            FirstRun = false;
			
            if (string.IsNullOrWhiteSpace(Plugin.Instance.Configuration.RecordingPath))
            {
                Plugin.Instance.Configuration.RecordingPath = Path.Combine(_appPaths.TempDirectory, "embytv", "recordings");
            }
            Plugin.Instance.SaveConfiguration();
            if (isConfigChange)
            {
                await AddLineupIfNeeded(config, cancellationToken).ConfigureAwait(false);
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
                catch (ArgumentException e)
                {
                    _logger.Debug("Cant authenticate with Schedules Direct/ user may not have filled the login information.");
                }
            }
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var epgData = await _tvGuide.getTvGuideForChannel(channelId, startDateUtc, endDateUtc, cancellationToken);
            var programInfos = epgData as IList<ProgramInfo> ?? epgData.ToList();
            if (!programInfos.Any())
            {
                _logger.Debug("Couldnt find any data on the epg provider looking through the local cache");
                epgData = GetEpgDataForChannel(channelId);
            }
            else
            {
                _logger.Debug("Found data on the epg provider saving it to the local cache");
                SaveEpgDataForChannel(channelId, programInfos);
            }
            foreach (var seriesTimer in seriesTimers)
            {
                if (seriesTimer.ChannelId == channelId || seriesTimer.RecordAnyChannel)
                {
                    _logger.Info("Proccesing series timers for show" + seriesTimer.Id + " on channel " + channelId);
                    UpdateTimersForSeriesTimer(epgData, seriesTimer);
                }
            }
            return epgData;
        }

        private void UpdateTimersForSeriesTimer(IEnumerable<ProgramInfo> epgData,SeriesTimer seriesTimer)
        {
                var tempTimers = seriesTimer.GetTimersForSeries(epgData, recordings,_logger);
                _logger.Info("Creating " + tempTimers.Count + " timers for series timer " + seriesTimer.Id);
                foreach (var timer in tempTimers)
                {
                    CreateTimerAsync(timer, CancellationToken.None);
                }   
        }

        public Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<RecordingInfo>)recordings);
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
            get { return "http://emby.media"; }
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
            _logger.Info("Streaming Channel "+channelId);
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
            mediaSourceInfo.Id = Guid.NewGuid().ToString("N");
            streams.Add(mediaSourceInfo.Id,mediaSourceInfo);
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
            var seriesTimer = new SeriesTimer(info);
            List<ProgramInfo> pInfo;
            if (seriesTimer.RecordAnyChannel)
            {
                pInfo = GetEpgDataForAllChannels();
            }
            else
            {
                pInfo = GetEpgDataForChannel(info.ChannelId);
            }
            UpdateTimersForSeriesTimer(pInfo,seriesTimer);
            seriesTimers.Add(seriesTimer);
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
            var seriesTimer = new SeriesTimer(info);
            if (recording >= 0) { seriesTimers[recording] = new SeriesTimer(info); }
            List<ProgramInfo> pInfo;
            if (seriesTimer.RecordAnyChannel)
            {
                pInfo = GetEpgDataForAllChannels();
            }
            else
            {
                pInfo = GetEpgDataForChannel(info.ChannelId);
            }
            UpdateTimersForSeriesTimer(pInfo, seriesTimer);
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
