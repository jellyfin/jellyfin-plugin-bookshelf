using System.Net;

namespace Jellyfin.Plugin.Bookshelf.Tests.Http;

internal record MockHttpResponse(HttpStatusCode StatusCode, string Response);
