using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Extensions;
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
    private Task<OperationResult<List<ServiceEntry>>> _updateCacheTask;
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
        _cachedServices = _updateCacheTask.GetAwaiter().GetResult() is var result && result.Success
                              ? result.Value
                              : new List<ServiceEntry>();
        
        _updateCacheTimer = new Timer(_ => ReloadCache().Forget(), null, ttl, ttl);
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

        _cachedServices = await _updateCacheTask is var result && result.Success
                              ? result.Value
                              : _cachedServices;
    }

    public void Dispose()
    {
        _updateCacheTimer.Dispose();
        _consulAdapter.Dispose();
    }
}