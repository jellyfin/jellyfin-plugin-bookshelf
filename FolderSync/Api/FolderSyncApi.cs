using FolderSync.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using System;
using System.IO;
using System.Linq;

namespace FolderSync.Api
{
    [Route("/FolderSync/Folders/{Id}", "DELETE")]
    public class DeleteFolder : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/FolderSync/Folders/{Id}", "GET")]
    public class GetFolder : IReturn<SyncAccount>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/FolderSync/Folders", "POST")]
    public class AddFolder : SyncAccount, IReturnVoid
    {
    }

    [Authenticated]
    public class FolderSyncApi : IRestfulService
    {
        private readonly IJsonSerializer _json;

        public FolderSyncApi(IJsonSerializer json)
        {
            _json = json;
        }

        public void Delete(DeleteFolder request)
        {
            var config = Plugin.Instance.Configuration;

            config.SyncAccounts = config.SyncAccounts
                .Where(i => !string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Plugin.Instance.SaveConfiguration();
        }

        public void Post(AddFolder request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentNullException("Name");
            }
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                throw new ArgumentNullException("Path");
            }

            Directory.CreateDirectory(request.Path);

            var config = Plugin.Instance.Configuration;
            var list = config.SyncAccounts.ToList();

            if (string.IsNullOrWhiteSpace(request.Id))
            {
                request.Id = Guid.NewGuid().ToString("N");
                list.Add(_json.DeserializeFromString<SyncAccount>(_json.SerializeToString(request)));
            }
            else
            {
                var index = list.FindIndex(i => string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase));

                if (index == -1)
                {
                    throw new ResourceNotFoundException();
                }

                list[index] = _json.DeserializeFromString<SyncAccount>(_json.SerializeToString(request));
            }

            config.SyncAccounts = list.ToArray();

            Plugin.Instance.SaveConfiguration();
        }

        public object Get(GetFolder request)
        {
            return Plugin.Instance.Configuration.SyncAccounts.FirstOrDefault(i => string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
