using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace PodCasts.Entities
{
    class VodCastVideo : Video, IHasRemoteImage
    {
        public string RemoteImagePath { get; set; }

        public bool HasChanged(IHasRemoteImage copy)
        {
            return copy.RemoteImagePath != this.RemoteImagePath;
        }
    }
}
