using System;
using System.Globalization;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicInfo;

/// <summary>
/// Xml utilities.
/// </summary>
public interface IComicInfoXmlUtilities
{
    /// <summary>
    /// Read comic book metadata.
    /// </summary>
    /// <param name="xml">The xdocument to read metadata from.</param>
    /// <returns>The resulting book.</returns>
    Book? ReadComicBookMetadata(XDocument xml);

    /// <summary>
    /// Read people metadata.
    /// </summary>
    /// <param name="xdocument">The xdocument to read metadata from.</param>
    /// <param name="metadataResult">The metadata result to update.</param>
    void ReadPeopleMetadata(XDocument xdocument, MetadataResult<Book> metadataResult);

    /// <summary>
    /// Read culture info.
    /// </summary>
    /// <param name="xml">the xdocument to read metadata from.</param>
    /// <param name="xPath">The xpath to search.</param>
    /// <param name="commitResult">The action to take after parsing.</param>
    /// <returns>Read status.</returns>
    bool ReadCultureInfoInto(XDocument xml, string xPath, Action<CultureInfo> commitResult);
}
