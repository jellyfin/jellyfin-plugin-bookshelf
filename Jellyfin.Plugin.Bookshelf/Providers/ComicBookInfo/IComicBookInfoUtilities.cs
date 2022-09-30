using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo
{
    /// <summary>
    /// Utilities to help read JSON metadata.
    /// </summary>
    public interface IComicBookInfoUtilities
    {
        /// <summary>
        /// Read comic book metadata.
        /// </summary>
        /// <param name="comic">The comic to read metadata from.</param>
        /// <returns>The resulting book.</returns>
        Book? ReadComicBookMetadata(ComicBookInfoMetadata comic);

        /// <summary>
        /// Read people metadata.
        /// </summary>
        /// <param name="comic">The comic to read metadata from.</param>
        /// <param name="metadataResult">The metadata result to update.</param>
        void ReadPeopleMetadata(ComicBookInfoMetadata comic, MetadataResult<Book> metadataResult);

        /// <summary>
        /// Converts a language to the three letter iso name of the language.
        /// </summary>
        /// <param name="language">The language to convert.</param>
        /// <returns>The language represented by an three letter iso name.</returns>
        string? ReadCultureInfoAsThreeLetterIsoInto(string language);
    }
}
