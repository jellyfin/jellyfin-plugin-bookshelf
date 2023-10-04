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
        private string GetSearchResultWithNamedIssues() => TestHelpers.GetFixture("comic-vine-issue-search-named-issues.json");
        private string GetSearchResultWithNumberedIssues() => TestHelpers.GetFixture("comic-vine-issue-search-numbered-issues.json");

        private string GetSingleIssueResult() => TestHelpers.GetFixture("comic-vine-single-issue.json");

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
                ((Uri uri) => uri.AbsoluteUri.Contains("/search"), new MockHttpResponse(HttpStatusCode.OK, GetSearchResultWithNamedIssues())),
            });

            var mockedHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockedHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockedMessageHandler));

            IRemoteMetadataProvider<Book, BookInfo> provider = new ComicVineMetadataProvider(
                NullLogger<ComicVineMetadataProvider>.Instance,
                mockedHttpClientFactory,
                Substitute.For<IComicVineMetadataCacheManager>());

            var results = await provider.GetSearchResults(new BookInfo() { Name = "Fortress of blood" }, CancellationToken.None);

            Assert.True(results.All(result => result.SearchProviderName == ComicVineConstants.ProviderName));

            // TODO: Assert on the collection
            Assert.Fail();
        }

        [Fact]
        public async Task GetSearchResults_ByProviderId_Success()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region GetMetadata

        [Fact]
        public async Task GetMetadata_MatchesByName_Success()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task GetMetadata_MatchesByProviderId_Success()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task GetMetadata_MatchesByNameWithYearVariance_SkipsResult()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region GetSearchString

        [Fact]
        public void GetSearchString_WithSeriesAndName_ReturnsCorrectString()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void GetSearchString_WithSeriesAndIndex_ReturnsCorrectString()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void GetSearchString_WithNameAndYear_ReturnsCorrectString()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void GetSearchString_WithOnlyName_ReturnsCorrectString()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
