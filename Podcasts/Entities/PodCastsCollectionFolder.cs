using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using System.Linq;

namespace PodCasts.Entities
{
    /// <summary>
    /// Class TrailerCollectionFolder
    /// </summary>
    class PodCastsCollectionFolder : BasePluginFolder
    {
        public PodCastsCollectionFolder()
        {
            Name = "Podcasts";
        }

    }

    /// <summary>
    /// Class PluginFolderCreator
    /// </summary>
    public class PluginFolderCreator : IVirtualFolderCreator
    {
        /// <summary>
        /// Gets the folder.
        /// </summary>
        /// <returns>BasePluginFolder.</returns>
        public BasePluginFolder GetFolder()
        {
            return new PodCastsCollectionFolder
            {
                Id = "PodCasts".GetMBId(typeof(PodCastsCollectionFolder)),
            };

        }
    }
}
