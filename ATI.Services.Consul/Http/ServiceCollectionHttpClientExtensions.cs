using System;
using System.Collections.Generic;
using System.Linq;
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
    private static readonly HashSet<string> RegisteredServiceNames = new ();

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
    public static IServiceCollection AddConsulHttpClient<T>(this IServiceCollection services) where T : BaseServiceOptions
    {
        var className = typeof(T).Name;
        var settings = ConfigurationManager.GetSection(className).Get<T>();
        if (settings == null)
        {
            throw new Exception($"Cannot find section for {className}");
        }

        var serviceName = settings.ServiceName;
        
        var logger = LogManager.GetLogger(serviceName);
        
        if (!settings.UseHttpClientFactory || string.IsNullOrEmpty(settings.ConsulName))
        {
            logger.WarnWithObject($"Class ${className} has UseHttpClientFactory == false OR ConsulName == null while AddCustomHttpClient");
            return services;
        }

        // Each HttpClient must be added only once, otherwise we will get exceptions like 	System.InvalidOperationException: The 'InnerHandler' property must be null. 'DelegatingHandler' instances provided to 'HttpMessageHandlerBuilder' must not be reused or cached.
        // Handler: 'ATI.Services.Consul.Http.HttpConsulHandler
        // Possible reason - because HttpConsulHandler is singleton (not transient)
        // https://stackoverflow.com/questions/77542613/the-innerhandler-property-must-be-null-delegatinghandler-instances-provided
        if (RegisteredServiceNames.Contains(serviceName))
        {
            logger.WarnWithObject($"Class ${className} is already registered");
            return services;
        }
        
        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();

        services.AddHttpClient(serviceName, httpClient =>
            {
                // We will override this url by consul, but we need to set it, otherwise we will get exception because HttpRequestMessage doesn't have baseUrl (only relative)
                httpClient.BaseAddress = new Uri("http://localhost");
                httpClient.SetBaseFields(serviceVariablesOptions.GetServiceAsClientName(), serviceVariablesOptions.GetServiceAsClientHeaderName(),  settings.AdditionalHeaders);
            })
            .WithLogging<T>()
            .WithProxyFields<T>()
            .AddRetryPolicy(settings, logger)
            // Get new instance url for each retry (because 1 instance can be down)
            .WithConsul<T>()
            .AddHostSpecificCircuitBreakerPolicy(settings, logger)
            .AddTimeoutPolicy(settings.TimeOut)
            .WithMetrics<T>();
        
        RegisteredServiceNames.Add(serviceName);

        return services;
    }
}