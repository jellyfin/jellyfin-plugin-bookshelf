using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    /// <summary>
    /// The ComicBookImageProvider tries find either a image named "cover" or,
    /// in case that fails, just takes the first image inside the archive,
    /// hoping that it is the cover.
    /// </summary>
    public class ComicBookImageProvider : IDynamicImageProvider
    {
        private readonly string[] _comicBookExtensions = [".cb7", ".cbr", ".cbt", ".cbz"];
        private readonly string[] _coverExtensions = [".png", ".jpeg", ".jpg", ".webp", ".bmp", ".gif"];

        private readonly ILogger<ComicBookImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicBookImageProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ComicBookImageProvider}"/> interface.</param>
        public ComicBookImageProvider(ILogger<ComicBookImageProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Comic Book Archive Cover Extractor";

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            // Check if the file is a .cbz file
            var extension = Path.GetExtension(item.Path);
            if (_comicBookExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return LoadCover(item);
            }
            else
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Book;
        }

        /// <summary>
        /// Tries to load a cover from the .cbz archive. Returns a response
        /// with no image if nothing is found.
        /// </summary>
        /// <param name="item">Item to load a cover for.</param>
        private async Task<DynamicImageResponse> LoadCover(BaseItem item)
        {
            // The image will be loaded into memory, create stream
            var memoryStream = new MemoryStream();
            try
            {
                ImageFormat imageFormat;
                // Open the .cbz
                // This should return a valid reference or throw
                using (Stream stream = File.OpenRead(item.Path))
                using (var archive = ArchiveFactory.Open(stream))
                {
                    // If no cover is found, throw exception to log results
                    IArchiveEntry cover;
                    (cover, imageFormat) = FindCoverEntryInArchive(archive) ?? throw new InvalidOperationException("No supported cover found");

                    // Copy our cover to memory stream
                    await cover.OpenEntryStream().CopyToAsync(memoryStream).ConfigureAwait(false);
                }

                // Reset stream position after copying
                memoryStream.Position = 0;

                // Return the response
                return new DynamicImageResponse
                {
                    HasImage = true,
                    Stream = memoryStream,
                    Format = imageFormat,
                };
            }
            catch (Exception e)
            {
                // Log and return nothing
                _logger.LogError(e, "Failed to load cover from {Path}", item.Path);
                return new DynamicImageResponse { HasImage = false };
            }
        }

        /// <summary>
        /// Tries to find the entry containing the cover.
        /// </summary>
        /// <param name="archive">The archive to search.</param>
        /// <returns>The search result.</returns>
        private (IArchiveEntry CoverEntry, ImageFormat ImageFormat)? FindCoverEntryInArchive(IArchive archive)
        {
            IArchiveEntry? cover = null;

            // There are comics with a cover file, but others with varying names for the cover
            // e.g. attackontitan_vol1_Page_001 with no indication that this is the cover except
            // that it is the first jpeg entry (and page)
            foreach (var extension in _coverExtensions)
            {
                cover = archive.Entries.FirstOrDefault(e => e.Key == "cover" + extension);
                if (cover is not null)
                {
                    var imageFormat = GetImageFormat(extension);
                    // If we have found something, return immediately
                    return (cover, imageFormat);
                }
            }

            {
                cover = archive.Entries.OrderBy(x => x.Key).FirstOrDefault(x => _coverExtensions.Contains(Path.GetExtension(x.Key), StringComparison.OrdinalIgnoreCase));
                if (cover is not null)
                {
                    var imageFormat = GetImageFormat(Path.GetExtension(cover.Key ?? string.Empty));
                    return (cover, imageFormat);
                }
            }

            return null;
        }

        private static ImageFormat GetImageFormat(string extension) => extension.ToLowerInvariant() switch
        {
            ".jpg" => ImageFormat.Jpg,
            ".jpeg" => ImageFormat.Jpg,
            ".png" => ImageFormat.Png,
            ".webp" => ImageFormat.Webp,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            ".svg" => ImageFormat.Svg,
            _ => throw new ArgumentException($"Unsupported extension: {extension}"),
        };
    }
}
