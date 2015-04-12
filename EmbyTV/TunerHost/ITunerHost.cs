using MediaBrowser.Controller.LiveTv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbyTV.Configuration;
using MediaBrowser.Model.Dto;

namespace EmbyTV.TunerHost
{
    interface ITunerHost
    {


        Task GetDeviceInfo(CancellationToken cancellationToken);

        string model { get; set; }
        string deviceID { get; set; }

        Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken);

        string firmware { get; set; }

        Task<List<LiveTvTunerInfo>> GetTunersInfo(CancellationToken cancellationToken);

        string getWebUrl();

        MediaSourceInfo GetChannelStreamInfo(string channelId);
    }

    public enum TunerServerType 
    {
        HdHomerun = 1
    }

    public static class TunerHostConfig
    {
        public static FieldBuilder GetDefaultConfigurationFields(TunerServerType tunerServerType)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            fieldBuilder.Type = tunerServerType;
            List<ConfigurationField> userFields = new List<ConfigurationField>()
            {
                new ConfigurationField()
                {
                    Name = "Url",
                    Type = FieldType.Text,
                    defaultValue = "localhost",
                    Description = "Hostname or IP address of the HDHomerun",
                    Label = "Hostname/IP"
                }
                ,
                new ConfigurationField()
                {
                    Name = "OnlyFavorites",
                    Type = FieldType.Checkbox,
                    defaultValue = "true",
                    Description = "Only import starred channels on the HDHomerun",
                    Label = "Import Only Favorites"
                }
            };
            fieldBuilder.DefaultConfigurationFields = userFields;
            return fieldBuilder;
        }

        public static List<FieldBuilder> BuildDefaultForTunerHostsBuilders()
        {
            List<FieldBuilder> defaultTunerHostsConfigFields = new List<FieldBuilder>();
            foreach (TunerServerType serverType in Enum.GetValues(typeof(TunerServerType)))
            {
                defaultTunerHostsConfigFields.Add(GetDefaultConfigurationFields(serverType));
            }
            return defaultTunerHostsConfigFields;
        }
    }

}
