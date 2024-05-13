using System.Net;

namespace KekeCrawler.Test.Helpers
{
    public sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;
        private readonly string _mediaType;
        private Uri _requestedUri;

        public HttpMessageHandlerStub(HttpStatusCode statusCode = HttpStatusCode.OK, string content = "", string mediaType = "text/html")
        {
            _statusCode = statusCode;
            _content = content;
            _mediaType = mediaType;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requestedUri = request.RequestUri;

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_mediaType);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            return await Task.FromResult(response);
        }

        public void VerifyUri(Uri expectedUri)
        {
            Assert.Equal(expectedUri, _requestedUri);
        }
    }
}
