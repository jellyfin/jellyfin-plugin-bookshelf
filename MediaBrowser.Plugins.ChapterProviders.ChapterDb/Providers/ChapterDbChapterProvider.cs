using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Chapters;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.ChapterProviders.ChapterDb.Domain;

namespace MediaBrowser.Plugins.ChapterProviders.ChapterDb.Providers
{
    public class ChapterDbChapterProvider : IChapterProvider
    {
        private const string ApiKey = "ETET7TXFJH45YNYW0I4A";
        private const string BaseUrl = "http://chapterdb.org/chapters/";

        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(1, 1);
        private readonly IHttpClient _httpClient;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly ILogger _logger;

        public ChapterDbChapterProvider(IHttpClient httpClient, IXmlSerializer xmlSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _xmlSerializer = xmlSerializer;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public Task<ChapterResponse> GetChapters(string id, CancellationToken cancellationToken)
        {
            return GetChaptersAsync(id, cancellationToken);
        }

        public string Name
        {
            get { return "ChapterDb"; }
        }

        public Task<IEnumerable<RemoteChapterResult>> Search(ChapterSearchRequest request,
            CancellationToken cancellationToken)
        {
            return SearchAsync(request, cancellationToken);
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

        private async Task<IEnumerable<RemoteChapterResult>> SearchAsync(ChapterSearchRequest request,
            CancellationToken cancellationToken)
        {
            var duration = String.Empty;
            if (request.RuntimeTicks.HasValue)
            {
                duration = GetTimeString(request.RuntimeTicks.Value);
            }

            var url = BaseUrl + String.Format("search?title={0}&duration={1}", request.Name, duration);

            var options = GetHttpRequestOptions(cancellationToken, url);

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var result = _xmlSerializer.DeserializeFromStream(typeof (Results), stream) as Results;
                if (result == null) return new List<RemoteChapterResult>();

                var details = result.Detail;

                var runtime = request.RuntimeTicks;
                details = details
                    .OrderBy(i =>
                    {
                        if (!runtime.HasValue)
                        {
                            return 0;
                        }

                        return Math.Abs(runtime.Value - i.Source.Duration.Ticks);
                    })
                    .ThenBy(i => i.Confirmations)
                    .ToList();

                return details.Select(x =>
                {
                    var c = new RemoteChapterResult
                    {
                        Id = x.Ref.ChapterSetId,
                        Name = x.Title,
                        RunTimeTicks = x.Source.Duration.Ticks
                    };

                    _logger.Debug(string.Format("\"{0}\" results - {1}: {2} [{3}]", request.Name, c.Name, c.Id, GetTimeString(c.RunTimeTicks.Value)));

                    return c;
                });
            }
        }

        private async Task<ChapterResponse> GetChaptersAsync(string id, CancellationToken cancellationToken)
        {
            var url = BaseUrl + String.Format("{0}", id);

            var options = GetHttpRequestOptions(cancellationToken, url);

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false)) {
                var result = _xmlSerializer.DeserializeFromStream(typeof(Detail), stream) as Detail;
                if (result == null) return new ChapterResponse();

                var chapters = result.ChapterCollection.Chapters.Select(x => {
                    var c = new RemoteChapterInfo {
                        Name = x.Name,
                        StartPositionTicks = x.Time.Ticks,
                    };

                    _logger.Debug(string.Format("Chapters - {0} [{1}]", c.Name, GetTimeString(c.StartPositionTicks)));

                    return c;
                }).ToList();

                var response = new ChapterResponse
                {
                    Chapters = chapters,
                };

                return response;
            }
        }

        private HttpRequestOptions GetHttpRequestOptions(CancellationToken cancellationToken, string url)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                ResourcePool = _resourcePool,
                CancellationToken = cancellationToken,
            };

            options.RequestHeaders.Add("apikey", ApiKey);
            return options;
        }

        private string GetTimeString(long ticks)
        {
            return TimeSpan.FromTicks(ticks).ToString(@"hh\:mm\:ss");
        }
    }
}