using System;

namespace TVHeadEnd.HTSP
{
    public interface HTSListener
    {
        void onMessage(String action, Object obj);
    }
}
