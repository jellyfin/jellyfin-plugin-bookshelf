using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Search
{
    public class LocalTrailerDownloader
    {
        private readonly IHttpClient _httpClient;
        private readonly IDirectoryWatchers _directoryWatchers;
        private readonly ILogger _logger;

        public LocalTrailerDownloader(IHttpClient httpClient, IDirectoryWatchers directoryWatchers, ILogger logger)
        {
            _httpClient = httpClient;
            _directoryWatchers = directoryWatchers;
            _logger = logger;
        }

        /// <summary>
        /// Downloads the trailer for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DownloadTrailerForItem(BaseItem item, CancellationToken cancellationToken)
        {
            var url = await GetTrailerUrl(item, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            var responseInfo = await _httpClient.GetTempFileResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                Progress = new Progress<double>(),
                UserAgent = GetUserAgent(url)
            });

            var extension = responseInfo.ContentType.Split('/').Last();

            if (string.Equals("quicktime", extension, StringComparison.OrdinalIgnoreCase))
            {
                extension = "mov";
            }

            var savePath = Directory.Exists(item.Path) ?
                Path.Combine(item.Path, Path.GetFileNameWithoutExtension(item.Path) + "-trailer." + extension) :
                Path.Combine(Path.GetDirectoryName(item.Path), Path.GetFileNameWithoutExtension(item.Path) + "-trailer." + extension);

            if (!EntityResolutionHelper.IsVideoFile(savePath))
            {
                _logger.Warn("Unrecognized video extension {0}", savePath);
                return;
            }

            _directoryWatchers.TemporarilyIgnore(savePath);

            _logger.Info("Moving {0} to {1}", responseInfo.TempFilePath, savePath);

            try
            {
                var parentPath = Path.GetDirectoryName(savePath);

                if (!Directory.Exists(parentPath))
                {
                    Directory.CreateDirectory(parentPath);
                }

                File.Move(responseInfo.TempFilePath, savePath);
            }
            finally
            {
                _directoryWatchers.RemoveTempIgnore(savePath);
            }

            await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the trailer URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetTrailerUrl(BaseItem item, CancellationToken cancellationToken)
        {
            var url = await new HdNetTrailerSearch(_httpClient).Search(item, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(url))
            {
                _logger.Info("Found trailer {0} for {1}", url, item.Name);
            }

            return url;
        }

        /// <summary>
        /// Gets the user agent.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        private string GetUserAgent(string url)
        {
            if (url.IndexOf("apple.com", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return "QuickTime/7.6.2";
            }

            return "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.22 Safari/537.36";
        }
    }
}
