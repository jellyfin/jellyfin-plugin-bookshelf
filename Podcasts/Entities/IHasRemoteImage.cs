using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCasts.Entities
{
    interface IHasRemoteImage
    {
        string RemoteImagePath { get; set; }
        string PrimaryImagePath { get; }
        bool HasChanged(IHasRemoteImage copy);
    }
}
