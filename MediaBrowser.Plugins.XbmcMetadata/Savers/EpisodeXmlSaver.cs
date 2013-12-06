using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class EpisodeXmlSaver : IMetadataSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;
        private readonly IItemRepository _itemRepo;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public EpisodeXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IItemRepository itemRepo)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
            _itemRepo = itemRepo;
        }

        public string GetSavePath(BaseItem item)
        {
            return Path.ChangeExtension(item.Path, ".nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<episodedetails>");

            XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager, _userManager, _userDataRepo);

            if (item.IndexNumber.HasValue)
            {
                builder.Append("<episode>" + item.IndexNumber.Value.ToString(_usCulture) + "</episode>");
            }

            if (item.ParentIndexNumber.HasValue)
            {
                builder.Append("<season>" + item.ParentIndexNumber.Value.ToString(_usCulture) + "</season>");
            }

            if (item.PremiereDate.HasValue)
            {
                var formatString = Plugin.Instance.Configuration.ReleaseDateFormat;

                builder.Append("<aired>" + SecurityElement.Escape(item.PremiereDate.Value.ToString(formatString)) + "</aired>");
            }

            var episode = (Episode)item;

            if (episode.AirsAfterSeasonNumber.HasValue)
            {
                builder.Append("<airsafter_season>" + SecurityElement.Escape(episode.AirsAfterSeasonNumber.Value.ToString(_usCulture)) + "</airsafter_season>");
            }
            if (episode.AirsBeforeEpisodeNumber.HasValue)
            {
                builder.Append("<airsbefore_episode>" + SecurityElement.Escape(episode.AirsBeforeEpisodeNumber.Value.ToString(_usCulture)) + "</airsbefore_episode>");
            }
            if (episode.AirsBeforeSeasonNumber.HasValue)
            {
                builder.Append("<airsbefore_season>" + SecurityElement.Escape(episode.AirsBeforeSeasonNumber.Value.ToString(_usCulture)) + "</airsbefore_season>");
            }

            XmlSaverHelpers.AddMediaInfo((Episode)item, _itemRepo, builder);

            builder.Append("</episodedetails>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "aired",
                    "season",
                    "episode",
                    "airsafter_season",
                    "airsbefore_episode",
                    "airsbefore_season"
                });
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is Episode;
            }

            return false;
        }
    }
}
