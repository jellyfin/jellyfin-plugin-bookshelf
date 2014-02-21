using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Plugins.NesBox
{
    public class NesBoxExternalId : IExternalId
    {
        public string Key
        {
            get { return BaseNesBoxProvider.NesBoxExternalIdKeyName; }
        }

        public string Name
        {
            get { return "NESBox"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            var game = item as Game;

            return game != null && string.Equals(game.GameSystem, Model.Games.GameSystem.Nintendo, StringComparison.OrdinalIgnoreCase);
        }

        public string UrlFormatString
        {
            get { return "http://nesbox.com/game/{0}"; }
        }
    }

    public class NesBoxRomExternalId : IExternalId
    {
        public string Key
        {
            get { return BaseNesBoxProvider.NesBoxRomExternalIdKeyName; }
        }

        public string Name
        {
            get { return "NESBox Rom Id"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            var game = item as Game;

            return game != null && string.Equals(game.GameSystem, Model.Games.GameSystem.Nintendo, StringComparison.OrdinalIgnoreCase);
        }

        public string UrlFormatString
        {
            get { return "http://nesbox.com/game/rom/{0}"; }
        }
    }
}
