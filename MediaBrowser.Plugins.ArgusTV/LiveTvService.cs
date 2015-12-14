using System.Linq;
using ArgusTV.DataContracts;
using ArgusTV.ServiceProxy;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.ArgusTV.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;


namespace MediaBrowser.Plugins.ArgusTV
{
    public class LiveTvService : ILiveTvService
    {
        private readonly ILogger _logger;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IJsonSerializer _jsonSerializer;
        private readonly Dictionary<Guid, Dictionary<Guid,Guid>> _heartBeat = new Dictionary<Guid, Dictionary<Guid, Guid>>();
        private Timer _timer;

        private bool IsAvailable { get; set; }

        //The version we support in this plugin
        private const int ApiVersionLevel = 66;

        public LiveTvService(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;

            IsAvailable = false;
            InitializeServiceChannelFactories();

            _timer = new Timer(TimerTask, null , 0, 30000);
        }

        /// <summary>
        /// Initialize the ArgusTV ServiceChannelFactories 
        /// </summary>
        private void InitializeServiceChannelFactories()
        {
            _logger.Info("[ArgusTV] Start InitializeServiceChannelFactories");
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

                UtilsHelper.DebugInformation(_logger,string.Format("[ArgusTV] ServerSettings: {0}", _jsonSerializer.SerializeToString(serverSettings)));
                Proxies.Initialize(serverSettings);
                _logger.Info(string.Format("[ArgusTV] Successful initialized on server {0} with port {1}", serverIp, serverPort));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] It's not possible to initialize a connection to the ArgusTV Server on server {0} with port {1}", ex, serverIp, serverPort);
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

            try
            {
                if (!IsAvailable)
                {
                    if (!Proxies.IsInitialized)
                    {
                        InitializeServiceChannelFactories();
                    } 
                    else 
                    {
                        await CheckStatus(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] It's not possible to Ensure a connection to the ArgusTV Server", ex);
            }

        }

        /// <summary>
        /// Check the status of the ArgustTV Server
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        private async Task CheckStatus(CancellationToken cancellationToken)
        {
            _logger.Info("[ArgusTV] Start CheckStatus");

            try
            {
                switch (await Proxies.CoreService.Ping(ApiVersionLevel))
                {
                    case 0:
                        IsAvailable = true;
                        _logger.Info("[ArgusTV] ArgusTV is available]");
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
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] Check Status failed", ex);
            }
        }

        /// <summary>
        /// Gets the StatusInfo async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvServiceStatusInfo}</returns>
        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            _logger.Info("[ArgusTV] Start GetStatusInfoAsync");
            await EnsureConnectionAsync(cancellationToken);
            NewVersionInfo newVersionAvailable = null;
            string serverVersion = string.Empty;

            try
            {
                newVersionAvailable = Proxies.CoreService.IsNewerVersionAvailable().Result;
                serverVersion = await Proxies.CoreService.GetServerVersion();
                _logger.Info(string.Format("[ArgusTV] New Version Available: {0} & serverVersion: {1}", newVersionAvailable, serverVersion));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] Get Status Information failed", ex);
            }

            //TODO: Get more information

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
            _logger.Info("[ArgusTV] Start GetChannels Async");
            await EnsureConnectionAsync(cancellationToken);
            var config = Plugin.Instance.Configuration;
            var channels = new List<ChannelInfo>();
            int i = 0;

            try
            {
                foreach (var channel in Proxies.SchedulerService.GetAllChannels(ChannelType.Television).Result)
                {
                    channels.Add(new ChannelInfo
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

                UtilsHelper.DebugInformation(_logger, string.Format("[ArgusTV] Channels: {0}", _jsonSerializer.SerializeToString(channels)));

            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] Get Channels Async failed", ex);   
            }

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
            _logger.Info(string.Format("[ArgusTV] Start GetPrograms Async, retrieve all programs for ChannelId: {0}", channelId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                //We need the guideChannelId for the EPG 'Container'
                var guideChannelId = Proxies.SchedulerService.GetChannelById(Guid.Parse(channelId)).Result.GuideChannelId;

                if (guideChannelId == null) return new List<ProgramInfo>();
                var programs = Proxies.GuideService.GetChannelProgramsBetween(Guid.Parse(guideChannelId.ToString()), startDateUtc, endDateUtc).Result;

                var programList = new List<ProgramInfo>();

                foreach (var guideProgramSummary in programs)
                {
                    var program = new ProgramInfo
                    {
                        ChannelId = channelId,
                        Id = guideProgramSummary.GuideProgramId.ToString(),
                        Overview = Proxies.GuideService.GetProgramById(guideProgramSummary.GuideProgramId).Result.Description,
                        StartDate = guideProgramSummary.StartTimeUtc,
                        EndDate = guideProgramSummary.StopTimeUtc,
                        IsHD = (guideProgramSummary.Flags == GuideProgramFlags.HighDefinition),
                        IsRepeat = guideProgramSummary.IsRepeat,
                        IsPremiere = guideProgramSummary.IsPremiere,
                        HasImage = false, //TODO
                        //ImageUrl = , //TODO
                        //IsMovie = ,
                    };

                    if (!string.IsNullOrEmpty(guideProgramSummary.Title))
                    {
                        program.Name = guideProgramSummary.Title;
                    };

                    if (!string.IsNullOrEmpty(guideProgramSummary.SubTitle))
                    {
                        program.EpisodeTitle = guideProgramSummary.SubTitle;
                    };


                    if (!string.IsNullOrEmpty(guideProgramSummary.Category))
                    {
                        //We only have 1 category
                        program.Genres = new List<string>
                        {
                            guideProgramSummary.Category
                        };

                        program.IsSeries = GeneralHelpers.ContainsWord(guideProgramSummary.Category, "series",
                            StringComparison.OrdinalIgnoreCase);
                        program.IsNews = GeneralHelpers.ContainsWord(guideProgramSummary.Category, "news",
                            StringComparison.OrdinalIgnoreCase);
                        program.IsKids = GeneralHelpers.ContainsWord(guideProgramSummary.Category, "animation",
                            StringComparison.OrdinalIgnoreCase);
                        program.IsSports =
                            GeneralHelpers.ContainsWord(guideProgramSummary.Category, "sport",
                                StringComparison.OrdinalIgnoreCase) ||
                            GeneralHelpers.ContainsWord(guideProgramSummary.Category, "motor sports",
                                StringComparison.OrdinalIgnoreCase) ||
                            GeneralHelpers.ContainsWord(guideProgramSummary.Category, "football",
                                StringComparison.OrdinalIgnoreCase) ||
                            GeneralHelpers.ContainsWord(guideProgramSummary.Category, "cricket",
                                StringComparison.OrdinalIgnoreCase);
                    }
                    programList.Add(program);
                }

                return programList;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] Get Programs Async failed", ex);               
            }

            return new List<ProgramInfo>();
        }

        /// <summary>
        /// Cancel pending scheduled Recording 
        /// </summary>
        /// <param name="timerId">The timerId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[ArgusTV] Start CancelTimer Async for recordingId: {0}", timerId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Proxies.SchedulerService.DeleteSchedule(Guid.Parse(timerId)).Wait(cancellationToken);
                _logger.Info(string.Format("[ArgusTV] Successful cancelled the pending Recording for recordingId: {0}", timerId));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] It's not possible to cancel the pending recording with recordingId: {0} on the ArgusTV Server", ex, timerId);
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
            _logger.Info(string.Format("[ArgusTV] Start CancelSeriesTimer Async for recordingId: {0}", timerId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Proxies.SchedulerService.DeleteSchedule(Guid.Parse(timerId)).Wait(cancellationToken);
                _logger.Info(string.Format("[ArgusTV] Successful cancelled the pending Recording for recordingId: {0}", timerId));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] It's not possible to cancel the pending recording with recordingId: {0} on the ArgusTV Server", ex, timerId);
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
            _logger.Info(string.Format("[ArgusTV] Start DeleteRecording Async for recordingId: {0}", recordingId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Proxies.ControlService.DeleteRecordingById(Guid.Parse(recordingId)).Wait(cancellationToken);
                _logger.Info(string.Format("[ArgusTV] Successful deleted the Recording for recordingId: {0}", recordingId));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] It's not possible to delete the recording with recordingId: {0} on the ArgusTV Server", ex, recordingId);
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
            _logger.Info(string.Format("[ArgusTV] Start CreateTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Schedule newSchedule = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

                newSchedule.PostRecordMinutes = (info.PostPaddingSeconds / 60);
                newSchedule.PreRecordMinutes = (info.PrePaddingSeconds / 60);
                newSchedule.Name = info.Name;

                newSchedule.Rules = UpdateRules(new List<ScheduleRule>(), info, null);

                UtilsHelper.DebugInformation(_logger, string.Format("[ArgusTV] CreateTimer with the following schedule: {0}", _jsonSerializer.SerializeToString(newSchedule)));
                Proxies.SchedulerService.SaveSchedule(newSchedule).Wait(cancellationToken);
                _logger.Info(string.Format("[ArgusTV] Successful CreateTimer for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] CreateTimer async for ChannelId: {0} & Name: {1}", ex, info.ChannelId, info.Name);
                throw new LiveTvConflictException();
            }
        }

        private async Task<Schedule> GetDefaultScheduleSettings(CancellationToken cancellationToken)
        {
            _logger.Info("[ArgusTV] Start GetDefaultScheduleSettings");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            return Proxies.SchedulerService.CreateNewSchedule(ChannelType.Television, ScheduleType.Recording).Result;
        }

        /// <summary>
        /// Create a recurrent recording
        /// </summary>
        /// <param name="info">The recurrend program info</param>
        /// <param name="cancellationToken">The CancelationToken</param>
        /// <returns></returns>
        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[ArgusTV] Start CreateSeriesTimer Async for channelId: {0} & Name: {1}", info.ChannelId, info.Name));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Schedule newSchedule = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

                newSchedule.PostRecordMinutes = (info.PostPaddingSeconds / 60);
                newSchedule.PreRecordMinutes = (info.PrePaddingSeconds / 60);
                newSchedule.Name = info.Name;

                newSchedule.Rules = UpdateRules(new List<ScheduleRule>(), null, info);

                UtilsHelper.DebugInformation(_logger, string.Format("[ArgusTV] CreateSeriesTimer with the following schedule: {0}", _jsonSerializer.SerializeToString(newSchedule)));
                Proxies.SchedulerService.SaveSchedule(newSchedule).Wait(cancellationToken);
                _logger.Info(string.Format("[ArgusTV] Successful CreateSeriesTimer for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] CreateSeriesTimer async for ChannelId: {0} & Name: {1}", ex, info.ChannelId, info.Name);
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
            _logger.Info(string.Format("[ArgusTV] Start UpdateTimer Async for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Schedule updateSchedule = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

                updateSchedule.PostRecordMinutes = (info.PostPaddingSeconds / 60);
                updateSchedule.PreRecordMinutes = (info.PrePaddingSeconds / 60);
                updateSchedule.Name = info.Name;

                updateSchedule.Rules = UpdateRules(new List<ScheduleRule>(), info, null);

                UtilsHelper.DebugInformation(_logger, string.Format("[ArgusTV] UpdateTimer with the following Schedule: {0}", _jsonSerializer.SerializeToString(updateSchedule)));
                Proxies.SchedulerService.SaveSchedule(updateSchedule).Wait(cancellationToken);
                _logger.Info(string.Format("[ArgusTV] Successful UpdateTimer for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] UpdateTimer async for ChannelId: {0} & Name: {1}", ex, info.ChannelId, info.Name);
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
            _logger.Info(string.Format("[ArgusTV] Start UpdateSeriesTimer Async for channelId: {0} & Name: {1}", info.ChannelId, info.Name));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                Schedule updateSchedule = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

                updateSchedule.PostRecordMinutes = (info.PostPaddingSeconds / 60);
                updateSchedule.PreRecordMinutes = (info.PrePaddingSeconds / 60);
                updateSchedule.Name = info.Name;

                updateSchedule.Rules = UpdateRules(new List<ScheduleRule>(), null, info);

                UtilsHelper.DebugInformation(_logger, string.Format("[ArgusTV] UpdateSeriesTimer with the following Schedule: {0}", _jsonSerializer.SerializeToString(updateSchedule)));
                Proxies.SchedulerService.SaveSchedule(updateSchedule).Wait(cancellationToken);
                _logger.Info(string.Format("[ArgusTV] Successful UpdateSeriesTimer for ChannelId: {0} & Name: {1}", info.ChannelId, info.Name));

            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] UpdateSeriesTimer async for ChannelId: {0} & Name: {1}", ex, info.ChannelId, info.Name);
                throw new LiveTvConflictException();
            }

        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ChannelInfo
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ProgramInfo
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
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
            _logger.Info(string.Format("[ArgusTV] Start GetRecordings Async"));
            await EnsureConnectionAsync(cancellationToken);
            List<RecordingInfo> timerInfos = new List<RecordingInfo>();

            try
            {
                var allRecordingGroups = Proxies.ControlService.GetAllRecordingGroups(ChannelType.Television, RecordingGroupMode.GroupByChannel).Result;

                var channels = (from allRecordingGroup in allRecordingGroups
                                select allRecordingGroup.ChannelId).Distinct();


                foreach (var channel in channels)
                {
                    timerInfos.AddRange(
                        Proxies.ControlService.GetFullRecordings(ChannelType.Television, null, null, null, channel)
                            .Result.Select(recording => new RecordingInfo
                            {
                                Id = recording.RecordingId.ToString(),
                                ChannelId = recording.ChannelId.ToString(),
                                Name = recording.Title,
                                Overview = recording.Description,
                                StartDate = recording.ProgramStartTimeUtc,
                                EndDate = recording.ProgramStopTimeUtc
                            }));
                }

                UtilsHelper.DebugInformation(_logger, string.Format("[ArgusTV] GetRecordings with the following RecordingInfo: {0}", _jsonSerializer.SerializeToString(timerInfos)));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] GetRecordings async failed", ex);
            }

            return timerInfos;
        }

        /// <summary>
        /// Get the pending Recordings.
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[ArgusTV] Start GetTimer Async"));
            await EnsureConnectionAsync(cancellationToken);
            List<TimerInfo> timerInfos = new List<TimerInfo>();

            try
            {
                foreach (UpcomingRecording upcomingRecording in Proxies.ControlService.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings).Result)
                {
                    timerInfos.Add(new TimerInfo
                    {
                        Id = upcomingRecording.Program.ScheduleId.ToString(),
                        ChannelId = upcomingRecording.Program.Channel.ChannelId.ToString(),
                        Name = upcomingRecording.Title,
                        Overview = Proxies.GuideService.GetProgramById(Guid.Parse(upcomingRecording.Program.GuideProgramId.ToString())).Result.Description,
                        StartDate = upcomingRecording.ActualStartTimeUtc,
                        ProgramId = upcomingRecording.Program.GuideProgramId.ToString(),
                        EndDate = upcomingRecording.ActualStopTimeUtc,
                        PostPaddingSeconds = upcomingRecording.Program.PostRecordSeconds,
                        PrePaddingSeconds = upcomingRecording.Program.PreRecordSeconds,
                    });
                }

                UtilsHelper.DebugInformation(_logger, string.Format("[ArgusTV] GetTimers with the following TimerInfo: {0}", _jsonSerializer.SerializeToString(timerInfos)));

            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] GetTimers async failed", ex);
            }

            return timerInfos;
        }

        public async Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            await EnsureConnectionAsync(cancellationToken);

            var timerDefaults = Proxies.SchedulerService.CreateNewSchedule(ChannelType.Television, ScheduleType.Recording).Result;

            return new SeriesTimerInfo
            {
                PostPaddingSeconds = timerDefaults.PostRecordSeconds != null ? (int)timerDefaults.PostRecordSeconds : 0,
                PrePaddingSeconds = timerDefaults.PreRecordSeconds != null ? (int)timerDefaults.PreRecordSeconds : 0
            };
        }

        /// <summary>
        /// Get the recurrent recordings
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[ArgusTV] Start GetSeriesTimer Async"));
            await EnsureConnectionAsync(cancellationToken);
            List<SeriesTimerInfo> seriesTimerInfos = new List<SeriesTimerInfo>();

            try
            {
                List<UpcomingRecording> upcomingRecordings = Proxies.ControlService.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings).Result.GroupBy(u => u.Program.ScheduleId).Select(grp => grp.First()).ToList();

                foreach (UpcomingRecording upcomingRecording in upcomingRecordings)
                {
                    if (upcomingRecording != null && upcomingRecording.Program != null && upcomingRecording.Program.IsPartOfSeries)
                    {
                        var schedule = Proxies.SchedulerService.GetScheduleById(upcomingRecording.Program.ScheduleId).Result;
                        ScheduleRule daysOfWeekRule = schedule.Rules.SingleOrDefault(r => r.Type == ScheduleRuleType.DaysOfWeek);

                        var days = new List<DayOfWeek>();

                        if (daysOfWeekRule != null)
                        {
                            days = SchedulerHelper.GetDaysOfWeek((ScheduleDaysOfWeek)daysOfWeekRule.Arguments[0]);
                        }

                        seriesTimerInfos.Add(new SeriesTimerInfo
                        {
                            Id = upcomingRecording.Program.ScheduleId.ToString(),
                            ChannelId = upcomingRecording.Program.Channel.ChannelId.ToString(),
                            Name = upcomingRecording.Title,
                            Overview = Proxies.GuideService.GetProgramById(Guid.Parse(upcomingRecording.Program.GuideProgramId.ToString())).Result.Description,
                            StartDate = upcomingRecording.ActualStartTimeUtc,
                            ProgramId = upcomingRecording.Program.GuideProgramId.ToString(),
                            EndDate = upcomingRecording.ActualStopTimeUtc,
                            PostPaddingSeconds = upcomingRecording.Program.PostRecordSeconds,
                            PrePaddingSeconds = upcomingRecording.Program.PreRecordSeconds,
                            Days = days
                        });
                    }
                }

                UtilsHelper.DebugInformation(_logger,string.Format("[ArgusTV] GetSeriesTimers with the following TimerInfo: {0}", _jsonSerializer.SerializeToString(seriesTimerInfos)));

            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] GetSeriesTimers async failed", ex);
                throw new LiveTvConflictException();
            }


            return seriesTimerInfos;
        }

        public async Task<ChannelMediaInfo> GetChannelStream(string channelOid, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[ArgusTV] Start GetChannelStream Async for ChannelId: {0}", channelOid));
            await EnsureConnectionAsync(cancellationToken);
            var config = Plugin.Instance.Configuration;


            try
            {
                var channel = Proxies.SchedulerService.GetChannelById(Guid.Parse(channelOid)).Result;
                var result = Proxies.ControlService.TuneLiveStream(channel, null).Result;

                LiveStream liveStream = result.LiveStream;

                if (result.LiveStreamResult == LiveStreamResult.Succeeded)
                {
                    Guid uniqueId = Guid.NewGuid();
                    Dictionary<Guid, Guid> detail = new Dictionary<Guid, Guid>()
                    {
                        { Guid.Parse(channelOid), liveStream.RecorderTunerId}
                    };

                    _heartBeat.Add(uniqueId, detail);
                    
                    if (!config.EnableTimeschift)
                    {
                        return new ChannelMediaInfo
                        {
                            Id = uniqueId.ToString(),
                            Path = liveStream.RtspUrl,
                            Protocol = MediaProtocol.Rtsp,
                            ReadAtNativeFramerate = false,
                        };
                    }

                    //Standard UNC Path
                    return new ChannelMediaInfo
                    {
                        Id = uniqueId.ToString(),
                        Protocol = MediaProtocol.File,
                        Path = liveStream.TimeshiftFile,
                        Container = "tsbuffer"
                    };
                }

                if (result.LiveStreamResult == LiveStreamResult.NoFreeCardFound)
                {
                    _logger.Error("[ArgusTV] No Free Card Found");
                }

                if (result.LiveStreamResult == LiveStreamResult.ChannelTuneFailed)
                {
                    _logger.Error("[ArgusTV] Channel Tune Failed");
                }

                if (result.LiveStreamResult == LiveStreamResult.UnknownError)
                {
                    _logger.Error("[ArgusTV] Unknown Error");
                }

                if (result.LiveStreamResult == LiveStreamResult.IsScrambled)
                {
                    _logger.Error("[ArgusTV] Is Scrambled");
                }

                if (result.LiveStreamResult == LiveStreamResult.NotSupported)
                {
                    _logger.Error("[ArgusTV] Not Supported");
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] GetChannelStream async failed for ChannelId: {0}", ex, channelOid);
            }

            throw new ResourceNotFoundException(string.Format("Could not stream channel {0}", channelOid));
        }

        public async Task<ChannelMediaInfo> GetRecordingStream(string recordingId, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[ArgusTV] Start GetRecordingStream Async for RecordingId: {0}", recordingId));
            await EnsureConnectionAsync(cancellationToken);

            try
            {
                var recording = Proxies.ControlService.GetRecordingById(Guid.Parse(recordingId)).Result;

                return new ChannelMediaInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Path = recording.RecordingFileName,
                    Protocol = MediaProtocol.File,
                    Container = "ts"
                };

            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] GetRecordingStream async failed for RecordingId: {0}", ex, recordingId);
            }

            throw new ResourceNotFoundException(string.Format("Could not stream recording {0}", recordingId));
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Info(string.Format("[ArgusTV] Start CloseLiveStream Async for the stream with Channel/Recording Id: {0}", id));
            await EnsureConnectionAsync(cancellationToken);
            
            try
            {
                Dictionary<Guid, Guid> detail;
                 _heartBeat.TryGetValue(Guid.Parse(id), out detail);

                var runningLiveStream = Proxies.ControlService.GetLiveStreams().Result.SingleOrDefault(l => l.RecorderTunerId == detail.First().Value && l.Channel.ChannelId == detail.First().Key);
                _heartBeat.Remove(Guid.Parse(id));
                if (runningLiveStream != null)
                {
                    Proxies.ControlService.StopLiveStream(runningLiveStream).Wait(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] CloseLiveStream async failed for the stream with ChannelId: {0}", ex, id);
            }
        }

        public async Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        public Task<MediaSourceInfo> GetRecordingStream(string recordingId, string streamId, CancellationToken cancellationToken)
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

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
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

        public async void TimerTask(object state)
        {
            await EnsureConnectionAsync(new CancellationToken());

            try
            {
                var liveStreams = Proxies.ControlService.GetLiveStreams().Result;

                if (liveStreams.Any())
                {
                    foreach (var liveStream in liveStreams)
                    {
                        foreach (var row in _heartBeat)
                        {
                            if (row.Value.ContainsKey(liveStream.Channel.ChannelId) &&
                                row.Value.ContainsValue(liveStream.RecorderTunerId))
                            {
                                _logger.Info(string.Format("[ArgusTV] KeepLiveStreamAlive Channel: {0} with streamURL: {1} or streamFile: {2} ", liveStream.Channel.DisplayName, liveStream.RtspUrl, liveStream.TimeshiftFile));
                                await Proxies.ControlService.KeepLiveStreamAlive(liveStream);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("[ArgusTV] KeepStreamAlive Event failed", ex);
            }
        }

        private List<ScheduleRule> UpdateRules(List<ScheduleRule> rules, TimerInfo timerInfo, SeriesTimerInfo seriesTimerInfo)
        {
            if (timerInfo != null)
            {
                //Single Recording    
                SchedulerHelper.AppendTitleRule(rules, 0, timerInfo.Name);
                SchedulerHelper.AppendChannelsRule(rules, false, new List<Guid> { Guid.Parse(timerInfo.ChannelId) });
                SchedulerHelper.AppendOnDateAndDaysOfWeekRule(rules, ScheduleDaysOfWeek.None, timerInfo.StartDate.ToLocalTime());
                SchedulerHelper.AppendAroundTimeRule(rules, timerInfo.StartDate.ToLocalTime());

            }
            else if (seriesTimerInfo != null)
            {
                //Serie Recording
                SchedulerHelper.AppendTitleRule(rules, 0, seriesTimerInfo.Name);
                SchedulerHelper.AppendOnDateAndDaysOfWeekRule(rules, SchedulerHelper.GetScheduleDaysOfWeek(seriesTimerInfo.Days), seriesTimerInfo.StartDate.ToLocalTime());
                SchedulerHelper.AppendNewEpisodesOnlyRule(rules, seriesTimerInfo.RecordNewOnly);

                if (!seriesTimerInfo.RecordAnyTime)
                {
                    SchedulerHelper.AppendAroundTimeRule(rules, seriesTimerInfo.StartDate.ToLocalTime());
                }

                if (!seriesTimerInfo.RecordAnyChannel)
                {
                    SchedulerHelper.AppendChannelsRule(rules, false, new List<Guid> { Guid.Parse(seriesTimerInfo.ChannelId) });
                }
            }

            return rules;
        }
    }
}
