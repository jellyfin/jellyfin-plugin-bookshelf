using ArgusTV.DataContracts;
using ArgusTV.ServiceProxy;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.ArgusTV.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.ArgusTV
{
    public class LiveTvService : ILiveTvService
    {
        private readonly ILogger _logger;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private bool IsAvailable { get; set; }

        //The version we support in this plugin
        private const int ApiVersionLevel = 66;

        public LiveTvService(ILogger logger)
        {
            _logger = logger;

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

            try
            {
                var serverSettings = new ServerSettings()
                {
                    ServerName = config.ServerIp,
                    Port = config.ServerPort
                };

                Proxies.Initialize(serverSettings, false);
                _logger.Debug("[ArgusTV] Successful initialized");
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("[ArgusTV] It's not possible to initialize a connection to the ArgusTV Server with exception {0}", ex.Message));
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
            
            Int32 result = Proxies.CoreService.Ping(ApiVersionLevel);

            switch (result)
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
            _logger.Debug("[ArgusTV] Start GetStatusInfoAsync");
            await EnsureConnectionAsync(cancellationToken);

            NewVersionInfo newVersionAvailable = Proxies.CoreService.IsNewerVersionAvailable();
            string serverVersion = Proxies.CoreService.GetServerVersion();
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
            var channels = new List<ChannelInfo>();
            var i = 1;

            foreach (var channel in Proxies.GuideService.GetAllChannels(ChannelType.Television))
            {
                channels.Add(new ChannelInfo()
                {
                    Name = channel.Name,
                    Number = i.ToString(CultureInfo.InvariantCulture),
                    Id = channel.GuideChannelId.ToString(),
                    //ImageUrl = ,
                    HasImage = false, //TODO: Refactor
                });
                i++;
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
            _logger.Debug(string.Format("[ArgusTV] Start GetPrograms Async, retrieve all programs for ChannelId: {0}",channelId));
            await EnsureConnectionAsync(cancellationToken);

            var programs = Proxies.GuideService.GetChannelProgramsBetween(Guid.Parse(channelId), startDateUtc, endDateUtc);

            return programs.Select(program => new ProgramInfo()
            {
                ChannelId = program.GuideChannelId.ToString(), Id = program.GuideProgramId.ToString(),
                Overview = Proxies.GuideService.GetProgramById(program.GuideProgramId).Description, 
                StartDate = program.StartTimeUtc, 
                EndDate = program.StopTimeUtc,
                //Genres = program.Category,
                //OriginalAirDate = ,
                Name = program.Title,
                //OfficialRating = ,
                //CommunityRating = ,
                EpisodeTitle = program.SubTitle,
                //Audio = ,
                IsHD = (program.Flags == GuideProgramFlags.HighDefinition), IsRepeat = program.IsRepeat, 
                IsSeries = GeneralHelpers.ContainsWord(program.Category, "series", StringComparison.OrdinalIgnoreCase),
                //ImageUrl = ,
                HasImage = false, //TODO: Refactor
                IsNews = GeneralHelpers.ContainsWord(program.Category, "news", StringComparison.OrdinalIgnoreCase), 
                //IsMovie = ,
                IsKids = GeneralHelpers.ContainsWord(program.Category, "animation", StringComparison.OrdinalIgnoreCase),
                IsSports = GeneralHelpers.ContainsWord(program.Category, "sport", StringComparison.OrdinalIgnoreCase)
                            || GeneralHelpers.ContainsWord(program.Category, "motor sports", StringComparison.OrdinalIgnoreCase)
                            || GeneralHelpers.ContainsWord(program.Category, "football", StringComparison.OrdinalIgnoreCase)
                            || GeneralHelpers.ContainsWord(program.Category, "cricket", StringComparison.OrdinalIgnoreCase), 
                IsPremiere = program.IsPremiere,
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
                Proxies.SchedulerService.DeleteSchedule(Guid.Parse(timerId));
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
                Proxies.SchedulerService.DeleteSchedule(Guid.Parse(timerId));
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
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new recording
        /// </summary>
        /// <param name="info">The TimerInfo</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Debug("[ArgusTV] Start CreateTimer Async");
            await EnsureConnectionAsync(cancellationToken);

            var schedule = new Schedule()
            {
                ChannelType = ChannelType.Television,
                IsOneTime = true,
                Name = info.Name,
                PostRecordMinutes = info.PostPaddingSeconds / 60,
                PreRecordMinutes = info.PrePaddingSeconds / 60,
            };

            Proxies.SchedulerService.SaveSchedule(schedule);

            //NextPvr
            //var timerSettings = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            //timerSettings.allChannels = false;
            //timerSettings.ChannelOID = int.Parse(info.ChannelId, _usCulture);

            //if (!string.IsNullOrEmpty(info.ProgramId))
            //{
            //    timerSettings.epgeventOID = int.Parse(info.ProgramId, _usCulture);
            //}

            //timerSettings.post_padding_min = info.PostPaddingSeconds / 60;
            //timerSettings.pre_padding_min = info.PrePaddingSeconds / 60;
        }

        /// <summary>
        /// Create a recurrent recording
        /// </summary>
        /// <param name="info">The recurrend program info</param>
        /// <param name="cancellationToken">The CancelationToken</param>
        /// <returns></returns>
        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Debug("[ArgusTV] Start CreateTimer Async");
            await EnsureConnectionAsync(cancellationToken);

            var schedule = new Schedule()
            {
                ChannelType = ChannelType.Television,
                IsOneTime = false,
                Name = info.Name,
                PostRecordMinutes = info.PostPaddingSeconds / 60,
                PreRecordMinutes = info.PrePaddingSeconds / 60,
            };

            Proxies.SchedulerService.SaveSchedule(schedule);

        }

        /// <summary>
        /// Update a single Timer
        /// </summary>
        /// <param name="info">The program info</param>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
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
            await EnsureConnectionAsync(cancellationToken);
            List<RecordingInfo> timerInfos = new List<RecordingInfo>();

            //var test = _controlServiceProxy.get

            return timerInfos;
        }

        /// <summary>
        /// Get the pending Recordings.
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken);
            List<TimerInfo> timerInfos = new List<TimerInfo>();

            List<UpcomingRecording> upcomingRecordings = Proxies.ControlService.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings);

            foreach (UpcomingRecording upcomingRecording in upcomingRecordings)
            {
                timerInfos.Add(new TimerInfo()
                {
                    Name = upcomingRecording.Title,
                });
            }

            return timerInfos;

            /*
             * NEXT PVR
             * 
             var info = new TimerInfo();

            var recurr = i.recurr;
            if (recurr != null)
            {
                if (recurr.OID != 0)
                {
                    info.SeriesTimerId = recurr.OID.ToString(_usCulture);
                }

                info.Name = recurr.RecurringName;
            }

            var schd = i.schd;

            if (schd != null)
            {
                info.ChannelId = schd.ChannelOid.ToString(_usCulture);
                info.Id = schd.OID.ToString(_usCulture);
                info.Status = ParseStatus(schd.Status);
                info.StartDate = DateTime.Parse(schd.StartTime);
                info.EndDate = DateTime.Parse(schd.EndTime);

                info.PrePaddingSeconds = int.Parse(schd.PrePadding, _usCulture) * 60;
                info.PostPaddingSeconds = int.Parse(schd.PostPadding, _usCulture) * 60;

                info.Name = schd.Name;
            }

            var epg = i.epgEvent;

            if (epg != null)
            {
                //info.Audio = ListingsResponse.ParseAudio(epg.Audio);
                info.ProgramId = epg.OID.ToString(_usCulture);
                //info.OfficialRating = epg.Rating;
                //info.EpisodeTitle = epg.Subtitle;
                info.Name = epg.Title;
                info.Overview = epg.Desc;
                //info.Genres = epg.Genres;
                //info.IsRepeat = !epg.FirstRun;
                //info.CommunityRating = ListingsResponse.ParseCommunityRating(epg.StarRating);
                //info.IsHD = string.Equals(epg.Quality, "hdtv", StringComparison.OrdinalIgnoreCase);
            }

             */
        }

        public async Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            await EnsureConnectionAsync(cancellationToken);
            throw new NotImplementedException();
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
            get { return "ArgusTV"; }
        }

        public string HomePageUrl
        {
            get { return "http://www.argus-tv.com/"; }
        }
        public event EventHandler DataSourceChanged;
        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;
    }
}
