using System;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.API;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Vimeo
{
    class VimeoCategoryDownloader
    {
        private readonly ILogger _logger;
        private IHttpClient _httpClient;
        private IJsonSerializer _jsonSerializer;

        public VimeoCategoryDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<Categories> GetVimeoCategoryList(int? startIndex, int? limit, CancellationToken cancellationToken)
        {
            int? page = null;

            if (startIndex.HasValue && limit.HasValue)
            {
                page = 1 + (startIndex.Value / limit.Value) % limit.Value;
            }

            return Plugin.vc.vimeo_categories_getAll(page: page, per_page: limit);
        }

        public async Task<Category> GetVimeoSubCategory(String catID, CancellationToken cancellationToken)
        {
            return Plugin.vc.vimeo_categories_getInfo(catID);
        }

    }
}
