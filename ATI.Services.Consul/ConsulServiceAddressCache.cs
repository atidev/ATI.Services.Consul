using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Consul;

namespace ATI.Services.Consul;

/// <summary>
/// Обеспечивает получение доступных сервисов от консула и их кеширование (опционально)
/// </summary>
internal class ConsulServiceAddressCache: IDisposable
{
    private readonly string _serviceName;
    private readonly string _environment;
    private readonly bool _passingOnly;
    private List<ServiceEntry> _cachedServices;
    private readonly Timer _updateCacheTimer;
    private Task<List<ServiceEntry>> _updateCacheTask;
    private readonly ConsulAdapter _consulAdapter;

    public ConsulServiceAddressCache(string serviceName,
                                     string environment,
                                     TimeSpan ttl,
                                     bool passingOnly = true)
    {
        _serviceName = serviceName;
        _environment = environment;
        _passingOnly = passingOnly;
        _consulAdapter = new ConsulAdapter();
        _updateCacheTask = _consulAdapter.GetPassingServiceInstancesAsync(_serviceName, _environment, passingOnly);
        _cachedServices = _updateCacheTask.GetAwaiter().GetResult();
        
        #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        _updateCacheTimer = new Timer(_ => ReloadCache(), null, ttl, ttl);
        #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
        
    /// <summary>
    /// Возвращает коллекцию сервисов 
    /// </summary>
    /// <returns></returns>
    public List<ServiceEntry> GetCachedObjectsAsync() => _cachedServices;

    /// <summary>
    /// Запускает таску на обновление кеша
    /// </summary>
    private async Task ReloadCache()
    {
        if(_updateCacheTask == null || _updateCacheTask.IsCompleted)
            _updateCacheTask = _consulAdapter.GetPassingServiceInstancesAsync(_serviceName, _environment, _passingOnly);
        
        _cachedServices = await _updateCacheTask;
    }

    public void Dispose()
    {
        _updateCacheTimer.Dispose();
    }
}