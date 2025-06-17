using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks;
using Jellyfin.Plugin.Bookshelf.Providers.OpenLibrary;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.Audiobook
{
    /// <summary>
    /// Audiobook metadata provider that extracts local metadata and fetches additional info from Google Books.
    /// </summary>
    public class AudiobookMetadataProvider : ILocalMetadataProvider<AudioBook>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<AudiobookMetadataProvider> _logger;
        private readonly GoogleBooksProvider _googleBooksProvider;
        private readonly OpenLibraryProvider _openLibraryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudiobookMetadataProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{AudiobookMetadataProvider}"/> interface.</param>
        /// <param name="googleBooksProvider">Instance of the Google Books provider.</param>
        /// <param name="openLibraryProvider">Instance of the OpenLibrary provider.</param>
        public AudiobookMetadataProvider(
            IFileSystem fileSystem,
            ILogger<AudiobookMetadataProvider> logger,
            GoogleBooksProvider googleBooksProvider,
            OpenLibraryProvider openLibraryProvider)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _googleBooksProvider = googleBooksProvider;
            _openLibraryProvider = openLibraryProvider;
        }

        /// <inheritdoc />
        public string Name => "Audiobook Metadata";

        /// <inheritdoc />
        public async Task<MetadataResult<AudioBook>> GetMetadata(
            ItemInfo info,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            _logger.LogError("Processing Audiobook metadata for {Path}", info.Path);
            var path = GetAudiobookFile(info.Path)?.FullName;

            if (path is null)
            {
                return new MetadataResult<AudioBook> { HasMetadata = false };
            }

            try
            {
                // First extract metadata from the Audiobook file itself
                var localResult = ExtractLocalMetadata(path, cancellationToken);
                if (localResult?.Item == null)
                {
                    return new MetadataResult<AudioBook> { HasMetadata = false };
                }

                // Create BookInfo for Google Books search using the extracted metadata
                var bookInfo = new BookInfo
                {
                    Name = localResult.Item.Name,
                    Path = path
                };

                // Note: BookInfo doesn't have an AuthorName property, but Google Books search
                // can still work effectively with just the title and other metadata

                // Try to get additional metadata from Google Books
                var googleResult = await _googleBooksProvider.GetMetadata(bookInfo, cancellationToken).ConfigureAwait(false);

                if (googleResult.HasMetadata && googleResult.Item != null)
                {
                    // Merge Google Books metadata with local metadata
                    var mergedResult = MergeMetadata(localResult, googleResult);

                    // Check if we need additional metadata from OpenLibrary
                    if (NeedsAdditionalMetadata(mergedResult.Item))
                    {
                        var openLibraryResult = await _openLibraryProvider.GetMetadata(bookInfo, cancellationToken).ConfigureAwait(false);
                        if (openLibraryResult.HasMetadata && openLibraryResult.Item != null)
                        {
                            mergedResult = MergeWithOpenLibrary(mergedResult, openLibraryResult);
                        }
                    }

                    return mergedResult;
                }

                // Google Books failed, try OpenLibrary as primary fallback
                var openLibraryFallbackResult = await _openLibraryProvider.GetMetadata(bookInfo, cancellationToken).ConfigureAwait(false);
                if (openLibraryFallbackResult.HasMetadata && openLibraryFallbackResult.Item != null)
                {
                    var mergedWithOpenLibrary = MergeWithOpenLibrary(localResult, openLibraryFallbackResult);
                    return mergedWithOpenLibrary;
                }

                // Return local metadata if both Google Books and OpenLibrary fail
                return localResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Audiobook metadata for {Path}", path);
                return new MetadataResult<AudioBook> { HasMetadata = false };
            }
        }

        private FileSystemMetadata? GetAudiobookFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.IsDirectory)
            {
                return null;
            }

            if (!AudiobookUtils.SupportedExtensions.Contains(
                    Path.GetExtension(fileInfo.FullName),
                    StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            return fileInfo;
        }

        private MetadataResult<AudioBook> ExtractLocalMetadata(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var file = TagLib.File.Create(path);
                var tagReader = new AudiobookTagReader<AudiobookMetadataProvider>(file, _logger);
                return tagReader.ReadMetadata(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read tags from Audiobook file: {Path}", path);
                return new MetadataResult<AudioBook> { HasMetadata = false };
            }
        }

        private MetadataResult<AudioBook> MergeMetadata(MetadataResult<AudioBook> localResult, MetadataResult<Book> googleResult)
        {
            if (localResult.Item == null || googleResult.Item == null)
            {
                return localResult;
            }

            var mergedBook = localResult.Item;

            // Prefer Google Books description over local (local often contains purchase info)
            if (!string.IsNullOrEmpty(googleResult.Item.Overview))
            {
                mergedBook.Overview = googleResult.Item.Overview;
            }

            // Merge genres from Google Books
            if (googleResult.Item.Genres != null)
            {
                foreach (var genre in googleResult.Item.Genres)
                {
                    if (!mergedBook.Genres.Contains(genre))
                    {
                        mergedBook.AddGenre(genre);
                    }
                }
            }

            // Add tags from Google Books
            if (googleResult.Item.Tags != null)
            {
                foreach (var tag in googleResult.Item.Tags)
                {
                    if (!mergedBook.Tags.Contains(tag))
                    {
                        mergedBook.AddTag(tag);
                    }
                }
            }

            // Use Google Books publication year if local doesn't have one
            if (mergedBook.ProductionYear == null && googleResult.Item.ProductionYear.HasValue)
            {
                mergedBook.ProductionYear = googleResult.Item.ProductionYear;
                mergedBook.PremiereDate = new DateTime(googleResult.Item.ProductionYear.Value, 1, 1);
            }

            // Add publisher from Google Books if not available locally
            if (googleResult.Item.Studios != null)
            {
                foreach (var studio in googleResult.Item.Studios)
                {
                    if (!mergedBook.Studios.Contains(studio))
                    {
                        mergedBook.AddStudio(studio);
                    }
                }
            }

            // Use community rating from Google Books if available
            if (mergedBook.CommunityRating == null && googleResult.Item.CommunityRating.HasValue)
            {
                mergedBook.CommunityRating = googleResult.Item.CommunityRating;
            }

            // Merge provider IDs
            if (googleResult.Item.ProviderIds != null)
            {
                foreach (var providerId in googleResult.Item.ProviderIds)
                {
                    mergedBook.SetProviderId(providerId.Key, providerId.Value);
                }
            }

            // Merge people, but prefer local authors
            var mergedResult = new MetadataResult<AudioBook>
            {
                Item = mergedBook,
                HasMetadata = true,
                People = new List<PersonInfo>(localResult.People ?? new List<PersonInfo>())
            };

            // Add non-author people from Google Books
            if (googleResult.People != null)
            {
                foreach (var person in googleResult.People)
                {
                    if (person.Type != Jellyfin.Data.Enums.PersonKind.Author)
                    {
                        mergedResult.AddPerson(person);
                    }
                }
            }

            // Use Google Books language if available
            if (!string.IsNullOrEmpty(googleResult.ResultLanguage))
            {
                mergedResult.ResultLanguage = googleResult.ResultLanguage;
            }

            return mergedResult;
        }

        private static bool NeedsAdditionalMetadata(AudioBook book)
        {
            // Check if we're missing key metadata that OpenLibrary might provide
            return string.IsNullOrEmpty(book.Overview) ||
                   string.IsNullOrEmpty(book.SeriesName) ||
                   book.Genres.Length == 0;
        }

        private MetadataResult<AudioBook> MergeWithOpenLibrary(MetadataResult<AudioBook> existing, MetadataResult<Book> openLibraryResult)
        {
            if (existing.Item == null || openLibraryResult.Item == null)
            {
                return existing;
            }

            var mergedBook = existing.Item;

            // Use OpenLibrary description if we don't have one
            if (string.IsNullOrEmpty(mergedBook.Overview) && !string.IsNullOrEmpty(openLibraryResult.Item.Overview))
            {
                mergedBook.Overview = openLibraryResult.Item.Overview;
            }

            // Use OpenLibrary series information if missing
            if (string.IsNullOrEmpty(mergedBook.SeriesName) && !string.IsNullOrEmpty(openLibraryResult.Item.SeriesName))
            {
                // Parse series name and number if in format "Series Name #1"
                if (AudiobookUtils.TryParseSeriesInfo(openLibraryResult.Item.SeriesName, out var parsedSeriesName, out var parsedBookNumber))
                {
                    mergedBook.SeriesName = parsedSeriesName;
                    if (parsedBookNumber.HasValue && !mergedBook.IndexNumber.HasValue)
                    {
                        mergedBook.IndexNumber = parsedBookNumber;
                    }

                }
                else
                {
                    mergedBook.SeriesName = openLibraryResult.Item.SeriesName;
                }
            }
            else if (!string.IsNullOrEmpty(mergedBook.SeriesName))
            {
            }
            else if (string.IsNullOrEmpty(openLibraryResult.Item.SeriesName))
            {
            }

            // Merge genres from OpenLibrary
            if (openLibraryResult.Item.Genres != null)
            {
                foreach (var genre in openLibraryResult.Item.Genres)
                {
                    if (!mergedBook.Genres.Contains(genre))
                    {
                        mergedBook.AddGenre(genre);
                    }
                }
            }

            // Use OpenLibrary publication year if missing
            if (mergedBook.ProductionYear == null && openLibraryResult.Item.ProductionYear.HasValue)
            {
                mergedBook.ProductionYear = openLibraryResult.Item.ProductionYear;
                mergedBook.PremiereDate = new DateTime(openLibraryResult.Item.ProductionYear.Value, 1, 1);
            }

            // Add publisher from OpenLibrary if not available
            if (openLibraryResult.Item.Studios != null)
            {
                foreach (var studio in openLibraryResult.Item.Studios)
                {
                    if (!mergedBook.Studios.Contains(studio))
                    {
                        mergedBook.AddStudio(studio);
                    }
                }
            }

            // Merge provider IDs
            if (openLibraryResult.Item.ProviderIds != null)
            {
                foreach (var providerId in openLibraryResult.Item.ProviderIds)
                {
                    mergedBook.SetProviderId(providerId.Key, providerId.Value);
                }
            }

            // Merge author information from OpenLibrary (authors only, biographies logged separately)
            var mergedPeople = new List<PersonInfo>(existing.People ?? new List<PersonInfo>());
            if (openLibraryResult.People != null)
            {
                foreach (var openLibraryPerson in openLibraryResult.People)
                {
                    // Check if we already have this author (by name and type)
                    var existingPerson = mergedPeople.FirstOrDefault(p =>
                        p.Name.Equals(openLibraryPerson.Name, StringComparison.OrdinalIgnoreCase) &&
                        p.Type == openLibraryPerson.Type);

                    if (existingPerson == null)
                    {
                        // Add new author (biography info was logged during OpenLibrary processing)
                        mergedPeople.Add(openLibraryPerson);
                    }
                }
            }

            return new MetadataResult<AudioBook>
            {
                Item = mergedBook,
                HasMetadata = true,
                People = mergedPeople,
                ResultLanguage = existing.ResultLanguage
            };
        }
    }
}
