using System;
using System.Collections.Generic;
using MediaBrowser.Plugins.StudioCleaner.Entities;

namespace MediaBrowser.Plugins.StudioCleaner.Configuration
{
    public class StudioOptions
    {
        public List<string> AllowedStudios { get; set; }
        public SerializableDictionary<string, string> StudioMappings { get; set; }
        public DateTime LastChange { get; set; }

        public StudioOptions()
        {
            AllowedStudios = new List<string>();
            StudioMappings = new SerializableDictionary<string, string>();
            LastChange = DateTime.MinValue;
        }
    }

}
