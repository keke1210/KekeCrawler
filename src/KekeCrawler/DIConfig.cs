﻿using Microsoft.Extensions.DependencyInjection;
using Polly.Extensions.Http;
using Polly;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Polly.Contrib.WaitAndRetry;
using KekeCrawler.HtmlParsers;

namespace KekeCrawler
{
    public static class DIConfig
    {
        public static IServiceCollection ConfigKekeCrawler(this ServiceCollection services, Action<Config> configureOptions)
        {
            services.Configure(configureOptions);

            services.AddHttpClient(Consts.HttpClientFactoryClientName, (serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<Config>>().Value;

                var cookie = config.Cookie;
                if (cookie is not null)
                {
                    client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Name}={cookie.Value};");
                }

                client.Timeout = config.HttpRequestTimeout;
            })
            .AddPolicyHandler(GetRetryPolicy());

            services.AddSingleton<IHtmlParser, AngleSharpHtmlParser>();
            services.AddScoped<ICrawler, Crawler>();
            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 3));
        }
    }
}
