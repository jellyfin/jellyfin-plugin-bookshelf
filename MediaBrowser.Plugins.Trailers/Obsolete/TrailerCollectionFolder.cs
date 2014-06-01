using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Plugins.Trailers.Obsolete
{
    /// <summary>
    /// Class TrailerCollectionFolder
    /// </summary>
    public class TrailerCollectionFolder : BasePluginFolder
    {
        public TrailerCollectionFolder()
        {
            Name = "Trailers";
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
            return new TrailerCollectionFolder
            {
                Id = "Trailers".GetMBId(typeof(TrailerCollectionFolder))
            };
        }
    }
}
