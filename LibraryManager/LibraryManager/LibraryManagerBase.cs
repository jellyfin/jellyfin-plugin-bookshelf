using MediaBrowser.Controller.Library;
using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using CommonIO;
using MediaBrowser.Controller.Plugins;

namespace LibraryManager
{
    class LibraryManagerBase : IServerEntryPoint
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public LibraryManagerBase(ILogger logger, ILibraryManager libraryManager)
        {
            //System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            logger.Info("[LibraryManager] LibraryManagerBase()");

            _logger = logger;
            _libraryManager = libraryManager;
        }

        public void Dispose()
        {
            _logger.Info("[LibraryManager] Dispose()");
        }

        public void Run()
        {
            _logger.Info("[LibraryManager] Run()");


           IEnumerable<BaseItem> result = _libraryManager.GetItemList(new InternalItemsQuery());
            long count = 0; 

            foreach (BaseItem currBaseItem in result)
            {
                count++;
              

                //    _logger.Info("[LibraryManager] " + currBaseItem.MediaType);
            }
            _logger.Info("[LibraryManager] " + count);
        }
    }
}

