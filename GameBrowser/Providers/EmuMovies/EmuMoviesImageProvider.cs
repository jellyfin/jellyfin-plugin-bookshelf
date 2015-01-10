using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace GameBrowser.Providers.EmuMovies
{
    public class EmuMoviesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;

        public EmuMoviesImageProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            foreach (var image in GetSupportedImages(item))
            {
                var sublist = await GetImages(item, image, cancellationToken).ConfigureAwait(false);

                list.AddRange(sublist);
            }

            return list;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            // TODO: Call GetEmuMoviesToken and replace the sessionId in the incoming url with the latest value. 

            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = Plugin.Instance.EmuMoviesSemiphore
            });
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            var game = (Game)item;

            switch (imageType)
            {
                case ImageType.Box:
                    return FetchImages(game, EmuMoviesMediaTypes.Cabinet, imageType, cancellationToken);
                case ImageType.Screenshot:
                    return FetchImages(game, EmuMoviesMediaTypes.Snap, imageType, cancellationToken);
                case ImageType.Disc:
                    return FetchImages(game, EmuMoviesMediaTypes.Cart, imageType, cancellationToken);
                case ImageType.Menu:
                    return FetchImages(game, EmuMoviesMediaTypes.Title, imageType, cancellationToken);
                default:
                    throw new ArgumentException("Unrecognized image type");
            }
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <param name="type">The type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        private async Task<IEnumerable<RemoteImageInfo>> FetchImages(Game game, EmuMoviesMediaTypes mediaType, ImageType type, CancellationToken cancellationToken)
        {
            var sessionId = await Plugin.Instance.GetEmuMoviesToken(cancellationToken);

            var list = new List<RemoteImageInfo>();

            if (sessionId == null) return list;

            var url = string.Format(EmuMoviesUrls.Search, HttpUtility.UrlEncode(game.Name), GetEmuMoviesPlatformFromGameSystem(game.GameSystem), mediaType, sessionId);

            using (var stream = await _httpClient.Get(url, Plugin.Instance.EmuMoviesSemiphore, cancellationToken).ConfigureAwait(false))
            {
                var doc = new XmlDocument();
                doc.Load(stream);

                if (doc.HasChildNodes)
                {
                    var nodes = doc.SelectNodes("Results/Result");

                    if (nodes != null)
                    {
                        foreach (XmlNode node in nodes)
                        {
                            if (node != null && node.Attributes != null)
                            {
                                var urlAttribute = node.Attributes["URL"];

                                if (urlAttribute != null && !string.IsNullOrEmpty(urlAttribute.Value))
                                {
                                    list.Add(new RemoteImageInfo
                                    {
                                        ProviderName = Name,
                                        Type = type,
                                        Url = urlAttribute.Value
                                    });
                                }
                            }
                        }
                    }

                }
            }

            return list;
        }

        private string GetEmuMoviesPlatformFromGameSystem(string platform)
        {
            string emuMoviesPlatform = null;

            switch (platform)
            {
                case "3DO":
                    emuMoviesPlatform = "Panasonic_3DO";

                    break;

                case "Amiga":
                    emuMoviesPlatform = "";

                    break;

                case "Arcade":
                    emuMoviesPlatform = "MAME";

                    break;

                case "Atari 2600":
                    emuMoviesPlatform = "Atari_2600";

                    break;

                case "Atari 5200":
                    emuMoviesPlatform = "Atari_5200";

                    break;

                case "Atari 7800":
                    emuMoviesPlatform = "Atari_7800";

                    break;

                case "Atari XE":
                    emuMoviesPlatform = "Atari_8_bit";

                    break;

                case "Atari Jaguar":
                    emuMoviesPlatform = "Atari_Jaguar";

                    break;

                case "Atari Jaguar CD":
                    emuMoviesPlatform = "Atari_Jaguar";

                    break;

                case "Colecovision":
                    emuMoviesPlatform = "Coleco_Vision";

                    break;

                case "Commodore 64":
                    emuMoviesPlatform = "Commodore_64";

                    break;

                case "Commodore Vic-20":
                    emuMoviesPlatform = "";

                    break;

                case "Intellivision":
                    emuMoviesPlatform = "Mattel_Intellivision";

                    break;

                case "Xbox":
                    emuMoviesPlatform = "Microsoft_Xbox";

                    break;

                case "Neo Geo":
                    emuMoviesPlatform = "SNK_Neo_Geo_AES";

                    break;

                case "Nintendo 64":
                    emuMoviesPlatform = "Nintendo_N64";

                    break;

                case "Nintendo DS":
                    emuMoviesPlatform = "Nintendo_DS";

                    break;

                case "Nintendo":
                    emuMoviesPlatform = "Nintendo_NES";

                    break;

                case "Game Boy":
                    emuMoviesPlatform = "Nintendo_Game_Boy";

                    break;

                case "Game Boy Advance":
                    emuMoviesPlatform = "Nintendo_Game_Boy_Advance";

                    break;

                case "Game Boy Color":
                    emuMoviesPlatform = "Nintendo_Game_Boy_Color";

                    break;

                case "Gamecube":
                    emuMoviesPlatform = "Nintendo_GameCube";

                    break;

                case "Super Nintendo":
                    emuMoviesPlatform = "Nintendo_SNES";

                    break;

                case "Virtual Boy":
                    emuMoviesPlatform = "";

                    break;

                case "Nintendo Wii":
                    emuMoviesPlatform = "";

                    break;

                case "DOS":
                    emuMoviesPlatform = "";

                    break;

                case "Windows":
                    emuMoviesPlatform = "";

                    break;

                case "Sega 32X":
                    emuMoviesPlatform = "Sega_Genesis";

                    break;

                case "Sega CD":
                    emuMoviesPlatform = "Sega_Genesis";

                    break;

                case "Dreamcast":
                    emuMoviesPlatform = "Sega_Dreamcast";

                    break;

                case "Game Gear":
                    emuMoviesPlatform = "Sega_Game_Gear";

                    break;

                case "Sega Genesis":
                    emuMoviesPlatform = "Sega_Genesis";

                    break;

                case "Sega Master System":
                    emuMoviesPlatform = "Sega_Master_System";

                    break;

                case "Sega Mega Drive":
                    emuMoviesPlatform = "Sega_Genesis";

                    break;

                case "Sega Saturn":
                    emuMoviesPlatform = "Sega_Saturn";

                    break;

                case "Sony Playstation":
                    emuMoviesPlatform = "Sony_Playstation";

                    break;

                case "PS2":
                    emuMoviesPlatform = "Sony_Playstation_2";

                    break;

                case "PSP":
                    emuMoviesPlatform = "Sony_PSP";

                    break;

                case "TurboGrafx 16":
                    emuMoviesPlatform = "NEC_TurboGrafx_16";

                    break;

                case "TurboGrafx CD":
                    emuMoviesPlatform = "NEC_TurboGrafx_16";
                    break;

                case "ZX Spectrum":
                    emuMoviesPlatform = "";
                    break;
            }

            return emuMoviesPlatform;

        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new[] { ImageType.Box, ImageType.Disc, ImageType.Screenshot, ImageType.Menu };
        }

        public string Name
        {
            get { return "Emu Movies"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Game;
        }

        public int Order
        {
            get
            {
                // Make sure it runs after games db since these images are lower resolution
                return 1;
            }
        }
    }
}
