using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace RokuMetadata.Drawing
{
    public class VideoProcessor
    {
        private readonly ILogger _logger;

        public VideoProcessor(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Run(Video item, CancellationToken cancellationToken)
        {
            _logger.Info("Creating roku thumbnails for {0}", item.Name);
        }
    }
}
