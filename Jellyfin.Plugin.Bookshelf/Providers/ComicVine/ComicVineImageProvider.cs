using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    //public class ComicVineImageProvider : IRemoteImageProvider
    //{
    //    private readonly IHttpClient _httpClient;

    //    public ComicVineImageProvider(IHttpClient httpClient)
    //    {
    //        _httpClient = httpClient;
    //        _jsonSerializer = jsonSerializer;
    //    }

    //    private readonly CultureInfo _usCulture = new CultureInfo("en-US");

    //    public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasMetadata item, CancellationToken cancellationToken)
    //    {
    //        var volumeId = item.GetProviderId(ComicVineVolumeExternalId.KeyName);

    //        var images = new List<RemoteImageInfo>();

    //        if (!string.IsNullOrEmpty(volumeId))
    //        {
    //            var issueNumber = ComicVineMetadataProvider.GetIssueNumberFromName(item.Name).ToString(_usCulture);

    //            await ComicVineMetadataProvider.Current.EnsureCacheFile(volumeId, issueNumber, cancellationToken).ConfigureAwait(false);

    //            var cachePath = ComicVineMetadataProvider.Current.GetCacheFilePath(volumeId, issueNumber);

    //            try
    //            {
    //                // TODO: use System.Text.Json as JsonSerializer
    //                var issueInfo = _jsonSerializer.DeserializeFromFile<SearchResult>(cachePath);

    //                if (issueInfo.results.Count > 0)
    //                {
    //                    var result = issueInfo.results[0].image;

    //                    if (!string.IsNullOrEmpty(result.medium_url))
    //                    {
    //                        images.Add(new RemoteImageInfo
    //                        {
    //                            Url = result.medium_url,
    //                            ProviderName = Name
    //                        });
    //                    }
    //                }
    //            }
    //            catch (FileNotFoundException)
    //            {
    //            }
    //            catch (DirectoryNotFoundException)
    //            {
    //            }
    //        }

    //        return images;
    //    }

    //    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    //    {
    //        return _httpClient.GetResponse(new HttpRequestOptions
    //        {
    //            CancellationToken = cancellationToken,
    //            Url = url,
    //            ResourcePool = Plugin.Instance.ComicVineSemiphore
    //        });
    //    }

    //    public IEnumerable<ImageType> GetSupportedImages(IHasMetadata item)
    //    {
    //        return new List<ImageType> { ImageType.Primary };
    //    }

    //    public string Name
    //    {
    //        get { return "Comic Vine"; }
    //    }

    //    public bool Supports(IHasMetadata item)
    //    {
    //        return item is Book;
    //    }
    //}
}
