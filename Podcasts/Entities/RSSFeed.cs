using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Diagnostics;
using System.Xml.XPath;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Net;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Notifications;

namespace PodCasts.Entities {
    public class RssFeed {

        public IEnumerable<BaseItem> Children { get; private set; }

        string url;
        SyndicationFeed _feed;

        public RssFeed(string url) {
            this.url = url;
        }

        public async Task Refresh(IProviderManager providerManager, CancellationToken cancellationToken)
        {
            try
            {
                using (var client = new WebClient())
                using (XmlReader reader = new SyndicationFeedXmlReader(client.OpenRead(url)))
                {
                    _feed = SyndicationFeed.Load(reader);
                    Children = await GetChildren(_feed, providerManager, cancellationToken);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.ErrorException("Error loading feed {0}", e, url);
                ServerEntryPoint.Instance.NotificationManager.SendNotification(new NotificationRequest
                                                                                   {
                                                                                       SendToUserMode = SendToUserType.Admins,
                                                                                       Level = NotificationLevel.Error,
                                                                                       NotificationType = "PluginError",
                                                                                       Name = "Podcasts: Error processing feed",
                                                                                       Description = "Could not process feed " + url
                                                                                   }, cancellationToken).ConfigureAwait(false);


            }
        }

        public string ImageUrl {
            get {
                if (_feed == null || _feed.ImageUrl == null) return null;
                return _feed.ImageUrl.AbsoluteUri;
            }
        }

        public string Title {
            get {
                if (_feed == null) return "";
                return _feed.Title.Text;
            }
        }

        public string Description {
            get {
                if (_feed == null) return null;
                return _feed.Description.Text;
            } 
        } 

        private static async Task<IEnumerable<BaseItem>> GetChildren(SyndicationFeed feed, IProviderManager providerManager, CancellationToken cancellationToken) {
            var podcasts = new List<BaseItem>();
            
            if (feed == null) return podcasts;

            Plugin.Logger.Debug("Processing Feed: {0}", feed.Title);

            foreach (var item in feed.Items) {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string path = null;

                    foreach (var link in item.Links.Where(link => link.RelationshipType == "enclosure"))
                    {
                        path = (link.Uri.AbsoluteUri);
                    }

                    if (path == null)
                    {
                        Plugin.Logger.Warn("No path. Skipping...");
                        continue;
                    }

                    var podcast = IsAudioFile(path) ? (BaseItem) new PodCastAudio {DateCreated = item.PublishDate.UtcDateTime, DateModified = item.PublishDate.UtcDateTime, Name = item.Title.Text, DisplayMediaType = "Audio"} :
                                                        new VodCastVideo {DateCreated = item.PublishDate.UtcDateTime, DateModified = item.PublishDate.UtcDateTime, Name = item.Title.Text, DisplayMediaType = "Movie"};

                    Plugin.Logger.Debug("Found podcast: {0}", podcast.Name);

                    podcast.Path = path;
                    podcast.Id = podcast.Path.GetMBId(podcast.GetType());
                    podcasts.Add(podcast);

                    // itunes podcasts sometimes don't have a summary 
                    if (item.Summary != null && item.Summary.Text != null)
                    {
                        podcast.Overview = HttpUtility.HtmlDecode(Regex.Replace(item.Summary.Text, @"<(.|\n)*?>", string.Empty));

                        var match = Regex.Match(item.Summary.Text, @"<img src=[\""\']([^\'\""]+)", RegexOptions.IgnoreCase);
                        if (match.Groups.Count > 1)
                        {
                            // this will get downloaded later if we need it
                            (podcast as IHasRemoteImage).RemoteImagePath = match.Groups[1].Value;
                        }
                    }

                    if (item.PublishDate != null) podcast.PremiereDate = item.PublishDate.DateTime;

                    // Get values of syndication extension elements for a given namespace
                    var iTunesNamespaceUri = "http://www.itunes.com/dtds/podcast-1.0.dtd";
                    var yahooNamespaceUri = "http://search.yahoo.com/mrss/";
                    var iTunesExt = item.ElementExtensions.FirstOrDefault(x => x.OuterNamespace == iTunesNamespaceUri);
                    var yahooExt = item.ElementExtensions.FirstOrDefault(x => x.OuterNamespace == yahooNamespaceUri);
                    var iTunesNavigator = iTunesExt != null ? new XPathDocument(iTunesExt.GetReader()).CreateNavigator() : null;
                    var yahooNavigator = yahooExt != null ? new XPathDocument(yahooExt.GetReader()).CreateNavigator() : null;

                    var iTunesResolver = iTunesNavigator != null ? new XmlNamespaceManager(iTunesNavigator.NameTable) : null;
                    var yahooResolver = yahooNavigator != null ? new XmlNamespaceManager(yahooNavigator.NameTable) : null;
                    if (iTunesResolver != null ) iTunesResolver.AddNamespace("itunes", iTunesNamespaceUri);
                    if (yahooResolver != null) yahooResolver.AddNamespace("media", yahooNamespaceUri);

                    // Prefer this image
                    //if (string.IsNullOrEmpty(podcast.PrimaryImagePath))
                    {
                        var thumbNavigator = yahooNavigator != null ? yahooNavigator.SelectSingleNode("media:thumbnail", yahooResolver) : null;
                        if (thumbNavigator == null && yahooNavigator != null)
                        {
                            // Some feeds bury them all inside a content element - try that
                            var contentNavigator = yahooNavigator.SelectSingleNode("media:content", yahooResolver);
                            thumbNavigator = contentNavigator.SelectSingleNode("media:thumbnail", yahooResolver);
                        }

                        var imageUrl = thumbNavigator != null ? thumbNavigator.GetAttribute("url","") : "";
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            // This will get downloaded later if we need it...
                            (podcast as IHasRemoteImage).RemoteImagePath = imageUrl;
                        }
                    }

                    var explicitNavigator = iTunesNavigator != null ? iTunesNavigator.SelectSingleNode("itunes:explicit", iTunesResolver) : null;
                    podcast.OfficialRating = explicitNavigator != null ? explicitNavigator.Value == "no" ? "None" : "R" : null;

                    //if (podcast.Name.Contains("Film")) Debugger.Break();

                    var durationNavigator = iTunesNavigator != null ? iTunesNavigator.SelectSingleNode("itunes:duration", iTunesResolver) : null;

                    var duration = durationNavigator != null ? durationNavigator.Value : String.Empty;
                    if (!string.IsNullOrEmpty(duration))
                    {
                        try
                        {
                            podcast.RunTimeTicks = Convert.ToInt32(duration)*TimeSpan.TicksPerSecond;
                        }
                        catch (Exception)
                        {
                        } // we don't really care
                    }

                }
                catch (Exception e)
                {
                    Plugin.Logger.ErrorException("Error refreshing podcast item ", e);
                }
            }

            // TED Talks appends the same damn string on each title, fix it
            if (podcasts.Count > 5)
            {
                var common = podcasts[0].Name;

                foreach (var video in podcasts.Skip(1))
                {
                    while (!video.Name.StartsWith(common))
                    {
                        if (common.Length < 2)
                        {
                            break;
                        }
                        common = common.Substring(0, common.Length - 1);
                    }

                    if (common.Length < 2)
                    {
                        break;
                    }
                }
                
                if (common.Length > 2)
                {
                    foreach (var video in podcasts.Where(video => video.Name.Length > common.Length))
                    {
                        video.Name = video.Name.Substring(common.Length);
                    }
                }

            }

            return podcasts;
        }

        /// <summary>
        /// The audio file extensions
        /// </summary>
        public static readonly string[] AudioFileExtensions = new[]
            {
                ".mp3",
                ".flac",
                ".wma",
                ".aac",
                ".acc",
                ".m4a",
                ".m4b",
                ".wav",
                ".ape",
                ".ogg",
                ".oga"

            };

        private static readonly Dictionary<string, string> AudioFileExtensionsDictionary = AudioFileExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether [is audio file] [the specified args].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is audio file] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsAudioFile(string path)
        {
            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return AudioFileExtensionsDictionary.ContainsKey(extension);
        }

    }


    /// <summary>
    /// http://stackoverflow.com/questions/210375/problems-reading-rss-with-c-and-net-3-5 workaround datetime issues
    /// </summary>
    public class SyndicationFeedXmlReader : XmlTextReader
    {
        readonly string[] Rss20DateTimeHints = { "pubDate" };
        readonly string[] Atom10DateTimeHints = { "updated", "published", "lastBuildDate" };
        private bool isRss2DateTime = false;
        private bool isAtomDateTime = false;

        public SyndicationFeedXmlReader(Stream stream) : base(stream) { }

        public override bool IsStartElement(string localname, string ns)
        {
            isRss2DateTime = false;
            isAtomDateTime = false;

            if (Rss20DateTimeHints.Contains(localname)) isRss2DateTime = true;
            if (Atom10DateTimeHints.Contains(localname)) isAtomDateTime = true;

            return base.IsStartElement(localname, ns);
        }

        /// <summary>
        /// From Argotic MIT : http://argotic.codeplex.com/releases/view/14436
        /// </summary>
        private static string ReplaceRfc822TimeZoneWithOffset(string value)
        {

            //------------------------------------------------------------
            //	Perform conversion
            //------------------------------------------------------------
            value = value.Trim();
            if (value.EndsWith("UT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+0:00", value.TrimEnd("UT".ToCharArray()));
            }
            else if (value.EndsWith("UTC", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+0:00", value.TrimEnd("UTC".ToCharArray()));
            }
            else if (value.EndsWith("EST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-05:00", value.TrimEnd("EST".ToCharArray()));
            }
            else if (value.EndsWith("EDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-04:00", value.TrimEnd("EDT".ToCharArray()));
            }
            else if (value.EndsWith("CST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-06:00", value.TrimEnd("CST".ToCharArray()));
            }
            else if (value.EndsWith("CDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-05:00", value.TrimEnd("CDT".ToCharArray()));
            }
            else if (value.EndsWith("MST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-07:00", value.TrimEnd("MST".ToCharArray()));
            }
            else if (value.EndsWith("MDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-06:00", value.TrimEnd("MDT".ToCharArray()));
            }
            else if (value.EndsWith("PST", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-08:00", value.TrimEnd("PST".ToCharArray()));
            }
            else if (value.EndsWith("PDT", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-07:00", value.TrimEnd("PDT".ToCharArray()));
            }
            else if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}GMT", value.TrimEnd("Z".ToCharArray()));
            }
            else if (value.EndsWith("A", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-01:00", value.TrimEnd("A".ToCharArray()));
            }
            else if (value.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}-12:00", value.TrimEnd("M".ToCharArray()));
            }
            else if (value.EndsWith("N", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+01:00", value.TrimEnd("N".ToCharArray()));
            }
            else if (value.EndsWith("Y", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(null, "{0}+12:00", value.TrimEnd("Y".ToCharArray()));
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// From Argotic MIT : http://argotic.codeplex.com/releases/view/14436
        /// </summary>
        public static bool TryParseRfc822DateTime(string value, out DateTime result)
        {
            //------------------------------------------------------------
            //	Local members
            //------------------------------------------------------------
            DateTimeFormatInfo dateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] formats = new string[3];

            //------------------------------------------------------------
            //	Define valid RFC-822 formats
            //------------------------------------------------------------
            formats[0] = dateTimeFormat.RFC1123Pattern;
            formats[1] = "ddd',' d MMM yyyy HH:mm:ss zzz";
            formats[2] = "ddd',' dd MMM yyyy HH:mm:ss zzz";

            //------------------------------------------------------------
            //	Validate parameter  
            //------------------------------------------------------------
            if (String.IsNullOrEmpty(value))
            {
                result = DateTime.MinValue;
                return false;
            }

            //------------------------------------------------------------
            //	Perform conversion of RFC-822 formatted date-time string
            //------------------------------------------------------------
            return DateTime.TryParseExact(ReplaceRfc822TimeZoneWithOffset(value), formats, dateTimeFormat, DateTimeStyles.None, out result);
        }


        /// <summary>
        /// From Argotic MIT : http://argotic.codeplex.com/releases/view/14436
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseRfc3339DateTime(string value, out DateTime result)
        {
            //------------------------------------------------------------
            //	Local members
            //------------------------------------------------------------
            DateTimeFormatInfo dateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] formats = new string[15];

            //------------------------------------------------------------
            //	Define valid RFC-3339 formats
            //------------------------------------------------------------
            formats[0] = dateTimeFormat.SortableDateTimePattern;
            formats[1] = dateTimeFormat.UniversalSortableDateTimePattern;
            formats[2] = "yyyy'-'MM'-'dd'T'HH:mm:ss'Z'";
            formats[3] = "yyyy'-'MM'-'dd'T'HH:mm:ss.f'Z'";
            formats[4] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ff'Z'";
            formats[5] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fff'Z'";
            formats[6] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffff'Z'";
            formats[7] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fffff'Z'";
            formats[8] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffffff'Z'";
            formats[9] = "yyyy'-'MM'-'dd'T'HH:mm:sszzz";
            formats[10] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffzzz";
            formats[11] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fffzzz";
            formats[12] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffffzzz";
            formats[13] = "yyyy'-'MM'-'dd'T'HH:mm:ss.fffffzzz";
            formats[14] = "yyyy'-'MM'-'dd'T'HH:mm:ss.ffffffzzz";

            //------------------------------------------------------------
            //	Validate parameter  
            //------------------------------------------------------------
            if (String.IsNullOrEmpty(value))
            {
                result = DateTime.MinValue;
                return false;
            }

            //------------------------------------------------------------
            //	Perform conversion of RFC-3339 formatted date-time string
            //------------------------------------------------------------
            return DateTime.TryParseExact(value, formats, dateTimeFormat, DateTimeStyles.AssumeUniversal, out result);
        }

        public override string ReadString()
        {
            string dateVal = base.ReadString();

            try
            {
                if (isRss2DateTime)
                {
                    MethodInfo objMethod = typeof(Rss20FeedFormatter).GetMethod("DateFromString", BindingFlags.NonPublic | BindingFlags.Static);
                    Debug.Assert(objMethod != null);
                    objMethod.Invoke(null, new object[] { dateVal, this });

                }
                if (isAtomDateTime)
                {
                    MethodInfo objMethod = typeof(Atom10FeedFormatter).GetMethod("DateFromString", BindingFlags.NonPublic | BindingFlags.Instance);
                    Debug.Assert(objMethod != null);
                    objMethod.Invoke(new Atom10FeedFormatter(), new object[] { dateVal, this });
                }
            }
            catch (TargetInvocationException)
            {
                DateTime date;
                // Microsofts parser bailed 
                if (!TryParseRfc3339DateTime(dateVal, out date) && !TryParseRfc822DateTime(dateVal, out date))
                {
                    date = DateTime.UtcNow;
                }

                DateTimeFormatInfo dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
                dateVal = date.ToString(dtfi.RFC1123Pattern, dtfi);
            }

            return dateVal;

        }

    }
}
