using Jellyfin.Plugin.Bookshelf.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class PdfImageProviderTests
    {
        [Fact]
        public async Task GetImage_ReturnsCorrectImage()
        {
            IDynamicImageProvider provider = new PdfImageProvider(NullLogger<PdfImageProvider>.Instance);

            var image = await provider.GetImage(new Book
            {
                Path = TestHelpers.GetFixturePath("dracula-bram-stoker.pdf")
            }, ImageType.Primary, CancellationToken.None);

            Assert.True(image.HasImage);
            Assert.NotNull(image.Stream);
            Assert.True(image.Stream.Length > 0);
            Assert.Equal(ImageFormat.Png, image.Format);

            // Write the image to a file for manual verification if needed
            using var fileStream = File.Create("output.png");
            await image.Stream.CopyToAsync(fileStream);
            fileStream.Close();
        }
    }
}
