using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmbyTV.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;


namespace EmbyTV.TunerHost
{
    static class TunerHostFactory
    {
        public static ITunerHost CreateTunerHost(TunerUserConfiguration tunerUserConfiguration,ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient )
        {
            ITunerHost tunerHost;
            logger.Info("Creating a TunerHost of type: "+ tunerUserConfiguration.ServerType.ToString());
            switch (tunerUserConfiguration.ServerType)
            {
                case (TunerServerType.HdHomerun):
                    tunerHost = new HdHomeRunHost(logger,jsonSerializer,httpClient);
                    break;
                default:
                    throw new ApplicationException("Not a valid host");
            }
            foreach (var field in tunerUserConfiguration.UserFields)
            {
                logger.Info("Adding variable: "+field.Name+" with value of "+field.Value);
                tunerHost.GetType().GetProperty(field.Name).SetValue(tunerHost, field.Value, null);
            }
            logger.Info("Done Creating Tuner");
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
