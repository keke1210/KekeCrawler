using KekeCrawler.Test.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace KekeCrawler.Test
{
    public class CrawlerFetchPageContentTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly TestLogger<Crawler> _logger;
        private readonly Mock<IOptions<Config>> _configMock;
        private readonly Config _config;
        private Crawler _crawler;

        public CrawlerFetchPageContentTests()
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
        public async Task FetchPageContentAsync_ReturnsContent_WhenRequestIsSuccessful()
        {
            // Arrange
            var url = "https://example.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body>Test Content</body></html>");
            SetupCrawler(handlerStub);

            // Act
            var content = await _crawler.FetchPageContentAsync(url);

            // Assert
            Assert.NotEmpty(content);
            Assert.Contains("Test Content", content);
            handlerStub.VerifyUri(new Uri(url));
        }

        [Fact]
        public async Task FetchPageContentAsync_ReturnsEmptyString_WhenRequestFails()
        {
            // Arrange
            var url = "https://fail.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.BadRequest, string.Empty);
            SetupCrawler(handlerStub);

            // Act
            var content = await _crawler.FetchPageContentAsync(url);

            // Assert
            Assert.Empty(content);
            handlerStub.VerifyUri(new Uri(url));
        }

        [Fact]
        public async Task FetchPageContentAsync_FixesUrl_WhenUrlIsInvalid()
        {
            // Arrange
            var url = "https://example.com/invalid";
            var fixedUrl = "https://example.com/invalid/";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body>Fixed URL Content</body></html>");
            SetupCrawler(handlerStub);

            // Act
            var content = await _crawler.FetchPageContentAsync(url);

            // Assert
            Assert.NotEmpty(content);
            Assert.Contains("Fixed URL Content", content);
            handlerStub.VerifyUri(new Uri(fixedUrl));
        }
    }
}
