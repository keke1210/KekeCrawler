using KekeCrawler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;

var services = new ServiceCollection();
services.AddLogging(x => x.AddConsole());


int maxPagesToCrawl = 1000;
services.ConfigKekeCrawler(options =>
{
    options.Url = "https://google.com/";
    options.MaxPagesToCrawl = maxPagesToCrawl;
    options.HttpRequestTimeout = TimeSpan.FromSeconds(2);
    options.OnVisitPageTimeout = TimeSpan.FromSeconds(3);
    options.PageSelector = "//body";
    //options.Cookie = new CookieConfig { Name = "cookie_name", Value = "cookie_value" };
});

var serviceProvider = services.BuildServiceProvider();
var crawler = serviceProvider.GetRequiredService<ICrawler>();
var config = serviceProvider.GetRequiredService<IOptions<Config>>().Value;

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();


var cancellationTokenSource = new CancellationTokenSource();

// TODO: maybe add a timer when the crawler stops
// Calls Cancel after 3 seconds
_ = new Timer(
    state => 
        ((CancellationTokenSource)state!).Cancel(), 
        cancellationTokenSource, 
        (int)TimeSpan.FromSeconds(10).TotalMilliseconds,
        Timeout.Infinite);

var results = new List<PageData>();
try
{
    await crawler.CrawlAsync((url, content) =>
    {
        var data = new PageData { Url = url, Content = content };
        results.Add(data);
        logger.LogInformation("{resultsCount}/{maxPagesToCrawl}: Crawled: {url}", results.Count, maxPagesToCrawl, url);
        return Task.CompletedTask;
    }, cancellationTokenSource.Token);
}
catch (Exception ex) when (ex.Message.Equals("The operation was canceled."))
{
    Console.WriteLine(ex.Message);
}

await File.WriteAllTextAsync("knowledge.json", JsonSerializer.Serialize(results, new JsonSerializerOptions
{
    WriteIndented = true
}));

logger.LogInformation("knowledge.json file was saved.");

class PageData
{
    public string Url { get; set; }
    public string Content { get; set; }
}
