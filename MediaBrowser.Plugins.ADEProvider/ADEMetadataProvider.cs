using HtmlAgilityPack;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MediaBrowser.Plugins.ADEProvider
{
    public class ADEMetadataProvider : IRemoteMetadataProvider<AdultVideo, ItemLookupInfo>
    {
        private const string BaseUrl = "http://www.adultdvdempire.com/";
        private const string SearchUrl = BaseUrl + "allsearch/search?q={0}";
        internal readonly SemaphoreSlim ResourcePool = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public static ADEMetadataProvider Current;

        public ADEMetadataProvider(ILogger logger, IHttpClient httpClient, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _logger = logger;
            _httpClient = httpClient;
            _appPaths = appPaths;
            _fileSystem = fileSystem;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            Current = this;
        }

        public async Task<MetadataResult<AdultVideo>> GetMetadata(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<AdultVideo>();

            var adeId = info.GetProviderId("AdultDvdEmpire");
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
                result.Item = new AdultVideo();
                result.Item.SetProviderId("AdultDvdEmpire", adeId);
                result.Item.Name = name;

                await GetItemDetails(result.Item, adeId, cancellationToken);

                result.HasMetadata = true;
            }

            return result;
        }

        public string Name
        {
            get { return "Adult Dvd Empire"; }
        }

        private async Task GetItemDetails(AdultVideo item, string id, CancellationToken cancellationToken)
        {
            string html;

            _logger.Info("Getting details for: {0}", id);

            using (var stream = await GetInfo(id, cancellationToken).ConfigureAwait(false))
            {
                html = stream.ToStringFromStream();
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var divNodes = doc.DocumentNode.Descendants("div").ToList();

            if (!item.LockedFields.Contains(MetadataFields.Cast))
            {
                GetCast(divNodes, item);
            }

            if (!item.LockedFields.Contains(MetadataFields.Overview))
            {
                GetSynopsisAndTagLine(divNodes, item);
            }

            if (!item.LockedFields.Contains(MetadataFields.Genres))
            {
                GetCategories(divNodes, item);
            }

            GetDetails(divNodes, item);

            item.OfficialRating = "XXX";
        }

        public async Task<Stream> GetInfo(string adeId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(adeId))
            {
                throw new ArgumentNullException("adeId");
            }

            var cachePath = Path.Combine(_appPaths.CachePath, "ade", _fileSystem.GetValidFilename(adeId), "item.html");

            var fileInfo = new FileInfo(cachePath);

            // Check cache first
            if (!fileInfo.Exists || (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays > 7)
            {
                var searchItem = new SearchItem { Id = adeId };

                // Download and cache
                using (var stream = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = searchItem.Url,
                    CancellationToken = cancellationToken,
                    ResourcePool = ResourcePool

                }))
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

        private void GetDetails(IEnumerable<HtmlNode> divNodes, AdultVideo item)
        {
            var detailsNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section ProductInfo");
            if (detailsNode == null)
            {
                return;
            }

            var productHtml = detailsNode.InnerHtml;
            var releasedIndex = productHtml.IndexOf("<strong>Released</strong>", StringComparison.Ordinal);
            if (releasedIndex != -1)
            {
                var released = productHtml.Substring(releasedIndex + "<strong>Released</strong>".Length);
                released = released.Substring(0, released.IndexOf("<br", StringComparison.Ordinal));
                DateTime releasedDate;
                if (DateTime.TryParse(released, out releasedDate))
                {
                    item.PremiereDate = releasedDate;
                }
            }

            var studioNode = detailsNode.Descendants("a").FirstOrDefault();
            if (studioNode != null)
            {
                item.AddStudio(studioNode.InnerText.Trim());
            }
        }

        private void GetCategories(IEnumerable<HtmlNode> divNodes, AdultVideo item)
        {
            var categoriesNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section Categories");
            if (categoriesNode == null)
            {
                return;
            }

            var cats = categoriesNode.Descendants("p").FirstOrDefault();
            if (cats == null)
            {
                return;
            }

            item.Genres.Clear();

            foreach (var cat in cats.InnerText.Split(new[] { ',' }))
            {
                item.AddGenre(cat.Trim());
            }
        }

        private void GetSynopsisAndTagLine(IEnumerable<HtmlNode> divNodes, AdultVideo item)
        {
            var synopsisNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section Synopsis");
            if (synopsisNode == null)
            {
                return;
            }

            var synopsisText = synopsisNode.InnerText.Replace("Synopsis", string.Empty);

            var tagline = synopsisNode.ChildNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Tagline");
            if (tagline != null && !string.IsNullOrEmpty(tagline.InnerText))
            {
                var hasTagline = item as IHasTaglines;
                if (hasTagline != null)
                {
                    hasTagline.AddTagline(tagline.InnerText.Trim());
                }
                synopsisText = synopsisText.Replace(tagline.InnerText, string.Empty);
            }

            item.Overview = synopsisText.Trim();
        }

        private void GetCast(IEnumerable<HtmlNode> divNodes, AdultVideo item)
        {
            var castNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section Cast");

            if (castNode == null)
            {
                return;
            }

            var castList = castNode.Descendants("ul").FirstOrDefault();
            if (castList == null)
            {
                return;
            }

            var realCastList = castList.Descendants("li").ToList();
            foreach (var cast in realCastList)
            {
                var person = new PersonInfo
                {
                    Type = PersonType.Actor
                };

                var name = CleanItem(cast.InnerText, person);

                person.Name = name;

                item.AddPerson(person);
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
            var url = string.Format(SearchUrl, name);

            string html;

            _logger.Info("Searching for: {0}", name);

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = ResourcePool,
                CancellationToken = cancellationToken
            }).ConfigureAwait(false))
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
            var listView = listNode.FirstOrDefault(x => x.HasAttributes && x.Attributes["id"] != null && x.Attributes["id"].Value == "ListView");
            if (listView == null)
            {
                return null;
            }

            var result = new List<SearchItem>();

            foreach (var node in listView.ChildNodes)
            {
                if (node.HasAttributes)
                {
                    // Ignore these items in the list, nothing to see here
                    if ((node.Attributes["class"] == null
                        || node.Attributes["class"].Value == "clear")
                        || (node.Attributes["id"] == null
                        || node.Attributes["id"].Value == "ListFooter"))
                    {
                        continue;
                    }

                    try
                    {
                        var item = new SearchItem();

                        var id = node.Attributes["id"].Value;
                        item.Id = id.ToLower().Replace("item", string.Empty).Replace("_", string.Empty);

                        var name = node.SelectSingleNode("//p[@class='title']");
                        if (name != null)
                        {
                            item.Name = HttpUtility.HtmlDecode(name.InnerText);
                        }

                        var rank = node.SelectSingleNode("//span[@class='rank']");
                        if (rank != null)
                        {
                            item.Rank = int.Parse(rank.InnerText.Replace(".", string.Empty));
                        }

                        result.Add(item);
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

            Assembly assembly;
            var resourceName = string.Format("MediaBrowser.Plugins.ADEProvider.Assets.Assemblies.{0}.dll", askedAssembly.Name);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                assembly = Assembly.Load(assemblyData);
            }

            return assembly;
        }

        private class SearchItem
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public int Rank { get; set; }

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
