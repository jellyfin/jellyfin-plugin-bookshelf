using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.NesBox;
using System.IO;

namespace MediaBrowser.Plugins.SnesBox
{
    public class NesBoxProvider : BaseNesBoxProvider
    {
        public NesBoxProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer)
            : base(logManager, configurationManager, jsonSerializer)
        {
        }

        protected override string GameSystem
        {
            get { return Model.Games.GameSystem.SuperNintendo; }
        }

        protected override Stream GetCatalogStream()
        {
            var path = GetType().Namespace + ".games.json";

            return GetType().Assembly.GetManifestResourceStream(path);
        }
    }
}
