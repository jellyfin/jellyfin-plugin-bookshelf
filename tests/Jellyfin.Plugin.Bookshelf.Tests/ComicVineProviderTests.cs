using System.Net;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine;
using Jellyfin.Plugin.Bookshelf.Tests.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class ComicVineProviderTests
    {
        private readonly IComicVineApiKeyProvider _mockApiKeyProvider;

        public ComicVineProviderTests()
        {
            _mockApiKeyProvider = Substitute.For<IComicVineApiKeyProvider>();
            _mockApiKeyProvider.GetApiKey().Returns(Guid.NewGuid().ToString());
        }

        private string GetSearchResultWithNamedIssues() => TestHelpers.GetFixture("comic-vine-issue-search-named-issues.json");
        private string GetSearchResultWithNumberedIssues() => TestHelpers.GetFixture("comic-vine-issue-search-numbered-issues.json");

        private string GetSingleIssueResult() => TestHelpers.GetFixture("comic-vine-single-issue.json");
        private string GetSingleUnnamedIssueResult() => TestHelpers.GetFixture("comic-vine-single-numbered-issue.json");
        private string GetSingleVolumeResult() => TestHelpers.GetFixture("comic-vine-single-volume.json");

        private bool HasComicVineId(string id, Dictionary<string, string> providerIds)
        {
            return providerIds.Count == 1
                && providerIds.TryGetValue(ComicVineConstants.ProviderId, out string? providerId)
                && providerId == id;
        }

        #region GetSearchResults

        [Fact]
        public async Task GetSearchResults_ByName_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.OK, GetSearchResultWithNamedIssues())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new BookInfo() { Name = "Fortress of blood" }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            Assert.Collection(results,
                first =>
                {
                    Assert.Equal("Fortress Of Blood", first.Name);
                    Assert.True(HasComicVineId("attack-on-titan-10-fortress-of-blood/4000-441467", first.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/3556541-10.jpg", first.ImageUrl);
                    Assert.Equal("<p><em>FORTRESS OF BLOOD</em></p><p>" +
                        "<em>With no combat gear and Wall Rose breached, the 104th scrambles to evacuate the villages in the Titans' path. On their way to the safety of Wall Sheena, they decide to spend the night in Utgard Castle." +
                        " But their sanctuary becomes a slaughterhouse when they discover that, for some reason, these Titans attack at night!</em></p>" +
                        "<h2>Chapter Titles</h2><ul><li>Episode 39: Soldier</li><li>Episode 40: Ymir</li><li>Episode 41: Historia</li><li>Episode 42: Warrior</li></ul>", first.Overview);
                    Assert.Equal(2014, first.ProductionYear);
                },
                second =>
                {
                    Assert.Equal("Titan on the Hunt", second.Name);
                    Assert.True(HasComicVineId("attack-on-titan-6-titan-on-the-hunt/4000-424591", second.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/3506331-06.jpg", second.ImageUrl);
                    Assert.Equal("<p><em>TITAN ON THE HUNT</em></p><p><em>On the way to Eren’s home, deep in Titan territory, the Survey Corps ranks are broken by a charge led by a female Titan!" +
                        " But this Abnormal is different – she kills not to eat but to protect herself, and she seems to be looking for someone." +
                        " Armin comes to a shocking conclusion: She’s a human in a Titan’s body, just like Eren!</em></p>" +
                        "<h2>Chapter Titles</h2><ul><li>Episode 23: The Female Titan</li><li>Episode 24: The Titan Forest</li><li>Episode 25: Bite</li><li>Episode 26: The Easy Path</li></ul>", second.Overview);
                    Assert.Equal(2013, second.ProductionYear);
                },
                third =>
                {
                    Assert.Equal("Band 10", third.Name);
                    Assert.True(HasComicVineId("attack-on-titan-10-band-10/4000-546356", third.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/5400404-10.jpg", third.ImageUrl);
                    Assert.Equal("<p><em>Die Erde gehört riesigen Menschenfressern: den TITANEN!</em></p><p><em>" +
                        "Die letzten Menschen leben zusammengepfercht in einer Festung mit fünfzig Meter hohen Mauern.</em></p><p><em>" +
                        "Als ein kolossaler Titan die äußere Mauer einreißt, bricht ein letzter Kampf aus – um das Überleben der Menschheit!</em></p>" +
                        "<h2>Kapitel</h2><ul><li>39: Soldaten</li><li>40: Ymir</li><li>41: Historia</li><li>42: Krieger</li></ul>", third.Overview);
                    Assert.Equal(2015, third.ProductionYear);
                });
        }

        [Fact]
        public async Task GetSearchResults_ByProviderId_WithoutSlug_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/issue/4000-441467"), new MockHttpResponse(HttpStatusCode.OK, GetSingleIssueResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new BookInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "4000-441467" }
                }
            }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            Assert.Collection(results,
                first =>
                {
                    Assert.Equal("Fortress Of Blood", first.Name);
                    Assert.True(HasComicVineId("attack-on-titan-10-fortress-of-blood/4000-441467", first.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/3556541-10.jpg", first.ImageUrl);
                    Assert.Equal("<p><em>FORTRESS OF BLOOD</em></p><p>" +
                        "<em>With no combat gear and Wall Rose breached, the 104th scrambles to evacuate the villages in the Titans' path. On their way to the safety of Wall Sheena, they decide to spend the night in Utgard Castle." +
                        " But their sanctuary becomes a slaughterhouse when they discover that, for some reason, these Titans attack at night!</em></p>" +
                        "<h2>Chapter Titles</h2><ul><li>Episode 39: Soldier</li><li>Episode 40: Ymir</li><li>Episode 41: Historia</li><li>Episode 42: Warrior</li></ul>", first.Overview);
                    Assert.Equal(2014, first.ProductionYear);
                });
        }

        [Fact]
        public async Task GetSearchResults_ByProviderId_WithSlug_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/issue/4000-441467"), new MockHttpResponse(HttpStatusCode.OK, GetSingleIssueResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new BookInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "attack-on-titan-10-fortress-of-blood/4000-441467" }
                }
            }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            Assert.Collection(results,
                first =>
                {
                    Assert.Equal("Fortress Of Blood", first.Name);
                    Assert.True(HasComicVineId("attack-on-titan-10-fortress-of-blood/4000-441467", first.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/3556541-10.jpg", first.ImageUrl);
                    Assert.Equal("<p><em>FORTRESS OF BLOOD</em></p><p>" +
                        "<em>With no combat gear and Wall Rose breached, the 104th scrambles to evacuate the villages in the Titans' path. On their way to the safety of Wall Sheena, they decide to spend the night in Utgard Castle." +
                        " But their sanctuary becomes a slaughterhouse when they discover that, for some reason, these Titans attack at night!</em></p>" +
                        "<h2>Chapter Titles</h2><ul><li>Episode 39: Soldier</li><li>Episode 40: Ymir</li><li>Episode 41: Historia</li><li>Episode 42: Warrior</li></ul>", first.Overview);
                    Assert.Equal(2014, first.ProductionYear);
                });
        }

        [Fact]
        public async Task GetSearchResults_WithUnamedIssues_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.OK, GetSearchResultWithNumberedIssues())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new BookInfo() { Name = "Invincible #20" }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            Assert.Collection(results,
                first =>
                {
                    Assert.Equal("#020", first.Name);
                    Assert.True(HasComicVineId("invincible-20/4000-989412", first.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/11/110017/8943106-wwww.jpg", first.ImageUrl);
                    Assert.Empty(first.Overview);
                    Assert.Equal(2015, first.ProductionYear);
                },
                second =>
                {
                    Assert.Equal("#020", second.Name);
                    Assert.True(HasComicVineId("invincible-20/4000-128610", second.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/6/67663/2185628-20.jpg", second.ImageUrl);
                    Assert.Equal("<p><em>Mark Grayson is just like everyone else his age, except that his father is the most powerful superhero on the planet." +
                        " And now he's begun to inherit his father's powers. It all sounds okay at first, but how do you follow in your father's footsteps when you know you will never live up to his standards?" +
                        " For nine years now (or however long it's been since issue #6 came out) readers have been wondering, \"What's up with that robot zombie from issue #6?\"" +
                        " Well, wonder no longer, because he's in this issue! Mark is on campus at his new college and something is amiss. What lurks behind...oh, wait: You already know!</em></p>" +
                        "<p>Atom Eve decides to retire from the superhero business and use her powers to actually make a difference in the world." +
                        " Amber gets mad at Mark when he mysteriously disappears to fight a Reaniman that is attacking the campus, she mistakenly thinks he ran off like a coward." +
                        " If only she knew Mark is actually the brave superhero, Invincible. D. A. Sinclair formulates that his next Reaniman should be constructed from a...live subject!</p>", second.Overview);
                    Assert.Equal(2005, second.ProductionYear);
                },
                third =>
                {
                    Assert.Equal("Amici", third.Name);
                    Assert.True(HasComicVineId("invincible-20-amici/4000-989389", third.ProviderIds));
                    Assert.Equal("https://comicvine.gamespot.com/a/uploads/scale_avatar/11/110017/8943006-invincible-20-0001.jpg", third.ImageUrl);
                    Assert.Empty(third.Overview);
                    Assert.Equal(2016, third.ProductionYear);
                });
        }

        [Fact]
        public async Task GetSearchResults_WithErrorResponse_ReturnsNoResults()
        {
            var errorResponse = @"
{
	""error"": ""Invalid API Key"",
	""limit"": 0,
	""offset"": 0,
	""number_of_page_results"": 0,
	""number_of_total_results"": 0,
	""status_code"": 100,
	""results"": []
}";
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.Unauthorized, errorResponse)),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var results = await provider.GetSearchResults(new BookInfo() { Name = "Fortress of Blood" }, CancellationToken.None);

            Assert.Empty(results);
        }

        #endregion

        #region GetMetadata

        private void AssertMetadata(MetadataResult<Book> metadataResult, bool queriedById)
        {
            Assert.Equal(queriedById, metadataResult.QueriedById);
            Assert.True(metadataResult.HasMetadata);

            Assert.Collection(metadataResult.People,
                p =>
                {
                    Assert.Equal("Ben Applegate", p.Name);
                    Assert.Equal("Editor", p.Type);
                    Assert.True(HasComicVineId("ben-applegate/4040-74578", p.ProviderIds));
                },
                p =>
                {
                    Assert.Equal("Hajime Isayama", p.Name);
                    Assert.Equal("Writer", p.Type);
                    Assert.True(HasComicVineId("hajime-isayama/4040-64651", p.ProviderIds));
                },
                p =>
                {
                    Assert.Equal("Ko Ransom", p.Name);
                    Assert.Equal("Unknown", p.Type);
                    Assert.True(HasComicVineId("ko-ransom/4040-74576", p.ProviderIds));
                },
                p =>
                {
                    Assert.Equal("Steve Wands", p.Name);
                    Assert.Equal("Letterer", p.Type);
                    Assert.True(HasComicVineId("steve-wands/4040-47630", p.ProviderIds));
                },
                p =>
                {
                    Assert.Equal("Takashi Shimoyama", p.Name);
                    Assert.Equal("CoverArtist", p.Type);
                    Assert.True(HasComicVineId("takashi-shimoyama/4040-74571", p.ProviderIds));
                });

            Assert.True(HasComicVineId("attack-on-titan-10-fortress-of-blood/4000-441467", metadataResult.Item.ProviderIds));
            Assert.Equal("Fortress Of Blood", metadataResult.Item.Name);
            Assert.Equal("010 - Attack on Titan, Fortress Of Blood", metadataResult.Item.ForcedSortName);
            Assert.Collection(metadataResult.Item.Studios,
                s =>
                {
                    Assert.Equal("Kodansha Comics USA", s);
                });
            Assert.Equal(2014, metadataResult.Item.ProductionYear);
            Assert.Equal("<p><em>FORTRESS OF BLOOD</em></p>" +
                "<p><em>With no combat gear and Wall Rose breached, the 104th scrambles to evacuate the villages in the Titans' path." +
                " On their way to the safety of Wall Sheena, they decide to spend the night in Utgard Castle." +
                " But their sanctuary becomes a slaughterhouse when they discover that, for some reason, these Titans attack at night!</em></p>" +
                "<h2>Chapter Titles</h2><ul><li>Episode 39: Soldier</li><li>Episode 40: Ymir</li><li>Episode 41: Historia</li><li>Episode 42: Warrior</li></ul>", metadataResult.Item.Overview);
            Assert.Empty(metadataResult.Item.Genres);
            Assert.Empty(metadataResult.Item.Tags);
            Assert.Null(metadataResult.Item.CommunityRating);
        }

        [Fact]
        public async Task GetMetadata_MatchesByName_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.OK, GetSearchResultWithNamedIssues())),
                ((Uri uri) => uri.AbsoluteUri.Contains("/issue/4000-441467"), new MockHttpResponse(HttpStatusCode.OK, GetSingleIssueResult())),
                ((Uri uri) => uri.AbsoluteUri.Contains("/volume/4050-49866"), new MockHttpResponse(HttpStatusCode.OK, GetSingleVolumeResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var metadataResult = await provider.GetMetadata(new BookInfo()
            {
                SeriesName = "Attack on Titan",
                Name = "10 - Fortress of Blood"
            }, CancellationToken.None);

            AssertMetadata(metadataResult, false);
        }

        [Fact]
        public async Task GetMetadata_MatchesByProviderId_Success()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/issue/4000-441467"), new MockHttpResponse(HttpStatusCode.OK, GetSingleIssueResult())),
                ((Uri uri) => uri.AbsoluteUri.Contains("/volume/4050-49866"), new MockHttpResponse(HttpStatusCode.OK, GetSingleVolumeResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var metadataResult = await provider.GetMetadata(new BookInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "attack-on-titan-10-fortress-of-blood/4000-441467" }
                }
            }, CancellationToken.None);

            AssertMetadata(metadataResult, true);
        }

        [Fact]
        public async Task GetMetadata_WithNoCache_AddsToCache()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/issue/4000-441467"), new MockHttpResponse(HttpStatusCode.OK, GetSingleIssueResult())),
                ((Uri uri) => uri.AbsoluteUri.Contains("/volume/4050-49866"), new MockHttpResponse(HttpStatusCode.OK, GetSingleVolumeResult())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            var cache = Substitute.For<IComicVineMetadataCacheManager>();

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                cache,
                _mockApiKeyProvider);

            var metadataResult = await provider.GetMetadata(new BookInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "attack-on-titan-10-fortress-of-blood/4000-441467" }
                }
            }, CancellationToken.None);

            await cache.Received().AddToCache<IssueDetails>("4000-441467", Arg.Any<IssueDetails>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetMetadata_WithValidCache_GetsFromCache()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>());

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            var cache = Substitute.For<IComicVineMetadataCacheManager>();
            cache.HasCache("4000-441467").Returns(true);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                cache,
                _mockApiKeyProvider);

            var metadataResult = await provider.GetMetadata(new BookInfo()
            {
                ProviderIds = new Dictionary<string, string>()
                {
                    { ComicVineConstants.ProviderId, "attack-on-titan-10-fortress-of-blood/4000-441467" }
                }
            }, CancellationToken.None);

            await cache.DidNotReceive().AddToCache<IssueDetails>("4000-441467", Arg.Any<IssueDetails>(), Arg.Any<CancellationToken>());
            await cache.Received().GetFromCache<IssueDetails>("4000-441467",Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetMetadata_MatchesByIssueNumber_PicksCorrectResult()
        {
            var mockedMessageHandler = new MockHttpMessageHandler(new List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)>
            {
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.OK, GetSearchResultWithNumberedIssues())),
                ((Uri uri) => uri.AbsoluteUri.Contains("/issue/4000-128610"), new MockHttpResponse(HttpStatusCode.OK, GetSingleUnnamedIssueResult())),
                ((Uri uri) => uri.AbsoluteUri.Contains("/volume/"), new MockHttpResponse(HttpStatusCode.NotFound, string.Empty)),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            using var client = new HttpClient(mockedMessageHandler);
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            // Only one search result matches the provided year
            var metadataResult = await provider.GetMetadata(new BookInfo() { Name = "Invincible #20 (2005)" }, CancellationToken.None);

            Assert.False(metadataResult.QueriedById);
            Assert.True(metadataResult.HasMetadata);

            Assert.True(HasComicVineId("invincible-20/4000-128610", metadataResult.Item.ProviderIds));
            Assert.Equal("#020", metadataResult.Item.Name);
            Assert.Equal(2005, metadataResult.Item.ProductionYear);
            Assert.Equal("<p><em>Mark Grayson is just like everyone else his age, except that his father is the most powerful superhero on the planet." +
                " And now he's begun to inherit his father's powers. It all sounds okay at first, but how do you follow in your father's footsteps when you know you will never live up to his standards?" +
                " For nine years now (or however long it's been since issue #6 came out) readers have been wondering, \"What's up with that robot zombie from issue #6?\"" +
                " Well, wonder no longer, because he's in this issue! Mark is on campus at his new college and something is amiss." +
                " What lurks behind...oh, wait: You already know!</em></p>" +
                "<p>Atom Eve decides to retire from the superhero business and use her powers to actually make a difference in the world." +
                " Amber gets mad at Mark when he mysteriously disappears to fight a Reaniman that is attacking the campus, she mistakenly thinks he ran off like a coward." +
                " If only she knew Mark is actually the brave superhero, Invincible. D. A. Sinclair formulates that his next Reaniman should be constructed from a...live subject!</p>", metadataResult.Item.Overview);
        }

        #endregion

        #region GetSearchString

        [Fact]
        public void GetSearchString_WithSeriesAndName_ReturnsCorrectString()
        {
            ComicVineMetadataProvider provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                Substitute.For<IHttpClientFactory>(),
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var bookInfo = new BookInfo()
            {
                SeriesName = "Invincible",
                Name = "Eight is Enough",
            };
            var searchString = provider.GetSearchString(bookInfo);

            Assert.Equal("Invincible Eight is Enough", searchString);
        }

        [Fact]
        public void GetSearchString_WithSeriesAndIndex_ReturnsCorrectString()
        {
            ComicVineMetadataProvider provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                Substitute.For<IHttpClientFactory>(),
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var bookInfo = new BookInfo()
            {
                SeriesName = "Invincible",
                IndexNumber = 2
            };
            var searchString = provider.GetSearchString(bookInfo);

            Assert.Equal("Invincible 2", searchString);
        }

        [Fact]
        public void GetSearchString_WithAllValues_ReturnsCorrectString()
        {
            ComicVineMetadataProvider provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                Substitute.For<IHttpClientFactory>(),
                Substitute.For<IComicVineMetadataCacheManager>(),
                _mockApiKeyProvider);

            var bookInfo = new BookInfo()
            {
                SeriesName = "Invincible",
                Name = "Eight is Enough",
                IndexNumber = 2,
                Year = 2004
            };
            var searchString = provider.GetSearchString(bookInfo);

            // Year should be ignored
            Assert.Equal("Invincible 2 Eight is Enough", searchString);
        }

        #endregion
    }
}
