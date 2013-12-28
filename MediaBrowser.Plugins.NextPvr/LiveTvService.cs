using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
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

namespace MediaBrowser.Plugins.NextPvr
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        private string Sid { get; set; }

        private string WebserviceUrl { get; set; }
        private string Pin { get; set; }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public LiveTvService(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            var config = Plugin.Instance.Configuration;

            if (string.IsNullOrEmpty(config.WebServiceUrl))
            {
                throw new InvalidOperationException("NextPvr web service url must be configured.");
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
            string html;

            WebserviceUrl = Plugin.Instance.Configuration.WebServiceUrl;
            Pin = Plugin.Instance.Configuration.Pin;

            var options = new HttpRequestOptions
            {
                // This moment only device name xbmc is available
                Url = string.Format("{0}/service?method=session.initiate&ver=1.0&device=xbmc", WebserviceUrl),

                CancellationToken = cancellationToken
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    html = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            if (XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml.ToLower() == "ok")
            {
                var salt = XmlHelper.GetSingleNode(html, "//rsp/salt").InnerXml;
                var sid = XmlHelper.GetSingleNode(html, "//rsp/sid").InnerXml;

                var loggedIn = await Login(sid, salt, cancellationToken).ConfigureAwait(false);

                if (loggedIn)
                {
                    Sid = sid;
                }
            }
        }

        private async Task<bool> Login(string sid, string salt, CancellationToken cancellationToken)
        {
            string html;

            var md5 = EncryptionHelper.GetMd5Hash(Pin);
            var strb = new StringBuilder();

            strb.Append(":");
            strb.Append(md5);
            strb.Append(":");
            strb.Append(salt);

            var md5Result = EncryptionHelper.GetMd5Hash(strb.ToString());

            var options = new HttpRequestOptions()
                {
                    // This moment only device name xbmc is available
                    Url =
                        string.Format("{0}/service?method=session.login&&sid={1}&md5={2}", WebserviceUrl, sid, md5Result),

                    CancellationToken = cancellationToken
                };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    html = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            return XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml.ToLower() == "ok";
        }

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/GuideService/Channels", Plugin.Instance.Configuration.WebServiceUrl)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new ChannelResponse(Plugin.Instance.Configuration.WebServiceUrl).GetChannels(stream, _jsonSerializer).ToList();
            }
        }

        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ManageService/Get/SortedFilteredList", baseUrl)
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
                return new RecordingResponse(baseUrl).GetRecordings(stream, _jsonSerializer);
            }
        }

        private async Task CancelRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            string html;

            var options = new HttpRequestOptions()
                {
                    CancellationToken = cancellationToken,
                    Url =
                        string.Format("{0}/service?method=recording.delete&recording_id={1}&sid={2}", WebserviceUrl,
                                      recordingId, Sid)
                };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    html = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            if (!string.Equals(XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException("Operation failed");
            }
        }

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            return CancelRecordingAsync(recordingId, cancellationToken);
        }

        public async Task ScheduleRecordingAsync(string name, string channelId, DateTime startTime, TimeSpan duration, CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            string html;

            var options = new HttpRequestOptions()
                {
                    CancellationToken = cancellationToken,
                    Url =
                        string.Format("{0}/service?method=recording.save&name={1}&channel={2}&time_t={3}&duration={4}&sid={5}", WebserviceUrl,
                                      name, channelId, startTime, duration, Sid)
                };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    html = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            if (!string.Equals(XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException("Operation failed");
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

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/GuideService/Listing?stime={1}&etime={2}&channelId={3}",
                Plugin.Instance.Configuration.WebServiceUrl,
                ApiHelper.GetCurrentUnixTimestampSeconds(DateTime.UtcNow.AddYears(-1)).ToString(_usCulture),
                ApiHelper.GetCurrentUnixTimestampSeconds(DateTime.UtcNow.AddYears(1)).ToString(_usCulture),
                channelId)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new ListingsResponse(Plugin.Instance.Configuration.WebServiceUrl).GetPrograms(stream, _jsonSerializer, channelId).ToList();
            }
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            return DeleteRecordingAsync(timerId, cancellationToken);
        }

        public async Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Record", baseUrl)
            };

            var timerSettings = await GetDefaultScheduleSettings(cancellationToken).ConfigureAwait(false);

            timerSettings.allChannels = false;
            timerSettings.ChannelOID = int.Parse(info.ChannelId, _usCulture);

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                timerSettings.epgeventOID = int.Parse(info.ProgramId, _usCulture);
            }
            else
            {
                // TODO: format the dates
                //timerSettings.startDate = info.StartDate;
                //timerSettings.endDate = info.EndDate;
            }

            timerSettings.post_padding_min = info.PostPaddingSeconds / 60;
            timerSettings.pre_padding_min = info.PrePaddingSeconds / 60;

            var postContent = _jsonSerializer.SerializeToString(timerSettings);

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            var response = await _httpClient.Post(options).ConfigureAwait((false));

            // TODO: throw LiveTvConflictException if this fails due to conflict
        }

        public async Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ManageService/Get/SortedFilteredList", baseUrl)
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
                return new RecordingResponse(baseUrl).GetTimers(stream, _jsonSerializer);
            }
        }

        public async Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ManageService/Get/SortedFilteredList", baseUrl)
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
                return new RecordingResponse(baseUrl).GetSeriesTimers(stream, _jsonSerializer);
            }
        }

        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            var baseUrl = Plugin.Instance.Configuration.WebServiceUrl;

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Record", baseUrl)
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

            options.RequestContent = postContent;
            options.RequestContentType = "application/json";

            var response = await _httpClient.Post(options).ConfigureAwait((false));
        }

        public Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Get/SchedSettingsObj",
                Plugin.Instance.Configuration.WebServiceUrl)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new TimerDefaultsResponse().GetDefaultTimerInfo(stream, _jsonSerializer);
            }
        }

        private async Task<ScheduleSettings> GetDefaultScheduleSettings(CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = string.Format("{0}/public/ScheduleService/Get/SchedSettingsObj",
                Plugin.Instance.Configuration.WebServiceUrl)
            };

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                return new TimerDefaultsResponse().GetScheduleSettings(stream, _jsonSerializer);
            }
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


        public Task<StreamResponseInfo> GetChannelStream(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<StreamResponseInfo> GetRecordingStream(string recordingId, CancellationToken cancellationToken)
        {
            var recordings = await GetRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var recording = recordings.First(i => string.Equals(i.Id, recordingId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(recording.Path) && File.Exists(recording.Path))
            {
                return new StreamResponseInfo
                {
                    MimeType = MimeTypes.GetMimeType(recording.Path),
                    Stream = File.OpenRead(recording.Path)
                };
            }

            if (!string.IsNullOrEmpty(recording.Url))
            {
                var response = await _httpClient.GetResponse(new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = recording.Url

                }).ConfigureAwait(false);

                return new StreamResponseInfo
                {
                    MimeType = response.ContentType,
                    Stream = response.Content
                };
            }

            throw new ResourceNotFoundException(string.Format("No stream exists for recording {0}", recording));
        }
    }
}
