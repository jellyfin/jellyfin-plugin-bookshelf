using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace MediaBrowser.Plugins.UStream
{
    public class UStreamListingDownloader
    {

        private ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public UStreamListingDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<RootObject> GetEpisodeList(int offset, InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            using (var json = await _httpClient.Get(
                String.Format("http://revision3.com/api/getEpisodes.json?api_key=0b1faede6785d04b78735b139ddf2910f34ad601&show_id={0}&offset={1}&limit={2}",
                query.FolderId, offset, query.Limit), CancellationToken.None).ConfigureAwait(false))
            {
                return _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }
        }
       
    }
}
