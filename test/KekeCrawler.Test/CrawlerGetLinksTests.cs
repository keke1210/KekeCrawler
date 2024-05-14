using KekeCrawler.Test.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace KekeCrawler.Test
{
    public class CrawlerGetLinksTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly TestLogger<Crawler> _logger;
        private readonly Mock<IOptions<Config>> _configMock;
        private readonly Config _config;
        private Crawler _crawler;

        public CrawlerGetLinksTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _logger = new TestLogger<Crawler>();

            _config = new Config
            {
                Url = "https://example.com",
                Timeout = TimeSpan.FromSeconds(10),
                MaxPagesToCrawl = 10
            };

            _configMock = new Mock<IOptions<Config>>();
            _configMock.Setup(x => x.Value).Returns(_config);
        }

        private void SetupCrawler(HttpMessageHandlerStub handlerStub)
        {
            var httpClient = new HttpClient(handlerStub);
            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _crawler = new Crawler(_httpClientFactoryMock.Object, _configMock.Object, _logger, new HtmlDocumentFactory());
        }

        [Fact]
        public async Task GetLinksAsync_ReturnsEmpty_WhenContentIsEmpty()
        {
            // Arrange
            var url = "https://example.com/empty";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, string.Empty);
            SetupCrawler(handlerStub);

            Task OnVisitPageCallback(string pageUrl, string content) => Task.CompletedTask;

            // Act
            var links = await _crawler.GetLinksAsync(OnVisitPageCallback, url, CancellationToken.None);

            // Assert
            Assert.Empty(links);
        }

        [Fact]
        public async Task GetLinksAsync_InvokesCallback_WhenContentIsNotEmpty()
        {
            // Arrange
            var url = "https://example.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body>Test Content<a href='/page1'>Link</a></body></html>");
            SetupCrawler(handlerStub);

            bool callbackInvoked = false;
            Task OnVisitPageCallback(string pageUrl, string content)
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }

            // Act
            var links = await _crawler.GetLinksAsync(OnVisitPageCallback, url, CancellationToken.None);

            // Assert
            Assert.True(callbackInvoked);
            Assert.Single(links);
            Assert.Contains("https://example.com/page1", links);
        }

        [Fact]
        public async Task GetLinksAsync_ThrowsException_WhenCallbackFails()
        {
            // Arrange
            var url = "https://example.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body>Test Content</body></html>");
            SetupCrawler(handlerStub);

            Task OnVisitPageCallback(string pageUrl, string content)
            {
                throw new Exception("Callback failed");
            }

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _crawler.GetLinksAsync(OnVisitPageCallback, url, CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task GetLinksAsync_ReturnsLinks_WhenContentIsFetchedSuccessfully()
        {
            // Arrange
            var url = "https://example.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body>Test Content<a href='/page1'>Link</a></body></html>");
            SetupCrawler(handlerStub);

            Task OnVisitPageCallback(string pageUrl, string content) => Task.CompletedTask;

            // Act
            var links = await _crawler.GetLinksAsync(OnVisitPageCallback, url, CancellationToken.None);

            // Assert
            Assert.Single(links);
            Assert.Contains("https://example.com/page1", links);
        }
    }
}
