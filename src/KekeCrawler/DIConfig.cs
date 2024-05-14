using Microsoft.Extensions.DependencyInjection;
using Polly.Extensions.Http;
using Polly;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Polly.Contrib.WaitAndRetry;

namespace KekeCrawler
{
    public static class DIConfig
    {
        public static IServiceCollection ConfigKekeCrawler(this ServiceCollection services, Action<Config> configureOptions)
        {
            services.Configure(configureOptions);

            services.AddHttpClient("crawler", (serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<Config>>().Value;

                var cookie = config.Cookie;
                if (cookie is not null)
                {
                    client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Name}={cookie.Value};");
                }

                client.Timeout = config.Timeout;
            })
            .AddPolicyHandler(GetRetryPolicy());

            services.AddScoped<IHtmlDocumentFactory, HtmlDocumentFactory>();
            services.AddScoped<ICrawler, Crawler>();

            services.AddLogging();

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                // On production I would use a Jitter
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 3));
        }
    }
}
