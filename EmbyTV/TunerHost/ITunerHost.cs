using MediaBrowser.Controller.LiveTv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmbyTV.TunerHost
{
    interface ITunerHost
    {


        Task GetDeviceInfo(CancellationToken cancellationToken);

        string model { get; set; }

        Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken);

        string firmware { get; set; }

        Task<List<LiveTvTunerInfo>> GetTunersInfo(CancellationToken cancellationToken);

        string getWebUrl();

        string getChannelStreamInfo(string channelId);
    }

    public enum TunerServerType
    {
        HdHomerun = 1
    }
}
