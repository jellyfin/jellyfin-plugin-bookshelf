using System.Net;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine;
using Jellyfin.Plugin.Bookshelf.Tests.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class ComicVineImageProviderTests
    {
        [Fact]
        public async Task GetImages_WithAllLinks_ReturnsLargest()
        {
            var mockApiKeyProvider = Substitute.For<IComicVineApiKeyProvider>();
            mockApiKeyProvider.GetApiKey().Returns(Guid.NewGuid().ToString());

            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("issue/4000-441467", StringComparison.Ordinal), new MockHttpResponse(HttpStatusCode.OK, TestHelpers.GetFixture("comic-vine-single-issue.json")))
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteImageProvider provider = new ComicVineImageProvider(Substitute.For<IComicVineMetadataCacheManager>(), NullLogger<ComicVineImageProvider>.Instance, mockedHttpClientFactory, mockApiKeyProvider);

            var images = await provider.GetImages(new Book()
            {
                ProviderIds = { { ComicVineConstants.ProviderId, "attack-on-titan-10-fortress-of-blood/4000-441467" } }
            }, CancellationToken.None);

            Assert.Collection(
                images,
                large => Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_large/6/67663/3556541-10.jpg", large.Url));
        }
    }
}
