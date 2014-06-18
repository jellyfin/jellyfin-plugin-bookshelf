using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Net;
using ServiceStack;
using ServiceStack.Web;
using System.Text;

namespace MediaBrowser.Plugins.WindowsLiveTiles.Api
{
    [Route("/WindowsLiveTiles/Bookmark", "GET", Summary = "Gets a live tile bookmark page for a user")]
    public class GetBookmarkPage
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
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

    [Route("/WindowsLiveTiles/images/{Name}", "GET", Summary = "Gets a live tile notification image")]
    public class GetImage
    {
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
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

        public object Get(GetImage request)
        {
            var stream = GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".Images." + request.Name);

            return ResultFactory.GetResult(stream, "image/png");
        }

        public object Get(GetNotifications request)
        {
            // TODO: Implement tile scheme
            // http://msdn.microsoft.com/en-us/library/dn455106(v=vs.85).aspx

            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<tile>");
            sb.Append("<visual lang=\"en-US\" version=\"2\">");

            foreach (var tile in GetTiles(request.UserId, request.Index))
            {
                sb.AppendFormat("<binding template=\"{0}\">", tile.Name);

                var index = 0;
                foreach (var image in tile.Images)
                {
                    sb.AppendFormat("<image id=\"{0}\" src=\"{1}\" alt=\"{2}\"/>",
                        index.ToString(CultureInfo.InvariantCulture),
                        image,
                        "alt text");

                    index++;
                }

                index = 0;
                foreach (var text in tile.TextLines)
                {
                    sb.AppendFormat("<text id=\"{0}\">{1}</text>",
                        index.ToString(CultureInfo.InvariantCulture),
                        text);
                    
                    index++;
                }

                sb.Append("</binding>");
            }

            sb.Append("</visual>");
            sb.Append("</tile>");

            return ResultFactory.GetResult(sb.ToString(), "text/xml");
        }

        private string GetBookmarkPageHtml(string userId)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");

            sb.Append("<head>");

            sb.Append("<meta name=\"application-name\" content=\"Media Browser\">");
            sb.AppendFormat("<meta name=\"msapplication-config\" content=\"browserconfig.xml?userId={0}\" />", userId);
            sb.Append("<link rel=\"shortcut icon\" href=\"images/favicon.ico\" />");

            sb.Append("<title>Live Tile Bookmark Page</title>");

            sb.Append("</head>");

            sb.Append("<body>");
            sb.Append("<p>Instructions for use:</p>");
            sb.Append("<p>Pin this page to your Windows Start Screen using Internet Explorer 11 or greater.</p>");
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

            sb.Append("<square70x70logo src=\"images/square70x70logo.png\"/>");
            sb.Append("<square150x150logo src=\"images/square150x150logo.png\"/>");
            sb.Append("<wide310x150logo src=\"images/wide310x150logo.png\"/>");
            sb.Append("<square310x310logo src=\"images/square310x310logo.png\"/>");
            sb.Append("<TileColor>#222222</TileColor>");

            sb.Append("</tile>");

            sb.Append("<notification>");

            sb.AppendFormat("<polling-uri src=\"Notifications/{0}/1\"/>", userId);
            sb.AppendFormat("<polling-uri2 src=\"Notifications/{0}/2\"/>", userId);
            sb.AppendFormat("<polling-uri3 src=\"Notifications/{0}/3\"/>", userId);
            sb.AppendFormat("<polling-uri4 src=\"Notifications/{0}/4\"/>", userId);
            sb.AppendFormat("<polling-uri5 src=\"Notifications/{0}/5\"/>", userId);
            sb.Append("<frequency>30</frequency>");
            sb.Append("<cycle>1</cycle>");

            sb.Append("</notification>");

            sb.Append("</msapplication>");
            sb.Append("</browserconfig>");

            return sb.ToString();
        }

        private IEnumerable<TileTemplate> GetTiles(string userId, int index)
        {
            var list = new List<TileTemplate>();

            // 5 sections
            // 1: Latest movies
            // 2: Next up
            // 3: Latest episodes
            // 4: Latest channel media (fallback to latest movie)
            // 5: Current live tv programs (fallback to latest movie)

            return list;
        }
    }

    public class TileTemplate
    {
        public string Name { get; set; }
        public List<string> Images { get; set; }
        public List<string> TextLines { get; set; }

        public TileTemplate()
        {
            Images = new List<string>();
            TextLines = new List<string>();
        }
    }
}
