using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using RokuMetadata.Drawing;
using ServiceStack;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RokuMetadata.Api
{
    [Route("/Videos/{Id}/index.bif", "GET")]
    public class GetBifFile
    {
        [ApiMember(Name = "MediaSourceId", Description = "The media version id, if playing an alternate version", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "Width", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int Width { get; set; }

        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    public class BifService : IRestfulService, IHasResultFactory
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public IHttpResultFactory ResultFactory { get; set; }

        public ServiceStack.Web.IRequest Request { get; set; }

        public BifService(ILibraryManager libraryManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILogger logger)
        {
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<object> Get(GetBifFile request)
        {
            var item = _libraryManager.GetItemById(request.Id);
            var mediaSource =
                ((IHasMediaSources)item).GetMediaSources(false)
                    .FirstOrDefault(i => string.Equals(i.Id, request.MediaSourceId));

            var path = VideoProcessor.GetBifPath(item, mediaSource.Id, request.Width);

            _logger.Info("Looking for bif file: {0}", path);

            if (!File.Exists(path))
            {
                path = await new VideoProcessor(_logger, _mediaEncoder, _fileSystem).GetEmptyBif().ConfigureAwait(false);
            }

            _logger.Info("Returning bif file: {0}", path);
            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                ContentType = "application/octet-stream",
                Path = path
            });
        }
    }
}
