using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    /// <summary>
    /// The PdfImageProvider extracts the first page of a PDF as an image.
    /// </summary>
    public class PdfImageProvider : IDynamicImageProvider
    {
        private readonly ILogger<PdfImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfImageProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{PdfImageProvider}"/> interface.</param>
        public PdfImageProvider(ILogger<PdfImageProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "PDF Cover Extractor";

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            // Check if the file is a .pdf file
            var extension = Path.GetExtension(item.Path);
            if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(LoadCover(item));
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
        /// Load the cover from the PDF file.
        /// </summary>
        /// <param name="item">Item to load a cover for.</param>
        private DynamicImageResponse LoadCover(BaseItem item)
        {
            // The image will be loaded into memory, create stream
            var memoryStream = new MemoryStream();
            try
            {
                // Open the .pdf
                using var docReader = DocLib.Instance.GetDocReader(
                    item.Path,
                    new PageDimensions(1080, 1920));

                using var pageReader = docReader.GetPageReader(0);

                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();
                var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888);

                var bytes = pageReader.GetImage(new NaiveTransparencyRemover());

                // Convert to PNG using SkiaSharp
                using var image = SKImage.FromPixelCopy(imageInfo, bytes);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                data.SaveTo(memoryStream);

                // If no image data was written, throw exception to log results
                if (memoryStream.Length == 0)
                {
                    throw new InvalidOperationException("No image data was written from PDF conversion.");
                }

                // Reset stream position after copying
                memoryStream.Position = 0;

                // Return the response
                return new DynamicImageResponse
                {
                    HasImage = true,
                    Stream = memoryStream,
                    Format = ImageFormat.Png,
                };
            }
            catch (Exception e)
            {
                // Log and return nothing
                _logger.LogError(e, "Failed to load cover from {Path}", item.Path);
                memoryStream.Dispose();
                return new DynamicImageResponse { HasImage = false };
            }
        }
    }
}
