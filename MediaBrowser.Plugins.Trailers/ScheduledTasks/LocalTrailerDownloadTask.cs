using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Trailers.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.ScheduledTasks
{
    /// <summary>
    /// Class LocalTrailerDownloadTask
    /// </summary>
    class LocalTrailerDownloadTask : IScheduledTask
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        public LocalTrailerDownloadTask(ILibraryManager libraryManager, IHttpClient httpClient, ILogger logger, IJsonSerializer json, ILibraryMonitor libraryMonitor)
        {
            _libraryManager = libraryManager;
            _httpClient = httpClient;
            _logger = logger;
            _json = json;
            _libraryMonitor = libraryMonitor;
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get { return "Trailers"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Downloads local trailers for movies in your library."; }
        }

        /// <summary>
        /// Executes the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!Plugin.Instance.Configuration.EnableLocalTrailerDownloads)
            {
                return;
            }

            var items = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Movie>()
                .Where(i => i.LocationType == LocationType.FileSystem && i.LocalTrailerIds.Count == 0)
                .ToList();

            var numComplete = 0;

            foreach (var item in items)
            {
                try
                {
                    await new LocalTrailerDownloader(_httpClient, _libraryMonitor, _logger, _json).DownloadTrailerForItem(item, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading trailer for {0}", ex, item.Name);
                }

                numComplete++;

                double percent = numComplete;
                percent /= items.Count;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>IEnumerable{ITaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) }
                };
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Download local trailers"; }
        }
    }
}
