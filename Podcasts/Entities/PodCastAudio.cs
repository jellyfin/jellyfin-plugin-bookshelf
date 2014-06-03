using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;

namespace PodCasts.Entities
{
    class PodCastAudio : Audio, IHasRemoteImage
    {
        public string RemoteImagePath { get; set; }

        public bool HasChanged(IHasRemoteImage copy)
        {
            return copy.RemoteImagePath != this.RemoteImagePath;
        }
    }
}
