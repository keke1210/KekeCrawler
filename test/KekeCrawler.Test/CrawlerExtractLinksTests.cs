using Castle.Core.Logging;
using HtmlAgilityPack;
using KekeCrawler.Test.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KekeCrawler.Test
{
    public class CrawlerExtractLinksTests
    {
        private readonly TestLogger<Crawler> _logger;
        private readonly Mock<IHtmlDocumentFactory> _htmlDocumentFactoryMock;
        private readonly Crawler _crawler;

        public CrawlerExtractLinksTests()
        {
            _logger = new TestLogger<Crawler>();
            _htmlDocumentFactoryMock = new Mock<IHtmlDocumentFactory>();

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var configMock = new Mock<IOptions<Config>>();
            configMock.Setup(x => x.Value).Returns(new Config
            {
                Url = "https://example.com",
                OnVisitPageTimeout = TimeSpan.FromSeconds(10),
                MaxPagesToCrawl = 10
            });

            _crawler = new Crawler(httpClientFactoryMock.Object, configMock.Object, _logger, _htmlDocumentFactoryMock.Object);
        }

        [Fact]
        public void ExtractLinks_ShouldReturnLinksFromSameDomain()
        {
            // Arrange
            var baseUri = new Uri("https://example.com");
            var pageContent = @"
                <html>
                    <body>
                        <a href='/page1'>Page 1</a>
                        <a href='https://example.com/page2'>Page 2</a>
                        <a href='https://example.com/page3/#section'>Page 3</a>
                        <a href='https://otherdomain.com/page4'>Page 4</a>
                    </body>
                </html>";

            var documentMock = new HtmlDocument();
            documentMock.LoadHtml(pageContent);
            _htmlDocumentFactoryMock.Setup(factory => factory.Create()).Returns(documentMock);

            // Act
            var links = _crawler.ExtractLinks(baseUri, pageContent);

            // Assert
            Assert.Contains("https://example.com/page1", links);
            Assert.Contains("https://example.com/page2", links);
            Assert.DoesNotContain("https://example.com/page3#section", links);
            Assert.DoesNotContain("https://otherdomain.com/page4", links);
        }

        [Fact]
        public void ExtractLinks_ShouldHandleEmptyOrInvalidHref()
        {
            // Arrange
            var baseUri = new Uri("https://example.com");
            var pageContent = @"
                <html>
                    <body>
                        <a href=''>Empty Href</a>
                        <a>Missing Href</a>
                        <a href='#'>Fragment</a>
                    </body>
                </html>";

            var documentMock = new HtmlDocument();
            documentMock.LoadHtml(pageContent);
            _htmlDocumentFactoryMock.Setup(factory => factory.Create()).Returns(documentMock);

            // Act
            var links = _crawler.ExtractLinks(baseUri, pageContent);

            // Assert
            Assert.Single(links);
            Assert.Equal(baseUri.ToString(), links.First());
        }

        [Fact]
        public void ExtractLinks_ShouldReturnEmptyOnException()
        {
            // Arrange
            var baseUri = new Uri("https://example.com");
            var pageContent = @"
                <html>
                    <body>
                        <a href='/page1'>Page 1</a>
                    </body>
                </html>";

            _htmlDocumentFactoryMock.Setup(factory => factory.Create()).Throws(new Exception("Test exception"));

            // Act
            var links = _crawler.ExtractLinks(baseUri, pageContent);

            // Assert
            Assert.Empty(links);
            var logEntry = Assert.Single(_logger.LogEntries);
            Assert.Equal(LogLevel.Error, logEntry.LogLevel);
            Assert.Equal("Test exception", logEntry.Message);
        }
    }
}