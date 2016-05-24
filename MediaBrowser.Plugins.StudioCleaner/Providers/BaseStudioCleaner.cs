using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Plugins.StudioCleaner.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.StudioCleaner.Providers
{
    public class BaseStudioCleaner : ICustomMetadataProvider<Movie>,
        ICustomMetadataProvider<Series>,
        IHasOrder,
        IHasItemChangeMonitor
    {
        protected ILogger Logger { get; set; }
        public BaseStudioCleaner(ILogger logger)
        {
            Logger = logger;
        }

        public async Task<bool> MapStudios(BaseItem item, StudioOptions options, string itemName)
        {
            // if no mappings defined - then don't modify
            if (!options.AllowedStudios.Any()) return false;

            var changed = false;

            // use a dict to elminate duplicates
            var newStudio = new Dictionary<string, string>();
            foreach (var studio in item.Studios)
            {
                // First see if it is an allowed one
                if (options.AllowedStudios.Contains(studio, StringComparer.OrdinalIgnoreCase))
                {
                    newStudio[studio] = studio;
                }
                else
                {
                    string mappedStudio = null;
                    if (options.StudioMappings.TryGetValue(studio, out mappedStudio))
                    {
                        newStudio[mappedStudio] = mappedStudio;
                        Logger.Info("Studio '{0}' mapped to '{2}' for {1}.", studio, itemName, mappedStudio);
                    }
                    else
                    {
                        Logger.Info("Studio '{0}' removed from metadata for {1}.", studio, itemName);
                    }

                    changed = true;
                }

            }

            if (changed) item.Studios = newStudio.Values.ToList();
            return changed;
        }

        public async Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions directoryService, CancellationToken cancellationToken)
        {
            Logger.Debug("StudioCleaner : Fetching Movie Generes");
            return await MapStudios(item, Plugin.Instance.Configuration.MovieOptions, item.Name ?? "<Unknown>").ConfigureAwait(false) ?
                       ItemUpdateType.MetadataEdit : ItemUpdateType.None;
        }

        public async Task<ItemUpdateType> FetchAsync(Series item, MetadataRefreshOptions directoryService, CancellationToken cancellationToken)
        {
            Logger.Debug("StudioCleaner : Fetching TV Generes");
            return await MapStudios(item, Plugin.Instance.Configuration.SeriesOptions, item.Name ?? "<Unknown>").ConfigureAwait(false) ?
                       ItemUpdateType.MetadataEdit : ItemUpdateType.None;
        }

        public string Name { get { return "Studio Cleaner"; } }
        public int Order { get { return 100; } }
        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            if (item is Movie) return Plugin.Instance.Configuration.MovieOptions.LastChange > item.DateLastRefreshed;
            if (item is Series) return Plugin.Instance.Configuration.SeriesOptions.LastChange > item.DateLastRefreshed;
            return false;
        }
    }
}