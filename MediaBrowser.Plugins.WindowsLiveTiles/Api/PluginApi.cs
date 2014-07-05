using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using ServiceStack;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MediaBrowser.Plugins.WindowsLiveTiles.Api
{
    [Route("/WindowsLiveTiles/{UserId}/Bookmark", "GET", Summary = "Gets a live tile bookmark page for a user")]
    public class GetBookmarkPage
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/browserconfig", "GET", Summary = "Gets live tile configuration xml")]
    [Route("/WindowsLiveTiles/{UserId}/browserconfig", "GET", Summary = "Gets live tile configuration xml")]
    [Route("/WindowsLiveTiles/{UserId}/browserconfig.xml", "GET", Summary = "Gets live tile configuration xml")]
    public class GetBrowserConfig
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/WindowsLiveTiles/{UserId}/Notifications/{Index}", "GET", Summary = "Gets live tile notifications")]
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

        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;

        public PluginApi(IUserManager userManager, IUserDataManager userDataManager)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
        }

        public object Get(GetBookmarkPage request)
        {
            var html = GetBookmarkPageHtml(request.UserId);

            return ResultFactory.GetResult(html, "text/html");
        }

        public object Get(GetBrowserConfig request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                request.UserId = _userManager.Users.Select(i => i.Id.ToString("N")).First();
            }
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
                sb.AppendFormat("<binding template=\"{0}\" branding=\"name\">", tile.Name);

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

            sb.Append("<meta name=\"application-name\" content=\"Media Browser\" />");

            var configEndpoint = "browserconfig.xml";

            sb.AppendFormat("<meta name=\"msapplication-config\" content=\"{0}\" />", configEndpoint);
            sb.Append("<link rel=\"shortcut icon\" href=\"../images/favicon.ico\" />");

            sb.Append("<title>Media Browser</title>");

            //sb.Append("<script type=\"text/javascript\">if (!document.referrer){window.location='../dashboard/index.html';}</script>");

            sb.Append("</head>");

            sb.Append("<body>");
            sb.Append("<p>Instructions for use:</p>");
            sb.Append("<p>Pin this page to your Windows start screen using Internet Explorer 11 or greater.</p>");
            sb.Append("<p><a href=\"http://www.eightforums.com/tutorials/32238-internet-explorer-11-pin-website-start-windows-8-1-a.html\" target=\"_blank\">How to Pin Pages in Internet Explorer</a></p>");
            sb.Append("<p>If this device is used outside your home network, then it is recommended to open this page using a remote address.</p>");
            sb.Append("<p>Once pinned it will take at least 30 minutes before content will appear on your Windows start screen.</p>");
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

            sb.Append("<square70x70logo src=\"../images/square70x70logo.png\"/>");
            sb.Append("<square150x150logo src=\"../images/square150x150logo.png\"/>");
            sb.Append("<wide310x150logo src=\"../images/wide310x150logo.png\"/>");
            sb.Append("<square310x310logo src=\"../images/square310x310logo.png\"/>");
            sb.Append("<TileColor>#222222</TileColor>");

            sb.Append("</tile>");

            sb.Append("<notification>");

            sb.Append("<polling-uri src=\"Notifications/1\"/>");
            sb.Append("<polling-uri2 src=\"Notifications/2\"/>");
            sb.Append("<polling-uri3 src=\"Notifications/3\"/>");
            sb.Append("<polling-uri4 src=\"Notifications/4\"/>");
            sb.Append("<polling-uri5 src=\"Notifications/5\"/>");
            sb.Append("<frequency>30</frequency>");
            sb.Append("<cycle>1</cycle>");

            sb.Append("</notification>");

            sb.Append("</msapplication>");
            sb.Append("</browserconfig>");

            return sb.ToString();
        }

        private IEnumerable<TileTemplate> GetTiles(string userId, int index)
        {
            var user = _userManager.GetUserById(new Guid(userId));

            var list = new List<TileTemplate>();

            // 5 sections
            // 1: Latest movies
            // 2: Next up
            // 3: Latest episodes
            // 4: Latest channel media (fallback to latest movie)
            // 5: Current live tv programs (fallback to latest movie)

            // Available templates:
            // http://msdn.microsoft.com/en-us/library/hh761491.aspx

            var items = user.RootFolder.GetRecursiveChildren(user, true).ToList();

            items = items.Where(i => i is Movie || i is Series).ToList();

            var sortType = "Series";

            switch (index)
            {
                case 1:
                case 2:
                    sortType = "Movie";
                    break;

            }

            var sorted = items.Where(i => i.HasImage(ImageType.Primary, 0) && i.HasImage(ImageType.Backdrop, 0)).OrderBy(i => i.GetType().Name.Equals(sortType));

            var userIdGuid = new Guid(userId);
            switch (index)
            {
                // Latest
                case 1:
                case 3:
                    sorted = sorted.ThenByDescending(i =>
                    {
                        var series = i as Series;
                        if (series != null) return series.DateLastEpisodeAdded;

                        return i.DateCreated;
                    });
                    break;
                case 2:
                case 4:
                    sorted = sorted.ThenByDescending(i => _userDataManager.GetUserData(userIdGuid, i.GetUserDataKey()).IsFavorite);
                    break;

            }

            sorted = sorted.ThenBy(i => Guid.NewGuid());

            var item = sorted.FirstOrDefault();

            if (item != null)
            {
                var template = new TileTemplate
                {
                    Name = "TileWide310x150ImageAndText01"
                };

                template.TextLines.Add("Watch " + item.Name + " with Media Browser.");

                template.Images.Add(string.Format("../Items/{0}/Images/Backdrop/0?width=300&height=150", item.Id));

                list.Add(template);


                template = new TileTemplate
               {
                   Name = "TileSquare310x310ImageAndText01"
               };

                template.TextLines.Add("Watch " + item.Name + " with Media Browser.");

                template.Images.Add(string.Format("../Items/{0}/Images/Primary/0?width=300&height=300", item.Id));

                list.Add(template);


                template = new TileTemplate
                {
                    Name = "TileSquare150x150PeekImageAndText04"
                };

                template.TextLines.Add("Watch " + item.Name + " with Media Browser.");

                template.Images.Add(string.Format("../Items/{0}/Images/Primary/0?width=150&height=150", item.Id));

                list.Add(template);
            }

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
