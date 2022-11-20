using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ATI.Services.Consul
{
    [PublicAPI]
    public static class ConsulDeregistrationExtension
    {
        private const string DeregistrationAddress = "_internal/consul/deregister";

        public static IEndpointConventionBuilder MapConsulDeregistration(this IEndpointRouteBuilder builder,
            string deregistrationAddress = null, string consulAgentAddress = null)
        {
            return builder.MapDelete(deregistrationAddress ?? DeregistrationAddress,
                _ => DeregisterDelegate(consulAgentAddress));
        }

        private static async Task DeregisterDelegate(string consulAgentAddress = null)
        {
            await ConsulRegistrator.DeregisterInstanceAsync(consulAgentAddress);
        }
    }
}