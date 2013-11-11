using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Plugins.NextPvr.Helpers;

namespace MediaBrowser.Plugins.NextPvr
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService
    {
        private readonly IHttpClient _httpClient;

        private string Sid { get; set; }
        private string Salt { get; set; }

        private string WebserviceUrl { get; set; }
        private int Port { get; set; }
        private string Pin { get; set; }

        private bool IsConnected { get; set; }

        public LiveTvService(IHttpClient httpClient)
        {
            _httpClient = httpClient;

            WebserviceUrl = Plugin.Instance.Configuration.WebServiceUrl;
            Port = Plugin.Instance.Configuration.Port;
            Pin = Plugin.Instance.Configuration.Pin;
            
            if(string.IsNullOrEmpty(WebserviceUrl) && string.IsNullOrEmpty(Port.ToString(CultureInfo.InvariantCulture)) && string.IsNullOrEmpty(Pin))
            {
                InitiateSession();
                Login();
            }
        }

        /// <summary>
        /// Initiate the nextPvr session
        /// </summary>
        internal void InitiateSession()
        {
            string html;

            HttpRequestOptions options = new HttpRequestOptions()
                {
                    // This moment only device name xbmc is available
                    Url = string.Format("{0}:{1}/service?method=session.initiate&ver=1.0&device=xbmc", WebserviceUrl,Port)
                };

            using (var reader = new StreamReader( _httpClient.Get(options).Result))
            {
                html = reader.ReadToEnd();
            }


            if (XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml.ToLower() == "ok")
            {
                Salt = XmlHelper.GetSingleNode(html, "//rsp/salt").InnerXml;
                Sid = XmlHelper.GetSingleNode(html, "//rsp/sid").InnerXml;
            }
        }

        internal void Login()
        {
            string html;

            var md5 = EncryptionHelper.GetMd5Hash(Pin);
            StringBuilder strb = new StringBuilder();

            strb.Append(":");
            strb.Append(md5);
            strb.Append(":");
            strb.Append(Salt);

            var md5Result = EncryptionHelper.GetMd5Hash(strb.ToString());

            HttpRequestOptions options = new HttpRequestOptions()
            {
                // This moment only device name xbmc is available
                Url = string.Format("{0}:{1}/service?method=session.login&&sid={2}&md5={3}", WebserviceUrl, Port, Sid,md5Result)            
            };

            using (var reader = new StreamReader(_httpClient.Get(options).Result))
            {
                html = reader.ReadToEnd();
            }

            if (XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml.ToLower() == "ok")
          {
              IsConnected = true;
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

            if (IsConnected)
            {
                string html;
               

                HttpRequestOptions options = new HttpRequestOptions()
                    {
                        CancellationToken = cancellationToken,
                        Url = string.Format("{0}:{1}/service?method=channel.list&sid={2}", WebserviceUrl, Port, Sid)
                    };

                using (var reader = new StreamReader(_httpClient.Get(options).Result))
                {
                    html = reader.ReadToEnd();
                }

                if (XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml.ToLower() == "ok")
                {
                    channels.AddRange(from XmlNode node in XmlHelper.GetMultipleNodes(html, "//rsp/channels/channel")
                                      select new ChannelInfo()
                                          {
                                              Id = XmlHelper.GetSingleNode(node.OuterXml, "//id").InnerXml,
                                              Name = XmlHelper.GetSingleNode(node.OuterXml, "//name").InnerXml,
                                              ChannelType = ChannelHelper.GetChannelType(XmlHelper.GetSingleNode(node.OuterXml, "//type").InnerXml),
                                              ServiceName = Name
                                          });
                }
            }

            return await Task.FromResult<IEnumerable<ChannelInfo>>(channels);
        }

        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            List<RecordingInfo> recordings = new List<RecordingInfo>();

            if (IsConnected)
            {
                string html;

                HttpRequestOptions options = new HttpRequestOptions()
                {
                    CancellationToken = cancellationToken,
                    Url = string.Format("{0}:{1}/service?method=recording.list&sid={2}", WebserviceUrl, Port, Sid)
                };

                using (var reader = new StreamReader(_httpClient.Get(options).Result))
                {
                    html = reader.ReadToEnd();
                }

                if (XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml.ToLower() == "ok")
                {
                    recordings.AddRange(
                        from XmlNode node in XmlHelper.GetMultipleNodes(html, "//rsp/recordings/recording")
                        let startDate = DateTime.Parse(XmlHelper.GetSingleNode(node.OuterXml, "//start_time").InnerXml)
                        select new RecordingInfo()
                            {
                                Id = XmlHelper.GetSingleNode(node.OuterXml, "//id").InnerXml,
                                Name = XmlHelper.GetSingleNode(node.OuterXml, "//name").InnerXml,
                                Description = XmlHelper.GetSingleNode(node.OuterXml, "//desc").InnerXml,
                                StartDate = startDate,
                                Status = XmlHelper.GetSingleNode(node.OuterXml, "//status").InnerXml,
                                Quality = XmlHelper.GetSingleNode(node.OuterXml, "//quality").InnerXml,
                                ChannelName = XmlHelper.GetSingleNode(node.OuterXml, "//channel").InnerXml,
                                ChannelId = XmlHelper.GetSingleNode(node.OuterXml, "//channel_id").InnerXml,
                                Recurring = bool.Parse(XmlHelper.GetSingleNode(node.OuterXml, "//recurring").InnerXml),
                                RecurrringStartDate =
                                    DateTime.Parse(XmlHelper.GetSingleNode(node.OuterXml, "//recurring_start").InnerXml),
                                RecurringEndDate =
                                    DateTime.Parse(XmlHelper.GetSingleNode(node.OuterXml, "//recurring_end").InnerXml),
                                RecurringParent = XmlHelper.GetSingleNode(node.OuterXml, "//recurring_parent").InnerXml,
                                DayMask = XmlHelper.GetSingleNode(node.OuterXml, "//daymask").InnerXml.Split(',').ToList(),
                                EndDate =
                                    startDate.AddSeconds(
                                        (double.Parse(
                                            XmlHelper.GetSingleNode(node.OuterXml, "//duration_seconds").InnerXml)))
                            });
                }
            }

            return await Task.FromResult<IEnumerable<RecordingInfo>>(recordings);
        }

        public Task<IEnumerable<EpgFullInfo>> GetEpgAsync(string channelId, CancellationToken cancellationToken)
        {
            List<EpgFullInfo> epgFullInfos = new List<EpgFullInfo>();
            List<EpgInfo> epgInfos = new List<EpgInfo>();

            if (IsConnected)
            {
                string html;

                HttpRequestOptions options = new HttpRequestOptions()
                {
                    CancellationToken = cancellationToken,
                    Url = string.Format("{0}:{1}/service?method=channel.listings&channel_id={2}&sid={3}", WebserviceUrl, Port, channelId, Sid)
                };

                using (var reader = new StreamReader(_httpClient.Get(options).Result))
                {
                    html = reader.ReadToEnd();
                }

                if (XmlHelper.GetSingleNode(html, "//rsp/@stat").InnerXml.ToLower() == "ok")
                {
                    epgInfos.AddRange(
                        from XmlNode node in XmlHelper.GetMultipleNodes(html, "//rsp/listings/l")
                        let startDate = XmlHelper.GetSingleNode(node.OuterXml, "//start").InnerXml
                        let endDate = XmlHelper.GetSingleNode(node.OuterXml, "//end").InnerXml
                        select new EpgInfo()
                        {
                            Id = XmlHelper.GetSingleNode(node.OuterXml, "//id").InnerXml,
                            //Name = XmlHelper.GetSingleNode(node.OuterXml, "//name").InnerXml,
                            Description = XmlHelper.GetSingleNode(node.OuterXml, "//description").InnerXml,
                            StartDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(double.Parse(startDate)) / 1000d).ToLocalTime(),
                            EndDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(double.Parse(endDate)) / 1000d).ToLocalTime(),
                            Genre = XmlHelper.GetSingleNode(node.OuterXml, "//genre").InnerXml,
                        });

                    epgFullInfos.Add(new EpgFullInfo(){ChannelId = channelId, EpgInfos = epgInfos});
                }
            }

            return Task.FromResult<IEnumerable<EpgFullInfo>>(epgFullInfos);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Next Pvr"; }
        }
    }
}
