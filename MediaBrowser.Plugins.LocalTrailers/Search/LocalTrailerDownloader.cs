using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
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
        private readonly ILogger _logger;

        private readonly IChannelManager _channelManager;
        private readonly ILibraryMonitor _libraryMonitor;

        public LocalTrailerDownloader(ILogger logger, IChannelManager channelManager, ILibraryMonitor libraryMonitor)
        {
            _logger = logger;
            _channelManager = channelManager;
            _libraryMonitor = libraryMonitor;
        }

        /// <summary>
        /// Downloads the trailer for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="providersToMatch">The providers to match.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DownloadTrailerForItem(BaseItem item, ChannelMediaContentType contentType, List<MetadataProviders> providersToMatch, CancellationToken cancellationToken)
        {
            var providerValues = providersToMatch.Select(item.GetProviderId)
                .ToList();

            if (providerValues.All(string.IsNullOrWhiteSpace))
            {
                return;
            }

            var channelTrailers = await _channelManager.GetAllMediaInternal(new AllChannelMediaQuery
            {
                ContentTypes = new[] { contentType },
                ExtraTypes = new[] { ExtraType.Trailer }

            }, CancellationToken.None);

            var channelItem = channelTrailers
                .Items
                .OfType<IChannelMediaItem>()
                .FirstOrDefault(i =>
                {
                    var currentProviderValues = providersToMatch.Select(i.GetProviderId).ToList();

                    var index = 0;
                    foreach (var val in providerValues)
                    {
                        if (!string.IsNullOrWhiteSpace(val) && string.Equals(currentProviderValues[index], val, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                        index++;
                    }

                    return false;
                });

            if (channelItem == null)
            {
                return;
            }

            var destination = Directory.Exists(item.Path) ?
                Path.Combine(item.Path, Path.GetFileNameWithoutExtension(item.Path) + "-trailer") :
                Path.Combine(Path.GetDirectoryName(item.Path), Path.GetFileNameWithoutExtension(item.Path) + "-trailer");

            _libraryMonitor.ReportFileSystemChangeBeginning(Path.GetDirectoryName(destination)); 
            
            try
            {
                await _channelManager.DownloadChannelItem(channelItem, destination, new Progress<double>(), cancellationToken)
                        .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ChannelDownloadException)
            {
                // Logged at lower levels
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error downloading channel content for {0}", ex, item.Name);
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(Path.GetDirectoryName(destination), true);
            }
        }
    }
}
