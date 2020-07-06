using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    public class BookProviderFromOpf : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
    {
        private const string StandardOpfFile = "content.opf";
        private const string CalibreOpfFile = "metadata.opf";

        private const string DcNamespace = @"http://purl.org/dc/elements/1.1/";
        private const string OpfNamespace = @"http://www.idpf.org/2007/opf";

        private readonly ILogger<BookProviderFromOpf> _logger;
        private readonly IFileSystem _fileSystem;

        public BookProviderFromOpf(IFileSystem fileSystem, ILogger<BookProviderFromOpf> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string Name => "Open Packaging Format";

        private FileSystemMetadata GetXmlFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path));

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".opf");

            var file = _fileSystem.GetFileInfo(specificFile);

            if (file.Exists)
            {
                return file;
            }

            file = _fileSystem.GetFileInfo(Path.Combine(directoryPath, StandardOpfFile));

            return file.Exists ? file : _fileSystem.GetFileInfo(Path.Combine(directoryPath, CalibreOpfFile));
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var file = GetXmlFile(item.Path);
            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        public Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetXmlFile(info.Path).FullName;
            var result = new MetadataResult<Book>();

            try
            {
                var item = new Book();
                result.HasMetadata = true;
                result.Item = item;
                ReadOpfData(result, path, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }

            return Task.FromResult(result);
        }

        private void ReadOpfData(MetadataResult<Book> bookResult, string metaFile, CancellationToken cancellationToken)
        {
            var book = bookResult.Item;

            cancellationToken.ThrowIfCancellationRequested();

            var doc = new XmlDocument();
            doc.Load(metaFile);

            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("dc", DcNamespace);
            namespaceManager.AddNamespace("opf", OpfNamespace);

            var nameNode = doc.SelectSingleNode("//dc:title", namespaceManager);

            if (nameNode != null)
                book.Name = nameNode.InnerText;

            var overViewNode = doc.SelectSingleNode("//dc:description", namespaceManager);

            if (overViewNode != null)
                book.Overview = overViewNode.InnerText;


            var studioNode = doc.SelectSingleNode("//dc:publisher", namespaceManager);

            if (studioNode != null)
                book.AddStudio(studioNode.InnerText);

            var isbnNode = doc.SelectSingleNode("//dc:identifier[@opf:scheme='ISBN']", namespaceManager);

            if (isbnNode != null)
                book.SetProviderId("ISBN", isbnNode.InnerText);

            var amazonNode = doc.SelectSingleNode("//dc:identifier[@opf:scheme='AMAZON']", namespaceManager);

            if (amazonNode != null)
                book.SetProviderId("Amazon", amazonNode.InnerText);

            var genresNodes = doc.SelectNodes("//dc:subject", namespaceManager);

            if (genresNodes != null && genresNodes.Count > 0)
            {
                foreach (var node in genresNodes.Cast<XmlNode>().Where(node => !book.Tags.Contains(node.InnerText)))
                {
                    // Adding to tags because we can't be sure the values are all genres
                    book.Genres.Append(node.InnerText);
                }
            }

            var authorNode = doc.SelectSingleNode("//dc:creator", namespaceManager);

            if (authorNode != null)
            {
                var person = new PersonInfo { Name = authorNode.InnerText, Type = "Author" };

                bookResult.People.Add(person);
            }

            var seriesIndexNode = doc.SelectSingleNode("//opf:meta[@name='calibre:series_index']", namespaceManager);

            if (seriesIndexNode != null && seriesIndexNode.Attributes != null)
            {
                try
                {
                    book.IndexNumber = Convert.ToInt32(seriesIndexNode.Attributes["content"].Value);
                }
                catch (Exception)
                {
                    _logger.LogError("Error parsing Calibre series index");
                }
            }

            var seriesNameNode = doc.SelectSingleNode("//opf:meta[@name='calibre:series']", namespaceManager);

            if (seriesNameNode != null && seriesNameNode.Attributes != null)
            {
                try
                {
                    book.SeriesName = seriesNameNode.Attributes["content"].Value;
                }
                catch (Exception)
                {
                    _logger.LogError("Error parsing Calibre series name");
                }
            }

            var ratingNode = doc.SelectSingleNode("//opf:meta[@name='calibre:rating']", namespaceManager);

            if (ratingNode != null && ratingNode.Attributes != null)
            {
                try
                {
                    book.CommunityRating = Convert.ToInt32(ratingNode.Attributes["content"].Value);
                }
                catch (Exception)
                {
                    _logger.LogError("Error parsing Calibre rating node");
                }
            }
        }
    }
}
