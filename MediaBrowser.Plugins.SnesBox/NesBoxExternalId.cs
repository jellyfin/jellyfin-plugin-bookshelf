using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Plugins.NesBox;
using System;

namespace MediaBrowser.Plugins.SnesBox
{
    public class SnesBoxExternalId : IExternalId
    {
        public string Key
        {
            get { return BaseNesBoxProvider.NesBoxExternalIdKeyName; }
        }

        public string Name
        {
            get { return "SNESBox"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            var game = item as Game;

            return game != null && string.Equals(game.GameSystem, Model.Games.GameSystem.SuperNintendo, StringComparison.OrdinalIgnoreCase);
        }

        public string UrlFormatString
        {
            get { return "http://snesbox.com/game/{0}"; }
        }
    }

    public class SnesBoxRomExternalId : IExternalId
    {
        public string Key
        {
            get { return BaseNesBoxProvider.NesBoxRomExternalIdKeyName; }
        }

        public string Name
        {
            get { return "SNESBox Rom Id"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            var game = item as Game;

            return game != null && string.Equals(game.GameSystem, Model.Games.GameSystem.SuperNintendo, StringComparison.OrdinalIgnoreCase);
        }

        public string UrlFormatString
        {
            get { return null; }
        }
    }
}
