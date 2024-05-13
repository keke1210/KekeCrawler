namespace KekeCrawler
{
    public interface ICrawler
    {
        Task CrawlAsync(Func<string, string, Task> onVisitPageCallback, CancellationToken cancellationToken = default);
    }
}