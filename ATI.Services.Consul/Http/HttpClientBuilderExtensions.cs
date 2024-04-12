using ATI.Services.Common.Options;
using ATI.Services.Consul.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace PassportVerification.test;

[PublicAPI]
public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder WithConsul<TServiceOptions>(this IHttpClientBuilder httpClientBuilder)
        where TServiceOptions : BaseServiceOptions
    {
        httpClientBuilder.Services.AddSingleton<HttpConsulHandler<TServiceOptions>>();

        return httpClientBuilder
            .AddHttpMessageHandler<HttpConsulHandler<TServiceOptions>>();
    }

}