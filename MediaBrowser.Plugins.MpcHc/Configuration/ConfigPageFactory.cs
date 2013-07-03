using MediaBrowser.Theater.Interfaces.Presentation;
using System;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.MpcHc.Configuration
{
    class ConfigPageFactory : ISettingsPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Mpc-Hc"; }
        }

        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <returns>Page.</returns>
        public Page GetPage()
        {
            return new ConfigPage();
        }

        /// <summary>
        /// Gets the thumb URI.
        /// </summary>
        /// <value>The thumb URI.</value>
        public Uri ThumbUri
        {
            get { return Plugin.GetThumbUri(); }
        }

        public SettingsPageCategory Category
        {
            get { return SettingsPageCategory.Plugin; }
        }
    }
}
