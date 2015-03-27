using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;

namespace EmbyTV.GeneralHelpers
{
    public class PluginHelper
    {
        public IHttpClient httpClient{get;set;}
        public HttpRequestOptions httpOptions{get;set;}
        public CancellationToken cancellationToken{get;set;}
        public IJsonSerializer jsonSerializer{get;set;}
        public IXmlSerializer xmlSerializer{get;set;}
        public ILogger logger{get;set;}
        public string appName{get;set;}
        public bool debugOn { get; set; }
        public PluginHelper(string appName){
            this.appName=appName;
            debugOn = false;
        }
        
        public Stream response;
        public void useCancellationToken()     
        {
            httpOptions.CancellationToken=cancellationToken;
        }
        public async Task<Stream> Get(){
            response = await httpClient.Get(httpOptions).ConfigureAwait(false);
            return response;
        }
        public async Task<Stream> Post(){
           HttpResponseInfo httpRespInfo = await httpClient.Post(httpOptions).ConfigureAwait(false);
           response = httpRespInfo.Content;
           return response;
        }
        public async Task<Stream> Put()
        {
            HttpResponseInfo httpRespInfo = await httpClient.SendAsync(httpOptions,"PUT");
            response = httpRespInfo.Content;
            return response;
        }
        public void LogInfo(string info){
            logger.Info("[" + appName + "] " + info);
        }
        public void LogDebug(string debug)
        {
            if (debugOn) { logger.Debug("[" + appName + "] " + debug); }
        }
        public void LogError(string error)
        {
            logger.Error("[" + appName + "] " + error);
            throw new ApplicationException(appName +" "+ error);
        }
        public T DeserializeJSON<T>(Stream stream)
        {
            return jsonSerializer.DeserializeFromStream<T>(stream);
        }
        public T DeserializeJSON<T>(String jsonstring)
        {
            return jsonSerializer.DeserializeFromString<T>(jsonstring);
        }
    }
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
         /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestOptions"/> class.
        /// </summary>
        public HttpRequestOptionsMod():base()
        {
        }

    }

    public enum CacheMode
    {
        None = 0,
        Unconditional = 1
    }
}

