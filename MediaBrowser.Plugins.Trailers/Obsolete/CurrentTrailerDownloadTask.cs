using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Obsolete
{
    /// <summary>
    /// Downloads trailers from the web at scheduled times
    /// </summary>
    public class CurrentTrailerDownloadTask : IScheduledTask
    {
        /// <summary>
        /// The _HTTP client
        /// </summary>
        private readonly IHttpClient _httpClient;

        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }
        /// <summary>
        /// Gets or sets the library manager.
        /// </summary>
        /// <value>The library manager.</value>
        private ILibraryManager LibraryManager { get; set; }

        /// <summary>
        /// Gets or sets the provider manager.
        /// </summary>
        /// <value>The provider manager.</value>
        private IProviderManager ProviderManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentTrailerDownloadTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        public CurrentTrailerDownloadTask(ILibraryManager libraryManager, IProviderManager providerManager, IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            Logger = logger;
            LibraryManager = libraryManager;
            ProviderManager = providerManager;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) },

                    new IntervalTrigger { Interval = TimeSpan.FromHours(6) }
                };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // Get the list of trailers
            var trailers = await AppleTrailerListingDownloader.GetTrailerList(_httpClient, 
                Logger,
                true,
                cancellationToken).ConfigureAwait(false);

            progress.Report(1);

            var now = DateTime.UtcNow;

            var trailersToDownload = trailers
                .Where(trailer =>
                {
                    if (!Plugin.Instance.Configuration.MaxTrailerAge.HasValue)
                    {
                        return true;
                    }

                    // Not old if it still hasn't premiered.
                    if (!trailer.PremiereDate.HasValue || now < trailer.PremiereDate.Value)
                    {
                        return true;
                    }

                    return (now - trailer.PremiereDate.Value).TotalDays <=
                           Plugin.Instance.Configuration.MaxTrailerAge.Value;

                })
                .ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var numComplete = 0;

            var trailerFolder = LibraryManager.RootFolder.Children.OfType<TrailerCollectionFolder>().FirstOrDefault();

            if (trailerFolder == null)
            {
                return;
            }

            foreach (var trailer in trailersToDownload)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await DownloadTrailer(trailer, trailerFolder, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error downloading information for {0}", ex, trailer.Name);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= trailersToDownload.Count;

                progress.Report(1 + 69 * percent);
            }

            progress.Report(72);

            numComplete = 0;
            var children = trailerFolder.Children.ToList();

            foreach (var trailer in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await trailer.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error refreshing metadata for {0}", ex, trailer.Name);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= children.Count;

                progress.Report((28 * percent) + 72);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Downloads the trailer.
        /// </summary>
        /// <param name="trailer">The trailer.</param>
        /// <param name="trailerFolder">The trailer folder.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task DownloadTrailer(TrailerInfo trailer, Folder trailerFolder, CancellationToken cancellationToken)
        {
            var video = new Trailer
            {
                Path = trailer.TrailerUrl,
                CommunityRating = trailer.CommunityRating,
                Genres = trailer.Genres,
                IsHD = true,
                Name = trailer.Name,
                OfficialRating = trailer.OfficialRating,
                Overview = trailer.Overview,
                People = trailer.People,
                PremiereDate = trailer.PremiereDate,
                ProductionYear = trailer.ProductionYear,
                Studios = trailer.Studios
            };

            var existing = trailerFolder
                .Children
                .OfType<Trailer>()
                .FirstOrDefault(i => string.Equals(i.Path, trailer.TrailerUrl, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Name, trailer.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                video = existing;
            }
            else
            {
                video.Path = trailer.TrailerUrl;

                await trailerFolder.AddChild(video, cancellationToken).ConfigureAwait(false);
            }

            // Figure out which image we're going to download
            var imageUrl = trailer.HdImageUrl ?? trailer.ImageUrl;

            if (!video.HasImage(ImageType.Primary) && !string.IsNullOrEmpty(imageUrl))
            {
                await ProviderManager.SaveImage(video, imageUrl, Plugin.Instance.AppleTrailers, ImageType.Primary, null, cancellationToken).ConfigureAwait(false);

                await LibraryManager.UpdateItem(video, ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Refresh trailers"; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get
            {
                return "Trailers";
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Searches the web for upcoming movie trailers."; }
        }
    }
}