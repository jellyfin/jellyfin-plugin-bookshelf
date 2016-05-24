using MediaBrowser.Common.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using CommonIO;

namespace GameBrowser.Resolvers
{
    class ResolverHelper
    {
        public static int? GetTgdbId(string consoleType)
        {
            return TgdbId.ContainsKey(consoleType) ? TgdbId[consoleType] : 0;
        }

        public static Dictionary<string, int> TgdbId = new Dictionary<string, int>
        {
                                                        {"3DO", 25},
                                                        {"Amiga", 4911},
                                                        {"Arcade", 23},
                                                        {"Atari 2600", 22},
                                                        {"Atari 5200", 26},
                                                        {"Atari 7800", 27},
                                                        {"Atari Jaguar", 28},
                                                        {"Atari Jaguar CD", 29},
                                                        {"Atari XE", 30},
                                                        {"Colecovision", 31},
                                                        {"Commodore 64", 40},
                                                        {"DOS", 1},
                                                        {"Intellivision", 32},
                                                        {"Xbox", 14},
                                                        {"Xbox 360", 15},                                                        
                                                        {"Xbox One", 4920},
                                                        {"Neo Geo", 24},
                                                        {"Nintendo 64", 3},
                                                        {"Nintendo DS", 8},
                                                        {"Nintendo", 7}, 
                                                        {"Game Boy", 4},
                                                        {"Game Boy Advance", 5},
                                                        {"Game Boy Color", 41},
                                                        {"Gamecube", 2},
                                                        {"Super Nintendo", 6},
                                                        {"Nintendo Wii", 9},
                                                        {"Nintendo Wii U", 38},
                                                        {"PC", 1},
                                                        {"Sega 32X", 33},
                                                        {"Sega CD", 21},
                                                        {"Dreamcast", 16},
                                                        {"Game Gear", 20},
                                                        {"Sega Genesis", 18},
                                                        {"Sega Master System", 35},
                                                        {"Sega Mega Drive", 36},
                                                        {"Sega Saturn", 17},
                                                        {"Sony Playstation", 10},
                                                        {"PS2", 11},
                                                        {"PS3", 12},
                                                        {"PS4", 4919},
                                                        {"PSP", 13},
                                                        {"TurboGrafx 16", 34},
                                                        {"TurboGrafx CD", 34},
                                                        {"Windows", 1}
                                                    };

        public static string AttemptGetGamePlatformTypeFromPath(IFileSystem fileSystem, string path)
        {
            var system = Plugin.Instance.Configuration.GameSystems.FirstOrDefault(s => fileSystem.ContainsSubPath(s.Path, path) || string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase));

            return system != null ? system.ConsoleType : null;
        }

        public static string GetGameSystemFromPath(IFileSystem fileSystem, string path)
        {
            var platform = AttemptGetGamePlatformTypeFromPath(fileSystem, path);

            if (string.IsNullOrEmpty(platform))
            {
                return null;
            }

            return GetGameSystemFromPlatform(platform);
        }

        public static string GetGameSystemFromPlatform(string platform)
        {
            if (string.IsNullOrEmpty(platform))
            {
                throw new ArgumentNullException("platform");
            }

            switch (platform)
            {
                case "3DO":
                    return "Panasonic3DO";

                case "Amiga":
                    return "Amiga";

                case "Arcade":
                    return "Arcade";

                case "Atari 2600":
                    return "Atari2600";

                case "Atari 5200":
                    return "Atari5200";

                case "Atari 7800":
                    return "Atari7800";

                case "Atari XE":
                    return "AtariXE";

                case "Atari Jaguar":
                    return "AtariJaguar";

                case "Atari Jaguar CD":
                    return "AtariJaguarCD";

                case "Colecovision":
                    return "Colecovision";

                case "Commodore 64":
                    return "Commodore64";

                case "Commodore Vic-20":
                    return "CommodoreVic20";

                case "Intellivision":
                    return "Intellivision";

                case "Xbox":
                    return "MicrosoftXBox";

                case "Xbox 360":
                    return "MicrosoftXBox360";

                case "Xbox One":
                    return "MicrosoftXBoxOne";

                case "Neo Geo":
                    return "NeoGeo";

                case "Nintendo 64":
                    return "Nintendo64";

                case "Nintendo DS":
                    return "NintendoDS";

                case "Nintendo":
                    return "Nintendo";

                case "Game Boy":
                    return "NintendoGameBoy";

                case "Game Boy Advance":
                    return "NintendoGameBoyAdvance";

                case "Game Boy Color":
                    return "NintendoGameBoyColor";

                case "Gamecube":
                    return "NintendoGameCube";

                case "Super Nintendo":
                    return "SuperNintendo";

                case "Virtual Boy":
                    return "VirtualBoy";

                case "Nintendo Wii":
                    return "Wii";

                case "Nintendo Wii U":
                    return "WiiU";

                case "DOS":
                    return "DOS";

                case "Windows":
                    return "Windows";

                case "Sega 32X":
                    return "Sega32X";

                case "Sega CD":
                    return "SegaCD";

                case "Dreamcast":
                    return "SegaDreamcast";

                case "Game Gear":
                    return "SegaGameGear";

                case "Sega Genesis":
                    return "SegaGenesis";

                case "Sega Master System":
                    return "SegaMasterSystem";

                case "Sega Mega Drive":
                    return "SegaMegaDrive";

                case "Sega Saturn":
                    return "SegaSaturn";

                case "Sony Playstation":
                    return "SonyPlaystation";

                case "PS2":
                    return "SonyPlaystation2";

                case "PS3":
                    return "SonyPlaystation3";

                case "PS4":
                    return "SonyPlaystation4";

                case "PSP":
                    return "SonyPSP";

                case "TurboGrafx 16":
                    return "TurboGrafx16";

                case "TurboGrafx CD":
                    return "TurboGrafxCD";

                case "ZX Spectrum":
                    return "ZxSpectrum";

            }
            return null;
        }

        public static string GetDisplayMediaTypeFromPlatform(string platform)
        {
            if (string.IsNullOrEmpty(platform))
            {
                throw new ArgumentNullException("platform");
            }

            switch (platform)
            {
                case "3DO":
                    return "Panasonic3doGame";

                case "Amiga":
                    return "AmigaGame";

                case "Arcade":
                    return "ArcadeGame";

                case "Atari 2600":
                    return "Atari2600Game";

                case "Atari 5200":
                    return "Atari5200Game";

                case "Atari 7800":
                    return "Atari7800Game";

                case "Atari XE":
                    return "AtariXeGame";

                case "Atari Jaguar":
                    return "JaguarGame";

                case "Atari Jaguar CD":
                    return "JaguarGame";

                case "Colecovision":
                    return "ColecovisionGame";

                case "Commodore 64":
                    return "C64Game";

                case "Commodore Vic-20":
                    return "Vic20Game";

                case "Intellivision":
                    return "IntellivisionGame";

                case "Xbox":
                    return "XboxGame";

                case "Xbox 360":
                    return "Xbox360Game";

                case "Xbox One":
                    return "XboxOneGame";

                case "Neo Geo":
                    return "NeoGeoGame";

                case "Nintendo 64":
                    return "N64Game";

                case "Nintendo DS":
                    return "NesGame";

                case "Nintendo":
                    return "NesGame";

                case "Game Boy":
                    return "GameBoyGame";

                case "Game Boy Advance":
                    return "GameBoyAdvanceGame";

                case "Game Boy Color":
                    return "GameBoyColorGame";

                case "Gamecube":
                    return "GameCubeGame";

                case "Super Nintendo":
                    return "SnesGame";

                case "Virtual Boy":
                    return "NesGame";

                case "Nintendo Wii":
                    return "NesGame";

                case "Nintendo Wii U":
                    return "WiiUGame";

                case "DOS":
                    return "DosGame";

                case "Windows":
                    return "WindowsGame";

                case "Sega 32X":
                    return "GenesisGame";

                case "Sega CD":
                    return "GenesisGame";

                case "Dreamcast":
                    return "GenesisGame";

                case "Game Gear":
                    return "GenesisGame";

                case "Sega Genesis":
                    return "GenesisGame";

                case "Sega Master System":
                    return "GenesisGame";

                case "Sega Mega Drive":
                    return "GenesisGame";

                case "Sega Saturn":
                    return "GenesisGame";

                case "Sony Playstation":
                    return "PsOneGame";

                case "PS2":
                    return "Ps2Game";

                case "PS3":
                    return "Ps3Game";

                case "PS4":
                    return "Ps4Game";

                case "PSP":
                    return "PlayStationPortableGame";

                case "TurboGrafx 16":
                    return "TurboGrafx16Game";

                case "TurboGrafx CD":
                    return "TurboGrafx16Game";

                case "ZX Spectrum":
                    return "ZXSpectrumGame";

            }
            return null;
        }
    }
}
