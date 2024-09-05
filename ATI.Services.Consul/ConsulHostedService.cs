using System;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ATI.Services.Consul;

[PublicAPI]
public class ConsulHostedService(
    IOptions<ConsulRegistratorOptions> consulRegistratorOptions,
    ConsulRegistrator registrator) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        if (bool.TryParse(ConfigurationManager.AppSettings("ConsulEnabled"), out var enabled) && enabled)
            return registrator.RegisterServicesAsync(consulRegistratorOptions.Value,
                                                     ConfigurationManager.GetApplicationPort());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        Console.WriteLine("ConsulHostedService is stopping.");
        return registrator.DeregisterInstanceAsync();
    }
}