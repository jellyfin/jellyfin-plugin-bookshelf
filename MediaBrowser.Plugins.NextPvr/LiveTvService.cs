using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

            //TODO: Remove the following. Read from configuration
            WebserviceUrl = "http://192.168.1.170";
            Port = 8866;
            Pin = "0000";

            InitiateSession();
            Login();
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
                                              //ChannelType = 
                                              Name = XmlHelper.GetSingleNode(node.OuterXml, "//name").InnerXml,
                                              ServiceName = Name
                                          });
                }
            }

            return await Task.FromResult<IEnumerable<ChannelInfo>>(channels);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Next Pvr"; }
        }


        #region External Stuff
       

        
        #endregion
    }
}
