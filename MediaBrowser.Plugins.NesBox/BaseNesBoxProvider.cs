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

        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "3";
            }
        }

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
            //Logger.Info("Original name: " + name);
            var index = name.LastIndexOf('(');

            if (index != -1)
            {
                name = name.Substring(0, index);
            }

            var ret = RemoveSpecialCharacters(name);

            //Logger.Info("Comparable name: " + ret);
            return ret;
        }

        public static string RemoveSpecialCharacters(string str)
        {
            str = str.ReplaceString("adventures", "adventure", StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("mike tyson's", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("x2", "x 2", StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("x3", "x 3", StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("alien wars", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("Legend of the Seven Stars", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("The World Warriors", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("The World Warrior", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("Hyper Fighting", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("The New Challengers", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("Legend of the Seven Stars", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString(", The", string.Empty, StringComparison.OrdinalIgnoreCase)
                     .ReplaceString("The ", string.Empty, StringComparison.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            
            sb.Append(' ');
            sb.Append(str);
            sb.Append(' ');

            // Standardize this
            sb = sb.Replace(":", string.Empty)
                .Replace("-", string.Empty)
                .Replace("'", string.Empty)
                .Replace(" 1 ", " I ")
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

    public static class Extensions
    {
        public static string ReplaceString(this string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }
    }

    /// <summary>
    /// Class NesBoxGame
    /// </summary>
    public class NesBoxGame
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string name { get; set; }
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string url { get; set; }
        /// <summary>
        /// Gets or sets the play.
        /// </summary>
        /// <value>The play.</value>
        public string play { get; set; }
    }
}
