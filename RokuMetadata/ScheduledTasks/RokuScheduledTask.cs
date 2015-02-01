using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Logging;
using RokuMetadata.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RokuMetadata.ScheduledTasks
{
    public class RokuScheduledTask : IScheduledTask
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;

        public RokuScheduledTask(ILibraryManager libraryManager, ILogger logger, IMediaEncoder mediaEncoder, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
        }

        public string Category
        {
            get { return "Roku"; }
        }

        public string Description
        {
            get { return "Create thumbnails for enhanced seeking with Roku"; }
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var items = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Video>()
                .ToList();

            var numComplete = 0;

            foreach (var item in items)
            {
                try
                {
                    await new VideoProcessor(_logger, _mediaEncoder, _fileSystem)
                        .Run(item, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error creating roku thumbnails for {0}", ex, item.Name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= items.Count;
                percent *= 95;

                progress.Report(percent + 5);
            }
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new List<ITaskTrigger>
            {
                new DailyTrigger
                {
                    TimeOfDay = TimeSpan.FromHours(5)
                }
            };
        }

        public string Name
        {
            get { return "Create thumbnails"; }
        }
    }
}
