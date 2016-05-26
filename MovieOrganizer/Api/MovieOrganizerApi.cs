using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MovieOrganizer.Service;
using ServiceStack;
using System.Threading;

namespace MovieOrganizer.Api
{
    [Route("/Library/FileOrganizations/{Id}/Movie/OrganizeExt", "POST", Summary = "Performs organization of a movie file")]
    public class OrganizeMovie
    {
        [ApiMember(Name = "Id", Description = "Result Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "MovieName", Description = "Movie Name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MovieName { get; set; }

        [ApiMember(Name = "MovieYear", Description = "Movie Year", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MovieYear { get; set; }

        [ApiMember(Name = "TargetFolder", Description = "Target Folder", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string TargetFolder { get; set; }
    }

    [Authenticated]
    public class MovieOrganizerApi : IRestfulService
    {
        private readonly ILogger _logger;
        private readonly MovieOrganizerService _service;
        private readonly ILibraryManager _libraryManager;

        public MovieOrganizerApi(ILogManager logManager, MovieOrganizerService service, ILibraryManager libraryManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _service = service;
            _libraryManager = libraryManager;
        }

        public void Post(OrganizeMovie request)
        {
            var task = _service.PerformMovieOrganization(new MovieFileOrganizationRequest
            {
                ResultId = request.Id,
                Name = request.MovieName,
                Year = request.MovieYear,
                TargetFolder = request.TargetFolder
            });

            // Wait 2s for exceptions that may occur and would be automatically forwarded to the client for immediate error display
            task.Wait(2000);
        }
    }
}
