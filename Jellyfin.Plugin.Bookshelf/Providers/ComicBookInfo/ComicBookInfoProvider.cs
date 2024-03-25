using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives.Zip;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo
{
    /// <summary>
    /// Comic book info provider.
    /// </summary>
    public class ComicBookInfoProvider : IComicFileProvider, IComicBookInfoUtilities
    {
        private readonly ILogger<ComicBookInfoProvider> _logger;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicBookInfoProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ComicBookInfoProvider}"/> interface.</param>
        public ComicBookInfoProvider(IFileSystem fileSystem, ILogger<ComicBookInfoProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public async ValueTask<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetComicBookFile(info.Path)?.FullName;

            if (path is null)
            {
                _logger.LogError("Could not load Comic for {Path}", info.Path);
                return new MetadataResult<Book> { HasMetadata = false };
            }

            try
            {
                Stream stream = File.OpenRead(path);
                await using (stream.ConfigureAwait(false))
                using (var archive = ZipArchive.Open(stream)) // not yet async: https://github.com/adamhathcock/sharpcompress/pull/565
                {
                    if (archive.IsComplete)
                    {
                        var volume = archive.Volumes.First();
                        if (volume.Comment is null)
                        {
                            _logger.LogInformation("{Path} does not contain any ComicBookInfo metadata", info.Path);
                            return new MetadataResult<Book> { HasMetadata = false };
                        }

                        var comicBookMetadata = JsonSerializer.Deserialize<ComicBookInfoFormat>(volume.Comment, JsonDefaults.Options);
                        if (comicBookMetadata is null)
                        {
                            _logger.LogError("Failed to load ComicBookInfo metadata from archive comment for {Path}", info.Path);
                            return new MetadataResult<Book> { HasMetadata = false };
                        }

                        return SaveMetadata(comicBookMetadata);
                    }

                    _logger.LogError("Could not load ComicBookInfo metadata for {Path}", info.Path);
                    return new MetadataResult<Book> { HasMetadata = false };
                }
            }
            catch (Exception)
            {
                _logger.LogError("Failed to load ComicBookInfo metadata for {Path}", info.Path);
                return new MetadataResult<Book> { HasMetadata = false };
            }
        }

        /// <inheritdoc />
        public bool HasItemChanged(BaseItem item)
        {
            var file = GetComicBookFile(item.Path);

            if (file is null)
            {
                return false;
            }

            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        private MetadataResult<Book> SaveMetadata(ComicBookInfoFormat comic)
        {
            if (comic.Metadata is null)
            {
                return new MetadataResult<Book> { HasMetadata = false };
            }

            var book = ReadComicBookMetadata(comic.Metadata);

            if (book is null)
            {
                return new MetadataResult<Book> { HasMetadata = false };
            }

            var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

            if (comic.Metadata.Language is not null)
            {
                metadataResult.ResultLanguage = ReadCultureInfoInto(comic.Metadata.Language);
            }

            if (comic.Metadata.Credits.Count > 0)
            {
                ReadPeopleMetadata(comic.Metadata, metadataResult);
            }

            return metadataResult;
        }

        /// <inheritdoc />
        public Book? ReadComicBookMetadata(ComicBookInfoMetadata comic)
        {
            var book = new Book();
            var hasFoundMetadata = false;

            hasFoundMetadata |= ReadStringInto(comic.Title, title => book.Name = title);
            hasFoundMetadata |= ReadStringInto(comic.Series, series => book.SeriesName = series);
            hasFoundMetadata |= ReadStringInto(comic.Genre, genre => book.AddGenre(genre));
            hasFoundMetadata |= ReadStringInto(comic.Comments, overview => book.Overview = overview);
            hasFoundMetadata |= ReadStringInto(comic.Publisher, publisher => book.SetStudios(new[] { publisher }));

            if (comic.PublicationYear is not null)
            {
                book.ProductionYear = comic.PublicationYear;
                hasFoundMetadata = true;
            }

            if (comic.Issue is not null)
            {
                book.IndexNumber = comic.Issue;
                hasFoundMetadata = true;
            }

            if (comic.Tags.Count > 0)
            {
                book.Tags = comic.Tags.ToArray();
                hasFoundMetadata = true;
            }

            if (comic.PublicationYear is not null && comic.PublicationMonth is not null)
            {
                book.PremiereDate = ReadTwoPartDateInto(comic.PublicationYear.Value, comic.PublicationMonth.Value);
                hasFoundMetadata = true;
            }

            if (hasFoundMetadata)
            {
                return book;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public void ReadPeopleMetadata(ComicBookInfoMetadata comic, MetadataResult<Book> metadataResult)
        {
            foreach (var person in comic.Credits)
            {
                if (person.Person is null || person.Role is null)
                {
                    continue;
                }

                if (person.Person.Contains(',', StringComparison.InvariantCultureIgnoreCase))
                {
                    var name = person.Person.Split(',');
                    person.Person = name[1].Trim(' ') + " " + name[0].Trim(' ');
                }

                if (!Enum.TryParse(person.Role, out PersonKind personKind))
                {
                    personKind = PersonKind.Unknown;
                }

                if (string.Equals("Colorer", person.Role, StringComparison.OrdinalIgnoreCase))
                {
                    personKind = PersonKind.Colorist;
                }

                var personInfo = new PersonInfo { Name = person.Person, Type = personKind };
                metadataResult.AddPerson(personInfo);
            }
        }

        /// <inheritdoc />
        public string? ReadCultureInfoInto(string language)
        {
            try
            {
                return CultureInfo.GetCultureInfo(language).DisplayName;
            }
            catch (Exception)
            {
                // Ignored
                return null;
            }
        }

        private bool ReadStringInto(string? data, Action<string> commitResult)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                commitResult(data);
                return true;
            }

            return false;
        }

        private DateTime? ReadTwoPartDateInto(int year, int month)
        {
            // Try-Catch because DateTime actually wants a real date, how boring
            try
            {
                // The format does not provide a day, set it to be always the first day of the month
                var dateTime = new DateTime(year, month, 1);
                return dateTime;
            }
            catch (Exception)
            {
                // Nothing to do here
                return null;
            }
        }

        private FileSystemMetadata? GetComicBookFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.IsDirectory)
            {
                return null;
            }

            // Only parse files that are known to have internal metadata
            return fileInfo.Extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase) ? fileInfo : null;
        }
    }
}
