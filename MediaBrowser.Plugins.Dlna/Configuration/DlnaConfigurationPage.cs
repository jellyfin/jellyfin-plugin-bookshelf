using MediaBrowser.Controller.Plugins;
using System;
using System.IO;

namespace MediaBrowser.Plugins.Dlna.Configuration
{
    /// <summary>
    /// Class DlnaConfigurationPage
    /// </summary>
    class DlnaConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Dlna"; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("MediaBrowser.Plugins.Dlna.Configuration.configPage.html");
        }
        
        /// <summary>
        /// Gets the date last modified.
        /// </summary>
        /// <value>The date last modified.</value>
        public DateTime DateLastModified
        {
            get { return Plugin.Instance.AssemblyDateLastModified; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the plugin id.
        /// </summary>
        /// <value>The plugin id.</value>
        public Guid? PluginId
        {
            get { return Plugin.Instance.Id; }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version
        {
            get { return Plugin.Instance.Version.ToString(); }
        }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }
    }
}
