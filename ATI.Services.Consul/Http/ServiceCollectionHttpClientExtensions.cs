using System;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Http.Extensions;
using ATI.Services.Common.Options;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Consul.Http;

[PublicAPI]
public static class ServiceCollectionHttpClientExtensions
{
    /// <summary>
    /// Add HttpClient to HttpClientFactory with retry/cb/timeout policy
    /// Will add only if UseHttpClientFactory == true
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TAdapter">Type of the http adapter for typed HttpClient</typeparam>
    /// <typeparam name="TServiceOptions"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddConsulHttpClient<TAdapter, TServiceOptions>(this IServiceCollection services) 
        where TAdapter : class
        where TServiceOptions : BaseServiceOptions
    {
        var settings = GetSettings<TServiceOptions>();

        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();

        services.AddHttpClient<TAdapter>(httpClient =>
            {
                // We will override this url by consul, but we need to set it, otherwise we will get exception because HttpRequestMessage doesn't have baseUrl (only relative)
                httpClient.BaseAddress = new Uri("http://localhost");
                httpClient.SetBaseFields(serviceVariablesOptions.GetServiceAsClientName(),
                    serviceVariablesOptions.GetServiceAsClientHeaderName(), settings.AdditionalHeaders);
            })
            .AddDefaultHandlers(settings, HttpClientBuilderExtensions.WithConsul<TServiceOptions>);

        return services;
    }
    
    public static IServiceCollection AddConsulHttpClient<TAdapterInterface, TAdapter, TServiceOptions>(this IServiceCollection services) 
        where TAdapter : class, TAdapterInterface
        where TAdapterInterface : class
        where TServiceOptions : BaseServiceOptions
    {
        var settings = GetSettings<TServiceOptions>();

        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();

        services.AddHttpClient<TAdapterInterface, TAdapter>(httpClient =>
            {
                // We will override this url by consul, but we need to set it, otherwise we will get exception because HttpRequestMessage doesn't have baseUrl (only relative)
                httpClient.BaseAddress = new Uri("http://localhost");
                httpClient.SetBaseFields(serviceVariablesOptions.GetServiceAsClientName(),
                    serviceVariablesOptions.GetServiceAsClientHeaderName(), settings.AdditionalHeaders);
            })
            .AddDefaultHandlers(settings, HttpClientBuilderExtensions.WithConsul<TServiceOptions>);

        return services;
    }

    /// <summary>
    /// Add HttpClient to HttpClientFactory with retry/cb/timeout policy
    /// Will add only if UseHttpClientFactory == true
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TAdapter">Type of the http adapter for typed HttpClient</typeparam>
    /// <typeparam name="TServiceOptions"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddK8SHttpClient<TAdapter, TServiceOptions>(this IServiceCollection services) 
        where TAdapter : class
        where TServiceOptions : BaseServiceOptions
    {
        var settings = GetSettings<TServiceOptions>();

        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();

        services.ConfigureByName<K8sServiceOptions>();

        services.AddHttpClient<TAdapter>((sp, httpClient) =>
            {
                var k8SOption = sp.GetRequiredService<IOptions<K8sServiceOptions>>().Value;
                var serviceOption = sp.GetRequiredService<IOptions<TServiceOptions>>().Value;

                // We will override this url by consul, but we need to set it, otherwise we will get exception because HttpRequestMessage doesn't have baseUrl (only relative)
                httpClient.BaseAddress = new Uri(k8SOption.BaseUrl);
                httpClient.DefaultRequestHeaders.Add(k8SOption.ServiceNameHeaderKey, serviceOption.ServiceName);

                httpClient.SetBaseFields(serviceVariablesOptions.GetServiceAsClientName(),
                    serviceVariablesOptions.GetServiceAsClientHeaderName(), settings.AdditionalHeaders);
            })
            .AddDefaultHandlers(settings, HttpClientBuilderExtensions.WithK8S);

        return services;
    }
    
    public static IServiceCollection AddK8SHttpClient<TAdapterInterface, TAdapter, TServiceOptions>(this IServiceCollection services) 
        where TAdapter : class, TAdapterInterface
        where TAdapterInterface : class
        where TServiceOptions : BaseServiceOptions
    {
        var settings = GetSettings<TServiceOptions>();

        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();

        services.ConfigureByName<K8sServiceOptions>();

        services.AddHttpClient<TAdapterInterface, TAdapter>((sp, httpClient) =>
            {
                var k8SOption = sp.GetRequiredService<IOptions<K8sServiceOptions>>().Value;
                var serviceOption = sp.GetRequiredService<IOptions<TServiceOptions>>().Value;

                // We will override this url by consul, but we need to set it, otherwise we will get exception because HttpRequestMessage doesn't have baseUrl (only relative)
                httpClient.BaseAddress = new Uri(k8SOption.BaseUrl);
                httpClient.DefaultRequestHeaders.Add(k8SOption.ServiceNameHeaderKey, serviceOption.ServiceName);

                httpClient.SetBaseFields(serviceVariablesOptions.GetServiceAsClientName(),
                    serviceVariablesOptions.GetServiceAsClientHeaderName(), settings.AdditionalHeaders);
            })
            .AddDefaultHandlers(settings, HttpClientBuilderExtensions.WithK8S);

        return services;
    }

    private static TServiceOptions GetSettings<TServiceOptions>()
        where TServiceOptions : BaseServiceOptions
    {
        var className = typeof(TServiceOptions).Name;
        var settings = ConfigurationManager.GetSection(className).Get<TServiceOptions>();
        if (settings == null)
        {
            throw new Exception($"Cannot find section for {className}");
        }
        
        if (string.IsNullOrEmpty(settings.ConsulName))
        {
            throw new Exception($"Class {className} has ConsulName == null while AddConsulHttpClient");
        }

        return settings;
    }

    private static IHttpClientBuilder AddDefaultHandlers<TServiceOptions>(this IHttpClientBuilder builder, TServiceOptions settings, Func<IHttpClientBuilder, IHttpClientBuilder> configure)
    where TServiceOptions : BaseServiceOptions
    {
        var logger = LogManager.GetLogger(settings.ServiceName);
        
        return builder
            .WithLogging<TServiceOptions>()
            .WithProxyFields<TServiceOptions>()
            .AddRetryPolicy(settings, logger)
            // Get new instance url for each retry (because 1 instance can be down)
            .WithAddressHandler<TServiceOptions>(configure)
            .AddHostSpecificCircuitBreakerPolicy(settings, logger)
            .AddTimeoutPolicy(settings.TimeOut)
            .WithMetrics<TServiceOptions>();
        // we don't override PooledConnectionLifetime even we use HttpClient in static TAdapter
        // because we are getting new host from consul for each request
        // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
    }
}