using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
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
            var list = new List<RemoteImageInfo>();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var frontCoverImg = doc.DocumentNode.SelectSingleNode("//a[@id='front-cover']/img");
            if (frontCoverImg != null && frontCoverImg.HasAttributes && frontCoverImg.Attributes["src"] != null)
            {
                var url = frontCoverImg.Attributes["src"].Value;
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = url,
                    Type = ImageType.Primary
                });

            }

            var backCoverImg = doc.DocumentNode.SelectSingleNode("//a[@id='back-cover']");
            if (backCoverImg != null && backCoverImg.HasAttributes && backCoverImg.Attributes["href"] != null)
            {
                var url = backCoverImg.Attributes["href"].Value;
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = url,
                    Type = ImageType.BoxRear
                });
            }

            var shots = doc.DocumentNode.SelectNodes("//a[@rel='screenshots']");
            if (shots != null)
            {
                foreach (var shot in shots)
                {
                    if (list.Count < 10 && shot.HasAttributes && shot.Attributes["href"] != null)
                    {
                        var url = shot.Attributes["href"].Value;
                        list.Add(new RemoteImageInfo
                        {
                            ProviderName = Name,
                            Url = url,
                            Type = ImageType.Backdrop
                        });
                    }
                }
            }

            return list;
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
            return item is Movie;
        }
    }
}
