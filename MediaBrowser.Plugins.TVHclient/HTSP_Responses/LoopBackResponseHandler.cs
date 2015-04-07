using MediaBrowser.Plugins.TVHclient.Helper;
using MediaBrowser.Plugins.TVHclient.HTSP;

namespace MediaBrowser.Plugins.TVHclient.HTSP_Responses
{
    public class LoopBackResponseHandler : HTSResponseHandler
    {
        private readonly SizeQueue<HTSMessage> _responseDataQueue;

        public LoopBackResponseHandler()
        {
            _responseDataQueue = new SizeQueue<HTSMessage>(1);
        }

        public void handleResponse(HTSMessage response)
        {
            _responseDataQueue.Enqueue(response);
        }

        public HTSMessage getResponse()
        {
            return _responseDataQueue.Dequeue();
        }
    }
}
