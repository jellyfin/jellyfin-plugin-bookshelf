using MediaBrowser.Controller.LiveTv;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.NextPvr
{
    /// <summary>
    /// Class LiveTvService
    /// </summary>
    public class LiveTvService : ILiveTvService
    {
        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        public Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            var channels = new List<ChannelInfo>
                {
                    new ChannelInfo
                        {
                             Name = "NBC",
                              ServiceName = Name
                        }
                };

            return Task.FromResult<IEnumerable<ChannelInfo>>(channels);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Next Pvr"; }
        }
    }
}
