using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace MediaBrowser.Plugins.Revision3
{
    public class Revision3ListingDownloader
    {

        private ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public Revision3ListingDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<RootObject> GetEpisodeList(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            RootObject reg;

            using (var json = await _httpClient.Get("http://revision3.com/api/getEpisodes.json?api_key=0b1faede6785d04b78735b139ddf2910f34ad601&show_id="
                + query.FolderId, CancellationToken.None).ConfigureAwait(false))
            {
                reg = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }

            return reg;
        }
       
    }
}
