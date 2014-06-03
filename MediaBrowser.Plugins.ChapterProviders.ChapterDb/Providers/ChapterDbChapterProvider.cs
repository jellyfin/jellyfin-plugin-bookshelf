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
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.ChapterProviders.ChapterDb.Domain;

namespace MediaBrowser.Plugins.ChapterProviders.ChapterDb.Providers
{
    public class TagChimpChapterProvider : IChapterProvider
    {
        private const string ApiKey = "ETET7TXFJH45YNYW0I4A";
        private const string BaseUrl = "http://chapterdb.org/chapters/";

        private readonly IHttpClient _httpClient;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(1, 1);

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
            get { throw new NotImplementedException(); }
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
                    VideoContentType.Movie
                };
            }
        }

        private async Task<IEnumerable<RemoteChapterResult>> FetchAsyncInternal(ChapterSearchRequest request,
            CancellationToken cancellationToken)
        {
            var url = BaseUrl + String.Format("search?title={0}", request.Name);

            var options = new HttpRequestOptions
            {
                Url = url,
                ResourcePool = _resourcePool,
                CancellationToken = cancellationToken,
            };

            options.RequestHeaders.Add("apiKey", ApiKey);

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var result = _xmlSerializer.DeserializeFromStream(typeof (Results), stream) as Results;
                if (result == null) return new List<RemoteChapterResult>();

                int i = 0;
                return result.Detail.Chapters.Select(x => new RemoteChapterResult
                {
                    Id = (i++).ToString(),
                    Name = x.Name,
                    RunTimeTicks = x.Time.Ticks
                });
            }
        }
    }
}