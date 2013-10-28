using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.NesBox
{
    public abstract class BaseNesBoxProvider : BaseMetadataProvider
    {
        protected IJsonSerializer JsonSerializer;

        protected BaseNesBoxProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer)
            : base(logManager, configurationManager)
        {
            JsonSerializer = jsonSerializer;
        }

        protected abstract string GameSystem { get; }
        protected abstract Stream GetCatalogStream();

        public override bool Supports(BaseItem item)
        {
            var game = item as Game;

            return game != null && string.Equals(game.GameSystem, GameSystem, StringComparison.OrdinalIgnoreCase);
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            // Already have it
            if (!string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.NesBox)) &&
                !string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.NesBoxRom)))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var stream = GetCatalogStream())
            {
                var catalog = JsonSerializer.DeserializeFromStream<List<NesBoxGame>>(stream);

                cancellationToken.ThrowIfCancellationRequested();
                
                FetchData(catalog, item);
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return TrueTaskResult;
        }

        private void FetchData(IEnumerable<NesBoxGame> catalog, BaseItem item)
        {
            var name = GetComparableName(item.Name);

            var match = catalog.FirstOrDefault(i => string.Equals(GetComparableName(i.name), name, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                var id = match.url.TrimEnd('/').Split('/').Last();

                item.SetProviderId(MetadataProviders.NesBox, id);

                var rom = match.play.TrimEnd('/').Split('/').Last();

                item.SetProviderId(MetadataProviders.NesBoxRom, rom);
            }
        }

        private string GetComparableName(string name)
        {
            Logger.Info("Original: " + name);
            
            var index = name.LastIndexOf('(');

            if (index != -1)
            {
                name = name.Substring(0, index);
            }

            var ret = RemoveSpecialCharacters(name);
            Logger.Info("GetComparableName: " + ret);

            return ret;
        }

        public static string RemoveSpecialCharacters(string str)
        {
            str = str.ReplaceString("The ", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString(", The", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("adventures", "adventure", StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("mike tyson's", string.Empty, StringComparison.OrdinalIgnoreCase);

            var sb = new StringBuilder();

            sb.Append(' ');
            sb.Append(str);
            sb.Append(' ');

            // Standardize this
            sb = sb.Replace(" 1 ", " I ")
                .Replace(" 2 ", " II ")
                .Replace(" 3 ", " III ")
                .Replace(" 4 ", " IV ")
                .Replace(" 5 ", " V ")
                .Replace(" 6 ", " VI ")
                .Replace(" 7 ", " VII ")
                .Replace(" 8 ", " VIII ");

            str = sb.ToString().Trim();

            sb.Clear();
            
            foreach (var c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            
            return sb.ToString();
        }
    }
}
