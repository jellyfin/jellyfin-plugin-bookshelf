using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MetadataViewer.DTO;
using MetadataViewer.Service;
using ServiceStack;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace MetadataViewer.Api
{
    [Route("/Items/{ItemId}/MetadataRaw", "GET", Summary = "Gets raw metadata for an item")]
    public class GetMetadataRaw : IReturn<MetadataRawTable>
    {
        [ApiMember(Name = "ItemId", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string ItemId { get; set; }
    }

    [Authenticated]
    public class MetadataViewerApi : IRestfulService
    {
        private readonly ILogger _logger;
        private readonly MetadataViewerService _service;
        private readonly ILibraryManager _libraryManager;

        public MetadataViewerApi(ILogManager logManager, MetadataViewerService service, ILibraryManager libraryManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _service = service;
            _libraryManager = libraryManager;
        }

        public object Get(GetMetadataRaw request)
        {
            var item = _libraryManager.GetItemById(request.ItemId);

            return _service.GetMetadataRaw(item, CancellationToken.None);
        }
    }
}
