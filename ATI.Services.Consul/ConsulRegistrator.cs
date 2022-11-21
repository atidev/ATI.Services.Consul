using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Consul
{
    public static class ConsulRegistrator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static Timer _reregistrationTimer;
        private static HashSet<string> RegisteredServices { get; set; } = new();

        public static async Task RegisterServicesAsync(ConsulRegistratorOptions consulRegistratorOptions, int applicationPort)
        {
            foreach (var consulServiceOptions in consulRegistratorOptions.ConsulServiceOptions)
            {
                consulServiceOptions.Check.HTTP = $"http://{consulServiceOptions.ServiceDockerName ?? "localhost"}:{applicationPort}{consulServiceOptions.Check.HTTP}";
                await DeregisterFromConsulAsync($"{consulServiceOptions.ServiceName}-{Dns.GetHostName()}-{applicationPort}",
                    consulRegistratorOptions.ConsulAgentAddress);
            }

            _reregistrationTimer = new Timer(async _ => await RegisterServicesAsyncPrivate(consulRegistratorOptions, applicationPort), 
                null, 
                TimeSpan.FromSeconds(0), 
                consulRegistratorOptions.ReregistrationPeriod);
        }

        private static async Task RegisterServicesAsyncPrivate(ConsulRegistratorOptions consulRegistratorOptions, int applicationPort)
        {
            try
            {
                foreach (var consulServiceOptions in consulRegistratorOptions.ConsulServiceOptions)
                    await RegisterToConsulAsync(consulServiceOptions, applicationPort, consulRegistratorOptions.ConsulAgentAddress);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
        
        private static async Task RegisterToConsulAsync(ConsulServiceOptions options, int applicationPort,
            string consulAgentAddress = null)
        {
            var serviceId = $"{options.ServiceName}-{Dns.GetHostName()}-{applicationPort}";
            RegisteredServices.Add(serviceId);

            var swaggerUrls = JsonConvert.SerializeObject(options.SwaggerUrls);
            
            using var client = new ConsulClient(configuration =>
            {
                if (!string.IsNullOrEmpty(consulAgentAddress))
                    configuration.Address = new Uri(consulAgentAddress);
            });
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

        public static async Task DeregisterInstanceAsync(string consulAgentAddress = null)
        {
            await _reregistrationTimer.DisposeAsync();
            try
            {
                foreach (var serviceId in RegisteredServices)
                {
                    await DeregisterFromConsulAsync(serviceId, consulAgentAddress);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static async Task DeregisterFromConsulAsync(string serviceId, string consulAgentAddress = null)
        {
            try
            {
                using var client = new ConsulClient(configuration =>
                {
                    if (!string.IsNullOrEmpty(consulAgentAddress))
                        configuration.Address = new Uri(consulAgentAddress);
                });
                await client.Agent.ServiceDeregister(serviceId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Не удалось дерегистрировать {serviceId} из консула.");
            }
        }
    }
}
