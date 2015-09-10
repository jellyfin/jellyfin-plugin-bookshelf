using EmbyTV.EPGProvider;
using MediaBrowser.Controller.Net;
using ServiceStack;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmbyTV.Configuration;
using EmbyTV.TunerHost;

namespace EmbyTV.Api
{
    [Route("/EmbyTv/SchedulesDirect/Headends", "GET")]
    public class GetSchedulesDirectHeadends : IReturn<HeadendsResult>
    {
    }
    [Route("/EmbyTv/Tuner/ConfigurationFields", "GET")]
    public class GetTunerConfigurationFields : IReturn<ConfigurationFieldsDefaults>
    {
    }
    
    public class EmbyTvConfigService : IRestfulService
    {
        public async Task<object> Get(GetSchedulesDirectHeadends request)
        {
            var headends = await SchedulesDirect.Current.GetHeadends(Plugin.Instance.Configuration.zipCode, CancellationToken.None).ConfigureAwait(false);
            var availableLineups = await SchedulesDirect.Current.getLineups(CancellationToken.None).ConfigureAwait(false);

            return new HeadendsResult
            {
                Headends = headends,
                AvaliableLineups = availableLineups
            };
        }
        public async Task<object> Get(GetTunerConfigurationFields request)
        {
            return new ConfigurationFieldsDefaults {DefaultsBuilders = TunerHostStatics.BuildDefaultForTunerHostsBuilders()};
        }
    }

    public class HeadendsResult
    {
        public List<Headend> Headends { get; set; }
        public List<string> AvaliableLineups { get; set; }
    }

    public class ConfigurationFieldsDefaults
    {
        public List<FieldBuilder> DefaultsBuilders { get; set; } 
    }
}
