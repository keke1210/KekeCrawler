using KekeCrawler.HtmlParsers;
using KekeCrawler.Test.Helpers;

namespace KekeCrawler.Test
{
    public class AngleSharpHtmlParserTests
    {
        private readonly IHtmlParser _htmlParser;

        public AngleSharpHtmlParserTests()
        {
            _htmlParser = new AngleSharpHtmlParser(new TestLogger<IHtmlParser>());
        }

        [Fact]
        public void SelectContent_ValidSelector_ReturnsSelectedContent()
        {
            // Arrange
            var htmlContent = "<html><body><div>Content</div></body></html>";
            var selector = "body";
            var expectedContent = "<div>Content</div>";

            // Act
            var result = _htmlParser.SelectContent(htmlContent, selector);

            // Assert
            Assert.Equal(expectedContent, result);
        }

        [Fact]
        public void SelectContent_InvalidSelector_ReturnsEmptyString()
        {
            // Arrange
            var htmlContent = "<html><body><div>Content</div></body></html>";
            var selector = "invalid";

            // Act
            var result = _htmlParser.SelectContent(htmlContent, selector);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void SelectContent_InvalidNullEmptyOrWhitespaceSelector_SelectorWillNotBeApplied()
        {
            // Arrange
            var htmlContent = "<html><head></head><body><div>Content</div></body></html>";

            // Act
            var result = _htmlParser.SelectContent(htmlContent, null!);

            // Assert
            Assert.Equal(htmlContent, result);
        }

        [Fact]
        public void SelectContent_EmptyContent_ReturnsEmptyString()
        {
            // Arrange
            var htmlContent = string.Empty;
            var selector = "body";

            // Act
            var result = _htmlParser.SelectContent(htmlContent, selector);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void SelectContent_NullContent_ReturnsEmptyString()
        {
            // Arrange
            string htmlContent = null!;
            var selector = "body";

            // Act
            var result = _htmlParser.SelectContent(htmlContent, selector);

            // Assert
            Assert.Equal(string.Empty, result);
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

            // Act
            var links = _htmlParser.ExtractLinks(baseUri, pageContent).ToList();

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
                    <a href='/valid'>Valid</a>
                </body>
            </html>";

            // Act
            var links = _htmlParser.ExtractLinks(baseUri, pageContent).ToList();

            // Assert
            Assert.Single(links);
            Assert.Contains("https://example.com/valid", links);
        }

        [Fact]
        public void ExtractLinks_ShouldHandleMalformedHtml()
        {
            // Arrange
            var baseUri = new Uri("https://example.com");
            var pageContent = "<html><body><a href='/page1'>Page 1</a></body"; // Malformed HTML

            // Act
            var links = _htmlParser.ExtractLinks(baseUri, pageContent);

            // Assert
            Assert.Single(links);
            Assert.Contains("https://example.com/page1", links);
        }
    }
}
