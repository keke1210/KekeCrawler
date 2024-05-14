using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;

namespace KekeCrawler.HtmlParsers
{
    internal sealed class AngleSharpHtmlParser : IHtmlParser
    {
        private readonly ILogger<IHtmlParser> _logger;

        public AngleSharpHtmlParser(ILogger<IHtmlParser> logger)
        {
            _logger = logger;
        }

        public string SelectContent(string pageContent, string selector)
        {
            try
            {
                var parser = new HtmlParser();
                var document = parser.ParseDocument(pageContent);
                var node = document?.QuerySelector(selector);
                return node?.InnerHtml ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return string.Empty;
            }
        }

        public IEnumerable<string> ExtractLinks(Uri baseUri, string pageContent)
        {
            try
            {
                var parser = new HtmlParser();
                var document = parser.ParseDocument(pageContent);

                var links = new HashSet<string>();
                foreach (var linkNode in document.QuerySelectorAll("a[href]"))
                {
                    var href = linkNode.GetAttribute("href");
                    if (!string.IsNullOrWhiteSpace(href) && Uri.TryCreate(baseUri, href, out var result))
                    {
                        if (result.Host == baseUri.Host && !result.Fragment.StartsWith('#'))
                        {
                            links.Add(result.AbsoluteUri);
                        }
                    }
                }

                return links;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Enumerable.Empty<string>();
            }
        }
    }
}
