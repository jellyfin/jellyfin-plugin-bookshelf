using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public static class XmlSaverHelpers
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified XML.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <param name="path">The path.</param>
        /// <param name="xmlTagsUsed">The XML tags used.</param>
        public static void Save(StringBuilder xml, string path, IEnumerable<string> xmlTagsUsed)
        {
            if (File.Exists(path))
            {
                var tags = xmlTagsUsed.ToList();

                tags.AddRange(new[]
                {
                    "plot",
                    "customrating",
                    "lockdata",
                    "type",
                    "dateadded",
                    "title",
                    "rating",
                    "year",
                    "sorttitle",
                    "mpaa",
                    "mpaadescription",
                    "aspectratio",
                    "website",
                    "collectionnumber",
                    "tmdbid",
                    "rottentomatoesid",
                    "language",
                    "tvcomid",
                    "budget",
                    "revenue",
                    "tagline",
                    "studio",
                    "genre",
                    "tag",
                    "runtime",
                    "actor",
                    "criticratingsummary",
                    "criticrating"
                });

                var position = xml.ToString().LastIndexOf("</", StringComparison.OrdinalIgnoreCase);
                xml.Insert(position, GetCustomTags(path, tags));
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml.ToString());

            //Add the new node to the document.
            xmlDocument.InsertBefore(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes"), xmlDocument.DocumentElement);

            var parentPath = Path.GetDirectoryName(path);

            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            var wasHidden = false;

            var file = new FileInfo(path);

            // This will fail if the file is hidden
            if (file.Exists)
            {
                if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    file.Attributes &= ~FileAttributes.Hidden;

                    wasHidden = true;
                }
            }

            using (var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var streamWriter = new StreamWriter(filestream, Encoding.UTF8))
                {
                    xmlDocument.Save(streamWriter);
                }
            }

            if (wasHidden)
            {
                file.Refresh();

                // Add back the attribute
                file.Attributes |= FileAttributes.Hidden;
            }
        }

        /// <summary>
        /// Gets the custom tags.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="xmlTagsUsed">The XML tags used.</param>
        /// <returns>System.String.</returns>
        private static string GetCustomTags(string path, ICollection<string> xmlTagsUsed)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            var nodes = doc.DocumentElement.ChildNodes.Cast<XmlNode>()
                .Where(i => !xmlTagsUsed.Contains(i.Name))
                .Select(i => i.OuterXml)
                .ToArray();

            return string.Join(Environment.NewLine, nodes);
        }
        
        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="builder">The builder.</param>
        public static void AddCommonNodes(BaseItem item, StringBuilder builder)
        {
            builder.Append("<plot><![CDATA[" + (item.Overview ?? string.Empty) + "]]></plot>");
            builder.Append("<customrating>" + SecurityElement.Escape(item.CustomRating ?? string.Empty) + "</customrating>");
            builder.Append("<lockdata>" + item.DontFetchMeta.ToString().ToLower() + "</lockdata>");

            if (!string.IsNullOrEmpty(item.DisplayMediaType))
            {
                builder.Append("<type>" + SecurityElement.Escape(item.DisplayMediaType) + "</type>");
            }

            builder.Append("<dateadded>" + SecurityElement.Escape(item.DateCreated.ToString("yyyy-MM-dd HH:mm:ss")) + "</dateadded>");

            builder.Append("<title>" + SecurityElement.Escape(item.Name ?? string.Empty) + "</title>");

            foreach (var person in item.People
                .Where(i => string.Equals(i.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase)))
            {
                builder.Append("<director>" + SecurityElement.Escape(person.Name) + "</director>");
            }

            foreach (var person in item.People
                .Where(i => string.Equals(i.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase)))
            {
                builder.Append("<writer>" + SecurityElement.Escape(person.Name) + "</writer>");
            }

            foreach (var trailer in item.RemoteTrailers)
            {
                builder.Append("<trailer>" + SecurityElement.Escape(trailer.Url) + "</trailer>");
            }
            
            if (item.CommunityRating.HasValue)
            {
                builder.Append("<rating>" + SecurityElement.Escape(item.CommunityRating.Value.ToString(UsCulture)) + "</rating>");
            }

            if (item.ProductionYear.HasValue)
            {
                builder.Append("<year>" + SecurityElement.Escape(item.ProductionYear.Value.ToString(UsCulture)) + "</year>");
            }

            if (!string.IsNullOrEmpty(item.ForcedSortName))
            {
                builder.Append("<sorttitle>" + SecurityElement.Escape(item.ForcedSortName) + "</sorttitle>");
            }

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                builder.Append("<mpaa>" + SecurityElement.Escape(item.OfficialRating) + "</mpaa>");
            }

            if (!string.IsNullOrEmpty(item.OfficialRatingDescription))
            {
                builder.Append("<mpaadescription>" + SecurityElement.Escape(item.OfficialRatingDescription) + "</mpaadescription>");
            }

            if (!string.IsNullOrEmpty(item.AspectRatio))
            {
                builder.Append("<aspectratio>" + SecurityElement.Escape(item.AspectRatio) + "</aspectratio>");
            }

            if (!string.IsNullOrEmpty(item.HomePageUrl))
            {
                builder.Append("<website>" + SecurityElement.Escape(item.HomePageUrl) + "</website>");
            }

            var rt = item.GetProviderId(MetadataProviders.RottenTomatoes);

            if (!string.IsNullOrEmpty(rt))
            {
                builder.Append("<rottentomatoesid>" + SecurityElement.Escape(rt) + "</rottentomatoesid>");
            }

            var tmdbCollection = item.GetProviderId(MetadataProviders.TmdbCollection);

            if (!string.IsNullOrEmpty(tmdbCollection))
            {
                builder.Append("<collectionnumber>" + SecurityElement.Escape(tmdbCollection) + "</collectionnumber>");
            }

            var tmdb = item.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(tmdb))
            {
                builder.Append("<tmdbid>" + SecurityElement.Escape(tmdb) + "</tmdbid>");
            }

            var tvcom = item.GetProviderId(MetadataProviders.Tvcom);

            if (!string.IsNullOrEmpty(tvcom))
            {
                builder.Append("<tvcomid>" + SecurityElement.Escape(tvcom) + "</tvcomid>");
            }

            if (!string.IsNullOrEmpty(item.Language))
            {
                builder.Append("<language>" + SecurityElement.Escape(item.Language) + "</language>");
            }

            if (item.PremiereDate.HasValue && !(item is Episode))
            {
                builder.Append("<premiered>" + SecurityElement.Escape(item.PremiereDate.Value.ToShortDateString()) + "</premiered>");
            }

            if (item.CriticRating.HasValue)
            {
                builder.Append("<criticrating>" + SecurityElement.Escape(item.CriticRating.Value.ToString(UsCulture)) + "</criticrating>");
            }

            if (!string.IsNullOrEmpty(item.CriticRatingSummary))
            {
                builder.Append("<criticratingsummary><![CDATA[" + item.Overview + "]]></criticratingsummary>");
            }
            
            if (item.Budget.HasValue)
            {
                builder.Append("<budget>" + SecurityElement.Escape(item.Budget.Value.ToString(UsCulture)) + "</budget>");
            }

            if (item.Revenue.HasValue)
            {
                builder.Append("<revenue>" + SecurityElement.Escape(item.Revenue.Value.ToString(UsCulture)) + "</revenue>");
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.OriginalRunTimeTicks ?? item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                builder.Append("<runtime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</runtime>");
            }

            if (item.Taglines.Count > 0)
            {
                foreach (var tagline in item.Taglines)
                {
                    builder.Append("<tagline>" + SecurityElement.Escape(tagline) + "</tagline>");
                }
            }

            if (item.Genres.Count > 0)
            {
                foreach (var genre in item.Genres)
                {
                    builder.Append("<genre>" + SecurityElement.Escape(genre) + "</genre>");
                }
            }

            if (item.Studios.Count > 0)
            {
                foreach (var studio in item.Studios)
                {
                    builder.Append("<studio>" + SecurityElement.Escape(studio) + "</studio>");
                }
            }

            if (item.Tags.Count > 0)
            {
                builder.Append("<tags>");

                foreach (var tag in item.Tags)
                {
                    builder.Append("<tag>" + SecurityElement.Escape(tag) + "</tag>");
                }

                builder.Append("</tags>");
            }

            foreach (var person in item.People
                .Where(i => !string.Equals(i.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase) && !string.Equals(i.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase)))
            {
                builder.Append("<actor>");
                builder.Append("<name>" + SecurityElement.Escape(person.Name) + "</name>");
                builder.Append("<role>" + SecurityElement.Escape(person.Role) + "</role>");
                builder.Append("</actor>");
            }

        }

    }
}
