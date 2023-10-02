using System.Net;
using Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks;
using Jellyfin.Plugin.Bookshelf.Tests.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class GoogleBooksProviderTests
    {
        // From the query 'https://www.googleapis.com/books/v1/volumes?q=children+of+time+2015'
        private string GetTestSearchResult() => TestHelpers.GetFixture("google-books-volume-search.json");

        private string GetEnglishTestVolumeResult() => TestHelpers.GetFixture("google-books-single-volume-en.json");
        private string GetFrenchTestVolumeResult() => TestHelpers.GetFixture("google-books-single-volume-fr.json");

        private bool HasGoogleId(string id, Dictionary<string, string> providerIds)
        {
            return providerIds.Count == 1
                && providerIds.ContainsKey(GoogleBooksConstants.ProviderId)
                && providerIds[GoogleBooksConstants.ProviderId] == id;
        }

        #region GetSearchResults

        [Fact]
        public async Task GetSearchResults_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("volumes?q="), new MockHttpResponse(HttpStatusCode.OK, GetTestSearchResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Book, BookInfo> provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, mockedHttpClientFactory);

            var results = await provider.GetSearchResults(new BookInfo() { Name = "Children of Time" }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == GoogleBooksConstants.ProviderName));

            Assert.Collection(
            results,
            first =>
            {
                Assert.Equal("Children of Time", first.Name);
                Assert.True(HasGoogleId("49T5twEACAAJ", first.ProviderIds));
                Assert.Equal("http://books.google.com/books/content?id=49T5twEACAAJ&printsec=frontcover&img=1&zoom=1&source=gbs_api", first.ImageUrl);
                Assert.Equal("Adrian Tchaikovksy's award-winning novel Children of Time, is the epic story of humanity's battle for survival on a terraformed planet. Who will inherit this new Earth? The last remnants of the human race left a dying Earth, desperate to find a new home among the stars. Following in the footsteps of their ancestors, they discover the greatest treasure of the past age - a world terraformed and prepared for human life. But all is not right in this new Eden. In the long years since the planet was abandoned, the work of its architects has borne disastrous fruit. The planet is not waiting for them, pristine and unoccupied. New masters have turned it from a refuge into mankind's worst nightmare. Now two civilizations are on a collision course, both testing the boundaries of what they will do to survive. As the fate of humanity hangs in the balance, who are the true heirs of this new Earth?span", first.Overview);
                Assert.Equal(2018, first.ProductionYear);

            },
            second =>
            {
                Assert.Equal("Dans la toile du temps", second.Name);
                Assert.True(HasGoogleId("G7utDwAAQBAJ", second.ProviderIds));
                Assert.Equal("http://books.google.com/books/content?id=G7utDwAAQBAJ&printsec=frontcover&img=1&zoom=1&edge=curl&source=gbs_api", second.ImageUrl);
                Assert.Equal("La Terre est au plus mal... Ses derniers habitants n’ont plus qu’un seul espoir : coloniser le \"Monde de Kern\", une planète lointaine, spécialement terraformée pour l’espèce humaine. Mais sur ce \"monde vert\" paradisiaque, tout ne s’est pas déroulé comme les scientifiques s’y attendaient. Une autre espèce que celle qui était prévue, aidée par un nanovirus, s’est parfaitement adaptée à ce nouvel environnement et elle n’a pas du tout l’intention de laisser sa place. Le choc de deux civilisations aussi différentes que possible semble inévitable. Qui seront donc les héritiers de l’ancienne Terre ? Qui sortira vainqueur du piège tendu par la toile du temps ? Premier roman de l’auteur paru en France, Dans la toile du temps s’inscrit dans la lignée du cycle Élévation de David Brin. Il nous fait découvrir l’évolution d’une civilisation radicalement autre et sa confrontation inévitable avec l’espèce humaine. Le roman a reçu le prix Arthur C. Clarke en 2016", second.Overview);
                Assert.Equal(2019, second.ProductionYear);
            });
        }

        #endregion

        #region GetMetadata

        [Fact]
        public async Task GetMetadata_MatchesByName_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("volumes?q="), new MockHttpResponse(HttpStatusCode.OK, GetTestSearchResult())),
                ((Uri uri) => uri.AbsoluteUri.Contains("volumes/49T5twEACAAJ"), new MockHttpResponse(HttpStatusCode.OK, GetEnglishTestVolumeResult()))
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Book, BookInfo> provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, mockedHttpClientFactory);

            var metadataResult = await provider.GetMetadata(new BookInfo() { Name = "Children of Time" }, CancellationToken.None);

            Assert.False(metadataResult.QueriedById);
            Assert.True(metadataResult.HasMetadata);
            Assert.Equal("en", metadataResult.ResultLanguage);

            Assert.Collection(metadataResult.People,
                p =>
                {
                    Assert.Equal("Adrian Tchaikovsky", p.Name);
                    Assert.Equal("Author", p.Type);
                });

            Assert.True(HasGoogleId("49T5twEACAAJ", metadataResult.Item.ProviderIds));
            Assert.Equal("Children of Time", metadataResult.Item.Name);
            Assert.Collection(metadataResult.Item.Studios,
                s =>
                {
                    Assert.Equal("Orbit", s);
                });
            Assert.Equal(2018, metadataResult.Item.ProductionYear);
            Assert.Equal("<b>Adrian Tchaikovksy's award-winning novel <i>Children of Time</i>, is the epic story of humanity's battle for survival on a terraformed planet.</b><b>" +
                "<br></b>Who will inherit this new Earth?<br><br>" +
                "The last remnants of the human race left a dying Earth, desperate to find a new home among the stars. " +
                "Following in the footsteps of their ancestors, they discover the greatest treasure of the past age - a world terraformed and prepared for human life." +
                "<br><br>But all is not right in this new Eden. In the long years since the planet was abandoned, the work of its architects has borne disastrous fruit. " +
                "The planet is not waiting for them, pristine and unoccupied. New masters have turned it from a refuge into mankind's worst nightmare." +
                "<br><br>Now two civilizations are on a collision course, both testing the boundaries of what they will do to survive. " +
                "As the fate of humanity hangs in the balance, who are the true heirs of this new Earth?span", metadataResult.Item.Overview);
            Assert.Collection(metadataResult.Item.Genres,
                genre => Assert.Equal("Fiction", genre));
            Assert.Collection(metadataResult.Item.Tags,
                tag => Assert.Equal("Science Fiction", tag),
                tag => Assert.Equal("Alien Contact", tag),
                tag => Assert.Equal("Genetic Engineering", tag),
                tag => Assert.Equal("Hard Science Fiction", tag),
                tag => Assert.Equal("Space Exploration", tag),
                tag => Assert.Equal("Space Opera", tag)
            );
            Assert.Equal(8, metadataResult.Item.CommunityRating);
        }

        [Fact]
        public async Task GetMetadata_MatchesByProviderId_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("volumes/G7utDwAAQBAJ"), new MockHttpResponse(HttpStatusCode.OK, GetFrenchTestVolumeResult()))
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Book, BookInfo> provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, mockedHttpClientFactory);

            var metadataResult = await provider.GetMetadata(new BookInfo()
            {
                Name = "Children of Time",
                ProviderIds = { { GoogleBooksConstants.ProviderId, "G7utDwAAQBAJ" } }
            }, CancellationToken.None);

            Assert.True(metadataResult.QueriedById);
            Assert.True(metadataResult.HasMetadata);
            Assert.Equal("fr", metadataResult.ResultLanguage);

            Assert.Collection(metadataResult.People,
                p =>
                {
                    Assert.Equal("Adrian Tchaikovsky", p.Name);
                    Assert.Equal("Author", p.Type);
                });

            Assert.True(HasGoogleId("G7utDwAAQBAJ", metadataResult.Item.ProviderIds));
            Assert.Equal("Dans la toile du temps", metadataResult.Item.Name);
            Assert.Collection(metadataResult.Item.Studios,
                s =>
                {
                    Assert.Equal("Editions Gallimard", s);
                });
            Assert.Equal(2019, metadataResult.Item.ProductionYear);
            Assert.Equal("La Terre est au plus mal... Ses derniers habitants n’ont plus qu’un seul espoir : coloniser le \"Monde de Kern\", une planète lointaine, spécialement terraformée pour l’espèce humaine. " +
                "Mais sur ce \"monde vert\" paradisiaque, tout ne s’est pas déroulé comme les scientifiques s’y attendaient. " +
                "Une autre espèce que celle qui était prévue, aidée par un nanovirus, s’est parfaitement adaptée à ce nouvel environnement et elle n’a pas du tout l’intention de laisser sa place. " +
                "Le choc de deux civilisations aussi différentes que possible semble inévitable. " +
                "Qui seront donc les héritiers de l’ancienne Terre ? Qui sortira vainqueur du piège tendu par la toile du temps ? " +
                "Premier roman de l’auteur paru en France, Dans la toile du temps s’inscrit dans la lignée du cycle Élévation de David Brin. " +
                "Il nous fait découvrir l’évolution d’une civilisation radicalement autre et sa confrontation inévitable avec l’espèce humaine. " +
                "Le roman a reçu le prix Arthur C. Clarke en 2016", metadataResult.Item.Overview);
            Assert.Collection(metadataResult.Item.Genres,
                genre => Assert.Equal("Fiction", genre));
            Assert.Collection(metadataResult.Item.Tags,
                tag => Assert.Equal("Science Fiction", tag));
            Assert.Null(metadataResult.Item.CommunityRating);
        }

        [Fact]
        public async Task GetMetadata_MatchesByNameWithYearVariance_SkipsResult()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("volumes?q="), new MockHttpResponse(HttpStatusCode.OK, GetTestSearchResult())),
                ((Uri uri) => uri.AbsoluteUri.Contains("volumes/49T5twEACAAJ"), new MockHttpResponse(HttpStatusCode.OK, GetEnglishTestVolumeResult()))
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Book, BookInfo> provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, mockedHttpClientFactory);

            var metadataResult = await provider.GetMetadata(new BookInfo() { Name = "Children of Time (2015)" }, CancellationToken.None);

            Assert.False(metadataResult.HasMetadata);
            Assert.Null(metadataResult.Item);
        }

        #endregion

        #region GetBookMetadata

        [Fact]
        public void GetBookMetadata_WithName_CorrectlyMatchesFileName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            var bookInfo = new BookInfo() { Name = "Children of Time" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Time", bookInfo.Name);
        }


        [Fact]
        public void GetBookMetadata_WithNameAndDefaultSeriesName_CorrectlyResetSeriesName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            var bookInfo = new BookInfo() { SeriesName = CollectionType.Books, Name = "Children of Time" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Time", bookInfo.Name);
            Assert.Equal(string.Empty, bookInfo.SeriesName);
        }

        [Fact]
        public void GetBookMetadata_WithNameAndYear_CorrectlyMatchesFileName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            var bookInfo = new BookInfo() { Name = "Children of Time (2015)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Time", bookInfo.Name);
            Assert.Equal(2015, bookInfo.Year);
        }

        [Fact]
        public void GetBookMetadata_WithIndexAndName_CorrectlyMatchesFileName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            var bookInfo = new BookInfo() { Name = "1 - Children of Time" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Time", bookInfo.Name);
            Assert.Equal(1, bookInfo.IndexNumber);
        }

        [Fact]
        public void GetBookMetadata_WithIndexAndNameInFolder_CorrectlyMatchesFileName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            // The series can already be identified from the folder name
            var bookInfo = new BookInfo() { SeriesName = "Children of Time", Name = "2 - Children of Ruin" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal("Children of Ruin", bookInfo.Name);
            Assert.Equal(2, bookInfo.IndexNumber);
        }

        [Fact]
        public void GetBookMetadata_WithIndexNameAndYear_CorrectlyMatchesFileName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            var bookInfo = new BookInfo() { Name = "1 - Children of Time (2015)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Time", bookInfo.Name);
            Assert.Equal(1, bookInfo.IndexNumber);
            Assert.Equal(2015, bookInfo.Year);
        }

        [Fact]
        public void GetBookMetadata_WithComicFormat_CorrectlyMatchesFileName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            // Complete format
            var bookInfo = new BookInfo() { Name = "Children of Time (2015) #2 (of 3) (2019)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Empty(bookInfo.Name);
            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal(2, bookInfo.IndexNumber);
            Assert.Equal(2019, bookInfo.Year);

            // Without series year
            bookInfo = new BookInfo() { Name = "Children of Time #2 (of 3) (2019)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Empty(bookInfo.Name);
            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal(2, bookInfo.IndexNumber);
            Assert.Equal(2019, bookInfo.Year);

            // Without total count
            bookInfo = new BookInfo() { Name = "Children of Time #2 (2019)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Empty(bookInfo.Name);
            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal(2, bookInfo.IndexNumber);
            Assert.Equal(2019, bookInfo.Year);

            // With only issue number
            bookInfo = new BookInfo() { Name = "Children of Time #2" };
            provider.GetBookMetadata(bookInfo);

            Assert.Empty(bookInfo.Name);
            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal(2, bookInfo.IndexNumber);
        }

        [Fact]
        public void GetBookMetadata_WithGoodreadsFormat_CorrectlyMatchesFileName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            // Goodreads format
            var bookInfo = new BookInfo() { Name = "Children of Ruin (Children of Time, #2)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Ruin", bookInfo.Name);
            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal(2, bookInfo.IndexNumber);

            // Goodreads format with year added
            bookInfo = new BookInfo() { Name = "Children of Ruin (Children of Time, #2) (2019)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Ruin", bookInfo.Name);
            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal(2, bookInfo.IndexNumber);
            Assert.Equal(2019, bookInfo.Year);
        }

        [Fact]
        public void GetBookMetadata_WithSeriesAndName_OverridesSeriesName()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());

            var bookInfo = new BookInfo() { SeriesName = "Adrian Tchaikovsky", Name = "Children of Ruin (Children of Time, #2)" };
            provider.GetBookMetadata(bookInfo);

            Assert.Equal("Children of Ruin", bookInfo.Name);
            Assert.Equal("Children of Time", bookInfo.SeriesName);
            Assert.Equal(2, bookInfo.IndexNumber);
        }

        #endregion

        #region GetSearchString

        [Fact]
        public void GetSearchString_WithSeriesAndName_ReturnsCorrectString()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());
            var bookInfo = new BookInfo()
            {
                SeriesName = "Invincible",
                Name = "Eight is Enough",
                IndexNumber = 2,
                Year = 2004
            };
            var searchString = provider.GetSearchString(bookInfo);

            Assert.Equal("Invincible Eight is Enough", searchString);
        }

        [Fact]
        public void GetSearchString_WithSeriesAndIndex_ReturnsCorrectString()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());
            var bookInfo = new BookInfo()
            {
                SeriesName = "Invincible",
                IndexNumber = 2
            };
            var searchString = provider.GetSearchString(bookInfo);

            Assert.Equal("Invincible 2", searchString);
        }

        [Fact]
        public void GetSearchString_WithNameAndYear_ReturnsCorrectString()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());
            var bookInfo = new BookInfo()
            {
                Name = "Eight is Enough",
                Year = 2004
            };
            var searchString = provider.GetSearchString(bookInfo);

            Assert.Equal("Eight is Enough 2004", searchString);
        }

        [Fact]
        public void GetSearchString_WithOnlyName_ReturnsCorrectString()
        {
            GoogleBooksProvider provider = new GoogleBooksProvider(NullLogger<GoogleBooksProvider>.Instance, Substitute.For<IHttpClientFactory>());
            var bookInfo = new BookInfo()
            {
                Name = "Eight is Enough",
            };
            var searchString = provider.GetSearchString(bookInfo);

            Assert.Equal("Eight is Enough", searchString);
        }

        #endregion
    }
}
