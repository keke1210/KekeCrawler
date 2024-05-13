using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KekeCrawler;

namespace CrawlerTestApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            services.ConfigureCrawlerLib(options =>
            {
                options.Url = "https://google.com/";
                options.MaxPagesToCrawl = 1000;
                options.OutputFileName = "output.json";
                options.Timeout = TimeSpan.FromSeconds(5);
                //options.Cookie = new CookieConfig { Name = "cookie_name", Value = "cookie_value" };
            });

            var serviceProvider = services.BuildServiceProvider();

            var crawler = serviceProvider.GetRequiredService<ICrawler>();
            var config = serviceProvider.GetRequiredService<IOptions<Config>>().Value;

            await crawler.CrawlAsync(async (url, content) =>
            {
                try
                {
                    Console.WriteLine($"Crawled: {url}");
                    string fileName = UrlToFileName(url);

                    await File.WriteAllTextAsync(fileName, content);
                    Console.WriteLine($"Saved file: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save page {url}: {ex.Message}");
                }
            });
        }

        private static string UrlToFileName(string url)
        {
            var uri = new Uri(url);
            string scheme = uri.Scheme + "://";
            string fileName = url.Replace(scheme, "").TrimEnd('/').Replace("/", "_") + ".html";
            return fileName;
        }
    }
}
