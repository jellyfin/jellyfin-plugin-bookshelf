using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Linq;
using CommonIO;
using MetadataViewer.Api;
using MetadataViewer.Service;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;

namespace Trakt
{
    /// <summary>
    /// All communication between the server and the plugins server instance should occur in this class.
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private MetadataViewerApi _api;
        private MetadataViewerService _service;

        public static ServerEntryPoint Instance { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonSerializer"></param>
        /// <param name="sessionManager"> </param>
        /// <param name="userDataManager"></param>
        /// <param name="libraryManager"> </param>
        /// <param name="logger"></param>
        /// <param name="httpClient"></param>
        /// <param name="appHost"></param>
        /// <param name="fileSystem"></param>
        public ServerEntryPoint(IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogManager logger, IServerApplicationHost appHost, IFileSystem fileSystem)
        {
            Instance = this;
            _libraryManager = libraryManager;
            _logger = logger.GetLogger("MetadataViewer");
            _appHost = appHost;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;

            _service = new MetadataViewerService(_configurationManager, logger, _fileSystem, _appHost);
            _api = new MetadataViewerApi(logger, _service, _libraryManager);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
            ////_sessionManager.PlaybackStart += KernelPlaybackStart;
            ////_sessionManager.PlaybackStopped += KernelPlaybackStopped;
            ////_libraryManager.ItemAdded += LibraryManagerItemAdded;
            ////_libraryManager.ItemRemoved += LibraryManagerItemRemoved;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            ////_sessionManager.PlaybackStart -= KernelPlaybackStart;
            ////_sessionManager.PlaybackStopped -= KernelPlaybackStopped;
            ////_libraryManager.ItemAdded -= LibraryManagerItemAdded;
            ////_libraryManager.ItemRemoved -= LibraryManagerItemRemoved;
        }
    }
}