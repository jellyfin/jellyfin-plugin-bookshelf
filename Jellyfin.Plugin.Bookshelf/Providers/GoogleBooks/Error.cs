using System.Collections.Generic;
using System.Net;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks;

internal class Error
{
    public HttpStatusCode Code { get; set; }

    public string Message { get; set; } = string.Empty;

    public IEnumerable<ErrorDetails> Errors { get; set; } = [];
}
