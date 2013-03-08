using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.Linq;

namespace MediaBrowser.Plugins.Trailers.Resolvers
{
    /// <summary>
    /// Class TrailerResolver
    /// </summary>
    public class TrailerResolver : BaseVideoResolver<Trailer>
    {
        private readonly IServerApplicationPaths _applicationPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailerResolver" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        public TrailerResolver(IServerApplicationPaths applicationPaths)
        {
            _applicationPaths = applicationPaths;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override Trailer Resolve(ItemResolveArgs args)
        {
            // Must be a directory and under the trailer download folder
            if (args.IsDirectory && args.Path.StartsWith(Plugin.Instance.DownloadPath, StringComparison.OrdinalIgnoreCase))
            {
                // The trailer must be a video file
                return FindTrailer(args);
            }

            return null;
        }

        /// <summary>
        /// Finds a movie based on a child file system entries
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        private Trailer FindTrailer(ItemResolveArgs args)
        {
            // Loop through each child file/folder and see if we find a video
            return args.FileSystemChildren
                .Where(c => !c.IsDirectory)
                .Select(child => base.Resolve(new ItemResolveArgs(_applicationPaths)
                {
                    FileInfo = child,
                    Path = child.Path
                }))
                .FirstOrDefault(i => i != null);
        }
    }
}
