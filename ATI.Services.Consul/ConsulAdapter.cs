using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Consul;
using NLog;

namespace ATI.Services.Consul;

internal class ConsulAdapter: IDisposable
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private ConsulClient _consulClient;

    /// <summary>
    /// Возвращает список живых сервисов
    /// </summary>
    /// <returns></returns>
    public async Task<List<ServiceEntry>> GetPassingServiceInstancesAsync(string serviceName, string environment, bool passingOnly = true, ulong index = 0, TimeSpan? waitTime = null)
    {
        try
        {
            _consulClient = new ConsulClient();
            var fromConsul = await _consulClient.Health.Service(serviceName, environment, passingOnly, new QueryOptions { WaitIndex = index, WaitTime = waitTime});
            if (fromConsul.StatusCode == HttpStatusCode.OK)
            {
                return fromConsul.Response?.ToList();
            }

            _logger.Error($"По запросу в консул {serviceName}:{environment}, вернулся ответ со статусом: {fromConsul.StatusCode}");
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
        
        return new List<ServiceEntry>();
    }

    public void Dispose()
    {
        _consulClient?.Dispose();
    }
}