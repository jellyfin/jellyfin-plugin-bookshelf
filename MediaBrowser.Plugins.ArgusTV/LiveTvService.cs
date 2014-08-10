using System.Linq;
using ArgusTV.DataContracts;
using ArgusTV.ServiceProxy;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.ArgusTV.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.ArgusTV
{
    public class LiveTvService : ILiveTvService
    {
        private readonly ILogger _logger;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IJsonSerializer _jsonSerializer;

        private bool IsAvailable { get; set; }

        //The version we support in this plugin
        private const int ApiVersionLevel = 66;

        public LiveTvService(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;

            IsAvailable = false;
            InitializeServiceChannelFactories();
        }

        /// <summary>
        /// Initialize the ArgusTV ServiceChannelFactories 
        /// </summary>
        private void InitializeServiceChannelFactories()
        {
            _logger.Debug("[ArgusTV] Start InitializeServiceChannelFactories");
            var config = Plugin.Instance.Configuration;
            var serverIp = config.ServerIp;
            var serverPort = config.ServerPort;

            try
            {
                var serverSettings = new ServerSettings
                {
                    ServerName = serverIp,
                    Port = serverPort
                };

                _logger.Debug(string.Format("[ArgusTV] ServerSettings: {0}", _jsonSerializer.SerializeToString(serverSettings)));
                Proxies.Initialize(serverSettings, false);
                _logger.Debug(string.Format("[ArgusTV] Successful initialized on server {0} with port {1}",serverIp,serverPort));
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("[ArgusTV] It's not possible to initialize a connection to the ArgusTV Server on server {0} with port {1} with exception {2}", serverIp , serverIp ,ex.Message));
            }

        }

        /// <summary>
        /// Ensure that we are connected to the ArgusTV server
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            var config = Plugin.Instance.Configuration;

            if (string.IsNullOrEmpty(config.ServerIp))
            {
                _logger.Error("[ArgusTV] The ArgusTV ServerIp must be configured.");
                throw new InvalidOperationException("The ArgusTV ServerIp must be configured.");
            }

            if (string.IsNullOrEmpty(config.ServerPort.ToString(_usCulture)))
            {
                _logger.Error("[ArgusTV] The ArgusTV ServerPort must be configured.");
                throw new InvalidOperationException("The ArgusTV ServerPort must be configured.");
            }

            if (!IsAvailable)
            {
                await CheckStatus(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Check the status of the ArgustTV Server
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        private async Task CheckStatus(CancellationToken cancellationToken)
        {
            _logger.Debug("[ArgusTV] Start CheckStatus");
            
            switch (await Proxies.CoreService.Ping(ApiVersionLevel))
            {
                case 0:
                    IsAvailable = true;
                    _logger.Debug("[ArgusTV] ArgusTV is available]");
                    break;
                case -1:
                    IsAvailable = false;
                    _logger.Error("[ArgustTV] Plugin is too old for the ArgusTV server");
                    break;
                case 1:
                    IsAvailable = false;
                    _logger.Error("[ArgustTV] Plugin is too new for the ArgusTV server");
                    break;
            }
         }

        /// <summary>
        /// Gets the StatusInfo async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvServiceStatusInfo}</returns>
        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            //TODO: Get more information
            _logger.Debug("[ArgusTV] Start GetStatusInfoAsync");
            await EnsureConnectionAsync(cancellationToken);

            NewVersionInfo newVersionAvailable = Proxies.CoreService.IsNewerVersionAvailable().Result;
            string serverVersion = await Proxies.CoreService.GetServerVersion();
            _logger.Debug(string.Format("[ArgusTV] New Version Available: {0} & serverVersion: {1}",newVersionAvailable,serverVersion));

            return new LiveTvServiceStatusInfo
            {
                HasUpdateAvailable = (newVersionAvailable != null),
                Version = serverVersion
            };
        }

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("[ArgusTV] Start GetChannels Async");
            await EnsureConnectionAsync(cancellationToken);
            var config = Plugin.Instance.Configuration;
            var channels = new List<ChannelInfo>();
            int i = 0;
            
            foreach (var channel in Proxies.SchedulerService.GetAllChannels(ChannelType.Television).Result)
            {
                channels.Add(new ChannelInfo()
                {
                    Name = channel.DisplayName,
                    Number = i.ToString(),
                    Id = channel.ChannelId.ToString(),
                    ImageUrl = string.Format("http://{0}:{1}/ArgusTV/Scheduler/ChannelLogo/{2}/1900-01-01", config.ServerIp, config.ServerPort, channel.ChannelId.ToString()), 
                    HasImage = true, //TODO: Important?
                    ChannelType = Model.LiveTv.ChannelType.TV,
                });
                i++;
            }
            
            _logger.Debug(string.Format("[ArgusTV] Channels: {0}", _jsonSerializer.SerializeToString(channels)));

            return channels;
        }

        /// <summary>
        /// Gets the Programs async
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="startDateUtc"></param>
        /// <param name="endDateUtc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            _logger.Debug(string.Format("[ArgusTV] Start GetPrograms Async, retrieve all programs for ChannelId: {0}",channelId));
            await EnsureConnectionAsync(cancellationToken);

            //We need the guideChannelId for the EPG 'Container'
            var guideChannelId = Proxies.SchedulerService.GetChannelById(Guid.Parse(channelId)).Result.GuideChannelId; //GuideChannelId;

            if (guideChannelId == null) return new List<ProgramInfo>();
            var programs = Proxies.GuideService.GetChannelProgramsBetween(Guid.Parse(guideChannelId.ToString()), startDateUtc, endDateUtc).Result;

            return programs.Select(program => new ProgramInfo()
            {
                ChannelId = channelId, 
                Id = program.GuideProgramId.ToString(), 
                Overview = Proxies.GuideService.GetProgramById(program.GuideProgramId).Result.Description, 
                StartDate = program.StartTimeUtc,
                EndDate = program.StopTimeUtc, 
                Genres = new List<string>() {program.Category}, //We only have 1 category
                //OriginalAirDate = , //TODO
                Name = program.Title,
                //OfficialRating = , //TODO
                //CommunityRating = , //TODO
                EpisodeTitle = program.SubTitle,
                //Audio = , //TODO
                IsHD = (program.Flags == GuideProgramFlags.HighDefinition), 
                IsRepeat = program.IsRepeat, 
                IsSeries = GeneralHelpers.ContainsWord(program.Category, "series", StringComparison.OrdinalIgnoreCase),
                //ImageUrl = , //TODO
                HasImage = false, //TODO
                IsNews = GeneralHelpers.ContainsWord(program.Category, "news", StringComparison.OrdinalIgnoreCase),
                //IsMovie = ,
                IsKids = GeneralHelpers.ContainsWord(program.Category, "animation", StringComparison.OrdinalIgnoreCase),
                IsSports = GeneralHelpers.ContainsWord(program.Category, "sport", StringComparison.OrdinalIgnoreCase) || 
                            GeneralHelpers.ContainsWord(program.Category, "motor sports", StringComparison.OrdinalIgnoreCase) || 
                            GeneralHelpers.ContainsWord(program.Category, "football", StringComparison.OrdinalIgnoreCase) || 
                            GeneralHelpers.ContainsWord(program.Category, "cricket", StringComparison.OrdinalIgnoreCase), 
                IsPremiere = program.IsPremiere
            }).ToList();
        }

        /// <summary>
        /// Cancel pending scheduled Recording 
        /// </summary>
        /// <param name="timerId">The timerId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            _logger.Debug(string.Format("[ArgusTV] Start CancelTimer Async for recordingId: {0}", timerId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Proxies.SchedulerService.DeleteSchedule(Guid.Parse(timerId)).Wait(cancellationToken);
                _logger.Debug(string.Format("[ArgusTV] Successful canceled the pending Recording for recordingId: {0}", timerId));
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("[ArgusTV] It's not possible to cancel the pending recording with recordingId: {0} on the ArgusTV Server with exception {1}", timerId , ex.Message));
            }
        }

        /// <summary>
        /// Cancel the Series Timer
        /// </summary>
        /// <param name="timerId">The Timer Id</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            _logger.Debug(string.Format("[ArgusTV] Start CancelSeriesTimer Async for recordingId: {0}", timerId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Proxies.SchedulerService.DeleteSchedule(Guid.Parse(timerId)).Wait(cancellationToken);
                _logger.Debug(string.Format("[ArgusTV] Successful canceled the pending Recording for recordingId: {0}",timerId));
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("[ArgusTV] It's not possible to cancel the pending recording with recordingId: {0} on the ArgusTV Server with exception {1}",timerId, ex.Message));
            }
        }

        /// <summary>
        /// Delete the Recording async from the disk
        /// </summary>
        /// <param name="recordingId">The recordingId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            _logger.Debug(string.Format("[ArgusTV] Start DeleteRecording Async for recordingId: {0}", recordingId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                //TODO: Recording filename?
                //Proxies.ControlService.DeleteRecording(Guid.Parse(recordingId), true);
                
                _logger.Debug(string.Format("[ArgusTV] Successful deleted the Recording for recordingId: {0}", recordingId));
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("[ArgusTV] It's not possible to delete the recording with recordingId: {0} on the ArgusTV Server with exception {1}", recordingId, ex.Message));
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
            _logger.Debug(string.Format("[ArgusTV] Start CreateTimer Async for ChannelId: {0} & Name: {1}",info.ChannelId, info.Name));
            await EnsureConnectionAsync(cancellationToken);

            Schedule newSchedule = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            newSchedule.PostRecordMinutes = (info.PostPaddingSeconds / 60);
            newSchedule.PreRecordMinutes = (info.PrePaddingSeconds / 60);

            newSchedule.Name = info.Name;
            newSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.TitleEquals, info.Name));
            newSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.OnDate, info.StartDate.Date.ToLocalTime())); //TODO: Argus uses only date so no problem to add the localTime
            newSchedule.Rules.Add(ScheduleRuleType.AroundTime, new ScheduleTime(info.StartDate.ToLocalTime().Hour, info.StartDate.ToLocalTime().Minute, info.StartDate.ToLocalTime().Second));
            newSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.Channels, Guid.Parse(info.ChannelId)));

            try
            {
                _logger.Debug(string.Format("[ArgusTV] CreateTimer with the following schedule: {0}", _jsonSerializer.SerializeToString(newSchedule)));
               Proxies.SchedulerService.SaveSchedule(newSchedule).Wait(cancellationToken);
               _logger.Debug(string.Format("[ArgusTV] Successful CeateTimer for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            }
            catch (Exception ex)
            {
                _logger.Debug(string.Format("[ArgusTV] CreateTimer async for ChannelId: {0} & Name: {1} with exception: {2}", info.ChannelId, info.Name, ex.Message));
                throw new LiveTvConflictException();
            }
        }

        private async Task<Schedule> GetDefaultScheduleSettings(CancellationToken cancellationToken)
        {
            _logger.Debug("[ArgusTV] Start GetDefaultScheduleSettings");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            return Proxies.SchedulerService.CreateNewSchedule(ChannelType.Television,ScheduleType.Recording).Result;
        }

        /// <summary>
        /// Create a recurrent recording
        /// </summary>
        /// <param name="info">The recurrend program info</param>
        /// <param name="cancellationToken">The CancelationToken</param>
        /// <returns></returns>
        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Debug(string.Format("[ArgusTV] Start CreateSeriesTimer Async for channelId: {0} & Name: {1}",info.ChannelId, info.Name));
            await EnsureConnectionAsync(cancellationToken);

            Schedule newSchedule = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            newSchedule.PostRecordMinutes = (info.PostPaddingSeconds / 60);
            newSchedule.PreRecordMinutes = (info.PrePaddingSeconds / 60);

            newSchedule.Name = info.Name;

            //TODO: Series Timer (how to know that?)
            newSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.TitleEquals, info.Name));
            newSchedule.Rules.Add(ScheduleRuleType.AroundTime, new ScheduleTime(info.StartDate.ToLocalTime().Hour, info.StartDate.ToLocalTime().Minute, info.StartDate.ToLocalTime().Second));

            if (!info.RecordAnyChannel)
            {
                newSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.Channels, Guid.Parse(info.ChannelId)));
            }


            try
            {
                Proxies.SchedulerService.SaveSchedule(newSchedule).Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Debug(string.Format("[ArgusTV] CreateSeriesTimer async for ChannelId: {0} & Name: {1} with exception: {2}", info.ChannelId, info.Name, ex.Message));
                throw new LiveTvConflictException();
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
            _logger.Debug(string.Format("[ArgusTV] Start UpdateTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            await EnsureConnectionAsync(cancellationToken);

            Schedule updateSchedule = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            updateSchedule.PostRecordMinutes = (info.PostPaddingSeconds / 60);
            updateSchedule.PreRecordMinutes = (info.PrePaddingSeconds / 60);

            updateSchedule.Name = info.Name;
            updateSchedule.IsOneTime = true;
            updateSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.TitleEquals, info.Name));
            updateSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.OnDate, info.StartDate.Date.ToLocalTime()));
            updateSchedule.Rules.Add(ScheduleRuleType.AroundTime, new ScheduleTime(info.StartDate.ToLocalTime().Hour, info.StartDate.ToLocalTime().Minute, info.StartDate.ToLocalTime().Second));
            updateSchedule.Rules.Add(new ScheduleRule(ScheduleRuleType.Channels, Guid.Parse(info.ChannelId)));

            try
            {
                _logger.Debug(string.Format("[ArgusTV] UpdateTimer with the following Schedule: {0}",  _jsonSerializer.SerializeToString(updateSchedule)));
                Proxies.SchedulerService.SaveSchedule(updateSchedule).Wait(cancellationToken);
                _logger.Debug(string.Format("[ArgusTV] Successful UpdateTimer for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            }
            catch (Exception ex)
            {
                _logger.Debug(string.Format("[ArgusTV] UpdateTimer async for ChannelId: {0} & Name: {1} with exception: {2}", info.ChannelId, info.Name, ex.Message));
                throw new LiveTvConflictException();
            }
        }

        /// <summary>
        /// Update the series Timer
        /// </summary>
        /// <param name="info">The series program info</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        public Task<StreamResponseInfo> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ChannelInfo
            throw new NotImplementedException();
        }

        public Task<StreamResponseInfo> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ProgramInfo
            throw new NotImplementedException();
        }

        public Task<StreamResponseInfo> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to RecordingInfo
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the Recordings async
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}</returns>
        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            _logger.Debug(string.Format("[ArgusTV] Start GetRecordings Async"));
            await EnsureConnectionAsync(cancellationToken);
            List<RecordingInfo> timerInfos = new List<RecordingInfo>();

            foreach (var recording in Proxies.ControlService.GetActiveRecordings().Result)
            {
                timerInfos.Add(new RecordingInfo()
                {
                    ChannelId = recording.Program.Channel.ChannelId.ToString(),
                    Name = recording.RecordingFileName,
                    Overview = Proxies.GuideService.GetProgramById(Guid.Parse(recording.Program.GuideProgramId.ToString())).Result.Description,
                    StartDate = recording.ActualStartTimeUtc,
                    ProgramId = recording.Program.GuideProgramId.ToString(),
                    EndDate = recording.ActualStopTimeUtc
                });
            }

            _logger.Debug(string.Format("[ArgusTV] GetRecordings with the following RecordingInfo: {0}", _jsonSerializer.SerializeToString(timerInfos)));
            return timerInfos;
        }

        /// <summary>
        /// Get the pending Recordings.
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Debug(string.Format("[ArgusTV] Start GetTimer Async"));
            await EnsureConnectionAsync(cancellationToken);
            List<TimerInfo> timerInfos = new List<TimerInfo>();

            foreach (UpcomingRecording upcomingRecording in Proxies.ControlService.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings).Result)
            {
                timerInfos.Add(new TimerInfo()
                {
                    Id = upcomingRecording.Program.ScheduleId.ToString(),
                    ChannelId =  upcomingRecording.Program.Channel.ChannelId.ToString(),
                    Name = upcomingRecording.Title,
                    Overview = Proxies.GuideService.GetProgramById(Guid.Parse(upcomingRecording.Program.GuideProgramId.ToString())).Result.Description,
                    StartDate = upcomingRecording.ActualStartTimeUtc,
                    ProgramId = upcomingRecording.Program.GuideProgramId.ToString(),
                    EndDate = upcomingRecording.ActualStopTimeUtc,
                    PostPaddingSeconds = upcomingRecording.Program.PostRecordSeconds,
                    PrePaddingSeconds = upcomingRecording.Program.PreRecordSeconds,
                });
            }

            _logger.Debug(string.Format("[ArgusTV] GetTimers with the following TimerInfo: {0}", _jsonSerializer.SerializeToString(timerInfos)));
            return timerInfos; 
        }

        public async Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            await EnsureConnectionAsync(cancellationToken);

            var timerDefaults = Proxies.SchedulerService.CreateNewSchedule(ChannelType.Television, ScheduleType.Recording).Result;
            
            return new SeriesTimerInfo()
            {
                PostPaddingSeconds = timerDefaults.PostRecordSeconds != null ? (int) timerDefaults.PostRecordSeconds:0,
                PrePaddingSeconds =  timerDefaults.PreRecordSeconds != null ? (int) timerDefaults.PreRecordSeconds:0
            };
        }

        /// <summary>
        /// Get the recurrent recordings
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        public async Task<LiveStreamInfo> GetRecordingStream(string recordingId, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        public async Task<LiveStreamInfo> GetChannelStream(string channelId, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        public async Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        public async Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

       /// <value>The name.</value>
        public string Name
        {
            get { return "ARGUS TV"; }
        }

        public string HomePageUrl
        {
            get { return "http://www.argus-tv.com/"; }
        }
        public event EventHandler DataSourceChanged;
        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;
    }
}
