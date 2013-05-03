using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Trailers.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.ScheduledTasks
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
        private ILogger Logger { get; set;}
        /// <summary>
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        private Kernel Kernel { get; set; }
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
        /// <param name="kernel">The kernel.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        public CurrentTrailerDownloadTask(Kernel kernel, ILibraryManager libraryManager, IProviderManager providerManager, IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            Logger = logger;
            Kernel = kernel;
            LibraryManager = libraryManager;
            ProviderManager = providerManager;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new[] { new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) } };
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
            var trailers = await AppleTrailerListingDownloader.GetTrailerList(_httpClient, cancellationToken).ConfigureAwait(false);

            progress.Report(1);

            var trailerFolder = LibraryManager.RootFolder.Children.OfType<TrailerCollectionFolder>().First();

            var trailersToDownload = trailers
                .Where(t => !IsOldTrailer(t.Video))
                .ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var numComplete = 0;

            foreach (var trailer in trailersToDownload)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await DownloadTrailer(trailer, trailerFolder, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error downloading information for {0}", ex, trailer.Video.Name);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= trailersToDownload.Count;

                progress.Report(100 * percent);
            }

            await LibraryManager.SaveChildren(trailerFolder.Id, trailerFolder.Children, cancellationToken).ConfigureAwait(false);
            
            if (Plugin.Instance.Configuration.DeleteOldTrailers)
            {
                // Enforce MaxTrailerAge
                DeleteOldTrailers();
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
            var video = trailer.Video;

            var existing = trailerFolder
                .Children
                .OfType<Trailer>()
                .FirstOrDefault(i => string.Equals(i.Path, trailer.TrailerUrl, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                video = existing;
            }
            else
            {
                video.Path = trailer.TrailerUrl;

                video.Id = video.Path.GetMBId(typeof(Trailer));

                trailerFolder.Children.Add(video);

                await LibraryManager.CreateItem(video, cancellationToken).ConfigureAwait(false);
            }

            // Figure out which image we're going to download
            var imageUrl = trailer.HdImageUrl ?? trailer.ImageUrl;

            if (!video.HasImage(ImageType.Primary) && !string.IsNullOrEmpty(imageUrl))
            {
                var path = await
                    ProviderManager.DownloadAndSaveImage(video, imageUrl, "folder.jpg", false, Plugin.Instance.AppleTrailers, cancellationToken)
                                   .ConfigureAwait(false);

                video.SetImage(ImageType.Primary, path);

                await LibraryManager.UpdateItem(video, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Determines whether [is old trailer] [the specified trailer].
        /// </summary>
        /// <param name="trailer">The trailer.</param>
        /// <returns><c>true</c> if [is old trailer] [the specified trailer]; otherwise, <c>false</c>.</returns>
        private bool IsOldTrailer(Trailer trailer)
        {
            if (!Plugin.Instance.Configuration.MaxTrailerAge.HasValue)
            {
                return false;
            }

            if (!trailer.PremiereDate.HasValue)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            // Not old if it still hasn't premiered.
            if (now < trailer.PremiereDate.Value)
            {
                return false;
            }

            return (DateTime.UtcNow - trailer.PremiereDate.Value).TotalDays >
                   Plugin.Instance.Configuration.MaxTrailerAge.Value;
        }

        /// <summary>
        /// Deletes trailers that are older than the supplied date
        /// </summary>
        private void DeleteOldTrailers()
        {
            var collectionFolder = (Folder)LibraryManager.RootFolder.Children.First(c => c.GetType().Name.Equals(typeof(TrailerCollectionFolder).Name));

            foreach (var trailer in collectionFolder.RecursiveChildren.OfType<Trailer>().Where(IsOldTrailer))
            {
                Logger.Info("Deleting old trailer: " + trailer.Name);

                Directory.Delete(Path.GetDirectoryName(trailer.Path), true);
            }
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Find current trailers"; }
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
            get { return "Searches the web for upcoming movie trailers, and downloads them based on your Trailer plugin settings."; }
        }
    }
}
