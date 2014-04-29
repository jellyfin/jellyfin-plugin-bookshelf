using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.NextPvr.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Plugins.NextPvr.Responses;

namespace MediaBrowser.Plugins.NextPvr
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly ILogger _logger;
        private int _liveStreams = 0;
        private readonly Dictionary<int, int> _heartBeat = new Dictionary<int, int>();
        
        private string Sid { get; set; }

        public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer,ILogger logger)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        /// <summary>
        /// Ensure that we are connected to the NextPvr server
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            var config = Plugin.Instance.Configuration;

            if (string.IsNullOrEmpty(config.WebServiceUrl))
            {
                _logger.Error("[NextPvr] Web service url must be configured.");
                throw new InvalidOperationException("NextPvr web service url must be configured.");
            }

            if (string.IsNullOrEmpty(config.Pin))
            {
                _logger.Error("[NextPvr] Pin must be configured.");
                throw new InvalidOperationException("NextPvr pin must be configured.");
            }

            if (string.IsNullOrEmpty(Sid))
            {
                await InitiateSession(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initiate the nextPvr session
        /// </summary>
        private async Task InitiateSession(CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start InitiateSession");
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/Util/NPVR/Client/Instantiate", baseUrl)
            };
            
            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var clientKeys = new InstantiateResponse().GetClientKeys(stream, _jsonSerializer, _logger);
                
                var sid = clientKeys.sid;
                var salt = clientKeys.salt;
                _logger.Info(sid);
                
                var loggedIn = await Login(sid,salt, cancellationToken).ConfigureAwait(false);

                if (loggedIn)
                {
                    _logger.Info("[NextPvr] Session initiated. LoggedIn.");
                    Sid = sid;
                }
            }
        }

        /// <summary>
        /// Initialize the NextPvr session
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="salt"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> Login(string sid,string salt, CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start Login procedure");
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;
            var pin = Plugin.Instance.Configuration.Pin;
            _logger.Info(pin);

            var strb = new StringBuilder();
            var md5Result = EncryptionHelper.GetMd5Hash(strb.Append(":").Append(EncryptionHelper.GetMd5Hash(pin)).Append(":").Append(salt).ToString());

            var options = new HttpRequestOptions()
                {
                    Url = string.Format("{0}/public/Util/NPVR/Client/Initialize/{1}?sid={2}" , baseUrl,md5Result,sid),
                    CancellationToken = cancellationToken
                };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new InitializeResponse().LoggedIn(stream, _jsonSerializer,_logger);
            }
        }

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start GetChannels Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/GuideService/Channels?sid={1}", baseUrl,Sid)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new ChannelResponse(Plugin.Instance.Configuration.WebServiceUrl).GetChannels(stream, _jsonSerializer,_logger).ToList();
            }
        }

        /// <summary>
        /// Gets the Recordings async
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}</returns>
        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start GetRecordings Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ManageService/Get/SortedFilteredList?sid={1}", baseUrl,Sid)
            };

            var filterOptions = new
            {
                resultLimit = -1,
                datetimeSortSeq = 0,
                channelSortSeq = 0,
                titleSortSeq = 0,
                statusSortSeq = 0,
                datetimeDecending = false,
                channelDecending = false,
                titleDecending = false,
                statusDecending = false,
                All = false,
                None = false,
                Pending = false,
                InProgress = true,
                Completed = true,
                Failed = true,
                Conflict = false,
                Recurring = false,
                Deleted = false,
                FilterByName = false,
                NameFilter = (string)null,
                NameFilterCaseSensative = false
            };

            var postContent = _jsonSerializer.SerializeToString(filterOptions);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            var response = await _httpClient.Post(options).ConfigureAwait(false);

            using (var stream = response.Content)
            {
                return new RecordingResponse(baseUrl).GetRecordings(stream, _jsonSerializer,_logger);
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
            _logger.Debug("[NextPvr] Start Delete Recording Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Delete/{1}?sid={2}", baseUrl, recordingId, Sid)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                bool? error = new CancelDeleteRecordingResponse().RecordingError(stream, _jsonSerializer,_logger);

                if (error == null || error == true)
                {
                    _logger.Error("[NextPvr] Failed to cancel the recording");
                    throw new ApplicationException("Failed to cancel the recording");
                }
                _logger.Debug("[NextPvr] Deleted Recording: {0}", recordingId);
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Next Pvr"; }
        }

        /// <summary>
        /// Cancel pending scheduled Recording 
        /// </summary>
        /// <param name="timerId">The timerId</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns></returns>
        public async Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start Cancel Recording Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/CancelRec/{1}?sid={2}", baseUrl, timerId, Sid)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                bool? error = new CancelDeleteRecordingResponse().RecordingError(stream, _jsonSerializer,_logger);

                if (error == null || error == true)
                {
                    _logger.Error("[NextPvr] Failed to cancel the recording: {0}", timerId);
                    throw new ApplicationException("Failed to cancel the recording");
                }
                _logger.Debug("[NextPvr] Cancelled Recording: {0}", timerId);
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
            _logger.Debug("[NextPvr] Start CreateTimer Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Record?sid={1}", baseUrl,Sid)
            };

            var timerSettings = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            timerSettings.allChannels = false;
            timerSettings.ChannelOID = int.Parse(info.ChannelId, _usCulture);

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                timerSettings.epgeventOID = int.Parse(info.ProgramId, _usCulture);
            }

            timerSettings.post_padding_min = info.PostPaddingSeconds / 60;
            timerSettings.pre_padding_min = info.PrePaddingSeconds / 60;

            var postContent = _jsonSerializer.SerializeToString(timerSettings);
            _logger.Debug("[NextPvr] TimerSettings for CreateTimer: {0}", postContent);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            try
            {
                await _httpClient.Post(options).ConfigureAwait((false));
            }
            catch (HttpException ex)
            {
                _logger.Debug("[NextPvr] " + ex.Message);
                throw new LiveTvConflictException();
            }
        }

        /// <summary>
        /// Get the pending Recordings.
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start GetTimer Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ManageService/Get/SortedFilteredList?sid={1}", baseUrl,Sid)
            };

            var filterOptions = new
            {
                resultLimit = -1,
                datetimeSortSeq = 0,
                channelSortSeq = 0,
                titleSortSeq = 0,
                statusSortSeq = 0,
                datetimeDecending = false,
                channelDecending = false,
                titleDecending = false,
                statusDecending = false,
                All = false,
                None = false,
                Pending = true,
                InProgress = false,
                Completed = false,
                Failed = false,
                Conflict = true,
                Recurring = false,
                Deleted = false,
                FilterByName = false,
                NameFilter = (string)null,
                NameFilterCaseSensative = false
            };

            var postContent = _jsonSerializer.SerializeToString(filterOptions);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            var response = await _httpClient.Post(options).ConfigureAwait(false);

            using (var stream = response.Content)
            {
                return new RecordingResponse(baseUrl).GetTimers(stream, _jsonSerializer,_logger);
            }
        }

        /// <summary>
        /// Get the recurrent recordings
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        public async Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start GetSeriesTimer Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ManageService/Get/SortedFilteredList?sid={1}", baseUrl,Sid)
            };

            var filterOptions = new
            {
                resultLimit = -1,
                All = false,
                None = false,
                Pending = false,
                InProgress = false,
                Completed = false,
                Failed = false,
                Conflict = false,
                Recurring = true,
                Deleted = false
            };

            var postContent = _jsonSerializer.SerializeToString(filterOptions);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            var response = await _httpClient.Post(options).ConfigureAwait(false);

            using (var stream = response.Content)
            {
                return new RecordingResponse(baseUrl).GetSeriesTimers(stream, _jsonSerializer,_logger);
            }
        }

        /// <summary>
        /// Create a recurrent recording
        /// </summary>
        /// <param name="info">The recurrend program info</param>
        /// <param name="cancellationToken">The CancelationToken</param>
        /// <returns></returns>
        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start CreateSeriesTimer Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Record?sid={1}", baseUrl,Sid)
            };

            var timerSettings = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            timerSettings.allChannels = info.RecordAnyChannel;
            timerSettings.onlyNew = info.RecordNewOnly;
            timerSettings.recurringName = info.Name;
            timerSettings.recordAnyTimeslot = info.RecordAnyTime;

            timerSettings.recordAnyDay = info.Days.Count == 7;
            timerSettings.daySunday = info.Days.Contains(DayOfWeek.Sunday);
            timerSettings.dayMonday = info.Days.Contains(DayOfWeek.Monday);
            timerSettings.dayTuesday = info.Days.Contains(DayOfWeek.Tuesday);
            timerSettings.dayWednesday = info.Days.Contains(DayOfWeek.Wednesday);
            timerSettings.dayThursday = info.Days.Contains(DayOfWeek.Thursday);
            timerSettings.dayFriday = info.Days.Contains(DayOfWeek.Friday);
            timerSettings.daySaturday = info.Days.Contains(DayOfWeek.Saturday);

            if (!info.RecordAnyChannel)
            {
                timerSettings.ChannelOID = int.Parse(info.ChannelId, _usCulture);
            }

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                timerSettings.epgeventOID = int.Parse(info.ProgramId, _usCulture);
            }

            timerSettings.post_padding_min = info.PostPaddingSeconds / 60;
            timerSettings.pre_padding_min = info.PrePaddingSeconds / 60;

            var postContent = _jsonSerializer.SerializeToString(timerSettings);
            _logger.Debug("[NextPvr] TimerSettings for CreateSeriesTimer: {0}", postContent);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            try
            {
                await _httpClient.Post(options).ConfigureAwait((false));
            }
            catch (HttpException ex)
            {
                _logger.Debug("[NextPvr] " + ex.Message);
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
            _logger.Debug("[NextPvr] Start UpdateSeriesTimer Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/UpdateRecurr?sid={1}", baseUrl, Sid)
            };

            var timerSettings = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            timerSettings.recurrOID = int.Parse(info.Id);
            timerSettings.post_padding_min = info.PostPaddingSeconds / 60;
            timerSettings.pre_padding_min = info.PrePaddingSeconds / 60;
            timerSettings.recurringName = info.Name;
            timerSettings.keep_all_days = true;
            timerSettings.days_to_keep = 0;
            timerSettings.extend_end_time_min = 0;

            var postContent = _jsonSerializer.SerializeToString(timerSettings);
            _logger.Debug("[NextPvr] TimerSettings for UpdateSeriesTimer: {0}", postContent);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            try
            {
                await _httpClient.Post(options).ConfigureAwait((false));
            }
            catch (HttpException ex)
            {
                _logger.Debug("[NextPvr] " + ex.Message);
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
            _logger.Debug("[NextPvr] Start UpdateTimer Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/UpdateRec?sid={1}", baseUrl, Sid)
            };

            var timerSettings = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            timerSettings.scheduleOID = int.Parse(info.Id);
            timerSettings.post_padding_min = info.PostPaddingSeconds /60;
            timerSettings.pre_padding_min = info.PrePaddingSeconds / 60;

            var postContent = _jsonSerializer.SerializeToString(timerSettings);
            _logger.Debug("[NextPvr] TimerSettings for UpdateTimer: {0}", postContent);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            try
            {
                await _httpClient.Post(options).ConfigureAwait((false));
            }
            catch (HttpException ex)
            {
                _logger.Debug("[NextPvr] " + ex.Message);
                throw new LiveTvConflictException();
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
            _logger.Debug("[NextPvr] Start Cancel SeriesRecording Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/CancelRecurr/{1}?sid={2}", baseUrl, timerId, Sid)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                bool? error = new CancelDeleteRecordingResponse().RecordingError(stream, _jsonSerializer,_logger);

                if (error == null || error == true)
                {
                    _logger.Error("[NextPvr] Failed to cancel the recording: {0}", timerId);
                    throw new ApplicationException("Failed to cancel the recording");
                }
                _logger.Debug("[NextPvr] Cancelled Recording: {0}", timerId);
            }
        }

        /// <summary>
        /// Get the DefaultScheduleSettings
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        private async Task<ScheduleSettings> GetDefaultScheduleSettings(CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start GetDefaultScheduleSettings");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Get/SchedSettingsObj?sid={1}",baseUrl,Sid)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new TimerDefaultsResponse().GetScheduleSettings(stream, _jsonSerializer); //Debug information??
            }
        }

        public async Task<LiveStreamInfo> GetChannelStream(string channelOid, CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start ChannelStream");
            var config = Plugin.Instance.Configuration;
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;
            _liveStreams++;
            if (config.TimeShift)
            {
                var options = new HttpRequestOptions()
                {
                    CancellationToken = cancellationToken,
                    Url = string.Format("{0}/public/VLCService/Dump/StreamByChannel/OID/{1}", baseUrl, channelOid)
                };

                using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
                {
                    var vlcObj = new VLCResponse().GetVLCResponse(stream, _jsonSerializer,_logger);
                    _logger.Debug(vlcObj.StreamLocation);

                    while (!File.Exists(vlcObj.StreamLocation))
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                    await Task.Delay(20000).ConfigureAwait(false);
                    _logger.Debug("[NextPvr] Finishing wait");
                    _heartBeat.Add(_liveStreams, vlcObj.ProcessId);
                    return new LiveStreamInfo
                    {
                        Id = _liveStreams.ToString(CultureInfo.InvariantCulture),
                        Path = vlcObj.StreamLocation
                    };
                }
            }
            else
            {
                string streamUrl = string.Format("{0}/live?channeloid={1}&client=MB3.{2}", baseUrl, channelOid,_liveStreams.ToString());
                _logger.Debug("[NextPvr] Streaming " + streamUrl);
                return new LiveStreamInfo
                {
                    Id = _liveStreams.ToString(CultureInfo.InvariantCulture),
                    Url = streamUrl
                };               
            }
            throw new ResourceNotFoundException(string.Format("Could not stream channel {0}", channelOid));            
        }

        public async Task<LiveStreamInfo> GetRecordingStream(string recordingId, CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start GetRecordingStream");
            var recordings = await GetRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var recording = recordings.First(i => string.Equals(i.Id, recordingId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(recording.Path) && File.Exists(recording.Path))
            {
                _logger.Debug("[NextPvr] RecordingPath: {0}", recording.Path);
                return new LiveStreamInfo
                {
                    Path = recording.Path
                };
            }

            if (!string.IsNullOrEmpty(recording.Url))
            {
                _logger.Debug("[NextPvr] RecordingUrl: {0}", recording.Url);
                return new LiveStreamInfo
                {
                    Path = recording.Url
                };
            }

            _logger.Error("[NextPvr] No stream exists for recording {0}", recording);
            throw new ResourceNotFoundException(string.Format("No stream exists for recording {0}", recording));
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Closing " + id);
            var config = Plugin.Instance.Configuration;
            if (config.TimeShift)
            {
                var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

                var options = new HttpRequestOptions()
                {
                    CancellationToken = cancellationToken,
                    Url = string.Format("{0}/public/VLCService/KillVLC/{1}", baseUrl, _heartBeat[int.Parse(id)])
                };

                using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
                {
                    var ret = new VLCResponse().GetVLCReturn(stream, _jsonSerializer,_logger);
                    _heartBeat.Remove(int.Parse(id));
                }
            }
        }

        public async Task CopyFilesAsync(StreamReader Source, StreamWriter Destination)
        {
            _logger.Debug("[NextPvr] Start CopyFiles Async");
            char[] buffer = new char[0x1000];
            int numRead;
            while ((numRead = await Source.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await Destination.WriteAsync(buffer, 0, numRead);
            }
        }

        public async Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            _logger.Debug("[NextPvr] Start GetNewTimerDefault Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Get/SchedSettingsObj?sid{1}",baseUrl,Sid)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new TimerDefaultsResponse().GetDefaultTimerInfo(stream, _jsonSerializer,_logger);
            }
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            _logger.Debug("[NextPvr] Start GetPrograms Async");
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/GuideService/Listing?sid={1}&stime={2}&etime={3}&channelId={4}",
                baseUrl,Sid,
                ApiHelper.GetCurrentUnixTimestampSeconds(startDateUtc).ToString(_usCulture),
                ApiHelper.GetCurrentUnixTimestampSeconds(endDateUtc).ToString(_usCulture),
                channelId)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new ListingsResponse(baseUrl).GetPrograms(stream, _jsonSerializer, channelId,_logger).ToList();
            }
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public event EventHandler DataSourceChanged;

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            //Version Check
            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/Util/NPVR/VersionCheck?sid={1}",baseUrl, Sid)
            };

            bool upgradeAvailable;
            string serverVersion;

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var versionCheckResponse = new VersionCheckResponse(stream, _jsonSerializer);

                upgradeAvailable = versionCheckResponse.UpdateAvailable();
                serverVersion = versionCheckResponse.ServerVersion();
            }


            //Tuner information
            var optionsTuner = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/Util/Tuner/Stat?sid={1}", baseUrl, Sid)
            };

            List<LiveTvTunerInfo> tvTunerInfos;
            using (var stream = await _httpClient.Get(optionsTuner).ConfigureAwait(false))
            {
                var tuners = new TunerResponse(stream, _jsonSerializer);

                tvTunerInfos = tuners.LiveTvTunerInfos();
            }
            
            return new LiveTvServiceStatusInfo
            {
                 HasUpdateAvailable = upgradeAvailable,
                 Version = serverVersion,
                 Tuners = tvTunerInfos
            };
        }

        public string HomePageUrl
        {
            get { return "http://www.nextpvr.com/"; }
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<StreamResponseInfo> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ChannelInfo
            throw new NotImplementedException();
        }

        public Task<StreamResponseInfo> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to ProgramInfo
            throw new NotImplementedException();
        }

        public Task<StreamResponseInfo> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            // Leave as is. This is handled by supplying image url to RecordingInfo
            throw new NotImplementedException();
        }
    }
}
