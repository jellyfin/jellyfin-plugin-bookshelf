using System.Collections.Generic;
using System.Security.Policy;
using EmbyTV.TunerHost.Settings;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Plugins;

namespace EmbyTV.Configuration
{
    public class PluginConfiguration:BasePluginConfiguration
    {
        public string apiURL { get; set; }
        public string Port { get; set; }
        public bool loadOnlyFavorites { get; set; }
        public string hashPassword { get; set; }
        public string username { get; set; }
        public string tvLineUp { get; set; }
        public string avaliableLineups{get;set;}
        public string headendName { get; set; }
        public string headendValue { get; set; }
        public string zipCode { get; set; }
       // public TunerHostSettings Settings { get; set; }

        public PluginConfiguration()
        {
            Port = "5004";
            apiURL = "localhost";
            loadOnlyFavorites = true;
            tvLineUp = "";
            username = "";
            hashPassword = "";
            avaliableLineups = "";
            headendName = "";
            headendValue = "";
            zipCode = "";
        }
    }

    public class ConfigurationFields
    {
        public FieldType Type { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Label { get; set; }
    }

    public class SelectOptions
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public bool Selected { get; set; }
        public string Color { get; set; }
    }
    public enum FieldType
    {
        Private = 0,
        Hidden = 1,
        Text =2,
        Checkbox =3,
        Select =4
    }
    public class SelectField:ConfigurationFields
    {
        public SelectOptions Options { get; set; }
    }

}