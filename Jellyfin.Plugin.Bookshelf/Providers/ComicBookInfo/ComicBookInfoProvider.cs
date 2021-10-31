using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

#nullable enable
namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo
{
    public class ComicBookInfoProvider : IComicFileProvider
    {
        private readonly ILogger<ComicBookInfoProvider> _logger;

        private readonly IFileSystem _fileSystem;

        public ComicBookInfoProvider(IFileSystem fileSystem, ILogger<ComicBookInfoProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetComicBookFile(info.Path)?.FullName;

            if (path is null)
            {
                _logger.LogError("Could not load Comic for {Path}", info.Path);
                return new MetadataResult<Book> { HasMetadata = false };
            }

            try
            {
                using Stream stream = File.OpenRead(path);
                // not yet async: https://github.com/adamhathcock/sharpcompress/pull/565
                using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(stream);

                if (archive.IsComplete)
                {
                    var volume = archive.Volumes.First();
                    if (volume.Comment != null)
                    {
                        var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(volume.Comment));
                        var comicBookMetadata = await JsonSerializer.DeserializeAsync<ComicBookInfoFormat>(jsonStream, new JsonSerializerOptions()
                        {
                            NumberHandling = JsonNumberHandling.AllowReadingFromString
                        }, cancellationToken);

                        if (comicBookMetadata is null)
                        {
                            _logger.LogError("Failed to load ComicBookInfo metadata from archive comment for {Path}", info.Path);
                            return new MetadataResult<Book> { HasMetadata = false };
                        }

                        return SaveMetadata(comicBookMetadata);
                    }
                    else
                    {
                        _logger.LogInformation("{Path} does not contain any ComicBookInfo metadata", info.Path);
                        return new MetadataResult<Book> { HasMetadata = false };
                    }
                }
                _logger.LogError("Could not load ComicBookInfo metadata for {Path}", info.Path);
                return new MetadataResult<Book> { HasMetadata = false };
            }
            catch (Exception)
            {
                _logger.LogError("Failed to load ComicBookInfo metadata for {Path}", info.Path);
                return new MetadataResult<Book> { HasMetadata = false };
            }
        }

        public bool HasItemChanged(BaseItem item, IDirectoryService directoryService)
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
            var book = new Book
            {
                Name = comic.Metadata.Title,
                SeriesName = comic.Metadata.Series,
                ProductionYear = comic.Metadata.PublicationYear,
                IndexNumber = comic.Metadata.Issue,
                Tags = comic.Metadata.Tags
            };

            book.SetStudios(new[] { comic.Metadata.Series });
            book.PremiereDate = ReadTwoPartDateInto(comic.Metadata.PublicationYear, comic.Metadata.PublicationMonth);
            book.AddGenre(comic.Metadata.Genre);

            var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };
            metadataResult.ResultLanguage = ReadCultureInfoAsThreeLetterIsoInto(comic.Metadata.Language);
            foreach (var person in comic.Metadata.Credits)
            {
                var personInfo = new PersonInfo { Name = person.Person, Type = person.Role };
                metadataResult.AddPerson(personInfo);
            }

            return metadataResult;
        }

        private string? ReadCultureInfoAsThreeLetterIsoInto(string language)
        {
            try
            {
                return new CultureInfo(language).ThreeLetterISOLanguageName;
            }
            catch (Exception)
            {
                //Ignored
                return null;
            }
        }

        private DateTime? ReadTwoPartDateInto(int year, int month)
        {
            //Try-Catch because DateTime actually wants a real date, how boring
            try
            {
                //The format does not provide a day, set it to be always the first day of the month
                var dateTime = new DateTime(year, month, 1);
                return dateTime;
            }
            catch (Exception)
            {
                //Nothing to do here
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
            if (fileInfo.Extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase))
            {
                return fileInfo;
            }
            else
            {
                return null;
            }
        }
    }
}
