using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EmbyTV.Configuration;
using EmbyTV.TunerHost.HostDefinitions;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using EmbyTV.GeneralHelpers;


namespace EmbyTV.TunerHost
{
    public static class TunerHostFactory
    {
        public static ITunerHost CreateTunerHost(TunerUserConfiguration tunerUserConfiguration, ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            ITunerHost tunerHost;
            logger.Info("Creating a TunerHost of type: " + tunerUserConfiguration.ServerType);

            IEnumerable<Type> hostTypes = TunerHostStatics.GetAllTunerHostTypes();
            Type hostType = hostTypes.FirstOrDefault(t => String.Equals(t.Name, tunerUserConfiguration.ServerType, 
                   StringComparison.OrdinalIgnoreCase));
            tunerHost = (ITunerHost) Activator.CreateInstance(hostType,logger, jsonSerializer, httpClient);
            foreach (var field in tunerUserConfiguration.UserFields)
            {
                logger.Info("Adding variable: " + field.Name + " with value of " + field.Value);
                tunerHost.GetType().GetProperty(field.Name).SetValue(tunerHost, field.Value, null);
                logger.Info("Added: " + field.Name + " with value of " + field.Value);
            }
            logger.Info("Done Creating Tuner");
            return tunerHost;
        }
        public static ITunerHost CreateTunerHost(Type type)
        {
            ITunerHost tunerHost;
            tunerHost = (ITunerHost)Activator.CreateInstance(type);
            return tunerHost;
        }
        public static List<ITunerHost> CreateTunerHosts(List<TunerUserConfiguration> tunerUserConfigurations, ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            logger.Info("Creating a TunerHost list: " + tunerUserConfigurations.Count());
            List<ITunerHost> tunerHosts = new List<ITunerHost>();
            foreach (TunerUserConfiguration config in tunerUserConfigurations)
            {
                tunerHosts.Add(CreateTunerHost(config, logger, jsonSerializer, httpClient));
            }
            return tunerHosts;
        }
    }
}
