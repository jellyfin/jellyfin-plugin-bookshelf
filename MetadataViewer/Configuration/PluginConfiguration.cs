using MediaBrowser.Model.Plugins;
using System;

namespace MetadataViewer.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public DateTime MetadataEditorLastModifiedDate { get; set; }

        public PluginConfiguration()
        {
        }
    }
}
