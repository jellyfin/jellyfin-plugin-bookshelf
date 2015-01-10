using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace GameBrowser.Providers.GamesDb
{
    class GamesDbImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;

        public GamesDbImageProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public bool Supports(IHasImages item)
        {
            return item is Game || item is GameSystem;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var xmlPath = await GetXmlPath(item, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(xmlPath))
            {

                try
                {
                    AddImages(list, item is GameSystem, xmlPath, cancellationToken);
                }
                catch (FileNotFoundException)
                {
                    // Carry on.
                }
            }

            return list;
        }

        private async Task<string> GetXmlPath(IHasImages item, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(GamesDbExternalId.KeyName);

            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (item is Game)
            {
                await GamesDbGameProvider.Current.EnsureCacheFile(id, cancellationToken).ConfigureAwait(false);

                return GamesDbGameProvider.Current.GetCacheFilePath(id);
            }

            await GamesDbGameSystemProvider.Current.EnsureCacheFile(id, cancellationToken).ConfigureAwait(false);

            return GamesDbGameSystemProvider.Current.GetCacheFilePath(id);
        }

        private void AddImages(List<RemoteImageInfo> list, bool isConsole, string xmlPath, CancellationToken cancellationToken)
        {
            using (var streamReader = new StreamReader(xmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, new XmlReaderSettings
                {
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    ValidationType = ValidationType.None
                }))
                {
                    // With the exception of one element both games and gamesystems use the same xml structure for images.
                    reader.ReadToDescendant("Images");

                    using (var subReader = reader.ReadSubtree())
                    {
                        AddImages(list, isConsole, subReader, cancellationToken);
                    }
                }
            }
        }

        private void AddImages(List<RemoteImageInfo> list, bool isConsole, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "fanart":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Backdrop);
                                }
                                break;
                            }
                        case "screenshot":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    PopulateImageCategory(list, subReader, cancellationToken, ImageType.Screenshot);
                                }
                                break;
                            }
                        case "boxart":
                            {
                                var side = reader.GetAttribute("side");

                                if (side == null) break;

                                if (side.Equals("front", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    PopulateImage(list, reader, cancellationToken, ImageType.Primary);
                                }
                                else if (side.Equals("back", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // Have to account for console primary images being uploaded as side-back
                                    PopulateImage(list, reader, cancellationToken,
                                                  isConsole ? ImageType.Primary : ImageType.BoxRear);
                                }
                                break;
                            }
                        case "banner":
                            {
                                PopulateImage(list, reader, cancellationToken, ImageType.Banner);
                                break;
                            }
                        case "clearlogo":
                            {
                                PopulateImage(list, reader, cancellationToken, ImageType.Logo);
                                break;
                            }
                        default:
                            {
                                using (reader.ReadSubtree())
                                {
                                }
                                break;
                            }
                    }
                }
            }
        }



        private void PopulateImage(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken, ImageType type)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element)
            {
                var width = Convert.ToInt32(reader.GetAttribute("width"));
                var height = Convert.ToInt32(reader.GetAttribute("height"));
                var url = reader.ReadString();

                if (!string.IsNullOrEmpty(url))
                {
                    var info = new RemoteImageInfo
                    {
                        Type = type,
                        Width = width,
                        Height = height,
                        ProviderName = Name,
                        Url = TgdbUrls.BaseImagePath + url
                    };

                    list.Add(info);
                }
            }
        }



        private void PopulateImageCategory(List<RemoteImageInfo> list, XmlReader reader, CancellationToken cancellationToken, ImageType type)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "original":
                            {
                                var width = Convert.ToInt32(reader.GetAttribute("width"));
                                var height = Convert.ToInt32(reader.GetAttribute("height"));
                                var url = reader.ReadString();

                                if (!string.IsNullOrEmpty(url))
                                {
                                    var info = new RemoteImageInfo
                                    {
                                        Type = type,
                                        Width = width,
                                        Height = height,
                                        ProviderName = Name,
                                        Url = TgdbUrls.BaseImagePath + url
                                    };

                                    list.Add(info);
                                }
                                break;
                            }
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }



        public string Name
        {
            get { return "GamesDb"; }
        }

        public int Order
        {
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = Plugin.Instance.TgdbSemiphore
            });
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            if (item is Game)
            {
                return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.BoxRear,
                ImageType.Logo,
                ImageType.Banner,
                ImageType.Screenshot
            };
            }

            // GameSystem
            // TODO: Are these correct?
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.BoxRear,
                ImageType.Logo,
                ImageType.Banner
            };
        }
    }
}
