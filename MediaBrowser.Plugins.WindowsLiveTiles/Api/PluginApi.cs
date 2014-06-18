using MediaBrowser.Controller.Net;
using ServiceStack;
using ServiceStack.Web;
using System.Text;

namespace MediaBrowser.Plugins.WindowsLiveTiles.Api
{
    [Route("/WindowsLiveTiles/Bookmark/{UserId}", "GET", Summary = "Gets a live tile bookmark page for a user")]
    public class GetBookmarkPage
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/WindowsLiveTiles/browserconfig", "GET", Summary = "Gets live tile configuration xml")]
    public class GetBrowserConfig
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/WindowsLiveTiles/Notifications/{UserId}/{Index}", "GET", Summary = "Gets live tile notifications")]
    public class GetNotifications
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "Index", Description = "Index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Index { get; set; }
    }

    public class PluginApi : IRestfulService, IHasResultFactory
    {
        public IHttpResultFactory ResultFactory { get; set; }
        public IRequest Request { get; set; }

        public object Get(GetBookmarkPage request)
        {
            var html = GetBookmarkPageHtml(request.UserId);

            return ResultFactory.GetResult(html, "text/html");
        }

        public object Get(GetBrowserConfig request)
        {
            var xml = GetBrowserConfigXml(request.UserId);

            return ResultFactory.GetResult(xml, "text/xml");
        }

        public object Get(GetNotifications request)
        {
            var xml = "";

            // TODO: Implement tile scheme
            // http://msdn.microsoft.com/en-us/library/dn455106(v=vs.85).aspx

            // 5 sections
            // 1: Latest movies
            // 2: Next up
            // 3: Latest episodes
            // 4: Latest channel media (fallback to latest movie)
            // 5: Current live tv programs (fallback to latest movie)

            return ResultFactory.GetResult(xml, "text/xml");
        }

        private string GetBookmarkPageHtml(string userId)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");

            sb.Append("<head>");

            sb.Append("<meta name=\"application-name\" content=\"Media Browser\">");
            sb.AppendFormat("<meta name=\"msapplication-config\" content=\"browserconfig.xml?userId={0}\" />", userId);

            sb.Append("<title>Live Tile Bookmark Page</title>");

            sb.Append("</head>");

            sb.Append("<body>");
            sb.Append("<p>Instructions for use:</p>");
            sb.Append("<p>Pin this page in Internet Explorer 11 or greater.</p>");
            sb.Append("<p><a href=\"http://www.eightforums.com/tutorials/32238-internet-explorer-11-pin-website-start-windows-8-1-a.html\" target=\"_blank\">How to Pin Pages in Internet Explorer</a></p>");
            sb.Append("</body>");

            sb.Append("</html>");

            return sb.ToString();
        }

        private string GetBrowserConfigXml(string userId)
        {
            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<browserconfig>");
            sb.Append("<msapplication>");

            sb.Append("<tile>");

            // TODO: Add MB images as embedded resources, then add api endpoints

            sb.Append("<square70x70logo src=\"images/smalltile.png\"/>");
            sb.Append("<square150x150logo src=\"images/mediumtile.png\"/>");
            sb.Append("<wide310x150logo src=\"images/widetile.png\"/>");
            sb.Append("<square310x310logo src=\"images/largetile.png\"/>");
            sb.Append("<TileColor>#009900</TileColor>");

            sb.Append("</tile>");

            sb.Append("<notification>");

            sb.AppendFormat("<polling-uri src=\"Notifications/{0}/1\"/>", userId);
            sb.AppendFormat("<polling-uri2 src=\"Notifications/{0}/2\"/>", userId);
            sb.AppendFormat("<polling-uri3 src=\"Notifications/{0}/3\"/>", userId);
            sb.AppendFormat("<polling-uri4 src=\"Notifications/{0}/4\"/>", userId);
            sb.AppendFormat("<polling-uri5 src=\"Notifications/{0}/5\"/>", userId);
            sb.Append("<frequency>1</frequency>");
            sb.Append("<cycle>1</cycle>");

            sb.Append("</notification>");

            sb.Append("</msapplication>");
            sb.Append("</browserconfig>");

            return sb.ToString();
        }
    }
}
