using CommonIO;
using HtmlAgilityPack;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MediaBrowser.Plugins.ADEProvider
{
    public class ADEMetadataProvider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        private const string BaseUrl = "http://www.adultdvdempire.com/";
        private const string SearchUrl = BaseUrl + "allsearch/search?q={0}";
        internal readonly SemaphoreSlim ResourcePool = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationHost _applicationHost;

        public static ADEMetadataProvider Current;

        public ADEMetadataProvider(ILogger logger, IHttpClient httpClient, IApplicationPaths appPaths, IFileSystem fileSystem, IApplicationHost applicationHost)
        {
            _logger = logger;
            _httpClient = httpClient;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _applicationHost = applicationHost;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            Current = this;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var options = this.CreateHttpRequestOptions(url, cancellationToken);

            return _httpClient.GetResponse(options);
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var adeId = searchInfo.GetProviderId(ExternalId.KeyName);

            if (!string.IsNullOrEmpty(adeId))
            {
                var result = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

                if (result.HasMetadata)
                {
                    return new List<RemoteSearchResult>()
                    {
                        new RemoteSearchResult
                        {
                             Name = result.Item.Name,
                             ProductionYear  = result.Item.ProductionYear,
                             ProviderIds = result.Item.ProviderIds,
                             SearchProviderName = Name,
                             PremiereDate = result.Item.PremiereDate,
                             ImageUrl = result.Item.PrimaryImagePath
                        }
                    };
                }
            }

            var items = await GetSearchItems(searchInfo.Name, cancellationToken);

            return items.Select(i =>
            {
                var result = new RemoteSearchResult
                {
                    Name = i.Name,
                    ProductionYear = i.Year,
                    ImageUrl = i.ImageUrl
                };

                result.SetProviderId(ExternalId.KeyName, i.Id);

                return result;
            });
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>();

            var adeId = info.GetProviderId(ExternalId.KeyName);
            var name = info.Name;

            if (string.IsNullOrEmpty(adeId))
            {
                var items = await GetSearchItems(info.Name, cancellationToken);

                if (items != null && items.Any())
                {
                    var probableItem = items.FirstOrDefault(x => x.Rank == 1);

                    if (probableItem != null)
                    {
                        name = probableItem.Name;

                        adeId = probableItem.Id;
                    }
                }
            }

            if (!string.IsNullOrEmpty(adeId))
            {
                result.Item = new Movie();
                result.Item.SetProviderId(ExternalId.KeyName, adeId);
                result.Item.Name = name;

                await GetItemDetails(result, adeId, cancellationToken);

                result.HasMetadata = true;
            }

            return result;
        }

        public string Name
        {
            get { return "Adult Dvd Empire"; }
        }

        private async Task GetItemDetails(MetadataResult<Movie> result, string id, CancellationToken cancellationToken)
        {
            string html;
            Movie item = result.Item;

            _logger.Info("Getting details for: {0}", id);

            using (var stream = await GetInfo(id, cancellationToken).ConfigureAwait(false))
            {
                html = stream.ToStringFromStream();
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var divNodes = doc.DocumentNode.Descendants("div").ToList();

            GetCast(doc, result);

            GetSynopsisAndTagLine(doc, item);

            GetCategories(doc, result);

            GetDetails(doc, result);

            GetPrimaryImage(doc, result);

            if (string.IsNullOrEmpty(item.OfficialRating))
            {
                item.OfficialRating = "XXX";
            }
        }

        public async Task<Stream> GetInfo(string adeId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(adeId))
            {
                throw new ArgumentNullException("adeId");
            }

            var cachePath = Path.Combine(_appPaths.CachePath, "ade", _fileSystem.GetValidFilename(adeId), "item.html");

            var fileInfo = _fileSystem.GetFileInfo(cachePath);

            // Check cache first
            if (!fileInfo.Exists || (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays > 7)
            {
                var searchItem = new SearchItem { Id = adeId };

                var options = this.CreateHttpRequestOptions(searchItem.Url, cancellationToken);

                // Download and cache
                using (var stream = await _httpClient.Get(options))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));

                    using (var fileStream = _fileSystem.GetFileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }
            }

            return _fileSystem.GetFileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private void GetPrimaryImage(HtmlDocument doc, MetadataResult<Movie> result)
        {
            var frontCoverImg = doc.DocumentNode.SelectSingleNode("//a[@id='front-cover']/img");
            if (frontCoverImg != null && frontCoverImg.HasAttributes && frontCoverImg.Attributes["src"] != null)
            {
                var url = frontCoverImg.Attributes["src"].Value;
                var img = new ItemImageInfo();
                img.Type = ImageType.Primary;
                img.Path = url;
                result.Item.ImageInfos.Add(img);
            }
        }

        private void GetDetails(HtmlDocument doc, MetadataResult<Movie> result)
        {
            var infoAnchor = doc.DocumentNode.SelectSingleNode("//a[@name='productinfo']");
            if (infoAnchor == null)
            {
                return;
            }

            var infoUL = infoAnchor.ParentNode.SelectSingleNode(".//ul");
            if (infoUL == null)
            {
                return;
            }

            var items = infoUL.SelectNodes("li");
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                var smallItem = item.SelectSingleNode("small");
                if (smallItem != null)
                {
                    var label = smallItem.InnerText.Trim().Replace(":", string.Empty).ToLower();

                    switch (label)
                    {
                        case "released":
                            var released = item.LastChild.InnerText.Replace("\"", string.Empty).Trim();
                            DateTime releasedDate;
                            if (DateTime.TryParse(released, out releasedDate))
                            {
                                result.Item.PremiereDate = releasedDate;
                            }

                            break;

                        case "rating":
                            var rating = item.LastChild.InnerText.Replace("\"", string.Empty).Trim();
                            if (!string.IsNullOrEmpty(rating))
                            {
                                result.Item.OfficialRating = rating;
                            }

                            break;

                        case "production year":
                            var year = item.LastChild.InnerText.Replace("\"", string.Empty).Trim();
                            if (!string.IsNullOrEmpty(year))
                            {
                                int intYear;
                                if (int.TryParse(year, out intYear))
                                {
                                    result.Item.ProductionYear = intYear;
                                }
                            }

                            break;

                        case "studio":
                            var studio = item.LastChild.InnerText.Replace("\"", string.Empty).Trim();
                            if (!string.IsNullOrEmpty(studio))
                            {
                                result.Item.AddStudio(studio);
                            }

                            break;

                        case "upc code":
                            var upcCode = item.LastChild.InnerText.Replace("\"", string.Empty).Trim();
                            if (!string.IsNullOrEmpty(upcCode))
                            {
                                result.Item.SetProviderId(UpcCodeId.KeyName, upcCode);
                            }

                            break;
                    }
                }
            }
        }

        private void GetCategories(HtmlDocument doc, MetadataResult<Movie> result)
        {
            result.Item.Genres.Clear();

            var catAnchor = doc.DocumentNode.SelectSingleNode("//a[@name='categories']");

            if (catAnchor == null)
            {
                return;
            }

            var catList = catAnchor.ParentNode.Descendants("ul").FirstOrDefault();
            if (catList == null)
            {
                return;
            }

            var catItems = catList.Descendants("li").ToList();
            foreach (var item in catItems)
            {
                var itemAnchor = item.SelectSingleNode("a");

                if (itemAnchor != null && itemAnchor.NextSibling != null && itemAnchor.NextSibling is HtmlTextNode)
                {
                    var genre = itemAnchor.NextSibling.InnerText.Trim();
                    if (!string.IsNullOrEmpty(genre))
                    {
                        result.Item.AddGenre(itemAnchor.NextSibling.InnerText.Trim());
                    }
                }
            }
        }

        private void GetSynopsisAndTagLine(HtmlDocument doc, Movie item)
        {
            var h1Node = doc.DocumentNode.SelectSingleNode("//h1");
            if (h1Node != null)
            {
                item.Name = h1Node.InnerText.Trim();
            }

            var h4Nodes = doc.DocumentNode.SelectNodes("//h4");

            var synopsisNode = h4Nodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("synopsis"));
            if (synopsisNode == null)
            {
                return;
            }

            item.Overview = synopsisNode.InnerText.Trim();
        }

        private void GetCast(HtmlDocument doc, MetadataResult<Movie> result)
        {
            result.ResetPeople();

            var castAnchor = doc.DocumentNode.SelectSingleNode("//a[@name='cast']");

            if (castAnchor == null)
            {
                return;
            }

            var castList = castAnchor.ParentNode.Descendants("ul").FirstOrDefault();
            if (castList == null)
            {
                return;
            }

            var castItems = castList.Descendants("li").ToList();
            foreach (var item in castItems)
            {
                var itemAnchor = item.SelectSingleNode("a");

                if (itemAnchor != null)
                {
                    var person = new PersonInfo
                    {
                        Type = PersonType.Actor
                    };

                    var name = CleanItem(itemAnchor.InnerText, person);

                    person.Name = name;

                    if (itemAnchor.HasAttributes && itemAnchor.Attributes["href"] != null)
                    {
                        var href = itemAnchor.Attributes["href"].Value;
                        if (!string.IsNullOrEmpty(href))
                        {
                            var components = href.Split('/');
                            foreach (var part in components)
                            {
                                Int32 personId;
                                if (Int32.TryParse(part, out personId))
                                {
                                    person.SetProviderId(ExternalId.KeyName, personId.ToString());
                                }
                            }
                        }
                    }

                    result.AddPerson(person);
                }
            }
        }

        private string CleanItem(string name, PersonInfo person)
        {
            if (name.ToLower().Contains("director"))
            {
                name = name.Replace("Director", string.Empty).Replace("director", string.Empty).Trim();
                person.Type = PersonType.Director;
            }

            if (name.ToLower().Contains("producer"))
            {
                name = name.Replace("Producer", string.Empty).Replace("producer", string.Empty).Trim();
                person.Type = PersonType.Producer;
            }

            var endOfName = name.IndexOf(" - (", StringComparison.Ordinal);
            if (endOfName == -1)
            {
                return name.Trim();
            }

            return name.Substring(0, endOfName).Trim();
        }

        private async Task<List<SearchItem>> GetSearchItems(string name, CancellationToken cancellationToken)
        {
            var url = string.Format(SearchUrl, WebUtility.UrlEncode(name));

            string html;

            _logger.Info("Searching for: {0}", name);

            var options = this.CreateHttpRequestOptions(url, cancellationToken);

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                html = stream.ToStringFromStream();
            }

            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return ParseSearchHtml(doc);
        }

        private List<SearchItem> ParseSearchHtml(HtmlDocument doc)
        {
            var listNode = doc.DocumentNode.Descendants("div").ToList();
            var listView = listNode.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "item-list");
            if (listView == null)
            {
                return null;
            }

            var result = new List<SearchItem>();

            foreach (var node in listView.ChildNodes)
            {
                if (node.HasAttributes && node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("list-view-item"))
                {
                    try
                    {
                        var item = new SearchItem();

                        var id = node.Attributes["id"].Value;
                        item.Id = id.ToLower().Replace("item", string.Empty).Replace("_", string.Empty);

                        var img = node.SelectSingleNode(".//img");

                        if (img != null && img.Attributes["src"] != null)
                        {
                            item.ImageUrl = img.Attributes["src"].Value;
                        }

                        var h3 = node.SelectSingleNode(".//h3");

                        if (h3 != null)
                        {
                            var a = h3.SelectSingleNode("a");
                            if (a != null)
                            {
                                item.Name = HttpUtility.HtmlDecode(a.InnerText.Trim());
                            }

                            var smallNodes = h3.SelectNodes("small");
                            if (smallNodes != null && smallNodes.Count > 0)
                            {
                                var smallNode1 = smallNodes[0];
                                item.Rank = int.Parse(smallNode1.InnerText.Trim().Replace(".", string.Empty));
                            }

                            if (smallNodes != null && smallNodes.Count > 1)
                            {
                                var smallNode2 = smallNodes[1];
                                item.Year = int.Parse(smallNode2.InnerText.Trim().Replace("(", string.Empty).Replace(")", string.Empty));
                            }

                            result.Add(item);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            _logger.Info("Found {0} result(s)", result.Count);

            return result;
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var askedAssembly = new AssemblyName(args.Name);

            var resourceName = string.Format("MediaBrowser.Plugins.ADEProvider.Assets.Assemblies.{0}.dll", askedAssembly.Name);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }

        private HttpRequestOptions CreateHttpRequestOptions(string url, CancellationToken cancellationToken)
        {
            return new HttpRequestOptions()
            {
                CancellationToken = cancellationToken,
                Url = url,
                UserAgent = UserAgent,
                ResourcePool = Current.ResourcePool,
                LogErrorResponseBody = true,
                LogRequest = true
            };
        }

        private string UserAgent
        {
            get
            {
                var version = _applicationHost.ApplicationVersion.ToString();
                return string.Format("Emby/{0} +http://emby.media/", version);
            }
        }

        private class SearchItem
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public int Rank { get; set; }

            public int? Year { get; set; }

            public string ImageUrl { get; set; }

            public string Url
            {
                get
                {
                    return BaseUrl + Id;
                }
            }
        }
    }
}
