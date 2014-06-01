#if WINDOWS
using System.Web;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.Vimeo.VimeoAPI.Common
{
    public class AdvancedAPI
    {
        public const string RequestTokenUrl = "http://vimeo.com/oauth/request_token?";
        public const string RequestAuthorizeUrl = "http://vimeo.com/oauth/authorize?";
        public const string RequestAccessUrl = "http://vimeo.com/oauth/access_token";
        public const string StandardAdvancedApiUrl = "http://vimeo.com/api/rest/v2";
        public const string BaseVimeoUrl = "http://vimeo.com/api/v2/";

        string consumerKey;
        string consumerSecret;
        string requestPermission;

        private ILogger _logger;

        public AdvancedAPI(ILogger logger, string consumerKey, string consumerSecret, string permission="delete")
        {
            _logger = logger;
            ChangeKey(consumerKey, consumerSecret, permission);
        }

        public void ChangeKey(string consumerKey, string consumerSecret, string permission = "delete")
        {
            this.consumerKey = consumerKey;
            this.consumerSecret = consumerSecret;
            this.requestPermission = permission;
        }

        public string ExecuteGetCommand(string url, string userName, 
            string password, WebProxy proxy=null)
		{
            using (var wc = new WebClient())
			{
                if (proxy != null) wc.Proxy = proxy;
                _logger.Debug("GET: " + url);

				if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
					wc.Credentials = new NetworkCredential(userName, password);
                
				try
				{
					using (Stream stream = wc.OpenRead(url))
					{
						using (var reader = new StreamReader(stream))
						{
                            var r = reader.ReadToEnd();
                            _logger.Debug("\nOK: " + url + "\n[" + r.Length + "B]: " + r.Substring(0, Math.Min(256, r.Length)));
							return r;
						}
					}
				}
				catch (WebException ex)
				{
                    _logger.Debug("\nFAIL: " + url);
                    _logger.Debug("\nMSG: " + ex.Message);

					// Handle HTTP 404 errors gracefully and return a null string 
                    // to indicate there is no content.
					if (ex.Response is HttpWebResponse)
                        _logger.Debug("\nRSP: " + (ex.Response as HttpWebResponse).StatusCode);
                       
                    return null; //wtf was the other one for then? hmm
				}
			}
		}

        public string BuildOAuthApiRequestUrl(string url)
        {
            return BuildOAuthApiRequestUrl(url, string.Empty, string.Empty);
        }

        public string BuildOAuthApiRequestUrl(string url, string token, string tokenSecret)
        {
            Dictionary<string, string> x;
            return BuildOAuthApiRequestUrl(url, token, tokenSecret, out x, "GET");
        }

        public string BuildOAuthApiRequestUrl(string url, string token, string tokenSecret, out Dictionary<string, string> parameters, string httpMethod = "GET")
        {
            return BuildOAuthApiRequestUrl(
                url,
                token,
                tokenSecret,
                out parameters,
                httpMethod,
                consumerKey,
                consumerSecret);
        }

        public static string BuildOAuthApiRequestUrl(string url, string token, string tokenSecret, out Dictionary<string, string> parameters, string httpMethod, string consumerKey, string consumerSecret, bool addCallBack=true)
        {
            var oAuth = new OAuthBase();
            var nonce = oAuth.GenerateNonce();
            var timeStamp = oAuth.GenerateTimeStamp();

            string normalizedUrl;
            string normalizedRequestParameters;

            var uri = new Uri(url);

            var sig = oAuth.GenerateSignature(uri, consumerKey,
                    consumerSecret,
                    token, tokenSecret, httpMethod, timeStamp, nonce,
                    OAuthSignatureType.HMACSHA1,
                    out normalizedUrl, out normalizedRequestParameters, addCallBack);

            sig = WebUtility.UrlEncode(sig);

            parameters = new Dictionary<string, string>();
            var query = uri.Query;
            var newurl = url.Split('?')[0] + '?';
            
            if (query[0] == '?') query = query.Remove(0, 1);

            if (query.Length > 0)
                foreach (var item in query.Split('&'))
                {
                    var keyvalue = item.Split('=');
                    if (keyvalue[0] == "method")
                    {
                        newurl += item + "&";
                    }
                    else
                        parameters.Add(keyvalue[0], keyvalue[1]);
                }

            uri = new Uri(newurl);
            
            if (addCallBack)
                parameters.Add("oauth_callback", "oob"); //Steven

            parameters.Add("oauth_consumer_key", consumerKey);
            parameters.Add("oauth_nonce", nonce);
            parameters.Add("oauth_timestamp", timeStamp);
            parameters.Add("oauth_signature_method", "HMAC-SHA1");
            parameters.Add("oauth_version", "1.0");
            parameters.Add("oauth_signature", sig);

            var lst = from e in parameters orderby e.Key ascending select e;
            parameters = lst.ToDictionary(k => k.Key, k => k.Value);

            var sb = new StringBuilder(uri.ToString());
            foreach (var item in parameters)
                sb.AppendFormat(item.Key + "={0}&", item.Value);

            return sb.ToString();
        }

        public string GetUnauthorizedRequestToken(WebProxy proxy)
        {
            var url = BuildOAuthApiRequestUrl(RequestTokenUrl);
            return ExecuteGetCommand(url, null, null, proxy);
        }

        public string GetAuthorizationUrl(string unauthorizedToken)
        {
            return RequestAuthorizeUrl + unauthorizedToken + 
                "&permission="+requestPermission;
        }

        public string GetAccessToken(WebProxy proxy, string oauthToken, string secret, string oauthVerifier)
        {
            var url = BuildOAuthApiRequestUrl(RequestAccessUrl +
                "?oauth_verifier=" + oauthVerifier + "&oauth_token=" + oauthToken, oauthToken, secret);
            return
                ExecuteGetCommand(
                url,
                null,null,proxy);
        }

        public static string GetParameterValue(string url, string parameterName)
        {
            if (string.IsNullOrEmpty(url)) return null;
            var vars = url.Split('?', '&');
            return (from item in vars select item.Split('=') into pair where pair[0] == parameterName select pair[1]).FirstOrDefault();
        }
  
        public static string GetVimeoVideoIdFromURL(string videoURL)
        {
            if (videoURL.Contains("/"))
            {
                var splitter = videoURL.Split(new[] {'/'});

                return splitter[splitter.Length - 1];
            }
            
            int v;
            return int.TryParse(videoURL, out v) ? videoURL : string.Empty;
        }
    }
}