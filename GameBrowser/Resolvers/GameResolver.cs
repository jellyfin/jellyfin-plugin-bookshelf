using GameBrowser.Library.Utils;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;
using GameSystem = MediaBrowser.Model.Games.GameSystem;

namespace GameBrowser.Resolvers
{
    /// <summary>
    /// Class GameResolver
    /// </summary>
    public class GameResolver : ItemResolver<Game>
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public GameResolver(ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Run before any core resolvers
        /// </summary>
        public override ResolverPriority Priority
        {
            get
            {
                return ResolverPriority.First;
            }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Game.</returns>
        protected override Game Resolve(ItemResolveArgs args)
        {
            var collectionType = args.GetCollectionType();

            if (!string.Equals(collectionType, CollectionType.Games, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            
            var platform = ResolverHelper.AttemptGetGamePlatformTypeFromPath(_fileSystem, args.Path);

            if (string.IsNullOrEmpty(platform)) return null;

            if (args.IsDirectory)
            {
                return GetGame(args, platform);
            }

            // For MAME we will allow all games in the same dir
            if (string.Equals(platform, "Arcade"))
            {
                var extension = Path.GetExtension(args.Path);

                if (string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".7z", StringComparison.OrdinalIgnoreCase))
                {
                    // ignore zips that are bios roms.
                    if (MameUtils.IsBiosRom(args.Path)) return null;

                    var game = new Game
                    {
                        Name = MameUtils.GetFullNameFromPath(args.Path, _logger),
                        Path = args.Path,
                        GameSystem = "Arcade",
                        DisplayMediaType = "Arcade",
                        IsInMixedFolder = true
                    };
                    return game;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified path is game.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <param name="consoleType">The type of gamesystem this game belongs too</param>
        /// <returns>A Game</returns>
        private Game GetGame(ItemResolveArgs args, string consoleType)
        {
            var validExtensions = GetExtensions(consoleType);

            var gameFiles = args.FileSystemChildren.Where(f =>
            {
                var fileExtension = Path.GetExtension(f.FullName);

                return validExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);

            }).ToList();

            if (gameFiles.Count == 0)
            {
                _logger.Error("gameFiles is 0 for " + args.Path);
                return null;
            }

            var game = new Game
            {
                Path = gameFiles[0].FullName,
                GameSystem = ResolverHelper.GetGameSystemFromPlatform(consoleType)
            };

            game.IsPlaceHolder =
                string.Equals(game.GameSystem, GameSystem.Windows, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(game.GameSystem, GameSystem.DOS, StringComparison.OrdinalIgnoreCase);

            game.DisplayMediaType = ResolverHelper.GetDisplayMediaTypeFromPlatform(consoleType);

            if (gameFiles.Count > 1)
            {
                game.MultiPartGameFiles = gameFiles.Select(i => i.FullName).ToList();
                game.IsMultiPart = true;
            }

            return game;
        }


        private IEnumerable<string> GetExtensions(string consoleType)
        {
            switch (consoleType)
            {
                case "3DO":
                    return new[] { ".iso", ".cue" };

                case "Amiga":
                    return new[] { ".iso", ".adf" };

                case "Arcade":
                    return new[] { ".zip" };

                case "Atari 2600":
                    return new[] { ".bin", ".a26" };

                case "Atari 5200":
                    return new[] { ".bin", ".a52" };

                case "Atari 7800":
                    return new[] { ".a78" };

                case "Atari XE":
                    return new[] { ".rom" };

                case "Atari Jaguar":
                    return new[] { ".j64", ".zip" };

                case "Atari Jaguar CD": // still need to verify
                    return new[] { ".iso" };

                case "Colecovision":
                    return new[] { ".col", ".rom" };

                case "Commodore 64":
                    return new[] { ".d64", ".g64", ".prg", ".tap" };

                case "Commodore Vic-20":
                    return new[] { ".prg" };

                case "Intellivision":
                    return new[] { ".int", ".rom" };

                case "Xbox":
                    return new[] { ".iso" };

                case "Neo Geo":
                    return new[] { ".zip", ".iso" };

                case "Nintendo 64":
                    return new[] { ".z64", ".v64", ".usa", ".jap", ".pal", ".rom", ".n64", ".zip" };

                case "Nintendo DS":
                    return new[] { ".nds", ".zip" };

                case "Nintendo":
                    return new[] { ".nes", ".zip" };

                case "Game Boy":
                    return new[] { ".gb", ".zip" };

                case "Game Boy Advance":
                    return new[] { ".gba", ".zip" };

                case "Game Boy Color":
                    return new[] { ".gbc", ".zip" };

                case "Gamecube":
                    return new[] { ".iso", ".bin", ".img", ".gcm", ".gcz" };

                case "Super Nintendo":
                    return new[] { ".smc", ".zip", ".fam", ".rom", ".sfc", ".fig" };

                case "Virtual Boy":
                    return new[] { ".vb" };

                case "Nintendo Wii":
                    return new[] { ".iso", ".dol", ".ciso", ".wbfs", ".wad", ".gcz" };

                case "DOS":
                    return new[] { ".gbdos", ".disc" };

                case "Windows":
                    return new[] { ".gbwin", ".disc" };

                case "Sega 32X":
                    return new[] { ".iso", ".bin", ".img", ".zip", ".32x" };

                case "Sega CD":
                    return new[] { ".iso", ".bin", ".img" };

                case "Dreamcast":
                    return new[] { ".chd", ".gdi", ".cdi" };

                case "Game Gear":
                    return new[] { ".gg", ".zip" };

                case "Sega Genesis":
                    return new[] { ".smd", ".bin", ".gen", ".zip", ".md" };

                case "Sega Master System":
                    return new[] { ".sms", ".sg", ".sc", ".zip" };

                case "Sega Mega Drive":
                    return new[] { ".smd", ".zip", ".md" };

                case "Sega Saturn":
                    return new[] { ".iso", ".bin", ".img" };

                case "Sony Playstation":
                    return new[] { ".iso", ".bin", ".img", ".ps1" };

                case "PS2":
                    return new[] { ".iso", ".bin" };

                case "PSP":
                    return new[] { ".iso", ".cso" };

                case "TurboGrafx 16":
                    return new[] { ".pce", ".zip" };

                case "TurboGrafx CD":
                    return new[] { ".bin", ".iso" };

                case "ZX Spectrum":
                    return new[] { ".z80", ".tap", ".tzx" };

                default:
                    return new string[] { };
            }

        }
    }
}
