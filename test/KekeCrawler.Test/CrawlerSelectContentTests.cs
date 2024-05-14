using HtmlAgilityPack;
using KekeCrawler.Test.Helpers;
using Microsoft.Extensions.Options;
using Moq;

namespace KekeCrawler.Test
{
    public class CrawlerSelectContentTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly TestLogger<Crawler> _logger;
        private readonly Mock<IOptions<Config>> _configMock;
        private readonly Config _config;
        private readonly Mock<IHtmlDocumentFactory> _htmlDocumentFactoryMock;
        private Crawler _crawler;

        public CrawlerSelectContentTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _logger = new TestLogger<Crawler>();

            _config = new Config
            {
                Url = "http://example.com",
                MaxPagesToCrawl = 10,
                OnVisitPageTimeout = TimeSpan.FromSeconds(30),
                PageSelector = "//body"
            };

            _configMock = new Mock<IOptions<Config>>();
            _configMock.Setup(x => x.Value).Returns(_config);

            _htmlDocumentFactoryMock = new Mock<IHtmlDocumentFactory>();

            _crawler = new Crawler(
               _httpClientFactoryMock.Object,
               _configMock.Object,
               _logger,
               _htmlDocumentFactoryMock.Object
           );
        }

        [Fact]
        public void SelectContent_ValidSelector_ReturnsSelectedContent()
        {
            // Arrange
            var htmlContent = "<html><body><div>Content</div></body></html>";
            var expectedContent = "<div>Content</div>";
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);
            _htmlDocumentFactoryMock.Setup(factory => factory.Create()).Returns(document);

            // Act
            var result = _crawler.SelectContent(htmlContent, "//body");

            // Assert
            Assert.Equal(expectedContent, result);
        }

        [Fact]
        public void SelectContent_InvalidSelector_ReturnsEmptyString()
        {
            // Arrange
            var htmlContent = "<html><body><div>Content</div></body></html>";
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);
            _htmlDocumentFactoryMock.Setup(factory => factory.Create()).Returns(document);

            // Act
            var result = _crawler.SelectContent(htmlContent, "//invalid");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void SelectContent_EmptyContent_ReturnsEmptyString()
        {
            // Arrange
            var htmlContent = string.Empty;
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);
            _htmlDocumentFactoryMock.Setup(factory => factory.Create()).Returns(document);

            // Act
            var result = _crawler.SelectContent(htmlContent, "//body");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void SelectContent_NullContent_ReturnsEmptyString()
        {
            // Arrange
            var document = new HtmlDocument();
            _htmlDocumentFactoryMock.Setup(factory => factory.Create()).Returns(document);

            // Act
            var result = _crawler.SelectContent(null!, "//body");

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
