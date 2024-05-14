namespace KekeCrawler.Test
{
    public class CrawlerToFetchableUrlTests
    {
        [Theory]
        [InlineData("https://example.com/path", "https://example.com/path/")]
        [InlineData("https://example.com/path/", "https://example.com/path/")]
        [InlineData("https://example.com/path/file.html", "https://example.com/path/file.html")]
        [InlineData("https://example.com/path/file.html/", "https://example.com/path/file.html/")]
        [InlineData("https://example.com", "https://example.com/")]
        [InlineData("https://example.com/", "https://example.com/")]
        [InlineData("https://example.com/path.with.dots/file.html", "https://example.com/path.with.dots/file.html")]
        [InlineData("https://example.com/path.with.dots/file.html/", "https://example.com/path.with.dots/file.html/")]
        public void ToFetchableUrl_ShouldAddTrailingSlash_WhenNotPresentAndNotFile(string inputUrl, string expectedUrl)
        {
            // Act
            var result = Crawler.ToFetchableUrl(inputUrl);

            // Assert
            Assert.Equal(expectedUrl, result.ToString());
        }

        [Theory]
        [InlineData("https://example.com/path.with.dots", "https://example.com/path.with.dots")]
        [InlineData("https://example.com/path.with.dots/", "https://example.com/path.with.dots/")]
        public void ToFetchableUrl_ShouldNotAddTrailingSlash_WhenUrlContainsDotsAndNoFileExtension(string inputUrl, string expectedUrl)
        {
            // Act
            var result = Crawler.ToFetchableUrl(inputUrl);

            // Assert
            Assert.Equal(expectedUrl, result.ToString());
        }

        [Theory]
        [InlineData("invalidurl")]
        [InlineData("ht!tp://invalidurl")]
        public void ToFetchableUrl_ShouldThrowUriFormatException_WhenInvalidUrl(string inputUrl)
        {
            // Act & Assert
            Assert.Throws<UriFormatException>(() => Crawler.ToFetchableUrl(inputUrl));
        }
    }
}
