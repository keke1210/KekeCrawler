namespace KekeCrawler
{
    public sealed class Config
    {
        public required string Url { get; set; }
        public CookieConfig? Cookie { get; set; }
        public int? MaxPagesToCrawl { get; set; } = 1000;
        public TimeSpan HttpRequestTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan OnVisitPageTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Obtain the inner HTML of elements that match the specified selector using JS querySelector syntax.
        /// </summary>
        public string? PageSelector { get; set; }
    }

    public sealed class CookieConfig
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}