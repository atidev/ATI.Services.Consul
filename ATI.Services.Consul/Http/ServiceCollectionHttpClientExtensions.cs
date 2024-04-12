using System;
using System.Linq;
using System.Net.Http;
using ATI.Services.Common.Http.Extensions;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Options;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using PassportVerification.test;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Http;

[PublicAPI]
public static class ServiceCollectionHttpClientExtensions
{
    /// <summary>
    /// Dynamically add all inheritors of BaseServiceOptions as AddConsulHttpClient<T>
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddConsulHttpClients(this IServiceCollection services)
    {
        var servicesOptionsTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(BaseServiceOptions)));

        foreach (var serviceOptionType in servicesOptionsTypes)
        {
            var method = typeof(ServiceCollectionHttpClientExtensions)
                .GetMethod(nameof(AddConsulHttpClient), new[] { typeof(IServiceCollection) });
            var generic = method.MakeGenericMethod(serviceOptionType);
            generic.Invoke(null, new[] { services });
        }

        return services;
    }
    
    /// <summary>
    /// Add HttpClient to HttpClientFactory with retry/cb/timeout policy
    /// Will add only if UseHttpClientFactory == true
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>s
    public static IServiceCollection AddConsulHttpClient<T>(this IServiceCollection services, Action<HttpClient> additionalActions) where T : BaseServiceOptions
    {
        var className = typeof(T).Name;
        var settings = ConfigurationManager.GetSection(className).Get<T>();
        var logger = LogManager.GetLogger(settings.ServiceName);
        
        if (!settings.UseHttpClientFactory || string.IsNullOrEmpty(settings.ConsulName))
        {
            logger.WarnWithObject($"Class ${className} has UseHttpClientFactory == false while AddCustomHttpClient");
            return services;
        }
        
        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();

        services.AddHttpClient(settings.ServiceName, httpClient =>
            {
                // We will override this url by consul, but we need to set it, otherwise we will get exception because HttpRequestMessage doesn't have baseUrl (only relative)
                httpClient.BaseAddress = new Uri("http://localhost");
                httpClient.SetBaseFields(serviceVariablesOptions.GetServiceAsClientName(), serviceVariablesOptions.GetServiceAsClientHeaderName(),  settings.AdditionalHeaders);
                additionalActions(httpClient);
            })
            .WithLogging<T>()
            .WithProxyFields<T>()
            .AddRetryPolicy(settings, logger)
            // Get new instance url for each retry (because 1 instance can be down)
            .WithConsul<T>()
            .AddHostSpecificCircuitBreakerPolicy(settings, logger)
            .AddTimeoutPolicy(settings.TimeOut)
            .WithMetrics<T>();

        return services;
    }
}