using System;
using System.Linq;
using System.Threading;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    public static class OpfReader
    {
        private const string DcNamespace = @"http://purl.org/dc/elements/1.1/";
        private const string OpfNamespace = @"http://www.idpf.org/2007/opf";

        public static void ReadOpfData<TCategoryName>(
            MetadataResult<Book> bookResult,
            XmlDocument doc,
            CancellationToken cancellationToken,
            ILogger<TCategoryName> logger
        )
        {
            var book = bookResult.Item;

            cancellationToken.ThrowIfCancellationRequested();

            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("dc", DcNamespace);
            namespaceManager.AddNamespace("opf", OpfNamespace);

            var nameNode = doc.SelectSingleNode("//dc:title", namespaceManager);

            if (!string.IsNullOrEmpty(nameNode?.InnerText))
                book.Name = nameNode.InnerText;

            var overViewNode = doc.SelectSingleNode("//dc:description", namespaceManager);

            if (!string.IsNullOrEmpty(overViewNode?.InnerText))
                book.Overview = overViewNode.InnerText;


            var studioNode = doc.SelectSingleNode("//dc:publisher", namespaceManager);

            if (!string.IsNullOrEmpty(studioNode?.InnerText))
                book.AddStudio(studioNode.InnerText);

            var isbnNode = doc.SelectSingleNode("//dc:identifier[@opf:scheme='ISBN']", namespaceManager);

            if (!string.IsNullOrEmpty(isbnNode?.InnerText))
                book.SetProviderId("ISBN", isbnNode.InnerText);

            var amazonNode = doc.SelectSingleNode("//dc:identifier[@opf:scheme='AMAZON']", namespaceManager);

            if (!string.IsNullOrEmpty(amazonNode?.InnerText))
                book.SetProviderId("Amazon", amazonNode.InnerText);

            var genresNodes = doc.SelectNodes("//dc:subject", namespaceManager);

            if (genresNodes != null && genresNodes.Count > 0)
            {
                foreach (var node in genresNodes.Cast<XmlNode>().Where(node => !book.Tags.Contains(node.InnerText)))
                {
                    // Adding to tags because we can't be sure the values are all genres
                    book.AddGenre(node.InnerText);
                }
            }

            var authorNode = doc.SelectSingleNode("//dc:creator", namespaceManager);

            if (!string.IsNullOrEmpty(authorNode?.InnerText))
            {
                var person = new PersonInfo {Name = authorNode.InnerText, Type = "Author"};

                bookResult.AddPerson(person);
            }

            var seriesIndexNode = doc.SelectSingleNode("//opf:meta[@name='calibre:series_index']", namespaceManager);

            if (!string.IsNullOrEmpty(seriesIndexNode?.Attributes?["content"]?.Value))
            {
                try
                {
                    book.IndexNumber = Convert.ToInt32(seriesIndexNode.Attributes["content"].Value);
                }
                catch (Exception)
                {
                    logger.LogError("Error parsing Calibre series index");
                }
            }

            var seriesNameNode = doc.SelectSingleNode("//opf:meta[@name='calibre:series']", namespaceManager);

            if (!string.IsNullOrEmpty(seriesNameNode?.Attributes?["content"]?.Value))
            {
                try
                {
                    book.SeriesName = seriesNameNode.Attributes["content"].Value;
                }
                catch (Exception)
                {
                    logger.LogError("Error parsing Calibre series name");
                }
            }

            var ratingNode = doc.SelectSingleNode("//opf:meta[@name='calibre:rating']", namespaceManager);

            if (!string.IsNullOrEmpty(ratingNode?.Attributes?["content"]?.Value))
            {
                try
                {
                    book.CommunityRating = Convert.ToInt32(ratingNode.Attributes["content"].Value);
                }
                catch (Exception)
                {
                    logger.LogError("Error parsing Calibre rating node");
                }
            }
        }
    }
}