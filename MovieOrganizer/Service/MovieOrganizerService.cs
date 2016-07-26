using CommonIO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MovieOrganizer.Service
{
    public class MovieOrganizerService
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IServerManager _serverManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileOrganizationService _fileOrganizationService;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataViewerService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="libraryMonitor">The directory watchers.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="fileSystem">The file system.</param>
        public MovieOrganizerService(
            IServerConfigurationManager config, 
            ILogManager logManager, 
            IFileSystem fileSystem, 
            IServerApplicationHost appHost,
            ILibraryMonitor libraryMonitor,
            ILibraryManager libraryManager,
            ILocalizationManager localizationManager,
            IServerManager serverManager,
            IProviderManager providerManager,
            IFileOrganizationService fileOrganizationService)
        {
            _logger = logManager.GetLogger("MovieOrganizer");
            _config = config;
            _fileSystem = fileSystem;
            _libraryMonitor = libraryMonitor;
            _libraryManager = libraryManager;
            _localizationManager = localizationManager;
            _serverManager = serverManager;
            _providerManager = providerManager;
            _fileOrganizationService = fileOrganizationService;
        }

        public async Task PerformMovieOrganization(MovieFileOrganizationRequest request)
        {
            // The OrganizeWithCorrection function is not purely async. To workaround this, use .Yield() to immediately return to the caller
            //await Task.Yield();

            var organizer = new MovieFileOrganizer(_fileOrganizationService, _config, _fileSystem, _logger, _libraryManager,
                _libraryMonitor, _providerManager, _serverManager, _localizationManager);

            await organizer.OrganizeWithCorrection(request, GetAutoOrganizeOptions(), CancellationToken.None).ConfigureAwait(false); ;
        }

        private AutoOrganizeOptions GetAutoOrganizeOptions()
        {
            return (AutoOrganizeOptions)_config.GetConfiguration("autoorganize");
        }

    }
}
