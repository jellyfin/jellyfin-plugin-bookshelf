using EmbyTV.EPGProvider;
using MediaBrowser.Controller.Net;
using ServiceStack;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmbyTV.Api
{
    [Route("/EmbyTv/SchedulesDirect/Headends", "GET")]
    public class GetSchedulesDirectHeadends : IReturn<HeadendsResult>
    {
    }
    
    public class EmbyTvConfigService : IRestfulService
    {
        public async Task<object> Get(GetSchedulesDirectHeadends request)
        {
            var headends = await SchedulesDirect.Current.getHeadends(Plugin.Instance.Configuration.zipCode, CancellationToken.None).ConfigureAwait(false);
            var availableLineups = await SchedulesDirect.Current.getLineups(CancellationToken.None).ConfigureAwait(false);

            return new HeadendsResult
            {
                Headends = headends,
                AvaliableLineups = availableLineups
            };
        }
    }

    public class HeadendsResult
    {
        public List<Headend> Headends { get; set; }
        public List<string> AvaliableLineups { get; set; }
    }
}
