using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
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

        public RokuScheduledTask(ILibraryManager libraryManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
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
                await new VideoProcessor(_logger)
                    .Run(item, cancellationToken).ConfigureAwait(false);

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
            get { return ""; }
        }
    }
}
