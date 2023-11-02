using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    internal class ComicVineApiKeyProvider : IComicVineApiKeyProvider
    {
        private readonly ILogger<ComicVineApiKeyProvider> _logger;

        public ComicVineApiKeyProvider(ILogger<ComicVineApiKeyProvider> logger)
        {
            _logger = logger;
        }

        public string? GetApiKey()
        {
            var apiKey = Plugin.Instance?.Configuration.ComicVineApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Comic Vine API key is not set.");
                return null;
            }

            return apiKey;
        }
    }
}
