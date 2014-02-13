using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.ADEProvider
{
    public class ADERemoteImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;

        public ADERemoteImageProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var adeId = item.GetProviderId("AdultDvdEmpire");

            if (!string.IsNullOrEmpty(adeId))
            {
                using (var stream = await ADEMetadataProvider.Current.GetInfo(adeId, cancellationToken).ConfigureAwait(false))
                {
                    var html = stream.ToStringFromStream();

                    return GetImagesFromHtml(html);
                }

            }

            return new List<RemoteImageInfo>();
        }

        private IEnumerable<RemoteImageInfo> GetImagesFromHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var divNodes = doc.DocumentNode.Descendants("div").ToList();

            var list = new List<RemoteImageInfo>();

            var boxCoverNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["id"] != null && x.Attributes["id"].Value == "Boxcover");
            if (boxCoverNode == null || !boxCoverNode.ChildNodes.Any())
            {
                return list;
            }

            var frontCover = boxCoverNode.ChildNodes[0];
            var frontCoverUrl = GetHref(frontCover);
            if (!string.IsNullOrEmpty(frontCoverUrl))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = frontCoverUrl,
                    Type = ImageType.Primary
                });
            }

            if (boxCoverNode.ChildNodes.Count > 1)
            {
                var backCover = boxCoverNode.ChildNodes[1];
                var backCoverUrl = GetHref(backCover);

                if (!string.IsNullOrEmpty(backCoverUrl))
                {
                    list.Add(new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = backCoverUrl,
                        Type = ImageType.BoxRear
                    });
                }
            }

            return list;
        }

        private string GetHref(HtmlNode frontCover)
        {
            if (frontCover.HasAttributes && frontCover.Attributes["href"] != null)
            {
                return frontCover.Attributes["href"].Value;
            }

            return string.Empty;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = ADEMetadataProvider.Current.ResourcePool
            });
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType> { ImageType.Primary, ImageType.BoxRear };
        }

        public string Name
        {
            get { return "Adult Dvd Empire"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is AdultVideo;
        }
    }
}
