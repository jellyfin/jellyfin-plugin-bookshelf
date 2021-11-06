using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

#nullable enable
namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBook
{
    public interface IComicInfoXmlUtilities
    {
        Book? ReadComicBookMetadata(XDocument xml);

        void ReadPeopleMetadata(XDocument xdocument, MetadataResult<Book> metadataResult);

        bool ReadCultureInfoInto(XDocument xml, string xPath, Action<CultureInfo> commitResult);
    }
}
