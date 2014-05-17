using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System.Xml.XPath;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.API;

namespace MediaBrowser.Plugins.Vimeo
{
    class VimeoChannelDownloader
    {
        private readonly ILogger _logger;
        private IHttpClient _httpClient;
        private IJsonSerializer _jsonSerializer;

        public VimeoChannelDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<Channels> GetVimeoChannelList(CancellationToken cancellationToken)
        {

            var channels = Plugin.vc.vimeo_channels_getAll(_logger);
            return channels;
        }

    }
}
