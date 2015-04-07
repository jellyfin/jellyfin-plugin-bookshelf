using System;

namespace MediaBrowser.Plugins.TVHclient.HTSP
{
    public interface HTSListener
    {
        void onMessage(String action, Object obj);
    }
}
