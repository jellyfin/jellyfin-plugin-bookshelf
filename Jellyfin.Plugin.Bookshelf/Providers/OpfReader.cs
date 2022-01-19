using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    /// <summary>
    /// OPF reader.
    /// </summary>
    /// <typeparam name="TCategoryName">The type of category.</typeparam>
    public class OpfReader<TCategoryName>
    {
        private const string DcNamespace = @"http://purl.org/dc/elements/1.1/";
        private const string OpfNamespace = @"http://www.idpf.org/2007/opf";

        private readonly XmlNamespaceManager _namespaceManager;

        private readonly XmlDocument _document;

        private readonly ILogger<TCategoryName> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpfReader{TCategoryName}"/> class.
        /// </summary>
        /// <param name="doc">The xdocument to parse.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        public OpfReader(XmlDocument doc, ILogger<TCategoryName> logger)
        {
            _document = doc;
            _logger = logger;
            _namespaceManager = new XmlNamespaceManager(_document.NameTable);
            _namespaceManager.AddNamespace("dc", DcNamespace);
            _namespaceManager.AddNamespace("opf", OpfNamespace);
        }

        /// <summary>
        /// Checks the file path for the existence of a cover.
        /// </summary>
        /// <param name="opfRootDirectory">The root directory in which the opf metadata file is located.</param>
        /// <returns>Returns the found cover and it's type or null.</returns>
        public (string MimeType, string Path)? ReadCoverPath(string opfRootDirectory)
        {
            var coverImage = ReadEpubCoverInto(opfRootDirectory, "//opf:item[@properties='cover-image']");
            if (coverImage is not null)
            {
                return coverImage;
            }

            var coverId = ReadEpubCoverInto(opfRootDirectory, "//opf:item[@id='cover']");
            if (coverId is not null)
            {
                return coverId;
            }

            var coverImageId = ReadEpubCoverInto(opfRootDirectory, "//opf:item[@id='cover-image']");
            if (coverImageId is not null)
            {
                return coverImageId;
            }

            var metaCoverImage = _document.SelectSingleNode("//opf:meta[@name='cover']", _namespaceManager);
            var content = metaCoverImage?.Attributes?["content"]?.Value;
            if (string.IsNullOrEmpty(content) || metaCoverImage is null)
            {
                return null;
            }

            var coverPath = Path.Combine("Images", content);
            var coverFileManifest = _document.SelectSingleNode($"//opf:item[@href='{coverPath}']", _namespaceManager);
            var mediaType = coverFileManifest?.Attributes?["media-type"]?.Value;
            if (coverFileManifest?.Attributes is not null
                            && !string.IsNullOrEmpty(mediaType) && IsValidImage(mediaType))
            {
                return (mediaType, Path.Combine(opfRootDirectory, coverPath));
            }

            var coverFileIdManifest = _document.SelectSingleNode($"//opf:item[@id='{content}']", _namespaceManager);
            if (coverFileIdManifest is not null)
            {
                return ReadManifestItem(coverFileIdManifest, opfRootDirectory);
            }

            return null;
        }

        /// <summary>
        /// Read opf data.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The metadata result to update.</returns>
        public MetadataResult<Book> ReadOpfData(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var book = CreateBookFromOpf();
            var bookResult = new MetadataResult<Book> { Item = book, HasMetadata = true };
            ReadStringInto("//dc:creator", author =>
            {
                var person = new PersonInfo { Name = author, Type = "Author" };
                bookResult.AddPerson(person);
            });

            ReadStringInto("//dc:language", language => bookResult.ResultLanguage = language);

            return bookResult;
        }

        private Book CreateBookFromOpf()
        {
            var book = new Book();

            ReadStringInto("//dc:title", title => book.Name = title);
            ReadStringInto("//dc:description", summary => book.Overview = summary);
            ReadStringInto("//dc:publisher", publisher => book.AddStudio(publisher));
            ReadStringInto("//dc:identifier[@opf:scheme='ISBN']", isbn => book.SetProviderId("ISBN", isbn));
            ReadStringInto("//dc:identifier[@opf:scheme='AMAZON']", amazon => book.SetProviderId("Amazon", amazon));

            ReadStringInto("//dc:date", date =>
            {
                DateTime dateValue;
                if (DateTime.TryParse(date, out dateValue))
                {
                    book.PremiereDate = dateValue.Date;
                    book.ProductionYear = dateValue.Date.Year;
                }
            });

            var genresNodes = _document.SelectNodes("//dc:subject", _namespaceManager);

            if (genresNodes != null && genresNodes.Count > 0)
            {
                foreach (var node in genresNodes.Cast<XmlNode>().Where(node => !book.Tags.Contains(node.InnerText)))
                {
                    // Adding to tags because we can't be sure the values are all genres
                    book.AddGenre(node.InnerText);
                }
            }

            ReadInt32AttributeInto("//opf:meta[@name='calibre:series_index']", index => book.IndexNumber = index);
            ReadInt32AttributeInto("//opf:meta[@name='calibre:rating']", rating => book.CommunityRating = rating);

            var seriesNameNode = _document.SelectSingleNode("//opf:meta[@name='calibre:series']", _namespaceManager);

            if (!string.IsNullOrEmpty(seriesNameNode?.Attributes?["content"]?.Value))
            {
                try
                {
                    book.SeriesName = seriesNameNode.Attributes["content"]?.Value;
                }
                catch (Exception)
                {
                    _logger.LogError("Error parsing Calibre series name");
                }
            }

            return book;
        }

        private void ReadStringInto(string xPath, Action<string> commitResult)
        {
            var resultElement = _document.SelectSingleNode(xPath, _namespaceManager);
            if (resultElement is not null && !string.IsNullOrWhiteSpace(resultElement.InnerText))
            {
                commitResult(resultElement.InnerText);
            }
        }

        private void ReadInt32AttributeInto(string xPath, Action<int> commitResult)
        {
            var resultElement = _document.SelectSingleNode(xPath, _namespaceManager);
            var resultValue = resultElement?.Attributes?["content"]?.Value;
            if (!string.IsNullOrEmpty(resultValue))
            {
                try
                {
                    commitResult(Convert.ToInt32(resultValue, CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error converting to int32");
                }
            }
        }

        private (string MimeType, string Path)? ReadEpubCoverInto(string opfRootDirectory, string xPath)
        {
            var resultElement = _document.SelectSingleNode(xPath, _namespaceManager);
            if (resultElement is not null)
            {
                var resultValue = ReadManifestItem(resultElement, opfRootDirectory);
                return resultValue;
            }

            return null;
        }

        private (string MimeType, string Path)? ReadManifestItem(XmlNode manifestNode, string opfRootDirectory)
        {
            var href = manifestNode.Attributes?["href"]?.Value;
            var mediaType = manifestNode.Attributes?["media-type"]?.Value;

            if (string.IsNullOrEmpty(href)
                || string.IsNullOrEmpty(mediaType)
                || !IsValidImage(mediaType))
            {
                return null;
            }

            var coverPath = Path.Combine(opfRootDirectory, href);
            return (MimeType: mediaType, Path: coverPath);
        }

        private bool IsValidImage(string? mimeType)
        {
            return !string.IsNullOrEmpty(mimeType)
                   && !string.IsNullOrWhiteSpace(MimeTypes.ToExtension(mimeType));
        }
    }
}
