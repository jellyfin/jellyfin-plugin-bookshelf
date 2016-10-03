using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.StudioCleaner.Configuration;
using System;

namespace MediaBrowser.Plugins.StudioCleaner
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Studio Cleaner"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Manage and clean up studio for items in your library.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Holds our registration information
        /// </summary>
        public MBRegistrationRecord Registration { get; set; }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var oldConfig = Configuration;
            var config = configuration as PluginConfiguration;

            //Javascript doesn't give us the same date/time as c# so we need to set the last changed date here
            switch (config.LastChangedOption)
            {
                case "movies":
                    config.MovieOptions.LastChange = DateTime.UtcNow;
                    break;
                case "series":
                    config.SeriesOptions.LastChange = DateTime.UtcNow;
                    break;
            }

            base.UpdateConfiguration(configuration);
        }

    }
}
