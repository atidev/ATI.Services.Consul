using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Consul;

[PublicAPI]
public static class ConsulDeregistrationExtension
{
    private const string DeregistrationAddress = "_internal/consul/deregister";

    public static IEndpointConventionBuilder MapConsulDeregistration(this IEndpointRouteBuilder builder,
                                                                     string deregistrationAddress = null)
    {
        var registrator = builder.ServiceProvider.GetService<ConsulRegistrator>();
        return builder.MapDelete(deregistrationAddress ?? DeregistrationAddress, async _ => await registrator.DeregisterInstanceAsync());
    }
}