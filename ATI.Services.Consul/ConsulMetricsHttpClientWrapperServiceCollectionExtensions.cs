using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Consul;

[PublicAPI]
public static class ConsulMetricsHttpClientWrapperServiceCollectionExtensions
{
    public static IServiceCollection AddConsulMetricsHttpClientWrappers(this IServiceCollection services)
    {
        // Add IHttpClientFactory for ConsulMetricsHttpClientWrapper<>
        services.AddHttpClient();
        services.AddSingleton(typeof(ConsulMetricsHttpClientWrapper<>));
        return services;
    }
}