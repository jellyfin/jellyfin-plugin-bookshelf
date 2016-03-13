using CommonIO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MetadataViewer.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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

        public Task<MetadataRawTable> GetMetadataRaw(IHasMetadata item, CancellationToken cancellationToken)
        {
            var table = new MetadataRawTable();

            var service = _metadataServices.FirstOrDefault(i => i.CanRefresh(item));

            if (service == null)
            {
                _logger.Error("Unable to find a metadata service for item of type " + item.GetType().Name);
                return null;
            }

            return this.GetMetadataRaw(item, service, new MetadataRefreshOptions(_fileSystem), cancellationToken);
        }

        public async Task<MetadataRawTable> GetMetadataRaw(IHasMetadata item, IMetadataService service, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            List<string> ignoreProperties = new List<string>(new[] { "LocalAlternateVersions", "LinkedAlternateVersions", "IsThemeMedia", 
                "SupportsAddingToPlaylist", "AlwaysScanInternalMetadataPath", "ProviderIds", "IsFolder", "IsTopParent", "SupportsAncestors",
                "ParentId", "Parents", "PhysicalLocations", "LockedFields", "IsLocked", "DisplayPreferencesId", "Id", "ImageInfos", "SubtitleFiles", 
                "HasSubtitles", "IsPlaceHolder", "IsShortcut", "SupportsRemoteImageDownloading", "AdditionalParts", "IsStacked", "HasLocalAlternateVersions", 
                "IsArchive", "IsOffline", "IsHidden", "IsOwnedItem", "MediaSourceCount", "VideoType", "PlayableStreamFileNames", "Is3D", 
                "IsInMixedFolder", "SupportsLocalMetadata", "IndexByOptionStrings" });

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

            var providers = GetProviders(item).ToList();
            //var remoteProviders = new List<IMetadataProvider>();
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
                            var id = lookupItem.GetLookupInfo();

                            table.Headers.Add(providerName);

                            try
                            {
                                var mi = ifType.GetMethod("GetMetadata");
                                ////var mi = ifType.InvokeMember("GetMetadata", BindingFlags.InvokeMethod, )
                                dynamic task = mi.Invoke(providerCandidate, new object[] { id, cancellationToken });
                                await task;

                                var result = task.Result;

                                ////var result = x.GetMetadata((object)provider, id, cancellationToken);
                                ////var result = await provider.GetMetadata(id, cancellationToken).ConfigureAwait(false);

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
                                ////refreshResult.Failures++;
                                ////refreshResult.ErrorMessage = ex.Message;
                                _logger.ErrorException("Error in {0}", ex, providerCandidate.Name);
                            }

                            if (!resultItems.ContainsKey(providerName))
                            {
                                resultItems.Add(providerName, this.CreateNew(item.GetType()));
                            }
                        }
                    }
                }
            }


            var emptyItem = CreateNew(item.GetType());

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

                if (addRow && !ignoreProperties.Contains(row.Caption))
                {
                    table.Rows.Add(row);
                }
            }

            return table;
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
