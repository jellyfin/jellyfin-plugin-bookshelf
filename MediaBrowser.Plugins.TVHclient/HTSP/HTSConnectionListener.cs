using System;

namespace MediaBrowser.Plugins.TVHclient.HTSP
{
    public interface HTSConnectionListener
    {
        void onMessage(HTSMessage response);
        void onError(Exception ex);
    }
}
