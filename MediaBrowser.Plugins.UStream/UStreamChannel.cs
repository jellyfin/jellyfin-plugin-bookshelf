using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
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
using HtmlAgilityPack;

namespace MediaBrowser.Plugins.UStream
{
    public class UStreamChannel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public UStreamChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
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
            get { return "http://ustream.tv"; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, User user, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            ChannelItemResult result;

            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                return await GetCategories(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var folderID = query.FolderId.Split('_');
                query.FolderId = folderID[1];

                if (folderID[0] == "subcat")
                {
                    return await GetSubCategories(query, cancellationToken).ConfigureAwait(false);
                }
                if (folderID[0] == "streams")
                {
                    query.FolderId = folderID[2];
                    return await GetStreams(folderID[1], query, cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        private async Task<ChannelItemResult> GetCategories(CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            
            using (
                var site = await _httpClient.Get("http://www.ustream.tv/", CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                if (page.DocumentNode != null)
                {
                    foreach (var node in page.DocumentNode.SelectNodes("//ul[contains(@class, \"unstyled\")]/li[contains(@class, \"state\")]//a"))
                    {
                        HtmlAttribute link = node.Attributes["href"];
                        items.Add(new ChannelItemInfo
                        {
                            Name = node.InnerText,
                            Id = "subcat_" + link.Value.Split('/').Last(),
                            Type = ChannelItemType.Folder,
                        });
                    }
                }
            }


            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<ChannelItemResult> GetSubCategories(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (var site = await _httpClient.Get(String.Format("http://www.ustream.tv/new/explore/{0}/all", query.FolderId), CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                foreach (var node in page.DocumentNode.SelectNodes("//select[@id=\"FilterSubCategory\"]/option"))
                {
                    HtmlAttribute link = node.Attributes["value"];
                    
                    if (link.Value == "") continue;

                    _logger.Debug("PASSED");
                    items.Add(new ChannelItemInfo
                    {
                        Name = node.InnerText,
                        Id = "streams_" + query.FolderId + "_" + link.Value,
                        Type = ChannelItemType.Folder,
                    });
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<ChannelItemResult> GetStreams(string mainCategory, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();
            var items = new List<ChannelItemInfo>();

            using (var json = await _httpClient.Get(String.Format("http://www.ustream.tv/ajax-alwayscache/new/explore/{0}/all.json?subCategory={1}&type=live&location=anywhere&page={2}", mainCategory, query.FolderId, 1), CancellationToken.None).ConfigureAwait(false))
            {
                var reg = _jsonSerializer.DeserializeFromStream<RootObject>(json);
                
                page.LoadHtml(reg.pageContent);
                
                foreach (var node in page.DocumentNode.SelectNodes("//div[contains(@class, \"media-item\")]"))
                {
                    var url = node.SelectSingleNode(".//img/parent::a").Attributes["href"].Value;
                    var title = node.SelectSingleNode(".//h4/a/text()").InnerText;
                    var thumb = node.SelectSingleNode(".//img").Attributes["src"].Value;

                    items.Add(new ChannelItemInfo
                    {
                        Name = title,
                        ImageUrl = thumb,
                        Id = "stream_" + url,
                        Type = ChannelItemType.Media,
                        ContentType = ChannelMediaContentType.Clip,
                        IsInfiniteStream = true,
                        MediaType = ChannelMediaType.Video,
                    });
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            var page = new HtmlDocument();

            var channel = id.Split('_');

            using (var site = await _httpClient.Get("http://www.ustream.tv" + channel[1], CancellationToken.None).ConfigureAwait(false))
            {
                page.Load(site, Encoding.UTF8);
                var node =
                    page.DocumentNode.SelectSingleNode("//a[@data-content-id]").Attributes["data-content-id"].Value;

                return new List<ChannelMediaInfo>
                {
                    new ChannelMediaInfo
                    {
                        Path = "http://iphone-streaming.ustream.tv/uhls/"+node+"/streams/live/iphone/playlist.m3u8?appType=11&appVersion=2"
                    }

                };
               
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
            get { return "UStream"; }
        }



        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                CanSearch = false,
                MaxPageSize = 25,
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Clip
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },

                SupportsSortOrderToggle = true,

                DefaultSortFields = new List<ChannelItemSortField>
                {
                    ChannelItemSortField.DateCreated,
                    ChannelItemSortField.Name,
                    ChannelItemSortField.Runtime
                },
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

        public IOrderedEnumerable<ChannelItemInfo> OrderItems(List<ChannelItemInfo> items, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (query.SortBy.HasValue)
            {
                if (query.SortDescending)
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            return items.OrderByDescending(i => i.RunTimeTicks ?? 0);
                        case ChannelItemSortField.DateCreated:
                            return items.OrderByDescending(i => i.DateCreated ?? DateTime.MinValue);
                        default:
                            return items.OrderByDescending(i => i.Name);
                    }
                }
                else
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            return items.OrderBy(i => i.RunTimeTicks ?? 0);
                        case ChannelItemSortField.DateCreated:
                            return items.OrderBy(i => i.DateCreated ?? DateTime.MinValue);
                        default:
                            return items.OrderBy(i => i.Name);
                    }
                }
            }

            return items.OrderBy(i => i.Name);
        }

        
    }
}
