using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Consul;

public class ConsulRegistrator
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private Timer _reregistrationTimer;
    private readonly HashSet<string> _registeredServices = [];

    public async Task RegisterServicesAsync(ConsulRegistratorOptions consulRegistratorOptions, int applicationPort)
    {
        foreach (var consulServiceOptions in consulRegistratorOptions.ConsulServiceOptions)
        {
            consulServiceOptions.Check.HTTP = $"http://localhost:{applicationPort}{consulServiceOptions.Check.HTTP}";
            await DeregisterFromConsulAsync($"{consulServiceOptions.ServiceName}-{Dns.GetHostName()}-{applicationPort}");
        }

        if(_reregistrationTimer != null) 
            await _reregistrationTimer.DisposeAsync();
        
        _reregistrationTimer = new Timer(async _ => await RegisterServicesAsyncPrivate(consulRegistratorOptions, applicationPort), 
                                         null, 
                                         TimeSpan.FromSeconds(0), 
                                         consulRegistratorOptions.ReregistrationPeriod);
    }

    private async Task RegisterServicesAsyncPrivate(ConsulRegistratorOptions consulRegistratorOptions, int applicationPort)
    {
        try
        {
            foreach (var consulServiceOptions in consulRegistratorOptions.ConsulServiceOptions)
                await RegisterToConsulAsync(consulServiceOptions, applicationPort);
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }
        
    private async Task RegisterToConsulAsync(ConsulServiceOptions options, int applicationPort)
    {
        var serviceId = $"{options.ServiceName}-{Dns.GetHostName()}-{applicationPort}";
        _registeredServices.Add(serviceId);

        var swaggerUrls = JsonConvert.SerializeObject(options.SwaggerUrls);
            
        using var client = new ConsulClient();
        var cr = new AgentServiceRegistration
        {
            Name = options.ServiceName,
            ID = serviceId,
            Tags = options.Tags,
            Check = options.Check,
            Port = applicationPort,
            Meta = new Dictionary<string, string>
            {
                {"swagger_urls", swaggerUrls}
            }
        };
        await client.Agent.ServiceRegister(cr);
    }

    public async Task DeregisterInstanceAsync()
    {
        await _reregistrationTimer.DisposeAsync();
        try
        {
            foreach (var serviceId in _registeredServices)
            {
                await DeregisterFromConsulAsync(serviceId);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }

    private async Task DeregisterFromConsulAsync(string serviceId)
    {
        try
        {
            using var client = new ConsulClient();
            await client.Agent.ServiceDeregister(serviceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Не удалось дерегистрировать {serviceId} из консула.");
        }
    }
}