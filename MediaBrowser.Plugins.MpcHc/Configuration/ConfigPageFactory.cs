using MediaBrowser.Model.Dto;
using MediaBrowser.Theater.Interfaces.Presentation;
using System;

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

        public bool IsVisible(UserDto user)
        {
            return user != null && user.Configuration.IsAdministrator;
        }

        public Type PageType
        {
            get { return typeof(ConfigPage); }
        }
    }
}
