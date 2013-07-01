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
                    "criticrating",
                    "fileinfo",
                    "director",
                    "writer",
                    "trailer",
                    "premiered",
                    "releasedate"
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

        public static void AddMediaInfo<T>(T item, StringBuilder builder)
            where T : BaseItem, IHasMediaStreams
        {
            builder.Append("<fileinfo>");
            builder.Append("<streamdetails>");

            foreach (var stream in item.MediaStreams)
            {
                builder.Append("<" + stream.Type.ToString().ToLower() + ">");

                if (!string.IsNullOrEmpty(stream.Codec))
                {
                    builder.Append("<codec>" + SecurityElement.Escape(stream.Codec) + "</codec>");
                    builder.Append("<micodec>" + SecurityElement.Escape(stream.Codec) + "</micodec>");
                }

                if (stream.BitRate.HasValue)
                {
                    builder.Append("<bitrate>" + stream.BitRate.Value.ToString(UsCulture) + "</bitrate>");
                }

                if (stream.Width.HasValue)
                {
                    builder.Append("<width>" + stream.Width.Value.ToString(UsCulture) + "</width>");
                }

                if (stream.Height.HasValue)
                {
                    builder.Append("<height>" + stream.Height.Value.ToString(UsCulture) + "</height>");
                }

                if (!string.IsNullOrEmpty(stream.AspectRatio))
                {
                    builder.Append("<aspectratio>" + SecurityElement.Escape(stream.AspectRatio) + "</aspectratio>");
                }

                var framerate = stream.AverageFrameRate ?? stream.RealFrameRate;

                if (framerate.HasValue)
                {
                    builder.Append("<framerate>" + framerate.Value.ToString(UsCulture) + "</framerate>");
                }

                if (!string.IsNullOrEmpty(stream.Language))
                {
                    builder.Append("<language>" + SecurityElement.Escape(stream.Language) + "</language>");
                }

                if (!string.IsNullOrEmpty(stream.ScanType))
                {
                    builder.Append("<scantype>" + SecurityElement.Escape(stream.ScanType) + "</scantype>");
                }

                if (stream.Channels.HasValue)
                {
                    builder.Append("<channels>" + stream.Channels.Value.ToString(UsCulture) + "</channels>");
                }

                if (stream.SampleRate.HasValue)
                {
                    builder.Append("<samplingrate>" + stream.SampleRate.Value.ToString(UsCulture) + "</samplingrate>");
                }

                builder.Append("<default>" + SecurityElement.Escape(stream.IsDefault.ToString()) + "</default>");
                builder.Append("<forced>" + SecurityElement.Escape(stream.IsForced.ToString()) + "</forced>");

                if (stream.Type == MediaStreamType.Video)
                {
                    if (item.RunTimeTicks.HasValue)
                    {
                        var timespan = TimeSpan.FromTicks(item.RunTimeTicks.Value);

                        builder.Append("<duration>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</duration>");
                        builder.Append("<durationinseconds>" + Convert.ToInt32(timespan.TotalSeconds).ToString(UsCulture) + "</durationinseconds>");
                    }

                    var video = item as Video;

                    if (video != null && video.Video3DFormat.HasValue)
                    {
                        switch (video.Video3DFormat.Value)
                        {
                            case Video3DFormat.FullSideBySide:
                                builder.Append("<3DFormat>FSBS</3DFormat>");
                                break;
                            case Video3DFormat.FullTopAndBottom:
                                builder.Append("<3DFormat>FTAB</3DFormat>");
                                break;
                            case Video3DFormat.HalfSideBySide:
                                builder.Append("<3DFormat>HSBS</3DFormat>");
                                break;
                            case Video3DFormat.HalfTopAndBottom:
                                builder.Append("<3DFormat>HTAB</3DFormat>");
                                break;
                        }
                    }
                }

                builder.Append("</" + stream.Type.ToString().ToLower() + ">");
            }

            builder.Append("</streamdetails>");
            builder.Append("</fileinfo>");
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
                builder.Append("<releasedate>" + SecurityElement.Escape(item.PremiereDate.Value.ToShortDateString()) + "</releasedate>");
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

            foreach (var tagline in item.Taglines)
            {
                builder.Append("<tagline>" + SecurityElement.Escape(tagline) + "</tagline>");
            }

            foreach (var genre in item.Genres)
            {
                builder.Append("<genre>" + SecurityElement.Escape(genre) + "</genre>");
            }

            foreach (var studio in item.Studios)
            {
                builder.Append("<studio>" + SecurityElement.Escape(studio) + "</studio>");
            }

            foreach (var tag in item.Tags)
            {
                builder.Append("<tag>" + SecurityElement.Escape(tag) + "</tag>");
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
