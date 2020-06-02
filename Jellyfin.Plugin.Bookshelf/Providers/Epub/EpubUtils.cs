using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Jellyfin.Plugin.Bookshelf.Providers.Epub
{
    public static class EpubUtils
    {
        public static string ReadContentFilePath(ZipArchive epub)
        {
            var container = epub.GetEntry(Path.Combine("META-INF", "container.xml"));
            if (container == null)
            {
                return null;
            }

            using var containerStream = container.Open();

            XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";
            var containerDocument = XDocument.Load(containerStream);

            var element = containerDocument.Descendants(ns + "rootfile").FirstOrDefault();
            return element?.Attribute("full-path")?.Value;
        }
    }
}