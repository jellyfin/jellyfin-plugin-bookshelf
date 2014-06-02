using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.TuneIn
{
    public class TuneInChannel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public TuneInChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
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
                return "1";
            }
        }

        public bool IsEnabledFor(Controller.Entities.User user)
        {
            return true;
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, Controller.Entities.User user, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {

            if (query.FolderId == null)
            {
                return await GetMainMenu(cancellationToken).ConfigureAwait(false);
            }

            var channelID = query.FolderId.Split('_');

            if (channelID[0] == "subcat")
            {
                query.FolderId = channelID[1];
                return await GetSubMenu(query, cancellationToken).ConfigureAwait(false);
            }

            if (channelID[0] == "subsubcat")
            {
                query.FolderId = channelID[1];
                return await GetSubSubMenu(query, cancellationToken).ConfigureAwait(false);
            }

            if (channelID[0] == "stations")
            {
                query.FolderId = channelID[1];
                return await GetStations(query, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private async Task<ChannelItemResult> GetMainMenu(CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get("http://opml.radiotime.com/Browse.ashx?formats=mp3,aac", CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var body = page.DocumentNode.SelectSingleNode("//body");

                    foreach (var node in body.SelectNodes("./outline"))
                    {
                        items.Add(new ChannelItemInfo
                        {
                            Name = node.Attributes["text"].Value,
                            Id = "subcat_" + node.Attributes["url"].Value,
                            Type = ChannelItemType.Folder,
                        });
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items,
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<ChannelItemResult> GetSubMenu(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var body = page.DocumentNode.SelectSingleNode("//body");

                    foreach (var node in body.SelectNodes("./outline"))
                    {
                        items.Add(new ChannelItemInfo
                        {
                            Name = node.Attributes["text"].Value,
                            Id = "stations_" + node.Attributes["url"].Value,
                            Type = ChannelItemType.Folder,
                        });
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items,
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<ChannelItemResult> GetSubSubMenu(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var body = page.DocumentNode.SelectSingleNode("//body");

                    foreach (var node in body.SelectNodes("./outline"))
                    {
                        items.Add(new ChannelItemInfo
                        {
                            Name = node.Attributes["text"].Value,
                            Id = "stations_" + node.Attributes["url"].Value,
                            Type = ChannelItemType.Folder,
                        });
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items,
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<ChannelItemResult> GetStations(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (
                var site = await _httpClient.Get(query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var body = page.DocumentNode.SelectSingleNode("//body/outline");

                    foreach (var node in body.SelectNodes("./outline"))
                    {
                        items.Add(new ChannelItemInfo
                        {
                            Name = node.Attributes["text"].Value,
                            Id = "stream_" + node.Attributes["url"].Value,
                            Type = ChannelItemType.Media,
                            ContentType = ChannelMediaContentType.Podcast,
                            ImageUrl = node.Attributes["image"].Value,
                            MediaType = ChannelMediaType.Audio
                        });
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items,
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var channelID = id.Split('_');
            var page = new HtmlDocument();
            var items = new List<ChannelMediaInfo>();

            using (
                var site = await _httpClient.Get(channelID[1] + "&c=ebrowse", CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    var body = page.DocumentNode.SelectSingleNode("//body");

                    foreach (var node in body.SelectNodes("./outline"))
                    {
                        items.Add(new ChannelMediaInfo
                        {
                            Path = node.Attributes["URL"].Value.Replace("&amp;", "&"),
                            AudioBitrate = Convert.ToInt16(node.Attributes["bitrate"].Value)
                        });
                    }
                }
            }

            return items;
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
            get { return "TuneIn"; }
        }



        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                CanSearch = false,

                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.Song
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Audio
                  },
            };
        }

        public string HomePageUrl
        {
            get { return "http://www.tunein.com/"; }
        }




        public Task<ChannelItemResult> GetAllMedia(InternalAllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
