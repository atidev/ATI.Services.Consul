using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Consul;

[PublicAPI]
public static class ConsulMetricsHttpClientWrapperServiceCollectionExtensions
{
    [Obsolete("Use HttpClientFactory and ServiceCollection.AddConsulHttpClients instead")]
    public static IServiceCollection AddConsulMetricsHttpClientWrappers(this IServiceCollection services)
    {
        // Add IHttpClientFactory for ConsulMetricsHttpClientWrapper<>
        services.AddHttpClient();
        services.AddSingleton(typeof(ConsulMetricsHttpClientWrapper<>));
        services.AddHttpContextAccessor();
        return services;
    }
}