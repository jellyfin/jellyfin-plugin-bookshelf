using System.Net;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine;
using Jellyfin.Plugin.Bookshelf.Tests.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class ComicVinePersonImageProviderTests
    {
        [Fact]
        public async Task GetImages_WithAllLinks_ReturnsLargest()
        {
            var mockApiKeyProvider = Substitute.For<IComicVineApiKeyProvider>();
            mockApiKeyProvider.GetApiKey().Returns(Guid.NewGuid().ToString());

            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("person/4040-64651"), new MockHttpResponse(HttpStatusCode.OK, TestHelpers.GetFixture("comic-vine-person.json")))
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteImageProvider provider = new ComicVinePersonImageProvider(Substitute.For<IComicVineMetadataCacheManager>(), NullLogger<ComicVinePersonImageProvider>.Instance, mockedHttpClientFactory, mockApiKeyProvider);

            var images = await provider.GetImages(new Person()
            {
                ProviderIds = { { ComicVineConstants.ProviderId, "hajime-isayama/4040-64651" } }
            }, CancellationToken.None);

            Assert.Collection(
                images,
                large => Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_large/6/67663/4536545-30939.jpg", large.Url));
        }
    }
}
