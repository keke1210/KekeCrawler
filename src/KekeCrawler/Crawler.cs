using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo($"{nameof(KekeCrawler)}.Tests")]

namespace KekeCrawler
{
    internal sealed class Crawler : ICrawler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Config _config;
        private readonly ILogger<Crawler> _logger;
        private readonly AsyncPolicy _timeoutPolicy;
        private readonly IHtmlParser _htmlParser;
        public Crawler(IHttpClientFactory httpClientFactory, IOptions<Config> config, ILogger<Crawler> logger, IHtmlParser htmlParser)
        {
            _httpClientFactory = httpClientFactory;
            _config = config.Value;
            _logger = logger;
            _timeoutPolicy = Policy.TimeoutAsync(_config.OnVisitPageTimeout, TimeoutStrategy.Pessimistic);
            _htmlParser = htmlParser;
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

                IEnumerable<string> links = Enumerable.Empty<string>();
                try
                {
                    links = await _timeoutPolicy.ExecuteAsync(
                        ct => FetchAndExtractLinksAsync(currentUrl, onVisitPageCallback, ct),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (TimeoutRejectedException ex)
                {
                    _logger.LogError("Timeout: Processing of {Url} timed out. {ExceptionMessage}", currentUrl, ex.Message);
                }

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

        internal async Task<IEnumerable<string>> FetchAndExtractLinksAsync(string currentUrl, Func<string, string, Task> onVisitPageCallback, CancellationToken cancellationToken)
        {
            string pageContent = await FetchPageContentAsync(currentUrl, cancellationToken).ConfigureAwait(false);
            string selectedContent = _htmlParser.SelectContent(pageContent, _config.PageSelector);

            try
            {
                await onVisitPageCallback(currentUrl, selectedContent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError("Callback failed for {Url}: {ExceptionMessage}", currentUrl, ex.Message);
                throw;
            }

            var links = _htmlParser.ExtractLinks(new Uri(currentUrl), pageContent);
            return links;
        }

        internal async Task<string> FetchPageContentAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure the URL has a trailing slash if it's not a file
                Uri uri = ToFetchableUrl(url);

                HttpClient httpClient = _httpClientFactory.CreateClient("crawler");
                var response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching content from {Url}: {ExceptionMessage}", url, ex.Message);
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
    }
}