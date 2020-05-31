using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DI
    {
        public static void AddGooglePhotos(this IServiceCollection services)
            => services.AddGooglePhotos(_ => { });

        static string sectionKey = $"{nameof(CasCap)}:{nameof(GooglePhotosConfig)}";

        public static void AddGooglePhotos(this IServiceCollection services, Action<GooglePhotosConfig> configure)
        {
            services.AddSingleton<IConfigureOptions<GooglePhotosConfig>>(s =>
            {
                var configuration = s.GetService<IConfiguration?>();
                return new ConfigureOptions<GooglePhotosConfig>(options => configuration?.Bind(sectionKey, options));
            });
            services.AddHttpClient<GooglePhotosService>((s, client) =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                var config = configuration.GetSection(sectionKey).Get<GooglePhotosConfig>();
                config = config ?? new GooglePhotosConfig();//we use default BaseAddress if no config object injected in
                client.BaseAddress = new Uri(config.BaseAddress);
                client.DefaultRequestHeaders.Add("User-Agent", $"{nameof(CasCap)}.{AppDomain.CurrentDomain.FriendlyName}.{Environment.MachineName}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.Timeout = Timeout.InfiniteTimeSpan;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddTransientHttpErrorPolicy(policyBuilder =>
            {
                return policyBuilder.RetryAsync(retryCount: 3);
            })
            //https://github.com/aspnet/AspNetCore/issues/6804
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        }
    }
}