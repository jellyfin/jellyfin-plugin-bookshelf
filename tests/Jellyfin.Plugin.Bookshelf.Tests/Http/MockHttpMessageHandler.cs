namespace Jellyfin.Plugin.Bookshelf.Tests.Http
{
    /// <summary>
    /// HttpMessageHandler that returns a mocked response.
    /// </summary>
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<(Func<Uri, bool> RequestMatcher, MockHttpResponse Response)> _messageHandlers;

        public MockHttpMessageHandler(List<(Func<Uri, bool> requestMatcher, MockHttpResponse response)> messageHandlers)
        {
            _messageHandlers = messageHandlers;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(request.RequestUri));
            }

            var response = _messageHandlers.FirstOrDefault(x => x.RequestMatcher(request.RequestUri)).Response;

            if (response == null)
            {
                throw new InvalidOperationException($"No response found for request {request.RequestUri}");
            }

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = response.StatusCode,
                Content = new StringContent(response.Response)
            });
        }
    }

}
