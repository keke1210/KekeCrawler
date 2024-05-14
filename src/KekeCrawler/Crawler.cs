using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo($"{nameof(KekeCrawler)}.Tests")]

namespace KekeCrawler
{
    public sealed class Crawler : ICrawler
    {
        private readonly HttpClient _httpClient;
        private readonly Config _config;
        private readonly ILogger<Crawler> _logger;
        private readonly AsyncPolicy _timeoutPolicy;
        private readonly IHtmlDocumentFactory _htmlDocumentFactory;

        public Crawler(IHttpClientFactory httpClientFactory, IOptions<Config> config, ILogger<Crawler> logger, IHtmlDocumentFactory htmlDocumentFactory)
        {
            _httpClient = httpClientFactory.CreateClient("crawler");
            _config = config.Value;
            _logger = logger;
            _timeoutPolicy = Policy.TimeoutAsync(_config.Timeout);
            _htmlDocumentFactory = htmlDocumentFactory;
        }

        // BFS algorithm to crawl recursively
        public async Task CrawlAsync(Func<string, string, Task> onVisitPageCallback, CancellationToken cancellationToken = default)
        {
            var visitedUrls = new HashSet<string>();
            var urlQueue = new Queue<string>();

            urlQueue.Enqueue(_config.Url);
            visitedUrls.Add(_config.Url);

            while (urlQueue.Any() && visitedUrls.Count <= _config.MaxPagesToCrawl)
            {
                var currentUrl = urlQueue.Dequeue();

                var links = Enumerable.Empty<string>();
                await _timeoutPolicy.ExecuteAsync(async (ct) =>
                {
                    links = await GetLinksAsync(onVisitPageCallback, currentUrl, ct).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                foreach (var link in links)
                {
                    if (!visitedUrls.Contains(link))
                    {
                        visitedUrls.Add(link);
                        urlQueue.Enqueue(link);
                    }
                }
            }
        }

        internal async ValueTask<IEnumerable<string>> GetLinksAsync(Func<string, string, Task> onVisitPageCallback, string currentUrl, CancellationToken cancellationToken)
        {
            var pageContent = await FetchPageContentAsync(currentUrl, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent))
            {
                return Enumerable.Empty<string>();
            }

            try
            {
                await onVisitPageCallback(currentUrl, pageContent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError("Action failed: {0}", ex.Message);
                throw;
            }

            var links = ExtractLinks(new Uri(currentUrl), pageContent);
            return links;
        }

        internal async Task<string> FetchPageContentAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure the URL has a trailing slash if it's not a file
                Uri uri = ToFetchableUrl(url);

                var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching {url}: {ex.Message}");
                return string.Empty;
            }
        }

        internal static Uri ToFetchableUrl(string url)
        {
            var uri = new Uri(url);
            if (!uri.AbsolutePath.Contains('.') && !uri.AbsolutePath.EndsWith('/'))
            {
                uri = new Uri($"{uri}/");
            }

            return uri;
        }

        internal IEnumerable<string> ExtractLinks(Uri baseUri, string pageContent)
        {
            try
            {
                var document = _htmlDocumentFactory.Create();
                document.LoadHtml(pageContent);

                var links = new HashSet<string>();

                foreach (var linkNode in document?.DocumentNode?.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>())
                {
                    var href = linkNode.GetAttributeValue("href", string.Empty);
                    if (Uri.TryCreate(baseUri, href, out var result))
                    {
                        // take links that belong to the same domain 
                        // skip links that make you navigate within the same page (the ones that start with '#')
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