using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using EmbyTV.Configuration;

namespace EmbyTV.TunerHost.Settings
{
    public class TunerUserConfiguration
    {
        private TunerServerType TunerServerType { get; set; }
        private string Id { get; set; }
        public Dictionary<string, ConfigurationFields> UserFields { get; set; }

        public TunerUserConfiguration(TunerServerType tunerServerType)
        {
            TunerServerType = tunerServerType;
            UserFields = GetDefaultConfigurationFields(TunerServerType);
        }
        
        public string GetId()
        {
            return Id;
        }

        public TunerServerType GetTunerServerType()
        {
            return TunerServerType;
        }

        public static Dictionary<string, ConfigurationFields> GetDefaultConfigurationFields(
            TunerServerType tunerServerType)
        {
            Dictionary<string, ConfigurationFields> _userFields = new Dictionary<string, ConfigurationFields>();
            switch (tunerServerType)
            {
                case TunerServerType.HdHomerun:
                    _userFields.Add("url", new ConfigurationFields()
                    {
                        Type = FieldType.Text,
                        Value = "localhost",
                        Description = "Hostname or IP address of the HDHomerun",
                        Label = "Hostname/IP"
                    }
                        );
                    _userFields.Add("onlyFavorites", new ConfigurationFields()
                    {
                        Type = FieldType.Checkbox,
                        Value = "true",
                        Description = "Only import starred channels on the HDHomerun",
                        Label = "Import Only Favorites"
                    }
                        );
                    break;
            }
            return _userFields;
        }
    }


    public enum TunerServerType
    {
        HdHomerun = 1
    }
}

