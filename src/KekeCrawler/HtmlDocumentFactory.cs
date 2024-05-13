using HtmlAgilityPack;

namespace KekeCrawler
{
    public interface IHtmlDocumentFactory
    {
        HtmlDocument Create();
    }

    internal class HtmlDocumentFactory : IHtmlDocumentFactory
    {
        public HtmlDocument Create()
        {
            return new HtmlDocument();
        }
    }
}
