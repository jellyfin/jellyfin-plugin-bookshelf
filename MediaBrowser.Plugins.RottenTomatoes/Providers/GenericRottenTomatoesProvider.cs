using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.RottenTomatoes.Providers
{
    public class GenericRottenTomatoesProvider<T>
        where T : BaseItem, new()
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public GenericRottenTomatoesProvider(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<MetadataResult<T>> GetMetadata(ItemLookupInfo itemId, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

            //var tmdbId = itemId.GetProviderId(MetadataProviders.Tmdb);
            //var imdbId = itemId.GetProviderId(MetadataProviders.Imdb);

            //// Don't search for music video id's because it is very easy to misidentify. 
            //if (string.IsNullOrEmpty(tmdbId) && string.IsNullOrEmpty(imdbId) && typeof(T) != typeof(MusicVideo))
            //{
            //    tmdbId = await new MovieDbSearch(_logger, _jsonSerializer)
            //        .FindMovieId(itemId, cancellationToken).ConfigureAwait(false);
            //}

            return result;
        }
    }
}
