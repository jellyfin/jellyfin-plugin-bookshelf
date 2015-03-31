using System;
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
        public string hashPassword { get; set; }
        public string username { get; set; }
        public string tvLineUp { get; set; }
        public List<string> avaliableLineups { get; set; }
        public List<string> headendName { get; set; }
        public List<string> headendValue { get; set; }
        public string zipCode { get; set; }
        public List<FieldBuilder> TunerDefaultConfigurationsFields { get; set; }
        public List<TunerUserConfiguration> TunerHostsConfiguration { get; set; }

        public PluginConfiguration()
        {
            tvLineUp = "";
            username = "";
            hashPassword = "";
            avaliableLineups = new List<string>(){""};
            headendName = new List<string>() { "" };
            headendValue = new List<string>() { "" };
            zipCode = "";
            TunerDefaultConfigurationsFields = TunerHostConfig.BuildDefaultForTunerHostsBuilders();
            
        }
    }

    public class ConfigurationField
    {
        public FieldType Type { get; set; }
        public string defaultValue { get; set; }
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
    public class UserField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public class TunerUserConfiguration
    {
        public string ServerId { get; set; }
        public TunerServerType ServerType { get; set; }
        public List<UserField> UserFields { get; set; }

    }

    public class FieldBuilder
    {
        public TunerServerType Type { get; set; }
        public List<ConfigurationField> DefaultConfigurationFields { get; set; }

    }


}