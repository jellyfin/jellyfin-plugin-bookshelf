using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Linq;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.Common;

namespace MediaBrowser.Plugins.Vimeo.VimeoAPI.API
{
    [Serializable]
    public partial class VimeoClient
    {
        protected AdvancedAPI VimeoAPI;
        protected const string OAuthTokenKey = "oauth_token";
        protected const string OAuthTokenSecretKey = "oauth_token_secret";

        protected const string HMACSHA1SignatureType = "HMAC-SHA1";
        protected const string delimiter = "%2C";

        public const bool IsVFW = false;
        public string Token;
        public string TokenSecret;
        public string UserId;
        public string UserName;
        public long FreeQuota;
        public long MaxQuota;
        public long UsedQuota;
        public bool AllowsHD;
        public int QuotaResets;
        public WebProxy Proxy;
        public Person Me;

        public VimeoClient(string consumerKey, string consumerSecret, string permission="delete")
        {
            VimeoAPI = new AdvancedAPI(consumerKey, consumerSecret, permission);
            Trace.WriteLine("This software uses VimeoDotNet to connect to Vimeo API. To support this project visit http://support.saeedoo.com.");
        }

        public void ChangeKey(string consumerKey, string consumerSecret, string permission = "delete")
        {
            VimeoAPI.ChangeKey(consumerKey, consumerSecret, permission);
        }

        #region OAuth, Downloaders, etc
        public void GetUnauthorizedRequestToken()
        {
            var q = VimeoAPI.GetUnauthorizedRequestToken(Proxy);
            Token = AdvancedAPI.GetParameterValue(q, OAuthTokenKey);
            TokenSecret = AdvancedAPI.GetParameterValue(q, OAuthTokenSecretKey);
        }

        public string GenerateAuthorizationUrl()
        {
            return VimeoAPI.GetAuthorizationUrl(OAuthTokenKey + "=" + Token);
        }

        public void GetAccessToken(string verifier)
        {
            var q = VimeoAPI.GetAccessToken(Proxy, Token, TokenSecret, verifier);
            Token = AdvancedAPI.GetParameterValue(q, OAuthTokenKey);
            TokenSecret = AdvancedAPI.GetParameterValue(q, OAuthTokenSecretKey);
        }

        public string GetRequestUrl(string baseUrl, string method, Dictionary<string, string> parameters)
        {
            Dictionary<string, string> x;
            return GetRequestUrl(baseUrl, method, parameters, out x, "GET");
        }
        public string GetRequestUrl(string baseUrl, string method, Dictionary<string, string> parameters, out Dictionary<string, string> oauth_parameters, string httpMethod = "GET")
        {
            if (parameters == null) parameters = new Dictionary<string, string>();
            string url = baseUrl;
            if (!string.IsNullOrEmpty(method))
                 url += "?method=" + method;
            if (!string.IsNullOrEmpty(Token) && !parameters.ContainsKey(OAuthTokenKey))
                parameters.Add(OAuthTokenKey, Token);

            var lst = from e in parameters orderby e.Key ascending select e;
            foreach (var item in lst)
            {
                url += "&" + item.Key + "=";
                if (!string.IsNullOrEmpty(item.Value))
                {
                    if (item.Value[0] == '\'')
                        url += item.Value.Substring(1, item.Value.Length - 1);
                    else
                        url += /*HttpUtility*/ OAuthBase.UrlEncode(item.Value);
                }
            }
            return VimeoAPI.BuildOAuthApiRequestUrl(url, Token, TokenSecret, out oauth_parameters, httpMethod);
        }

        public XDocument ExecuteGetRequest(string method, Dictionary<string, string> parameters=null)
        {
            return ExecuteGetRequest(GetRequestUrl(
                AdvancedAPI.StandardAdvancedApiUrl, method, parameters));
        }

        public XDocument ExecuteGetRequest(string url)
        {
            if (url == null)
            {
                return null;
            }
            //mysterious error comes out of this hole
            //let's seal it
            try
            {
                return XDocument.Parse(
                    VimeoAPI.ExecuteGetCommand(url, null, null, Proxy),
                    LoadOptions.None);
            }
            catch
            {
                Debug.WriteLine("Error when executing get request\nwith url: " + url, "ExecuteGetRequest");
                return null;
            }
        }

        public static XDocument DownloadXML(string url)
        {
            return XDocument.Load(url);
        }

        public static string GetResponseState(XDocument xml)
        {
            try
            {
                return (string)xml.Element("rsp").Attribute("stat");
            }
            catch
            {
                return "error";
            }
        }

        public static bool IsResponseOK(XDocument xml)
        {
            return GetResponseState(xml) == "ok";
        }

        public bool IsResponseOK(string url)
        {
            return IsResponseOK(ExecuteGetRequest(url));
        }

        public bool IsResponseOK(string method, Dictionary<string, string> parameters=null)
        {
            return IsResponseOK(ExecuteGetRequest(method, parameters));
        }
        #endregion

        public bool Login()
        {
            if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(TokenSecret))
                return false;
            var loginResponse = vimeo_test_login();
            if (loginResponse.stat != "ok") return false;
            if (Me == null)
                if ((Me = vimeo_people_getInfo(UserId)) == null) return false;
            UserId = loginResponse.userid;
            UserName = loginResponse.username;
            return true;
        }

        public bool Login(string token, string tokenSecret)
        {
            Token = token;
            TokenSecret = tokenSecret;
            return Login();
        }

        public bool RefreshQuota()
        {
            if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(TokenSecret))
                return false;
            var quotaResponse = vimeo_videos_upload_getQuota();
            if (quotaResponse == null) return false;
            FreeQuota = quotaResponse.free;
            MaxQuota = quotaResponse.max;
            UsedQuota = quotaResponse.used;
            QuotaResets = quotaResponse.resets;
            AllowsHD = quotaResponse.hd_quota;
            return true;
        }

        #region API
        public class ResponseData
        {
            public string stat;
            public float ping;
        }
        public class UserIdNameDisplay
        {
            public string id;
            public string username;
            public string display_name;
        }

        #region vimeo.activity
        //.happenedToUser
        public Activities vimeo_activity_happenedToUser(string user_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>
                {{"user_id", user_id}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.activity.happenedToUser", parameters);
            return !IsResponseOK(x) ? null : Activities.FromElement(x.Element("rsp").Element("activities"));
        }

        //.userDid
        public Activities vimeo_activity_userDid(string user_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "user_id", user_id } };
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.activity.userDid", parameters);
            return !IsResponseOK(x) ? null : Activities.FromElement(x.Element("rsp").Element("activities"));
        }
        #endregion

        #region vimeo.albums
        //.addToWatchLater
        public bool vimeo_albums_addToWatchLater(string video_id)
        {
            return IsResponseOK ("vimeo.albums.addToWatchLater",
                new Dictionary<string, string> { { "video_id", video_id } });
        }

        //.addVideo
        public bool vimeo_albums_addVideo(string video_id, string album_id)
        {
            return IsResponseOK("vimeo.albums.addVideo",
                new Dictionary<string, string>{
                    {"video_id",video_id},
                    {"album_id",album_id}});
        }

        //.create
        public string vimeo_albums_create(string title, string description, string video_id, string videos=null)
        {
            var parameters = new Dictionary<string, string>{
                {"title",title},
                {"description",description},
                {"video_id",video_id}};
            if (!string.IsNullOrEmpty(videos)) parameters.Add("videos", videos);
            var x = ExecuteGetRequest("vimeo.albums.create", parameters);
            return !IsResponseOK(x) ? null : x.Element("rsp").Element("album").Attribute("id").Value;
        }

        //.delete
        public bool vimeo_albums_delete(string album_id)
        {
            return IsResponseOK("vimeo.albums.delete",
                new Dictionary<string, string> { { "album_id", album_id } });
        }

        //.getAll
        public enum AlbumsSortMethods
        {
            Newest, Oldest, Alphabetical, Default
        }
        public Albums vimeo_albums_getAll(string user_id, AlbumsSortMethods sortMethod=AlbumsSortMethods.Default, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>{
                {"user_id",user_id}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sortMethod != AlbumsSortMethods.Default)
                parameters.Add("sort",
                    sortMethod == AlbumsSortMethods.Newest ? "newest" :
                    sortMethod == AlbumsSortMethods.Oldest ? "oldest" :
                    sortMethod == AlbumsSortMethods.Alphabetical ? "alphabetical" : "");
            var x = ExecuteGetRequest("vimeo.albums.getAll", parameters);
            return !IsResponseOK(x) ? null : Albums.FromElement(x.Element("rsp").Element("albums"));
        }

        //.getVideos
        public Videos vimeo_albums_getVideos(string album_id, bool full_response=false, string password=null, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>{
                {"album_id",album_id},
                {"full_response", full_response ? "1" : "0"}};

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (!string.IsNullOrEmpty(password)) parameters.Add("password", password);

            var x = ExecuteGetRequest("vimeo.albums.getVideos", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.getWatchLater
        public Videos vimeo_albums_getWatchLater(bool full_response=false, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.albums.getWatchLater", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.removeFromWatchLater
        public bool vimeo_albums_removeFromWatchLater(string video_id)
        {
            return IsResponseOK("vimeo.albums.removeFromWatchLater",
                new Dictionary<string, string> { { "video_id", video_id } });
        }

        //.removeVideo
        public bool vimeo_albums_removeVideo(string video_id, string album_id)
        {
            return IsResponseOK("vimeo.albums.removeVideo",
                new Dictionary<string, string>{
                    {"video_id",video_id},
                    {"album_id",album_id}});
        }

        //.setDescription
        public bool vimeo_albums_setDescription(string album_id, string description)
        {
            return IsResponseOK("vimeo.albums.setDescription",
                new Dictionary<string, string>{
                    {"album_id", album_id},
                    {"description", description}});
        }

        //.setPassword
        public bool vimeo_albums_setPassword(string album_id, string password)
        {
            return IsResponseOK("vimeo.albums.setPassword",
                new Dictionary<string, string>{
                    {"album_id", album_id},
                    {"password", string.IsNullOrEmpty(password) ? "" : password}});
        }

        //.setTitle
        public bool vimeo_albums_setTitle(string album_id, string title)
        {
            return IsResponseOK("vimeo.albums.setTitle",
                new Dictionary<string, string>{
                    {"album_id", album_id},
                    {"title", title}});
        }
        #endregion

        #region vimeo.categories

        public Categories vimeo_categories_getAll(int? page = null, int? per_page = null)
        {
            var parameters = new Dictionary<string, string>();

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.categories.getAll", parameters);
            return !IsResponseOK(x) ? null : Categories.FromElement(x.Element("rsp").Element("categories"));
        }

        public Category vimeo_categories_getInfo(string category)
        {
            var x = ExecuteGetRequest("vimeo.categories.getInfo",
                new Dictionary<string, string> { { "category", category } });
            return !IsResponseOK(x) ? null : Category.FromElement(x.Element("rsp").Element("category"));
        }

        public Channels vimeo_categories_getRelatedChannels(string category, int? page = null, int? per_page = null)
        {
            var parameters = new Dictionary<string, string> {{"category", category}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.categories.getRelatedChannels", parameters);
            return !IsResponseOK(x) ? null : Channels.FromElement(x.Element("rsp").Element("channels"));
        }

        #endregion

        #region vimeo.channels
        //.addVideo
        public bool vimeo_channels_addVideo(string video_id, string channel_id)
        {
            return IsResponseOK("vimeo.channels.addVideo",
                new Dictionary<string, string>{
                    {"video_id",video_id},
                    {"channel_id",channel_id}});
        }

        //.getAll
        public enum ChannelsSortMethods
        {
            Newest, Oldest, Alphabetical, MostVideos, MostSubscribed, MostRecentlyUpdated, Default
        }
        public Channels vimeo_channels_getAll(string user_id=null, ChannelsSortMethods sortMethod=ChannelsSortMethods.Default, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(user_id)) parameters.Add("user_id", user_id);
            if (sortMethod != ChannelsSortMethods.Default)
                parameters.Add("sort",
                    sortMethod == ChannelsSortMethods.Newest ? "newest" :
                    sortMethod == ChannelsSortMethods.Oldest ? "oldest" :
                    sortMethod == ChannelsSortMethods.Alphabetical ? "alphabetical" :
                    sortMethod == ChannelsSortMethods.MostVideos ? "most_videos" :
                    sortMethod == ChannelsSortMethods.MostSubscribed ? "most_subscribed" :
                    sortMethod == ChannelsSortMethods.MostRecentlyUpdated ? "most_recently_updated" : "");

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.channels.getAll", parameters);
            return !IsResponseOK(x) ? null : Channels.FromElement(x.Element("rsp").Element("channels"));
        }

        //.getInfo
        public Channel vimeo_channels_getInfo(string channel_id)
        {
            var x = ExecuteGetRequest("vimeo.channels.getInfo",
                new Dictionary<string, string> { { "channel_id", channel_id } });
            return !IsResponseOK(x) ? null : Channel.FromElement(x.Element("rsp").Element("channel"));
        }

        //.getModerated
        public Channels vimeo_channels_getModerated(string user_id, ChannelsSortMethods sortMethod=ChannelsSortMethods.Default, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>
            {{"user_id", user_id}};
            if (sortMethod != ChannelsSortMethods.Default)
                parameters.Add("sort",
                    sortMethod == ChannelsSortMethods.Newest ? "newest" :
                    sortMethod == ChannelsSortMethods.Oldest ? "oldest" :
                    sortMethod == ChannelsSortMethods.Alphabetical ? "alphabetical" :
                    sortMethod == ChannelsSortMethods.MostVideos ? "most_videos" :
                    sortMethod == ChannelsSortMethods.MostSubscribed ? "most_subscribed" :
                    sortMethod == ChannelsSortMethods.MostRecentlyUpdated ? "most_recently_updated" : "");

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.channels.getModerated", parameters);
            return !IsResponseOK(x) ? null : Channels.FromElement(x.Element("rsp").Element("channels"));
        }

        //.getModerators
        public Channel.Moderators vimeo_channels_getModerators(string channel_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "channel_id", channel_id } };
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.channels.getModerators", parameters);
            return !IsResponseOK(x) ? null : Channel.Moderators.FromElement(x.Element("rsp").Element("moderators"));
        }

        //.getSubscribers
        public Contacts vimeo_channels_getSubscribers(string channel_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "channel_id", channel_id } };
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.channels.getSubscribers", parameters);
            return !IsResponseOK(x) ? null : Contacts.FromElement(x.Element("rsp").Element("subscribers"),"subscriber");
        }

        //.getVideos
        public Videos vimeo_channels_getVideos(string channel_id, bool full_response=false, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>{
                {"channel_id",channel_id},
                {"full_response", full_response ? "1" : "0"}};

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.channels.getVideos", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.removeVideo
        public bool vimeo_channels_removeVideo(string video_id, string channel_id)
        {
            return IsResponseOK("vimeo.channels.removeVideo",
                new Dictionary<string, string> { { "channel_id", channel_id }, {"video_id", video_id} });
        }

        //.subscribe
        public bool vimeo_channels_subscribe(string channel_id)
        {
            return IsResponseOK("vimeo.channels.subscribe",
                new Dictionary<string, string> { { "channel_id", channel_id } });
        }

        //.unsubscribe
        public bool vimeo_channels_unsubscribe(string channel_id)
        {
            return IsResponseOK("vimeo.channels.unsubscribe",
                new Dictionary<string, string> { { "channel_id", channel_id } });
        }
        #endregion
        
        #region vimeo.contacts
        public enum ContactsSortMethods
        {   Newest, Oldest, Alphabetical, MostCredited, Default }

        //.getAll
        public Contacts vimeo_contacts_getAll(string user_id, ContactsSortMethods sort = ContactsSortMethods.Default, int? page = null, int? per_page = null)
        {
            return vimeo_contacts_getAll(user_id, page, per_page, sort);
        }
        public Contacts vimeo_contacts_getAll(string user_id, int? page, int? per_page, ContactsSortMethods sort)
        {
            var _sort = sort == ContactsSortMethods.Alphabetical ? "alphabetical" :
                sort == ContactsSortMethods.MostCredited ? "most_credited" :
                sort == ContactsSortMethods.Newest ? "newest" :
                sort == ContactsSortMethods.Oldest ? "oldest" : "";
            var parameters = new Dictionary<string, string>();
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sort != ContactsSortMethods.Default) parameters.Add("sort", _sort);
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.contacts.getAll", parameters);
            return !IsResponseOK(x) ? null : Contacts.FromElement(x.Element("rsp").Element("contacts"));
        }

        //.getMutual
        public Contacts vimeo_contacts_getMutual(string user_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>();
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.contacts.getMutual", parameters);
            return !IsResponseOK(x) ? null : Contacts.FromElement(x.Element("rsp").Element("contacts"));
        }

        //.getOnline
        public Contacts vimeo_contacts_getOnline(string user_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>();
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.contacts.getOnline", parameters);
            return !IsResponseOK(x) ? null : Contacts.FromElement(x.Element("rsp").Element("contacts"));
        }

        //.getWhoAdded
        public Contacts vimeo_contacts_getWhoAdded(string user_id, ContactsSortMethods sort = ContactsSortMethods.Default, int? page=null, int? per_page=null)
        {
            return vimeo_contacts_getWhoAdded(user_id, page, per_page, sort);
        }
        public Contacts vimeo_contacts_getWhoAdded(string user_id, int? page, int? per_page, ContactsSortMethods sort)
        {
            var _sort = sort == ContactsSortMethods.Alphabetical ? "alphabetical" :
                sort == ContactsSortMethods.MostCredited ? "most_credited" :
                sort == ContactsSortMethods.Newest ? "newest" :
                sort == ContactsSortMethods.Oldest ? "oldest" : "";
            var parameters = new Dictionary<string, string>();
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sort != ContactsSortMethods.Default) parameters.Add("sort", _sort);
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.contacts.getWhoAdded", parameters);
            return !IsResponseOK(x) ? null : Contacts.FromElement(x.Element("rsp").Element("contacts"));
        }
        #endregion

        #region vimeo.groups
        //.addVideo
        public bool vimeo_groups_addVideo(string video_id, string group_id)
        {
            return IsResponseOK("vimeo.groups.addVideo",
                new Dictionary<string, string>{
                    {"video_id", video_id},
                    {"group_id", group_id}});
        }

        //.getAddable
        public Groups vimeo_groups_getAddable(string user_id, ChannelsSortMethods sortMethod=ChannelsSortMethods.Default, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "user_id", user_id } };
            if (sortMethod != ChannelsSortMethods.Default)
                parameters.Add("sort",
                    sortMethod == ChannelsSortMethods.Newest ? "newest" :
                    sortMethod == ChannelsSortMethods.Oldest ? "oldest" :
                    sortMethod == ChannelsSortMethods.Alphabetical ? "alphabetical" :
                    sortMethod == ChannelsSortMethods.MostVideos ? "most_videos" :
                    sortMethod == ChannelsSortMethods.MostSubscribed ? "most_subscribed" :
                    sortMethod == ChannelsSortMethods.MostRecentlyUpdated ? "most_recently_updated" : "");

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.groups.getAddable", parameters);
            return !IsResponseOK(x) ? null : Groups.FromElement(x.Element("rsp").Element("groups"));
        }

        //.getAll
        public Groups vimeo_groups_getAll(string user_id, ChannelsSortMethods sortMethod=ChannelsSortMethods.Default, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(user_id)) parameters.Add("user_id", user_id);
            if (sortMethod != ChannelsSortMethods.Default)
                parameters.Add("sort",
                    sortMethod == ChannelsSortMethods.Newest ? "newest" :
                    sortMethod == ChannelsSortMethods.Oldest ? "oldest" :
                    sortMethod == ChannelsSortMethods.Alphabetical ? "alphabetical" :
                    sortMethod == ChannelsSortMethods.MostVideos ? "most_videos" :
                    sortMethod == ChannelsSortMethods.MostSubscribed ? "most_subscribed" :
                    sortMethod == ChannelsSortMethods.MostRecentlyUpdated ? "most_recently_updated" : "");

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.groups.getAll", parameters);
            return !IsResponseOK(x) ? null : Groups.FromElement(x.Element("rsp").Element("groups"));
        }

        //.getInfo
        public Groups vimeo_groups_getInfo(string group_id)
        {
            var x = ExecuteGetRequest("vimeo.groups.getAll", new Dictionary<string,string>{{"group_id", group_id}} );
            return !IsResponseOK(x) ? null : Groups.FromElement(x.Element("rsp").Element("groups"));
        }

        //.getMembers
        public Contacts vimeo_groups_getMembers(string group_id, ChannelsSortMethods sortMethod=ChannelsSortMethods.Default, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "group_id", group_id } };
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.groups.getMembers", parameters);
            if (!IsResponseOK(x)) return null;

            var e = x.Element("rsp").Element("members");
            var cs = new Contacts
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            cs.AddRange(e.Elements("member").Select(Contact.FromElement));

            return cs;
        }

        //.getModerators
        public Contacts vimeo_groups_getModerators(string group_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "group_id", group_id } };
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.groups.getModerators", parameters);
            return !IsResponseOK(x) ? null : Contacts.FromElement(x.Element("rsp").Element("moderators"), "moderator");
        }

        //.getVideoComments
        public Comments vimeo_groups_getVideoComments(string group_id, string video_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { 
            { "group_id", group_id }, {"video_id", video_id} };
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.groups.getVideoComments", parameters);
            return !IsResponseOK(x) ? null : Comments.FromElement(x.Element("rsp").Element("comments"));
        }

        //.getVideos
        public enum GroupVideosSortMethods
        {
            Newest, Oldest, Featured, MostPlayed, MostCommented, MostLiked, Random, Default
        }
        public Videos vimeo_groups_getVideos(string group_id, bool full_response=false, GroupVideosSortMethods sortMethod=GroupVideosSortMethods.Default, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string>{
                {"group_id",group_id},
                {"full_response", full_response ? "1" : "0"}};

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            if (sortMethod != GroupVideosSortMethods.Default)
                parameters.Add("sort",
                    sortMethod == GroupVideosSortMethods.Newest ? "newest" :
                    sortMethod == GroupVideosSortMethods.Oldest ? "oldest" :
                    sortMethod == GroupVideosSortMethods.Featured ? "featured" :
                    sortMethod == GroupVideosSortMethods.MostPlayed ? "most_played" :
                    sortMethod == GroupVideosSortMethods.MostCommented ? "most_commented" :
                    sortMethod == GroupVideosSortMethods.MostLiked ? "most_liked" :
                    sortMethod == GroupVideosSortMethods.Random ? "random" : "");

            var x = ExecuteGetRequest("vimeo.groups.getVideos", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"),full_response);
        }

        //.join
        public bool vimeo_groups_join(string group_id)
        {
            return IsResponseOK("vimeo.groups.join", new Dictionary<string, string> { { "group_id", group_id } });
        }

        //.leave
        public bool vimeo_groups_leave(string group_id)
        {
            return IsResponseOK("vimeo.groups.leave", new Dictionary<string, string> { { "group_id", group_id } });
        }

        //.removeVideo
        public bool vimeo_groups_removeVideo(string video_id, string group_id)
        {
            return IsResponseOK("vimeo.groups.removeVideo", new Dictionary<string, string> {
            { "group_id", group_id } , {"video_id",video_id}});
        }
        #endregion

        #region vimeo.groups.forums
        //.getTopicComments
        public Comments vimeo_groups_forums_getTopicComments(string group_id, string topic_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "group_id", group_id }, { "topic_id", topic_id } };
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.groups.forums.getTopicComments", parameters);
            return !IsResponseOK(x) ? null : Comments.FromElement(x.Element("rsp").Element("comments"));
        }

        //.getTopics
        public Topics vimeo_groups_forums_getTopics(string group_id, int? page=null, int? per_page=null)
        {
            var parameters = new Dictionary<string, string> { { "group_id", group_id } };
            
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.groups.forums.getTopics", parameters);
            return !IsResponseOK(x) ? null : Topics.FromElement(x.Element("rsp").Element("topics"));
        }
        #endregion

        #region vimeo.oauth
        //.checkAccessToken
        public class vimeo_oauth_checkAccessTokenResponse
        {
            public string stat;
            public string token;
            public string permission;
            public UserIdNameDisplay user;
        }

        public vimeo_oauth_checkAccessTokenResponse vimeo_oauth_checkAccessToken()
        {
            var x = ExecuteGetRequest("vimeo.oauth.checkAccessToken", null);
            var r = new vimeo_oauth_checkAccessTokenResponse {stat = "n/a"};
            try
            {
                r.stat = (string)x.Element("rsp").Attribute("stat");
                r.token = (string)x.Element("rsp").Element("oauth").Element("token").Value;
                r.permission = (string)x.Element("rsp").Element("oauth").Element("permission").Value;
                r.user = new UserIdNameDisplay
                {
                    display_name = (string) x.Element("rsp").Element("oauth").Element("user").Attribute("display_name"),
                    id = (string) x.Element("rsp").Element("oauth").Element("user").Attribute("id"),
                    username = (string) (string) x.Element("rsp").Element("oauth").Element("user").Attribute("username")
                };
            }
            catch
            {
                r.stat = "error";
            }
            return r;
        }
#endregion

        #region vimeo.people
        //.addContact
        public bool vimeo_people_addContact(string user_id)
        {
            return IsResponseOK("vimeo.people.addContact", new Dictionary<string, string> { { "user_id", user_id } });
        }

        //.addSubscription
        public bool vimeo_people_addSubscription(string user_id, bool likes, bool appears, bool uploads)
        {
            var types = 
                (likes ? "likes" + delimiter : "") +
                (appears ? "appears" + delimiter : "") +
                (uploads ? "uploads" : "");
            //if (types[types.Length - 1] == ',') types = types.Substring(0, types.Length - 1);
            types = HttpUtility.UrlEncode(types);

            return IsResponseOK("vimeo.people.addSubscription", new Dictionary<string, string>{
                {"types", types}, {"user_id", user_id}});
        }

        //.findByEmail
        public UserIdNameDisplay vimeo_people_findByEmail(string user_id)
        {
            var x = ExecuteGetRequest("vimeo.people.findByEmail", new Dictionary<string, string> { { "user_id", user_id } });
            if (!IsResponseOK(x)) return null;
            var r = new UserIdNameDisplay
            {
                id = x.Element("rsp").Element("user").Attribute("id").Value,
                username = x.Element("rsp").Element("user").Element("username").Value,
                display_name = x.Element("rsp").Element("user").Element("display_name").Value
            };
            return r;
        }

        //.getInfo
        public Person vimeo_people_getInfo(string user_id)
        {
            var x = ExecuteGetRequest("vimeo.people.getInfo", new Dictionary<string, string>() { { "user_id", user_id } });
            return !IsResponseOK(x) ? null : Person.FromElement(x.Element("rsp").Element("person"));
        }

        //.getPortraitUrls
        public List<Thumbnail> vimeo_people_getPortraitUrls(string user_id)
        {
            var x = ExecuteGetRequest("vimeo.people.getPortraitUrls", new Dictionary<string, string>() { { "user_id", user_id } });
            return !IsResponseOK(x) ? null : Person.GetPortraits(x.Element("rsp").Element("portraits"));
        }

        //.getSubscriptions
        public Subscriptions vimeo_people_getSubscriptions(bool likes, bool uploads, bool appears, bool channel, bool group, int? page=null, int? per_page=null)
        {
            return vimeo_people_getSubscriptions(page, per_page, likes, uploads, appears, channel, group);
        }
        public Subscriptions vimeo_people_getSubscriptions(int? page, int? per_page, bool likes, bool uploads, bool appears, bool channel, bool group)
        {
            var types =
                (likes ? "likes" + delimiter : "") +
                (uploads ? "uploads" + delimiter : "") +
                (appears ? "appears" + delimiter : "") +
                (channel ? "channel" + delimiter : "") +
                (group ? "group" : "");
            //if (types[types.Length - 1] == delimiter) types = types.Substring(0, types.Length - 1);
            var parameters = new Dictionary<string,string>();
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            parameters.Add("types", types);
            var x = ExecuteGetRequest("vimeo.people.getSubscriptions", parameters);
            return !IsResponseOK(x) ? null : Subscriptions.FromElement(x.Element("rsp").Element("subscriptions"));
        }

        //.removeContact
        public bool vimeo_people_removeContact(string user_id)
        {
            return IsResponseOK("vimeo.people.removeContact",
                new Dictionary<string, string> { { "user_id" , user_id }});
        }

        //.removeSubscription
        public bool vimeo_people_removeSubscription(string user_id, bool likes, bool appears, bool uploads)
        {
            var types =
                (likes ? "likes" + delimiter : "") +
                (appears ? "appears" + delimiter : "") +
                (uploads ? "uploads" : "");
            //if (types[types.Length - 1] == delimiter) types = types.Substring(0, types.Length - 1);
            return IsResponseOK("vimeo.people.removeSubscription", new Dictionary<string, string>{
                {"types", types}, {"user_id", user_id}});
        }
        #endregion

        #region vimeo.test
        //.echo
        public bool vimeo_test_echo()
        {
            return IsResponseOK("vimeo.test.echo", null);
        }

        //.login
        public class vimeo_test_loginResponse
        {
            public string stat;
            public string userid;
            public string username;
        }

        public vimeo_test_loginResponse vimeo_test_login()
        {
            var x = ExecuteGetRequest("vimeo.test.login", null);
            var r = new vimeo_test_loginResponse {stat = "n/a"};
            try
            {
                r.stat = (string)x.Element("rsp").Attribute("stat");
                r.userid = (string)x.Element("rsp").Element("user").Attribute("id");
                r.username = (string)x.Element("rsp").Element("user").Element("username").Value;
            }
            catch
            {
                r.stat = "error";
            }
            return r;
        }

        //.null
        public bool test_null()
        {
            return IsResponseOK("vimeo.test.null", null);
        }
        #endregion

        #region vimeo.videos
        //.addCast
        public bool vimeo_videos_addCast(string role, string user_id, string video_id)
        {
            var parameters = new Dictionary<string, string>(){
                {"user_id", user_id},
                {"video_id", video_id}};
            if (!string.IsNullOrEmpty(role))
                parameters.Add("role", role);
            return IsResponseOK("vimeo.videos.addCast", parameters);
        }

        //.addPhotos
        public bool vimeo_videos_addPhotos(string photo_urls, string video_id)
        {
            var parameters = new Dictionary<string, string>{
                {"photo_urls", photo_urls},
                {"video_id", video_id}};
            return IsResponseOK("vimeo.videos.addPhotos", parameters);
        }

        //.addTags
        public bool vimeo_videos_addTags(string tags, string video_id)
        {
            return IsResponseOK("vimeo.videos.addTags", new Dictionary<string,string>{
                {"tags", tags.Replace(",", delimiter + "%20")}, {"video_id", video_id}});
        }

        //.clearTags
        public bool vimeo_videos_clearTags(string video_id)
        {
            return IsResponseOK("vimeo.videos.clearTags",
                new Dictionary<string, string> { { "video_id", video_id } });
        }

        //.delete
        public bool vimeo_videos_delete(string video_id)
        {
            return IsResponseOK("vimeo.videos.delete",
                new Dictionary<string, string> { { "video_id", video_id } });
        }

        //.getAll
        public enum VideosSortMethod
        {   Newest, Oldest, MostPlayed, MostCommented, MostLiked, Default, SearchRelevant }
        public Videos vimeo_videos_getAll(string user_id, bool full_response=false, VideosSortMethod sortMethod=VideosSortMethod.Default, int? page=null, int? per_page=null)
        {
            return vimeo_videos_getAll(full_response, page, per_page, sortMethod, user_id);
        }
        public Videos vimeo_videos_getAll(bool full_response, int? page, int? per_page, VideosSortMethod sortMethod, string user_id)
        {
            var parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sortMethod != VideosSortMethod.Default)
                parameters.Add("sort", sortMethod == VideosSortMethod.Newest ? "newest" :
                sortMethod == VideosSortMethod.Oldest ? "oldest" :
                sortMethod == VideosSortMethod.MostPlayed ? "most_played" :
                sortMethod == VideosSortMethod.MostCommented ? "most_commented" :
                sortMethod == VideosSortMethod.MostLiked ? "most_liked" : "");
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.videos.getAll", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.getAppearsIn
        public Videos vimeo_videos_getAppearsIn(bool full_response, int? page, int? per_page, VideosSortMethod sortMethod, string user_id)
        {
            var parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sortMethod != VideosSortMethod.Default)
                parameters.Add("sort", sortMethod == VideosSortMethod.Newest ? "newest" :
                sortMethod == VideosSortMethod.Oldest ? "oldest" :
                sortMethod == VideosSortMethod.MostPlayed ? "most_played" :
                sortMethod == VideosSortMethod.MostCommented ? "most_commented" :
                sortMethod == VideosSortMethod.MostLiked ? "most_liked" : "");
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.videos.getAppearsIn", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.getByTag
        public Videos vimeo_videos_getByTag(bool full_response, int? page, int? per_page, VideosSortMethod sortMethod, string tag)
        {
            var parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sortMethod != VideosSortMethod.Default)
                parameters.Add("sort", sortMethod == VideosSortMethod.Newest ? "newest" :
                sortMethod == VideosSortMethod.Oldest ? "oldest" :
                sortMethod == VideosSortMethod.MostPlayed ? "most_played" :
                sortMethod == VideosSortMethod.MostCommented ? "most_commented" :
                sortMethod == VideosSortMethod.MostLiked ? "most_liked" : "");
            parameters.Add("tag", tag);
            var x = ExecuteGetRequest("vimeo.videos.getByTag", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.getCast
        public List<Video.CastMember> vimeo_videos_getCast(string video_id, int? page, int? per_page)
        {
            var parameters = new Dictionary<string, string>{
                {"video_id", video_id}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.videos.getCast", parameters);
            return !IsResponseOK(x) ? null : x.Element("rsp").Element("cast").Elements("member").Select(Video.CastMember.FromElementFull).ToList();
        }

        //.getCollections
        public class Collection
        {
            public string id;
            public string name;
            public string type;
        }
        public List<Collection> vimeo_videos_getCollections(string video_id)
        {
            var x = ExecuteGetRequest("vimeo.videos.getCollections",
                new Dictionary<string, string> { { "video_id", video_id } });
            if (!IsResponseOK(x)) return null;
            var cs = new List<Collection>();
            if (x.Element("rsp").Element("collections") == null) return cs;
            cs.AddRange(x.Element("rsp").Element("collections").Elements("collection").Select(item => new Collection()
            {
                id = item.Attribute("id").Value, name = item.Attribute("name").Value, type = item.Attribute("type").Value
            }));
            return cs;
        }


        //.getInfo
        public Video vimeo_videos_getInfo(string video_id)
        {
            var x = ExecuteGetRequest("vimeo.videos.getInfo", 
                new Dictionary<string,string>{{"video_id",video_id}});
            return !IsResponseOK(x) ? null : global::MediaBrowser.Plugins.Vimeo.VimeoAPI.API.Video.FromElement(x.Element("rsp").Element("video"), true);
        }

        //.getLikers
        public Video.Likers vimeo_videos_getLikers(string video_id, int? page, int? per_page)
        {
            var parameters = new Dictionary<string, string>{
                {"video_id", video_id}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            var x = ExecuteGetRequest("vimeo.videos.getLikers", parameters);
            return !IsResponseOK(x) ? null : Video.Likers.FromElement(x.Element("rsp").Element("likers"));
        }

        //.getLikes
        public Videos vimeo_videos_getLikes(bool full_response, int? page, int? per_page, VideosSortMethod sortMethod, string user_id)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sortMethod != VideosSortMethod.Default)
                parameters.Add("sort", sortMethod == VideosSortMethod.Newest ? "newest" :
                sortMethod == VideosSortMethod.Oldest ? "oldest" :
                sortMethod == VideosSortMethod.MostPlayed ? "most_played" :
                sortMethod == VideosSortMethod.MostCommented ? "most_commented" :
                sortMethod == VideosSortMethod.MostLiked ? "most_liked" : "");
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.videos.getLikes", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.getSubscriptions
        public Videos vimeo_videos_getSubscriptions(bool full_response, int? page, int? per_page, VideosSortMethod sortMethod, string user_id)
        {
            var parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sortMethod != VideosSortMethod.Default)
                parameters.Add("sort", sortMethod == VideosSortMethod.Newest ? "newest" :
                sortMethod == VideosSortMethod.Oldest ? "oldest" :
                sortMethod == VideosSortMethod.MostPlayed ? "most_played" :
                sortMethod == VideosSortMethod.MostCommented ? "most_commented" :
                sortMethod == VideosSortMethod.MostLiked ? "most_liked" : "");
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.videos.getSubscriptions", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.getThumbnailUrls
        public List<Thumbnail> vimeo_videos_getThumbnailUrls(string video_id)
        {
            var x = ExecuteGetRequest("vimeo.videos.getThumbnailUrls", new Dictionary<string,string>{
                {"video_id", video_id}});
            return !IsResponseOK(x) ? null : Video.GetThumbnails(x.Element("rsp").Element("thumbnails"));
        }

        //.getUploaded
        public Videos vimeo_videos_getUploaded(bool full_response, int? page, int? per_page, VideosSortMethod sortMethod, string user_id)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (sortMethod != VideosSortMethod.Default)
                parameters.Add("sort", sortMethod == VideosSortMethod.Newest ? "newest" :
                sortMethod == VideosSortMethod.Oldest ? "oldest" :
                sortMethod == VideosSortMethod.MostPlayed ? "most_played" :
                sortMethod == VideosSortMethod.MostCommented ? "most_commented" :
                sortMethod == VideosSortMethod.MostLiked ? "most_liked" : "");
            parameters.Add("user_id", user_id);
            var x = ExecuteGetRequest("vimeo.videos.getUploaded", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.removeCast
        public bool vimeo_videos_removeCast(string user_id, string video_id)
        {
            return IsResponseOK("vimeo.videos.removeCast",
                new Dictionary<string, string>{{"user_id",user_id},
                    {"video_id",video_id}});
        }

        //.removeTag
        public bool vimeo_videos_removeTag(string video_id, string tag_id)
        {
            return IsResponseOK("vimeo.videos.removeTag",
                new Dictionary<string, string>{{"tag_id",tag_id},
                    {"video_id",video_id}});
        }

        //.search
        public Videos vimeo_videos_search(bool full_response, int? page, int? per_page, string query, VideosSortMethod sortMethod, string user_id)
        {
            var parameters = new Dictionary<string, string>{
                {"full_response", full_response ? "1" : "0"},
                {"query", query}};
            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());
            if (!string.IsNullOrEmpty(user_id)) parameters.Add("user_id", user_id);
            if (sortMethod != VideosSortMethod.Default)
                parameters.Add("sort", sortMethod == VideosSortMethod.Newest ? "newest" :
                sortMethod == VideosSortMethod.Oldest ? "oldest" :
                sortMethod == VideosSortMethod.MostPlayed ? "most_played" :
                sortMethod == VideosSortMethod.MostCommented ? "most_commented" :
                sortMethod == VideosSortMethod.MostLiked ? "most_liked" : 
                sortMethod == VideosSortMethod.SearchRelevant ? "relevant" : "");
            
            var x = ExecuteGetRequest("vimeo.videos.search", parameters);
            return !IsResponseOK(x) ? null : Videos.FromElement(x.Element("rsp").Element("videos"), full_response);
        }

        //.setDescription
        public bool vimeo_videos_setDescription(string video_id, string description)
        {
            return IsResponseOK("vimeo.videos.setDescription",
                   new Dictionary<string, string>{{"description",description},
                    {"video_id",video_id}});
        }

        //.setDownloadPrivacy
        public bool vimeo_videos_setDownloadPrivacy(string video_id, bool download)
        {
            return IsResponseOK("vimeo.videos.setDownloadPrivacy",
                   new Dictionary<string, string>{{"download",download.ToString().ToLower()},
                    {"video_id",video_id}});
        }

        //.setLicense
        public enum VideoLicenses
        {
            by,
            by_sa,
            by_nd,
            by_nc,
            by_nc_sa,
            by_nc_nd,
            None
        }
        public bool vimeo_videos_setLicense(string video_id, VideoLicenses licenseType)
        {
            return IsResponseOK("vimeo.videos.setDownloadPrivacy",
                   new Dictionary<string, string>{
                   {"license", "" + (
                       licenseType == VideoLicenses.by ? "by" :
                       licenseType == VideoLicenses.by_sa ? "by-sa" :
                       licenseType == VideoLicenses.by_nd ? "by-nd" :
                       licenseType == VideoLicenses.by_nc ? "by-nc" :
                       licenseType == VideoLicenses.by_nc_sa ? "by-nc-sa" :
                       licenseType == VideoLicenses.by_nc_nd ? "by-nc-nd" :
                       "0")
                   },
                    {"video_id",video_id}});
        }

        //.setLike
        public bool vimeo_videos_setLike(string video_id, bool like=true)
        {
            return IsResponseOK("vimeo.videos.setLike",
                   new Dictionary<string, string>{{"like",like.ToString().ToLower()},
                    {"video_id",video_id}});
        }

        //.setPrivacy
        public bool vimeo_videos_setPrivacy(string video_id, string privacy, string users=null, string password=null)
        {
            privacy = privacy.Trim().ToLower();
            var parameters = new Dictionary<string, string>{
                {"video_id",video_id},
                {"privacy",privacy}};
            switch (privacy)
            {
                case "password":
                    parameters.Add("password", password);
                    break;
                case "users":
                    parameters.Add("users", users.Replace(",", delimiter));
                    break;
            }
            return IsResponseOK("vimeo.videos.setPrivacy", parameters);
        }

        //.setTitle
        public bool vimeo_videos_setTitle(string video_id, string title)
        {
            return IsResponseOK("vimeo.videos.setTitle",
                   new Dictionary<string, string>{{"title",title},
                    {"video_id",video_id}});
        }
        #endregion

        #region vimeo.videos.comments
        //.addComment
        public string vimeo_videos_comments_addComment(
            string video_id,
            string reply_to_comment_id,
            string comment_text)
        {
            var parameters = new Dictionary<string, string>{
                {"video_id", video_id},
                {"comment_text", comment_text}};
            if (!string.IsNullOrEmpty(reply_to_comment_id))
                parameters.Add("reply_to_comment_id", reply_to_comment_id);
            var x = ExecuteGetRequest("vimeo.videos.comments.addComment", parameters);
            return !IsResponseOK(x) ? null : x.Element("rsp").Element("comment").Attribute("id").Value;
        }

        //.deleteComment
        public bool vimeo_videos_comments_deleteComment(string video_id, string comment_id)
        {
            return IsResponseOK("vimeo.videos.comments.deleteComment",
                new Dictionary<string, string> { { "video_id", video_id }, { "comment_id", comment_id } });
        }

        //.editComment
        public bool vimeo_videos_comments_editComment(string video_id, string comment_id, string comment_text)
        {
            return IsResponseOK("vimeo.videos.comments.editComment",
                new Dictionary<string, string> { 
                { "video_id", video_id }, { "comment_id", comment_id },
                {"comment_text", comment_text}});
        }

        //.getList
        public Comments vimeo_videos_comments_getList(string video_id, int? page, int? per_page)
        {
            var parameters = new Dictionary<string, string>{
                {"video_id", video_id}};

            if (page.HasValue) parameters.Add("page", page.Value.ToString());
            if (per_page.HasValue) parameters.Add("per_page", per_page.Value.ToString());

            var x = ExecuteGetRequest("vimeo.videos.comments.getList", parameters);
            return !IsResponseOK(x) ? null : Comments.FromElement(x.Element("rsp").Element("comments"));
        }
        #endregion

        #region vimeo.videos.upload
        //.checkTicket
        public Ticket vimeo_videos_upload_checkTicket(string ticket_id)
        {
            var parameters = new Dictionary<string, string> { { "ticket_id", ticket_id } };
            var x = ExecuteGetRequest("vimeo.videos.upload.checkTicket", parameters);
            if (!IsResponseOK(x)) return null;
            if (x.Element("rsp").Element("ticket").Attribute("valid").Value != "1") return new Ticket();
            return new Ticket()
            {
                id = x.Element("rsp").Element("ticket").Attribute("id").Value,
                endpoint = x.Element("rsp").Element("ticket").Attribute("endpoint").Value
            };
        }

        //.complete
        public string vimeo_videos_upload_complete(string filename, Ticket ticket)
        {
            return vimeo_videos_upload_complete(filename, ticket.id);
        }
        public string vimeo_videos_upload_complete(string filename, string ticket_id)
        {
            var parameters = new Dictionary<string, string> { { "filename", filename }, { "ticket_id", ticket_id } };
            var x = ExecuteGetRequest("vimeo.videos.upload.complete", parameters);
            return !IsResponseOK(x) ? null : x.Element("rsp").Element("ticket").Attribute("video_id").Value;
        }

        //.getQuota
        public class vimeo_videos_upload_getQuotaResponse
        {
            public string userid;
            public bool is_plus;
            public long free;
            public long max;
            public int resets;
            public long used;
            public bool hd_quota;
            public bool sd_quota;

            public static vimeo_videos_upload_getQuotaResponse FromElement(XElement e)
            {
                return new vimeo_videos_upload_getQuotaResponse
                {
                    userid = e.Element("user").Attribute("id").Value,
                    is_plus = e.Element("user").Attribute("is_plus").Value == "1",
                    free = long.Parse(e.Element("user").Element("upload_space").Attribute("free").Value),
                    max = long.Parse(e.Element("user").Element("upload_space").Attribute("max").Value),
                    resets = int.Parse(e.Element("user").Element("upload_space").Attribute("resets").Value),
                    used = long.Parse(e.Element("user").Element("upload_space").Attribute("used").Value),
                    hd_quota = e.Element("user").Element("hd_quota").Value == "1",
                    sd_quota = e.Element("user").Element("sd_quota").Value == "1"
                };
            }
        }
        public vimeo_videos_upload_getQuotaResponse vimeo_videos_upload_getQuota()
        {
            var x = ExecuteGetRequest("vimeo.videos.upload.getQuota", null);
            return !IsResponseOK(x) ? null : vimeo_videos_upload_getQuotaResponse.FromElement(x.Element("rsp"));
        }

        //.getTicket
        public Ticket vimeo_videos_upload_getTicket(string video_id=null, string mode=null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(video_id)) parameters.Add("video_id", video_id);
            if (!string.IsNullOrEmpty(mode)) parameters.Add("mode", mode);
            var x = ExecuteGetRequest("vimeo.videos.upload.getTicket", parameters);
            return !IsResponseOK(x) ? null : Ticket.FromElement(x.Element("rsp").Element("ticket"));
        }

        //.verifyChunks
        public Chunks vimeo_videos_upload_verifyChunks(Ticket ticket)
        {
            return vimeo_videos_upload_verifyChunks(ticket.id);
        }
        public Chunks vimeo_videos_upload_verifyChunks(string ticket_id)
        {
            var parameters = new Dictionary<string, string> { { "ticket_id", ticket_id } };
            var x = ExecuteGetRequest("vimeo.videos.upload.verifyChunks", parameters);
            return !IsResponseOK(x) ? null : Chunks.FromElement(x.Element("rsp").Element("ticket"));
        }
        #endregion
        #endregion
    }
}
