using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    /// <summary>
    /// The ComicBookImageProvider tries find either a image named "cover" or,
    /// in case that fails, just takes the first image inside the archive,
    /// hoping that it is the cover.
    /// </summary>
    public class ComicBookImageProvider : IDynamicImageProvider
    {
        private const string CbzFileExtension = ".cbz";

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
        public string Name => "Comic Book Zip Archive Cover Extractor";

        /// <inheritdoc />
        public async Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            // Check if the file is a .cbz file
            var extension = Path.GetExtension(item.Path);
            if (string.Equals(extension, CbzFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return await LoadCover(item).ConfigureAwait(false);
            }
            else
            {
                return new DynamicImageResponse { HasImage = false };
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
                // Open the .cbz
                // This should return a valid reference or throw
                using var archive = ZipFile.OpenRead(item.Path);

                // If no cover is found, throw exception to log results
                var (cover, imageFormat) = FindCoverEntryInZip(archive) ?? throw new InvalidOperationException("No supported cover found");

                // Copy our cover to memory stream
                await cover.Open().CopyToAsync(memoryStream).ConfigureAwait(false);

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
        private (ZipArchiveEntry coverEntry, ImageFormat imageFormat)? FindCoverEntryInZip(ZipArchive archive)
        {
            foreach (ImageFormat imageFormat in Enum.GetValues(typeof(ImageFormat)))
            {
                var extension = GetExtension(imageFormat);

                // There are comics with a cover file, but others with varying names for the cover
                // e.g. attackontitan_vol1_Page_001 with no indication that this is the cover except
                // that it is the first jpeg entry (and page)
                var cover = archive.GetEntry("cover" + extension)
                            ?? archive.Entries
                                .OrderBy(x => x.Name)
                                .FirstOrDefault(x => x.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

                // If we have found something, return immediately
                if (cover is not null)
                {
                    return (cover, imageFormat);
                }
            }

            return null;
        }

        private string GetExtension(ImageFormat imageFormat) => imageFormat switch
        {
            ImageFormat.Jpg => ".jpg",
            ImageFormat.Png => ".png",
            ImageFormat.Webp => ".webp",
            ImageFormat.Bmp => ".bmp",
            ImageFormat.Gif => ".gif",
            _ => throw new ArgumentException($"Unsupported ComicCoverType: {imageFormat.GetType()}"),
        };
    }
}
