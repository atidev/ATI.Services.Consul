using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Options;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Consul.Http;

public class HttpConsulHandler<T> : HttpConsulHandler where T : BaseServiceOptions
{
    public HttpConsulHandler(MetricsFactory metricsFactory, IOptions<T> serviceOptions) 
        : base(metricsFactory, serviceOptions.Value)
    {
    }
}

public class HttpConsulHandler : DelegatingHandler
{
    private readonly ConsulServiceAddress _serviceAddress;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    protected HttpConsulHandler(MetricsFactory metricsFactory, BaseServiceOptions serviceOptions)
    {
        _serviceAddress =
            new ConsulServiceAddress(metricsFactory, serviceOptions.ConsulName, serviceOptions.Environment);
        
        Logger.WarnWithObject("HttpConsulHandler constructor", new { serviceOptions.ServiceName });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var url = await _serviceAddress.ToHttpAsync();
        var relativeUrl = request.RequestUri?.PathAndQuery;
        request.RequestUri = new Uri(new Uri(url), relativeUrl);

        return await base.SendAsync(request, ct);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _serviceAddress?.Dispose();
        }

        base.Dispose(disposing);
    }
}