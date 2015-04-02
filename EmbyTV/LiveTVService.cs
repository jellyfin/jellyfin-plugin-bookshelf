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
﻿using MediaBrowser.Model.Connect;



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
        private List<Recording> timers;
        private List<RecordingSeries> seriesTimers;
        private string dataPath;
        private readonly IXmlSerializer _xmlSerializer;
        

       public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager, IXmlSerializer xmlSerializer)
        {
            _liveStreams = 0;
            _logger = logManager.GetLogger(Name);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            FirstRun = true;
            timers = new List<Recording>();
           _xmlSerializer = xmlSerializer;
            dataPath = Path.GetDirectoryName(Plugin.Instance.AssemblyFilePath)+@"\data\EmbyTV";
           _logger.Info("Directory is: "+dataPath);
            Directory.CreateDirectory(dataPath);
            RefreshConfigData(CancellationToken.None);
            timers = GetTimerData(dataPath, _xmlSerializer);
            seriesTimers= GetSeriesTimerData(dataPath,_xmlSerializer);
            Plugin.Instance.Configuration.TunerDefaultConfigurationsFields = TunerHostConfig.BuildDefaultForTunerHostsBuilders();
            Plugin.Instance.ConfigurationUpdated += (sender, args) => { RefreshConfigData(CancellationToken.None); };
   
        }

       private static List<RecordingSeries> GetSeriesTimerData(string dataPath, IXmlSerializer xmlSerializer)
       {
           List<RecordingSeries> dummy = new List<RecordingSeries>();
           var timerPath = dataPath + @"\seriesTimers.xml";
           if (File.Exists(timerPath))
           {
            return  (List<RecordingSeries>)xmlSerializer.DeserializeFromFile(dummy.GetType(),timerPath);
           }
           else { return dummy; }
       }
       private static List<Recording> GetTimerData(string dataPath,IXmlSerializer xmlSerializer)
       {
           List<Recording> dummy = new List<Recording>();
           var timerPath = dataPath + @"\timers.xml";
           if (File.Exists(timerPath))
           {
               return (List<Recording>) xmlSerializer.DeserializeFromFile(dummy.GetType(), timerPath);
           }
           else
           {
               return dummy;
           }
       }

        private void UpdateSeriesTimerData()
        {
           var timerPath = dataPath + @"\seriesTimers.xml";
         
           _xmlSerializer.SerializeToFile(seriesTimers,timerPath);
        }
        private void UpdateTimerData()
        {
            var timerPath = dataPath + @"\timers.xml";
            if (timers != null)
            {
                _xmlSerializer.SerializeToFile(timers, timerPath);
            }
            else
            {
                _logger.Info("Timers list is empty somethign is wrong.");
                _logger.Info("Timers list is empty somethign is wrong. " + timers.Count());
            }
        }


        /// <summary>
        /// Ensure that we are connected to the HomeRunTV server
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_tunerServer[0].getWebUrl()))
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

  

        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info("Closing " + id);
            return Task.FromResult(0);
        }



        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            timers.Add(new Recording(info));
            UpdateTimerData();
            return Task.FromResult(0);
        }

        public event EventHandler DataSourceChanged;

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            var remove = timers.SingleOrDefault(r => r.Id == recordingId);
            if(remove != null) { timers.Remove(remove);}
            return Task.FromResult(true);
        }



        public async void RefreshConfigData(CancellationToken cancellationToken)
        {
            var config = Plugin.Instance.Configuration;
            if (config.TunerHostsConfiguration != null)
            {
                _tunerServer = TunerHostFactory.CreateTunerHosts(config.TunerHostsConfiguration, _logger,_jsonSerializer, _httpClient);
            }
            FirstRun = false;
            _tvGuide = new EPGProvider.SchedulesDirect(config.username,config.hashPassword,config.lineup, _logger, _jsonSerializer, _httpClient);
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
            
            return Task.FromResult((IEnumerable<SeriesTimerInfo>) seriesTimers);
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
            return Task.FromResult((IEnumerable<TimerInfo>) timers);
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
            HttpRequestOptions options = new HttpRequestOptions()
            {
                Url = "http://192.168.2.238:5004/auto/v508?duration=10",
                CancellationToken = cancellationToken
            };
            string filePath = Path.GetTempPath() + "/test.ts";
            DVR.RecordingHelper.DownloadVideo(_httpClient, options, _logger,filePath).Start();
            return Task.FromResult(0);
        }

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            HttpRequestOptions options = new HttpRequestOptions()
            {
                Url = "http://192.168.2.238:5004/auto/v508?duration=10"
            };

            //DVR.RecordingHelper.DownloadVideo(_httpClient, options, _logger).Start();
            return Task.FromResult(0);
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
                                Index = -1,
                                IsInterlaced = true
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

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var remove = seriesTimers.SingleOrDefault(r => r.Id == timerId);
            if (remove != null) { seriesTimers.Remove(remove); }
            UpdateSeriesTimerData();
            return Task.FromResult(true);
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var remove = timers.SingleOrDefault(r => r.Id == timerId);
            if (remove != null) { timers.Remove(remove); }
            UpdateTimerData();
            return Task.FromResult(true);
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            seriesTimers.Add(new RecordingSeries(info));
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
            if (recording >= 0) { seriesTimers[recording] = new RecordingSeries(info);}
            UpdateSeriesTimerData();
            return Task.FromResult(true);
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            var recording = timers.FindIndex(r => r.Id == info.Id);
            if (recording >= 0) { timers[recording] = new Recording(info); }
            UpdateTimerData();
            return Task.FromResult(true);
        }
    }
}
