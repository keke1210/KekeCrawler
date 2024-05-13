using KekeCrawler.Test.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace KekeCrawler.Test
{
    public class CrawlerCrawlAsyncTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly TestLogger<Crawler> _logger;
        private readonly Mock<IOptions<Config>> _configMock;
        private readonly Config _config;
        private Crawler _crawler;

        public CrawlerCrawlAsyncTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _logger = new TestLogger<Crawler>();

            _config = new Config
            {
                Url = "https://example.com",
                Timeout = TimeSpan.FromSeconds(10),
                MaxPagesToCrawl = 3
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
        public async Task CrawlAsync_RespectsMaxPagesToCrawl()
        {
            // Arrange
            var url = "https://example.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body><a href='/page1'>Link</a><a href='/page2'>Link</a></body></html>");
            SetupCrawler(handlerStub);

            var visitedPages = new List<string>();
            Task OnVisitPageCallback(string pageUrl, string content)
            {
                visitedPages.Add(pageUrl);
                return Task.CompletedTask;
            }

            // Act
            await _crawler.CrawlAsync(OnVisitPageCallback, CancellationToken.None);

            // Assert
            Assert.Equal(_config.MaxPagesToCrawl, visitedPages.Count);
        }

        [Fact]
        public async Task CrawlAsync_InvokesCallbackForEachPage()
        {
            // Arrange
            var url = "https://example.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body><a href='/page1'>Link</a></body></html>");
            SetupCrawler(handlerStub);

            var visitedPages = new List<string>();
            Task OnVisitPageCallback(string pageUrl, string content)
            {
                visitedPages.Add(pageUrl);
                return Task.CompletedTask;
            }

            // Act
            await _crawler.CrawlAsync(OnVisitPageCallback, CancellationToken.None);

            // Assert
            Assert.Contains(url, visitedPages);
            Assert.Contains("https://example.com/page1", visitedPages);
        }

        [Fact]
        public async Task CrawlAsync_OnlyProcessesUrlsWithinSameDomain()
        {
            // Arrange
            var url = "https://example.com";
            var handlerStub = new HttpMessageHandlerStub(HttpStatusCode.OK, "<html><body><a href='/page1'>Link</a><a href='https://external.com/page'>External Link</a></body></html>");
            SetupCrawler(handlerStub);

            var visitedPages = new List<string>();
            Task OnVisitPageCallback(string pageUrl, string content)
            {
                visitedPages.Add(pageUrl);
                return Task.CompletedTask;
            }

            // Act
            await _crawler.CrawlAsync(OnVisitPageCallback, CancellationToken.None);

            // Assert
            Assert.Contains(url, visitedPages);
            Assert.Contains("https://example.com/page1", visitedPages);
            Assert.DoesNotContain("https://external.com/page", visitedPages);
        }
    }
}
