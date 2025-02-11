using System;
using System.Threading;
using ATI.Services.Common.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Consul.Http;

[PublicAPI]
public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder WithAddressHandler<TServiceOptions>(this IHttpClientBuilder builder,
        Func<IHttpClientBuilder, IHttpClientBuilder> configure) => configure(builder);

    public static IHttpClientBuilder WithK8S(IHttpClientBuilder httpClientBuilder) => httpClientBuilder;

    public static IHttpClientBuilder WithConsul<TServiceOptions>(IHttpClientBuilder httpClientBuilder)
        where TServiceOptions : BaseServiceOptions
    {
        httpClientBuilder.Services.AddSingleton<HttpConsulHandler<TServiceOptions>>();

        return httpClientBuilder
            .AddHttpMessageHandler<HttpConsulHandler<TServiceOptions>>()
            // By default, handlers are alive for 2 minutes
            // If we don't set InfiniteTimeSpan, every 2 minutes HttpConsulHandler will be recreated
            // And it will lead to new ConsulServiceAddress instances, which constructor is pretty expensive and will stop http requests for some time
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }

}