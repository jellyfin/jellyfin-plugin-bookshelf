using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.LocalTrailers.Search
{
    public class LocalTrailerDownloader
    {
        private readonly IHttpClient _httpClient;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        public LocalTrailerDownloader(IHttpClient httpClient, ILibraryMonitor libraryMonitor, ILogger logger, IJsonSerializer json)
        {
            _httpClient = httpClient;
            _libraryMonitor = libraryMonitor;
            _logger = logger;
            _json = json;
        }

        /// <summary>
        /// Downloads the trailer for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DownloadTrailerForItem(BaseItem item, CancellationToken cancellationToken)
        {
            var urls = await GetTrailerUrls(item, cancellationToken).ConfigureAwait(false);

            await DownloadTrailerForItem(item, urls, cancellationToken).ConfigureAwait(false);
        }

        private async Task DownloadTrailerForItem(BaseItem item, IEnumerable<string> urls, CancellationToken cancellationToken)
        {
            foreach (var url in urls
                .Where(i => !string.IsNullOrWhiteSpace(i)))
            {
                try
                {
                    await DownloadTrailerForItem(item, url, cancellationToken).ConfigureAwait(false);
                }
                catch (ArgumentException)
                {

                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading trailer", ex);
                }
            }
        }

        private async Task DownloadTrailerForItem(BaseItem item, string url, CancellationToken cancellationToken)
        {
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
                DeleteTempFile(responseInfo);
                throw new ArgumentException();
            }

            _libraryMonitor.ReportFileSystemChangeBeginning(savePath);

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
            catch
            {
                DeleteTempFile(responseInfo);

                throw;
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(savePath, true);
            }
        }

        private void DeleteTempFile(HttpResponseInfo response)
        {
            try
            {
                File.Delete(response.TempFilePath);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error deleting temp file {0}", ex, response.TempFilePath);
            }
        }

        /// <summary>
        /// Gets the trailer URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<List<string>> GetTrailerUrls(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<string>();

            list.AddRange(await new MovieListSearch(_httpClient, _json).Search(item, cancellationToken).ConfigureAwait(false));
            list.AddRange(await new HdNetTrailerSearch(_httpClient).Search(item, cancellationToken).ConfigureAwait(false));

            return list;
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
