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

        public virtual HttpAsyncTaskHandler ProcessRequestRaw(IHttpRequest request)
        {
            MemoryStream resultStream = null;

            if (request.PathInfo.Contains("/components/metadataviewer/metadataviewer.js"))
            {
                resultStream = HtmlHelper.ViewerScript;
            }
            else if (request.PathInfo.Contains("/components/metadataviewer/metadataviewer.template.html"))
            {
                resultStream = HtmlHelper.ViewerTemplate;
            }
            else if (request.PathInfo.Contains("/bower_components/emby-webcomponents/itemcontextmenu.js"))
            {
                resultStream = HtmlHelper.ModifiedContextMenu;
            }

            if (resultStream != null)
            {
                var handler = new CustomActionHandler((httpReq, httpRes) =>
                {
                    httpRes.ContentType = "text/html";

                    lock (resultStream)
                    {
                        resultStream.Seek(0, SeekOrigin.Begin);
                        resultStream.WriteTo(httpRes.OutputStream);
                        httpRes.EndRequest();
                    }

                    httpRes.End();
                });

                return handler;
            }

            return null;
        }
    }
}