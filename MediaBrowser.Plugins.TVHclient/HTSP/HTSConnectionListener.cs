using System;

namespace TVHeadEnd.HTSP
{
    public interface HTSConnectionListener
    {
        void onMessage(HTSMessage response);
        void onError(Exception ex);
    }
}
