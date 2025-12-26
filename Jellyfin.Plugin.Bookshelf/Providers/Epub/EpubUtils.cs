using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Jellyfin.Plugin.Bookshelf.Providers.Epub;

/// <summary>
/// Epub utils.
/// </summary>
public static class EpubUtils
{
    /// <summary>
    /// Attempt to read content from zip archive.
    /// </summary>
    /// <param name="epub">The zip archive.</param>
    /// <returns>The content file path.</returns>
    public static string? ReadContentFilePath(ZipArchive epub)
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
