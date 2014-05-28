using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MediaBrowser.Plugins.Revision3
{
    public class Revision3Channel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public Revision3Channel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "3";
            }
        }

        public string HomePageUrl
        {
            get { return "http://revision3.com"; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, User user, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<ChannelItemInfo> items;

            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                items = await GetChannels(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                items = await GetEpisodes(query, cancellationToken).ConfigureAwait(false);
            }

            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannels(CancellationToken cancellationToken)
        {
            var downloader = new Revision3ChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetRevision3ChannelList(cancellationToken);

            return channels.shows.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Folder,
                ImageUrl = i.images.logo_200,
                Name = i.name,
                Id = i.id
            });
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetEpisodes(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new Revision3ListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetEpisodeList(query, cancellationToken)
                .ConfigureAwait(false);

            return videos.episodes.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.images.medium,
                MediaType = ChannelMediaType.Video,
                Name = i.name,
                Type = ChannelItemType.Media,
                Id = i.slug
            });
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            using (
                var stream =
                    await
                        _httpClient.Get("http://revision3.com/" + id, cancellationToken)
                            .ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);

                    var HD = Regex.Match(html, "value=\"(?<url>.*)\">MP4: HD", RegexOptions.IgnoreCase);
                    var Large = Regex.Match(html, "value=\"(?<url>.*)\">MP4: Large", RegexOptions.IgnoreCase);
                    var Phone = Regex.Match(html, "value=\"(?<url>.*)\">MP4: Phone", RegexOptions.IgnoreCase);
                    var webHD = Regex.Match(html, "value=\"(?<url>.*)\">WebM: HD", RegexOptions.IgnoreCase);
                    var webLarge = Regex.Match(html, "value=\"(?<url>.*)\">WebM: Large", RegexOptions.IgnoreCase);
                    var webPhone = Regex.Match(html, "value=\"(?<url>.*)\">WebM: Phone", RegexOptions.IgnoreCase);

                    var video = new List<ChannelMediaInfo>();

                    if (HD.Success)
                    {
                        var url = HD.Groups["url"].Value;
                        video.Add(new ChannelMediaInfo { Path = url });
                    }
                    if (Large.Success)
                    {
                        var url = Large.Groups["url"].Value;
                        video.Add(new ChannelMediaInfo { Path = url });
                    }
                    if (Phone.Success)
                    {
                        var url = Phone.Groups["url"].Value;
                        video.Add(new ChannelMediaInfo { Path = url });
                    }
                    if (webHD.Success)
                    {
                        var url = webHD.Groups["url"].Value;
                        video.Add(new ChannelMediaInfo { Path = url });
                    }
                    if (webLarge.Success)
                    {
                        var url = webLarge.Groups["url"].Value;
                        video.Add(new ChannelMediaInfo { Path = url });
                    }
                    if (webPhone.Success)
                    {
                        var url = webPhone.Groups["url"].Value;
                        video.Add(new ChannelMediaInfo { Path = url });
                    }

                    return video;
                }
            }
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop
            };
        }

        public string Name
        {
            get { return "Revision 3"; }
        }



        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                CanSearch = false,

                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.Clip
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Video
                  }
            };
        }

        public bool IsEnabledFor(User user)
        {
            return true;
        }

        internal static string Escape(string text)
        {
            var array = new[]
	            {
		            '[',
		            '\\',
		            '^',
		            '$',
		            '.',
		            '|',
		            '?',
		            '*',
		            '+',
		            '(',
		            ')'
	            };

            var stringBuilder = new StringBuilder();
            var i = 0;
            var length = text.Length;

            while (i < length)
            {
                var character = text[i];

                if (Array.IndexOf(array, character) != -1)
                {
                    stringBuilder.Append("\\" + character.ToString());
                }
                else
                {
                    stringBuilder.Append(character);
                }
                i++;
            }
            return stringBuilder.ToString();
        }
    }
}
