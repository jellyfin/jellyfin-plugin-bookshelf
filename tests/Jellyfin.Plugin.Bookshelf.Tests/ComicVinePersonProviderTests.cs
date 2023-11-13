using System.Net;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine;
using Jellyfin.Plugin.Bookshelf.Tests.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class ComicVinePersonProviderTests
    {
        private readonly IComicVineApiKeyProvider _mockApiKeyProvider;

        public ComicVinePersonProviderTests()
        {
            _mockApiKeyProvider = Substitute.For<IComicVineApiKeyProvider>();
            _mockApiKeyProvider.GetApiKey().Returns(Guid.NewGuid().ToString());
        }

        private string GetPersonResult() => TestHelpers.GetFixture("comic-vine-person.json");
        private string GetPersonWithDescriptionResult() => TestHelpers.GetFixture("comic-vine-person-with-description.json");
        private string GetSearchResult() => TestHelpers.GetFixture("comic-vine-person-search.json");

        private bool HasComicVineId(string id, Dictionary<string, string> providerIds)
        {
            return providerIds.Count == 1
                && providerIds.ContainsKey(ComicVineConstants.ProviderId)
                && providerIds[ComicVineConstants.ProviderId] == id;
        }

        #region GetSearchResults

        [Fact]
        public async Task GetSearchResults_ByName_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.OK, GetSearchResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Person, PersonLookupInfo> provider = new ComicVinePersonProvider(
                NullLogger<ComicVinePersonProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new PersonLookupInfo() { Name = "Hajime Isayama" }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            Assert.Collection(results,
                first =>
                {
                    Assert.Equal("Hajime Isayama", first.Name);
                    Assert.True(HasComicVineId("hajime-isayama/4040-64651", first.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/4536545-30939.jpg", first.ImageUrl);
                    Assert.Equal("Mangaka and creator of Shingeki no kyoujin/Attack on Titan manga.", first.Overview);
                },
                second =>
                {
                    Assert.Equal("Hajime Kimura", second.Name);
                    Assert.True(HasComicVineId("hajime-kimura/4040-82481", second.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/11156/111569227/8660522-kimura.jpg", second.ImageUrl);
                    Assert.Equal("Writer of Master Keaton and co-writer for Golgo 13.", second.Overview);
                });
        }

        [Fact]
        public async Task GetSearchResults_ByProviderId_WithoutSlug_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/person/4040-64651"), new MockHttpResponse(HttpStatusCode.OK, GetPersonResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Person, PersonLookupInfo> provider = new ComicVinePersonProvider(
                NullLogger<ComicVinePersonProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new PersonLookupInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "4040-64651" }
                }
            }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            Assert.Collection(results,
                first =>
                {
                    Assert.Equal("Hajime Isayama", first.Name);
                    Assert.True(HasComicVineId("hajime-isayama/4040-64651", first.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/4536545-30939.jpg", first.ImageUrl);
                    Assert.Equal("Mangaka and creator of Shingeki no kyoujin/Attack on Titan manga.", first.Overview);
                });
        }

        [Fact]
        public async Task GetSearchResults_ByProviderId_WithSlug_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/person/4040-64651"), new MockHttpResponse(HttpStatusCode.OK, GetPersonResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Person, PersonLookupInfo> provider = new ComicVinePersonProvider(
                NullLogger<ComicVinePersonProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new PersonLookupInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "hajime-isayama/4040-64651" }
                }
            }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            Assert.Collection(results,
                 first =>
                 {
                     Assert.Equal("Hajime Isayama", first.Name);
                     Assert.True(HasComicVineId("hajime-isayama/4040-64651", first.ProviderIds));
                     Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/4536545-30939.jpg", first.ImageUrl);
                     Assert.Equal("Mangaka and creator of Shingeki no kyoujin/Attack on Titan manga.", first.Overview);
                 });
        }

        #endregion

        #region GetMetadata

        [Fact]
        public async Task GetMetadata_MatchesByName_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.OK, GetSearchResult())),
                ((Uri uri) => uri.AbsoluteUri.Contains("/person/4040-64651"), new MockHttpResponse(HttpStatusCode.OK, GetPersonResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Person, PersonLookupInfo> provider = new ComicVinePersonProvider(
                NullLogger<ComicVinePersonProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var metadataResult = await provider.GetMetadata(new PersonLookupInfo()
            {
                Name = "hajime isayama"
            }, CancellationToken.None);

            Assert.False(metadataResult.QueriedById);
            Assert.True(metadataResult.HasMetadata);

            Assert.True(HasComicVineId("hajime-isayama/4040-64651", metadataResult.Item.ProviderIds));

            Assert.Equal("Hajime Isayama", metadataResult.Item.Name);
            Assert.Equal("諫山創", metadataResult.Item.OriginalTitle);

            Assert.Equal("Mangaka and creator of Shingeki no kyoujin/Attack on Titan manga.", metadataResult.Item.Overview);
            Assert.Equal("http://www.blog.livedoor.jp/isayamahazime/", metadataResult.Item.HomePageUrl);
            Assert.Equal(new DateTimeOffset(1986, 08, 29, 0, 0, 0, TimeSpan.Zero).UtcDateTime, metadataResult.Item.PremiereDate);
            Assert.Null(metadataResult.Item.EndDate);
            Assert.Empty(metadataResult.Item.ProductionLocations);
        }

        [Fact]
        public async Task GetMetadata_MatchesByProviderId_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/person/4040-1537"), new MockHttpResponse(HttpStatusCode.OK, GetPersonWithDescriptionResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Person, PersonLookupInfo> provider = new ComicVinePersonProvider(
                NullLogger<ComicVinePersonProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var metadataResult = await provider.GetMetadata(new PersonLookupInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "joe-quesada/4040-1537" }
                }
            }, CancellationToken.None);

            Assert.True(metadataResult.QueriedById);
            Assert.True(metadataResult.HasMetadata);

            Assert.True(HasComicVineId("joe-quesada/4040-1537", metadataResult.Item.ProviderIds));

            Assert.Equal("Joe Quesada", metadataResult.Item.Name);
            Assert.Null(metadataResult.Item.OriginalTitle);

            Assert.Equal("<h2>Career</h2><p>Joseph Quesada was born on January 12, 1962 and is a comic book editor." +
                " Joe Quesada was born in New York to Cuban parents and was raised in Jackson Heights, Queens." +
                " He went to the School of Visual Arts, where he earned a BFA in illustration in 1984." +
                " Quesada’s art was heavily influenced by Manga, Japanese style artwork, which was shown by his drawings." +
                " He and his inking partner <a data-ref-id=\"4040-3068\" href=\"https://comicvine.gamespot.com/jimmy-palmiotti/4040-3068/\">Jimmy Palmiotti</a> made up an inking company called" +
                " <a data-ref-id=\"4010-651\" href=\"https://comicvine.gamespot.com/event-comics/4010-651/\">Event Comics</a>. While within this company, Quesada helped co-create some super heroes." +
                " One of his most notable creations was <a data-ref-id=\"4005-41248\" href=\"https://comicvine.gamespot.com/ash/4005-41248/\">Ash</a>, a firefighter with superpowers.</p>" +
                "<p>Quesada got his start in the early 90s working for <a data-ref-id=\"4010-485\" href=\"https://comicvine.gamespot.com/valiant/4010-485/\">Valiant Comics</a> on titles" +
                " <a data-ref-id=\"4005-4375\" href=\"https://comicvine.gamespot.com/ninjak/4005-4375/\">Ninjak</a> and <a data-ref-id=\"4050-4607\" href=\"https://comicvine.gamespot.com/solar-man-of-the-atom/4050-4607/\">Solar, Man of the Atom</a>." +
                " Soon after, he and his inking partner Jimmy Palmiotti created the publishing company, <a data-ref-id=\"4010-651\" href=\"https://comicvine.gamespot.com/event-comics/4010-651/\">Event Comics</a>." +
                " During this time, Quesada also contributed to <a data-ref-id=\"4010-10\" href=\"https://comicvine.gamespot.com/dc-comics/4010-10/\">DC Comics</a> for comics such as" +
                " <a data-ref-id=\"4005-2049\" href=\"https://comicvine.gamespot.com/the-ray/4005-2049/\">The Ray</a>.</p><p><b>Marvel Comics</b></p>" +
                "<p>In 1998, <a data-ref-id=\"4010-31\" href=\"https://comicvine.gamespot.com/marvel/4010-31/\">Marvel Comics</a> contracted Quesada and Event Comics to work on a new line," +
                " <a data-ref-id=\"4060-40427\" href=\"https://comicvine.gamespot.com/marvel-knights/4060-40427/\">Marvel Knights</a>. Using his personal contacts in the field, Quesada brought in names such as" +
                " <a data-ref-id=\"4040-40435\" href=\"https://comicvine.gamespot.com/brian-michael-bendis/4040-40435/\">Brian Michael Bendis</a>," +
                " <a data-ref-id=\"4040-9037\" href=\"https://comicvine.gamespot.com/david-mack/4040-9037/\">Dave Mack</a>, and <a data-ref-id=\"4040-40644\" href=\"https://comicvine.gamespot.com/garth-ennis/4040-40644/\">Garth Ennis</a>" +
                " to bring an original flair to the properties involved. During this time, Quesada worked with screenwriter/director" +
                " <a data-ref-id=\"4040-42844\" href=\"https://comicvine.gamespot.com/kevin-smith/4040-42844/\">Kevin Smith</a>, illustrating a new" +
                " <a data-ref-id=\"4005-24694\" href=\"https://comicvine.gamespot.com/daredevil/4005-24694/\">Daredevil</a> series, intent on bringing the wayward hero back to his roots.</p>" +
                "<p>In 2000, Quesada became editor-in-chief of Marvel Comics after the departure of <a data-ref-id=\"4040-40985\" href=\"https://comicvine.gamespot.com/bob-harras/4040-40985/\">Bob Harras</a>." +
                " He is the first artist to become editor-in-chief at <a data-ref-id=\"4010-31\" href=\"https://comicvine.gamespot.com/marvel/4010-31/\">Marvel</a>." +
                " Quesada's ascension in the ranks occurred simultaneously with that of <a data-ref-id=\"4040-55732\" href=\"https://comicvine.gamespot.com/bill-jemas/4040-55732/\">Bill Jemas</a>," +
                " who became Marvel's president. The two worked closely together, and their efforts culminated in the all-new Ultimate line of Marvel Comics that modernized classic heroes" +
                " such as <a data-ref-id=\"4050-7257\" href=\"https://comicvine.gamespot.com/ultimate-spider-man/4050-7257/\">Spider-Man</a> and the <a data-ref-id=\"4050-7258\" href=\"https://comicvine.gamespot.com/ultimate-x-men/4050-7258/\">X-Men</a>" +
                " for new, younger readers.</p><p>Quesada also imposed a moratorium on reviving characters who have died over the years and stated that \"dead is dead.\"" +
                " When asked about comics that revived deceased characters, Quesada stated that the policy wasn't an absolute mandate, but rather a rule of thumb to present to writers" +
                " so that stories requiring a resurrection of a character wouldn't become frequent or produced without gravity.</p>" +
                "<p>In 2010, Quesada was named Chief Creative Officer of Marvel Entertainment and left his editor-in-chief role in January 2011," +
                " being replaced by <a data-ref-id=\"4040-23115\" href=\"https://comicvine.gamespot.com/axel-alonso/4040-23115/\">Axel Alonso</a>.</p>",
                metadataResult.Item.Overview);
            Assert.Equal("http://www.joequesada.com/", metadataResult.Item.HomePageUrl);

            // Birth date should be UTC
            Assert.True(metadataResult.Item.PremiereDate.HasValue);
            Assert.Equal(new DateTimeOffset(1962, 12, 01, 0, 0, 0, TimeSpan.Zero).UtcDateTime, metadataResult.Item.PremiereDate);
            Assert.Equal(metadataResult.Item.PremiereDate.Value.ToUniversalTime(), metadataResult.Item.PremiereDate);

            Assert.Null(metadataResult.Item.EndDate);
            Assert.Equal(new string[] { "New York City" }, metadataResult.Item.ProductionLocations);
        }

        #endregion
    }
}
