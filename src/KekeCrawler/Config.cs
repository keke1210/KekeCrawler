namespace KekeCrawler
{
    public sealed class Config
    {
        public string Url { get; set; }
        public CookieConfig? Cookie { get; set; }
        public int? MaxPagesToCrawl { get; set; } = 1000;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);
    }

    public sealed class CookieConfig
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}