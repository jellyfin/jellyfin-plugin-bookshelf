using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Chapters;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using TagChimp;

namespace MediaBrowser.Plugins.ChapterProviders.TagChimp.Providers
{
    public class TagChimpChapterProvider : IChapterProvider
    {
        private const string ApiKey = "12222029995372A1980D08F";
        private const string BaseUrl = "https://www.tagchimp.com/ape/search.php?";

        private readonly IHttpClient _httpClient;
        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(1, 1);
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IXmlSerializer _xmlSerializer;

        public TagChimpChapterProvider(IHttpClient httpClient, IXmlSerializer xmlSerializer)
        {
            _httpClient = httpClient;
            _xmlSerializer = xmlSerializer;
        }

        public Task<ChapterResponse> GetChapters(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { return "tagChimp"; }
        }

        public Task<IEnumerable<RemoteChapterResult>> Search(ChapterSearchRequest request,
            CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(request, cancellationToken);
        }

        public IEnumerable<VideoContentType> SupportedMediaTypes
        {
            get
            {
                return new List<VideoContentType>
                {
                    VideoContentType.Episode,
                    VideoContentType.Movie
                };
            }
        }

        private async Task<IEnumerable<RemoteChapterResult>> FetchAsyncInternal(ChapterSearchRequest request,
            CancellationToken cancellationToken)
        {
            string imdbIdText = request.GetProviderId(MetadataProviders.Imdb);
            long imdbId = 0;
            long.TryParse(imdbIdText.TrimStart('t'), NumberStyles.Any, _usCulture, out imdbId);

            var p = new SearchParameters
            {
                Type = SearchType.Search,
                Title = request.Name,
                Language = request.Language,
                Show = request.SeriesName,
                Season = request.ParentIndexNumber,
                Episode = request.IndexNumber,
                VideoKind = request.ContentType == VideoContentType.Movie ? "Movie" : "TVShow",
                ImdbId = imdbId > 0 ? imdbId.ToString() : null,
                Token = ApiKey,
            };

            var url = BaseUrl + p.BuildQueryString();

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = _resourcePool,
                CancellationToken = cancellationToken
            }).ConfigureAwait(false))
            {
                var result = _xmlSerializer.DeserializeFromStream(typeof (SearchResults), stream) as SearchResults;
                if (result == null) return new List<RemoteChapterResult>();

                var first = result.Movies.FirstOrDefault();
                if (first != null)
                {
                    return first.Chapters.Items.Select(x => new RemoteChapterResult
                    {
                        Id = x.Index.ToString(),
                        Name = x.Title,
                        RunTimeTicks = x.StartTime.Ticks
                    });
                }
                else
                {
                    return new List<RemoteChapterResult>();
                }
            }
        }
    }
}