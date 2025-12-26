using System.Net;
using Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks;
using Jellyfin.Plugin.Bookshelf.Tests.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class GoogleBooksImageProviderTests
    {
        [Fact]
        public async Task GetImages_WithAllLinks_PicksLargestAndThumbnail()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("volumes/G7utDwAAQBAJ", StringComparison.Ordinal), new MockHttpResponse(HttpStatusCode.OK, TestHelpers.GetFixture("google-books-single-volume-fr.json")))
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteImageProvider provider = new GoogleBooksImageProvider(NullLogger<GoogleBooksImageProvider>.Instance, mockedHttpClientFactory);

            var images = await provider.GetImages(new Book()
            {
                ProviderIds = { { GoogleBooksConstants.ProviderId, "G7utDwAAQBAJ" } }
            }, CancellationToken.None);

            Assert.Collection(
                images,
                largest => Assert.Equal("http://books.google.com/books/publisher/content?id=G7utDwAAQBAJ&printsec=frontcover&img=1&zoom=6&edge=curl&imgtk=AFLRE70zvRYbN6L3AM1H-SFdT_b8RDDGh6SfKIC_erPvfkI3QnpI_sFSIyOjXKgLJqbxVttwKVw12OUkxkPGjlAekXU7tTbpS7OcUQ_XbxhKaIsoC6ekr32GtMzZ5WkHbGu6rRpdIYVQ&source=gbs_api", largest.Url),
                thumbnail => Assert.Equal("http://books.google.com/books/publisher/content?id=G7utDwAAQBAJ&printsec=frontcover&img=1&zoom=1&edge=curl&imgtk=AFLRE73iXAAA6Bipi-q6HwR1kz5-XegugreP1A2Mbu63gh2TQKdI1lOCoRg9EuW7sFt2RjQgDbAXaHQlBPe8TBY2mo0i2ngWotY1eAvIusIEaCLRD18wl0baMruHUs4b3QvBF56gznpu&source=gbs_api", thumbnail.Url));
        }
    }
}
