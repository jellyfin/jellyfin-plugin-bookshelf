using System;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Querying;
using MediaBrowser.Plugins.Vimeo.Configuration;
using ServiceStack;

namespace MediaBrowser.Plugins.Vimeo.Api
{
    [Route("/Vimeo/Auth/Request", "GET", Summary = "Gets Token")]
    public class GetToken : IReturn<QueryResult<PluginConfiguration>>
    {
    }

    internal class ServerApiEndpoints : IRestfulService
    {

        public object Get(GetToken request)
        {
            Plugin.vc.GetUnauthorizedRequestToken();

            var config = new PluginConfiguration
            {
                TokenURL = Plugin.vc.GenerateAuthorizationUrl(),
                Token = Plugin.vc.GetToken(),
                SecretToken = Plugin.vc.GetSecretToken()
            };

            return config;
        }
    }
}
