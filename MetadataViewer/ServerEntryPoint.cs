using CommonIO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MetadataViewer;
using MetadataViewer.Api;
using MetadataViewer.Service;
using ServiceStack;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;
using System.IO;
using System.Web;

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
        private readonly IHttpServer _httpServer;
        private readonly IServerConfigurationManager _configurationManager;
        private MetadataViewerApi _api;
        private MetadataViewerService _service;
        private string _htmlPath;

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
        public ServerEntryPoint(IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogManager logger, IServerApplicationHost appHost, IHttpServer httpServer, IFileSystem fileSystem)
        {
            Instance = this;
            _libraryManager = libraryManager;
            _logger = logger.GetLogger("MetadataViewer");
            _appHost = appHost;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _httpServer = httpServer;

            _htmlPath = InstallHelper.GetHtmlPath(_configurationManager.ApplicationPaths.PluginsPath);

            var serviceStackHost = (IAppHost)httpServer;
            serviceStackHost.RawHttpHandlers.Add(ProcessRequestRaw);

            _service = new MetadataViewerService(_configurationManager, logger, _fileSystem, _appHost);
            _api = new MetadataViewerApi(logger, _service, _libraryManager);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
        }

        public virtual IHttpHandler ProcessRequestRaw(IHttpRequest request)
        {
            if (request.PathInfo.Contains("/components/metadataviewer/metadataviewer.js"))
            {
                ////_logger.Info("RawHttpHandlers.ProcessRequestRaw {0}", request.PathInfo);
                var fileName = Path.Combine(_htmlPath, "metadataviewer.js");

                var handler = new CustomActionHandler((httpReq, httpRes) =>
                {
                    httpRes.ContentType = "text/html";
                    httpRes.WriteFile(fileName);
                    httpRes.End();
                });

                return handler;
            }

            if (request.PathInfo.Contains("/components/metadataviewer/metadataviewer.template.html"))
            {
                ////_logger.Info("RawHttpHandlers.ProcessRequestRaw {0}", request.PathInfo);
                var fileName = Path.Combine(_htmlPath, "metadataviewer.template.html");

                var handler = new CustomActionHandler((httpReq, httpRes) =>
                {
                    httpRes.ContentType = "text/html";
                    httpRes.WriteFile(fileName);
                    httpRes.End();
                });

                return handler;
            }

            if (request.PathInfo.Contains("/components/metadataeditor/metadataeditor.js"))
            {
                ////_logger.Info("RawHttpHandlers.ProcessRequestRaw {0}", request.PathInfo);
                var fileName = Path.Combine(_htmlPath, "metadataeditor.js");

                var handler = new CustomActionHandler((httpReq, httpRes) =>
                {
                    httpRes.ContentType = "text/html";
                    httpRes.WriteFile(fileName);
                    httpRes.End();
                });

                return handler;
            }

            ////_logger.Info("RawHttpHandlers.ProcessRequestRaw: {0}", request.PathInfo);

            return null;
        }
    }
}