using CommonIO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MetadataViewer.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MetadataViewer.Service
{
    public class MetadataViewerService
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private IMetadataService[] _metadataServices = { };
        private IMetadataProvider[] _metadataProviders = { };
        private IServerConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataViewerService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="libraryMonitor">The directory watchers.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="fileSystem">The file system.</param>
        public MetadataViewerService(IServerConfigurationManager configurationManager, ILogManager logManager, IFileSystem fileSystem, IServerApplicationHost appHost)
        {
            _logger = logManager.GetLogger("ProviderManager");
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;

            var metadataServices = appHost.GetExports<IMetadataService>();
            var metadataProviders = appHost.GetExports<IMetadataProvider>();

            _metadataServices = metadataServices.OrderBy(i => i.Order).ToArray();
            _metadataProviders = metadataProviders.ToArray();
        }

        public Task<MetadataRawTable> GetMetadataRaw(IHasMetadata item, string language, CancellationToken cancellationToken)
        {
            var table = new MetadataRawTable();

            var service = _metadataServices.FirstOrDefault(i => i.CanRefresh(item));

            if (service == null)
            {
                _logger.Error("Unable to find a metadata service for item of type " + item.GetType().Name);
                return null;
            }

            return this.GetMetadataRaw(item, service, language, cancellationToken);
        }

        public async Task<MetadataRawTable> GetMetadataRaw(IHasMetadata item, IMetadataService service, string language, CancellationToken cancellationToken)
        {
            List<string> ignoreProperties = new List<string>(new[] { "LocalAlternateVersions", "LinkedAlternateVersions", "IsThemeMedia", 
                "SupportsAddingToPlaylist", "AlwaysScanInternalMetadataPath", "ProviderIds", "IsFolder", "IsTopParent", "SupportsAncestors",
                "ParentId", "Parents", "PhysicalLocations", "LockedFields", "IsLocked", "DisplayPreferencesId", "Id", "ImageInfos", "SubtitleFiles", 
                "HasSubtitles", "IsPlaceHolder", "IsShortcut", "SupportsRemoteImageDownloading", "AdditionalParts", "IsStacked", "HasLocalAlternateVersions", 
                "IsArchive", "IsOffline", "IsHidden", "IsOwnedItem", "MediaSourceCount", "VideoType", "PlayableStreamFileNames", "Is3D", 
                "IsInMixedFolder", "SupportsLocalMetadata", "IndexByOptionStrings", "IsInSeasonFolder", "IsMissingEpisode", "IsVirtualUnaired",
                "ContainsEpisodesWithoutSeasonFolders", "IsPhysicalRoot", "IsPreSorted", "DisplaySpecialsWithSeasons", "IsRoot", "IsVirtualFolder", 
                "LinkedChildren", "Children", "RecursiveChildren", "LocationType", "SpecialFeatureIds", "LocalTrailerIds", "RemoteTrailerIds", 
                "RemoteTrailers" });

            var itemOfType = item as BaseItem;
            var lookupItem = item as IHasLookupInfo<ItemLookupInfo>;
            if (itemOfType == null || lookupItem == null)
            {
                _logger.Warn("GetMetadataRaw cannot run for {0}", item);
                return null;
            }

            var serviceType = service.GetType();
            var serviceGenericTypes = serviceType.BaseType.GenericTypeArguments;
            var serviceItemType = serviceGenericTypes[0];
            var serviceIdType = serviceGenericTypes[1];

            var table = new MetadataRawTable();
            var status = new MetadataStatus();
            var logName = item.LocationType == LocationType.Remote ? item.Name ?? item.Path : item.Path ?? item.Name;
            var resultItems = new Dictionary<string, BaseItem>();
            var emptyItem = CreateNew(item.GetType());
            var lookupInfo = lookupItem.GetLookupInfo();

            if (!string.IsNullOrWhiteSpace(language))
            {
                lookupInfo.MetadataLanguage = language;
            }

            var providers = GetProviders(item).ToList();
            var remoteProviders = new List<IRemoteMetadataProvider<BaseItem, ItemLookupInfo>>();

            foreach (var providerCandidate in providers)
            {
                foreach (var ifType in providerCandidate.GetType().GetInterfaces())
                {
                    var providerGenericTypes = ifType.GenericTypeArguments;
                    if (providerGenericTypes.Length == 2)
                    {
                        if (providerGenericTypes[0].Equals(serviceItemType) && providerGenericTypes[1].Equals(serviceIdType))
                        {
                            var providerName = providerCandidate.GetType().Name;
                            _logger.Debug("Running {0} for {1}", providerName, logName);
                            
                            table.Headers.Add(providerName);

                            try
                            {
                                var mi = ifType.GetMethod("GetMetadata");
                                dynamic task = mi.Invoke(providerCandidate, new object[] { lookupInfo, cancellationToken });
                                await task;

                                var result = task.Result;

                                if (result.HasMetadata)
                                {
                                    resultItems.Add(providerName, result.Item);
                                }
                                else
                                {
                                    _logger.Debug("{0} returned no metadata for {1}", providerName, logName);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                _logger.ErrorException("Error in {0}", ex, providerCandidate.Name);
                            }

                            if (!resultItems.ContainsKey(providerName))
                            {
                                resultItems.Add(providerName, emptyItem);
                            }
                        }
                    }
                }
            }

            FillLookupData(lookupInfo, table);

            var propInfos = GetItemProperties(item.GetType());

            foreach (var propInfo in propInfos)
            {
                bool addRow = false;
                var emptyValue = propInfo.GetValue(emptyItem);

                var row = new MetadataRawTable.MetadataRawRow();
                row.Caption = propInfo.Name;

                foreach (var key in resultItems.Keys)
                {
                    var resultItem = resultItems[key];

                    if (resultItem.Equals(emptyItem))
                    {
                        row.Values.Add(null);
                    }
                    else
                    {
                        var value = propInfo.GetValue(resultItem);

                        if (propInfo.PropertyType == typeof(DateTime))
                        {
                            DateTime dateValue = (DateTime)value;

                            row.Values.Add(dateValue.ToShortDateString());
                            if (dateValue != (DateTime)emptyValue)
                            {
                                addRow = true;
                            }
                        }
                        else if (propInfo.PropertyType == typeof(DateTime?))
                        {
                            DateTime? dateValue = (DateTime?)value;

                            if (dateValue.HasValue)
                            {
                                row.Values.Add(dateValue.Value.ToShortDateString());
                                if (dateValue != (DateTime?)emptyValue)
                                {
                                    addRow = true;
                                }
                            }
                            else
                            {
                                row.Values.Add(null);
                            }
                        }
                        else
                        {
                            row.Values.Add(value);
                            if (value != emptyValue)
                            {
                                addRow = true;
                            }
                        }
                    }
                }

                if (addRow && !ignoreProperties.Contains(row.Caption))
                {
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        private void FillLookupData(ItemLookupInfo lookupInfo, MetadataRawTable table)
        {
            var propInfos = GetItemProperties(lookupInfo.GetType());

            var name = lookupInfo.Name;

            if (lookupInfo.Year.HasValue)
            {
                name = string.Format("{0} ({1})", name, lookupInfo.Year.Value);
            }

            table.LookupData.Add(new KeyValuePair<string, object>("Name", name));

            foreach (var propInfo in propInfos)
            {
                switch (propInfo.Name)
                {
                    case "ProviderIds":
                        table.LookupData.Add(new KeyValuePair<string, object>(propInfo.Name, FlattenProviderIds(lookupInfo.ProviderIds)));
                        break;
                    case "SeriesProviderIds":
                        var seasonInfo = lookupInfo as SeasonInfo;
                        if (seasonInfo != null)
                        {
                            table.LookupData.Add(new KeyValuePair<string, object>(propInfo.Name, FlattenProviderIds(seasonInfo.SeriesProviderIds)));
                        }
                        
                        var episodeInfo = lookupInfo as EpisodeInfo;
                        if (episodeInfo != null)
                        {
                            table.LookupData.Add(new KeyValuePair<string, object>(propInfo.Name, FlattenProviderIds(episodeInfo.SeriesProviderIds)));
                        }
                        
                        break;
                    case "Name":
                    case "Year":
                    case "IsAutomated":
                    case "MetadataCountryCode":
                        break;
                    default:
                        var value = propInfo.GetValue(lookupInfo);
                        if (value != null)
                        {
                            if (propInfo.PropertyType == typeof(DateTime?))
                            {
                                DateTime? dateValue = (DateTime?)value;
                                table.LookupData.Add(new KeyValuePair<string, object>(propInfo.Name, dateValue.Value.ToShortDateString()));
                            }
                            else
                            {
                                table.LookupData.Add(new KeyValuePair<string, object>(propInfo.Name, value));
                            }
                        }

                        break;
                }
            }
        }

        private object FlattenProviderIds(Dictionary<string, string> providerIds)
        {
            string result = "";

            foreach (var item in providerIds)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    if (result.Length > 0)
                    {
                        result += ", ";
                    }

                    result += string.Format("{0}:{1}", item.Key, item.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{`0}.</returns>
        protected IEnumerable<IMetadataProvider> GetProviders(IHasMetadata item)
        {
            var options = GetMetadataOptions(item);

            var providers = _metadataProviders.OfType<IMetadataProvider>()
                .Where(i => CanRefresh(i, item, options, true, false))
                .OrderBy(i => GetConfiguredOrder(i, options))
                .ThenBy(GetDefaultOrder);

            return providers;
        }

        private MetadataOptions GetMetadataOptions(IHasImages item)
        {
            var type = item.GetType().Name;

            return _configurationManager.Configuration.MetadataOptions
                .FirstOrDefault(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase)) ??
                new MetadataOptions();
        }

        private bool CanRefresh(IMetadataProvider provider, IHasMetadata item, MetadataOptions options, bool includeDisabled, bool checkIsOwnedItem)
        {
            if (!includeDisabled)
            {
                // If locked only allow local providers
                if (item.IsLocked && !(provider is ILocalMetadataProvider) && !(provider is IForcedProvider))
                {
                    return false;
                }

                if (provider is IRemoteMetadataProvider)
                {
                    if (!item.IsInternetMetadataEnabled())
                    {
                        return false;
                    }

                    if (Array.IndexOf(options.DisabledMetadataFetchers, provider.Name) != -1)
                    {
                        return false;
                    }
                }
            }

            if (!item.SupportsLocalMetadata && provider is ILocalMetadataProvider)
            {
                return false;
            }

            // If this restriction is ever lifted, movie xml providers will have to be updated to prevent owned items like trailers from reading those files
            if (checkIsOwnedItem && item.IsOwnedItem)
            {
                if (provider is ILocalMetadataProvider || provider is IRemoteMetadataProvider)
                {
                    return false;
                }
            }

            return true;
        }

        private int GetConfiguredOrder(IMetadataProvider provider, MetadataOptions options)
        {
            // See if there's a user-defined order
            if (provider is ILocalMetadataProvider)
            {
                var index = Array.IndexOf(options.LocalMetadataReaderOrder, provider.Name);

                if (index != -1)
                {
                    return index;
                }
            }

            // See if there's a user-defined order
            if (provider is IRemoteMetadataProvider)
            {
                var index = Array.IndexOf(options.MetadataFetcherOrder, provider.Name);

                if (index != -1)
                {
                    return index;
                }
            }

            // Not configured. Just return some high number to put it at the end.
            return 100;
        }

        private int GetDefaultOrder(IMetadataProvider provider)
        {
            var hasOrder = provider as IHasOrder;

            if (hasOrder != null)
            {
                return hasOrder.Order;
            }

            return 0;
        }

        private BaseItem CreateNew(Type itemType)
        {
            return Activator.CreateInstance(itemType) as BaseItem;
        }

        private List<PropertyInfo> GetItemProperties(Type itemType)
        {
            var properties = new List<PropertyInfo>();

            var propInfos = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propInfo in propInfos)
            {
                properties.Add(propInfo);
            }

            return properties;
        }

    }
}
