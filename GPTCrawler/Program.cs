using KekeCrawler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Xml;

var services = new ServiceCollection();

services.ConfigKekeCrawler(options =>
{
    options.Url = "https://google.com/";
    options.MaxPagesToCrawl = 1000;
    options.Timeout = TimeSpan.FromSeconds(5);
    //options.Cookie = new CookieConfig { Name = "cookie_name", Value = "cookie_value" };
});

var serviceProvider = services.BuildServiceProvider();

var crawler = serviceProvider.GetRequiredService<ICrawler>();
var config = serviceProvider.GetRequiredService<IOptions<Config>>().Value;

var cancellationTokenSource = new CancellationTokenSource();

// TODO: maybe add a timer when the crawler stops
// Calls Cancel after 3 seconds
var timer = new Timer(state => ((CancellationTokenSource)state!).Cancel(), cancellationTokenSource, 3000, Timeout.Infinite);

var results = new List<PageData>();

try
{
    await crawler.CrawlAsync((url, content) =>
    {
        var data = new PageData { Url = url, Content = content };
        results.Add(data);
        Console.WriteLine($"Crawled: {url}");
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

class PageData
{
    public string Url { get; set; }
    public string Content { get; set; }
}
