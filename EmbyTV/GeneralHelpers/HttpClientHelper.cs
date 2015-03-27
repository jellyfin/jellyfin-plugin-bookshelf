using MediaBrowser.Common.Net;

namespace EmbyTV.GeneralHelpers
{
    /// <summary>
    /// Class HttpRequestOptions
    /// </summary>
    public class HttpRequestOptionsMod:HttpRequestOptions
    {
        public string Token
        {
            get { return GetHeaderValue("token"); }
            set
            {
                RequestHeaders["token"] = value;
            }
        }
        public void SetRequestHeader(string headerName, string value)
        {
                  RequestHeaders[headerName] = value;
        }
        private string GetHeaderValue(string name)
        {
            string value;

            RequestHeaders.TryGetValue(name, out value);

            return value;
        }
    }
}

