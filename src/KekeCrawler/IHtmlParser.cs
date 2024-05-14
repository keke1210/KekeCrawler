namespace KekeCrawler
{
    public interface IHtmlParser
    {
        string SelectContent(string pageContent, string selector);
        IEnumerable<string> ExtractLinks(Uri baseUri, string pageContent);
    }
}
