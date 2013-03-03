using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Plugins.Trailers.Entities
{
    /// <summary>
    /// Class TrailerCollectionFolder
    /// </summary>
    class TrailerCollectionFolder : BasePluginFolder
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return Plugin.Instance.Configuration.FolderName;
            }
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public override string Path
        {
            get { return ServerEntryPoint.Instance.DownloadPath; }
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
            return new TrailerCollectionFolder();
        }
    }
}
