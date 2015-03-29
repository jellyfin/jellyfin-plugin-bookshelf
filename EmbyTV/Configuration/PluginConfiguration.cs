using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Plugins;
using EmbyTV.TunerHost;

namespace EmbyTV.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string apiURL { get; set; }
        public bool loadOnlyFavorites { get; set; }
        public string hashPassword { get; set; }
        public string username { get; set; }
        public string tvLineUp { get; set; }
        public string avaliableLineups { get; set; }
        public string headendName { get; set; }
        public string headendValue { get; set; }
        public string zipCode { get; set; }
        public List<TunerUserConfiguration> TunerConfigurationsFields { get; set; }

        public PluginConfiguration()
        {
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


    public class ConfigurationField
    {
        public FieldType Type { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Label { get; set; }
        public string ParentId { get; set; }
        public string Name { get; set; }
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
        Text = 2,
        Checkbox = 3,
        Select = 4
    }

    public class SelectField : ConfigurationField
    {
        public SelectOptions Options { get; set; }
    }

    public class TunerUserConfiguration
    {
        public string ServerId { get; set; }
        public TunerServerType ServerType { get; set; }
        public List<ConfigurationField> ConfigurationFields { get; set; }

        public static List<ConfigurationField> GetDefaultConfigurationFields(TunerServerType tunerServerType)
        {
            List<ConfigurationField> userFields = new List<ConfigurationField>();
            switch (tunerServerType)
            {
                case TunerServerType.HdHomerun:
                    userFields.Add(new ConfigurationField()
                    {
                        Name = "Url",
                        Type = FieldType.Text,
                        Value = "localhost",
                        Description = "Hostname or IP address of the HDHomerun",
                        Label = "Hostname/IP"
                    }
                        );
                    userFields.Add(new ConfigurationField()
                    {
                        Name = "OnlyFavorites",
                        Type = FieldType.Checkbox,
                        Value = "true",
                        Description = "Only import starred channels on the HDHomerun",
                        Label = "Import Only Favorites"
                    }
                        );
                    break;
            }
            return userFields;
        }
    }
}