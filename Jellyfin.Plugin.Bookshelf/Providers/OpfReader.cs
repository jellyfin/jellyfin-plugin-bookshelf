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

        public static void ReadOpfData(
            MetadataResult<Book> bookResult,
            XmlDocument doc,
            CancellationToken cancellationToken,
            ILogger logger
        )
        {
            var book = bookResult.Item;
            
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
                    logger.LogError("Error parsing Calibre series index");
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
                    logger.LogError("Error parsing Calibre series name");
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
                    logger.LogError("Error parsing Calibre rating node");
                }
            }

        }
    }
}