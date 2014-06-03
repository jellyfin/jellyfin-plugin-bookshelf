using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
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
using PodCasts.Entities;

namespace PodCasts.Tasks
{
    /// <summary>
    /// Downloads trailers from the web at scheduled times
    /// </summary>
    public class PodCastsDownloadTask : IScheduledTask
    {
        /// <summary>
        /// The _HTTP client
        /// </summary>
        private readonly IHttpClient _httpClient;

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
        /// Initializes a new instance of the <see cref="PodCastsDownloadTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public PodCastsDownloadTask(ILibraryManager libraryManager, IProviderManager providerManager, IHttpClient httpClient, ILogger logger)
        {
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
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(23) },

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
            // At start-up we don't have this yet
            while (Plugin.Instance.Registration == null)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!Plugin.Instance.Registration.IsValid)
            {
                Plugin.Logger.Warn("PodCasts trial has expired.");
                throw new ApplicationException("PodCasts Expired.");
            }

            progress.Report(1);

            // Find Podcast folder and grab current children
            var podcastFolder = LibraryManager.RootFolder.Children.OfType<PodCastsCollectionFolder>().FirstOrDefault();
            if (podcastFolder == null)
            {
                throw new ApplicationException("Unable to find PodCast collection folder during update.");
            }

            var currentPodcasts = podcastFolder.Children.Cast<VodCast>().ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var totalFeeds = Plugin.Instance.Configuration.Feeds.Count;
            var currentFeedIds = new List<Guid>();
            var numComplete = 0;

            // Get the feeds to grab
            foreach (var feedUrl in Plugin.Instance.Configuration.Feeds)
            {
                var id = feedUrl.GetMBId(typeof (VodCast));
                currentFeedIds.Add(id);
                var vodCast = currentPodcasts.FirstOrDefault(p => p.Id == id);
                if (vodCast == null)
                {
                    vodCast = new VodCast {Url = feedUrl, Id = id, DisplayMediaType = "Folder"};
                    await podcastFolder.AddChild(vodCast, cancellationToken).ConfigureAwait(false);
                }

                var feed = new RssFeed(feedUrl);
                await feed.Refresh(ProviderManager, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (vodCast.PrimaryImagePath == null && feed.ImageUrl != null)
                {
                    await ProviderManager.SaveImage(vodCast, feed.ImageUrl, Plugin.ResourcePool, ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                }

                vodCast.Overview = feed.Description;
                vodCast.Name = feed.Title;

                await ServerEntryPoint.Instance.ItemRepository.SaveItem(vodCast, cancellationToken).ConfigureAwait(false);

                // Now validate using normal routines and our feed children
                vodCast.NonCachedChildren = feed.Children;
                await vodCast.ValidatePodcastChildren().ConfigureAwait(false);

                // And download any images we need to
                foreach (var child in vodCast.Children.Where(child => string.IsNullOrEmpty(child.PrimaryImagePath)))
                {
                    var podcast = child as IHasRemoteImage;

                    if (podcast != null && podcast.RemoteImagePath != null)
                    {
                        //download remote image
                        await ProviderManager.SaveImage(child, podcast.RemoteImagePath, Plugin.ResourcePool, ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                        await ServerEntryPoint.Instance.ItemRepository.SaveItem(child, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= totalFeeds;

                progress.Report(100*percent);

            }

            // Remove any that aren't there anymore
            foreach (var child in podcastFolder.Children.Where(child => !currentFeedIds.Contains(child.Id)))
            {
                await podcastFolder.RemoveChild(child, cancellationToken).ConfigureAwait(false);
            }

            Plugin.Instance.Configuration.LastFeedUpdate = DateTime.Now;
            Plugin.Instance.SaveConfiguration();
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Refresh podcasts"; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get
            {
                return "Library";
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Downloads the configured Pod Casts."; }
        }
    }
}
