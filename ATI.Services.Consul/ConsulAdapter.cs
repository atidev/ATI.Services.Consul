using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Metrics;
using Consul;
using NLog;

namespace ATI.Services.Consul;

internal class ConsulAdapter: IDisposable
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly ConsulClient _consulClient = new();
    private readonly MetricsInstance _metrics;

    public ConsulAdapter(MetricsFactory metricsFactory)
    {
        _metrics = metricsFactory.CreateHttpClientMetricsFactory(nameof(ConsulAdapter), "consul");
    }

    

    /// <summary>
    /// Возвращает список живых инстансов сервиса
    /// </summary>
    /// <returns></returns>
    public async Task<OperationResult<List<ServiceEntry>>> GetPassingServiceInstancesAsync(
        string serviceName,
        string environment,
        bool passingOnly = true)
    {
        try
        {
            using (_metrics.CreateMetricsTimer("consul", "/health/service/:service"))
            {
                var fromConsul = await _consulClient.Health.Service(serviceName, environment, passingOnly);
                if (fromConsul.StatusCode == HttpStatusCode.OK)
                {
                    return new(fromConsul.Response?.ToList());
                }

                _logger.Error(
                    $"По запросу в консул {serviceName}:{environment}, вернулся ответ со статусом: {fromConsul.StatusCode}");
            }
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }

        return new(ActionStatus.InternalServerError);
    }

    public void Dispose()
    {
        _consulClient?.Dispose();
    }
}