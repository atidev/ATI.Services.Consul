using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Extensions;
using Consul;
using NLog;


namespace ATI.Services.Consul
{
    public class ConsulServiceAddress: IDisposable
    {
        private readonly string _environment;
        private readonly string _serviceName;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Timer _updateCacheTimer;
        private ConsulServiceAddressCache CachedServices { get; }

        public ConsulServiceAddress(string serviceName, string environment, TimeSpan? timeToReload = null, bool useCaching = true)
        {
            timeToReload ??= TimeSpan.FromSeconds(60);
            _environment = environment;
            _serviceName = serviceName;

            CachedServices = new ConsulServiceAddressCache(useCaching, _serviceName, _environment);
            
            _updateCacheTimer = new Timer(_ => CachedServices.ReloadCache(), null, timeToReload.Value,
                timeToReload.Value);
        }

        public async Task<List<ServiceEntry>> GetAll()
        {
            return await CachedServices.GetCachedObjectsAsync();
        }

        public async Task<string> ToHttp()
        {
            var serviceInfo = (await CachedServices.GetCachedObjectsAsync()).RandomItem();
            var address = string.IsNullOrWhiteSpace(serviceInfo?.Service?.Address)
                ? serviceInfo?.Node.Address
                : serviceInfo.Service.Address;

            if (string.IsNullOrWhiteSpace(address) || serviceInfo.Service == null)
            {
                _logger.Warn($"Не удалось взять настройки из консула для {_serviceName}:{_environment}");
                return null;
            }

            return $"http://{address}:{serviceInfo.Service.Port}";
        }

        public async Task<(string, int)> GetAddressAndPort()
        {
            var serviceInfo = (await CachedServices.GetCachedObjectsAsync()).RandomItem();
            var address = string.IsNullOrWhiteSpace(serviceInfo?.Service?.Address)
                ? serviceInfo?.Node.Address
                : serviceInfo.Service.Address;

            if (string.IsNullOrWhiteSpace(address) || serviceInfo.Service == null)
            {
                _logger.Warn($"Не удалось взять настройки из консула для {_serviceName}:{_environment}");
                return default;
            }

            return (address, serviceInfo.Service.Port);
        }
        
        public void Dispose()
        {
            _updateCacheTimer.Dispose();
        }
    }

    public class ConsulServiceAddressCache
    {
        private Task<List<ServiceEntry>> _reloadCacheTask;
        private List<ServiceEntry> _cachedServices;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly bool _useCaching;
        private readonly string _serviceName;
        private readonly string _environment;

        public ConsulServiceAddressCache(bool useCaching, string serviceName, string environment)
        {
            _useCaching = useCaching;
            _serviceName = serviceName;
            _environment = environment;
            
            if (!_useCaching)
                return;

            _cachedServices = new();
            _reloadCacheTask = GetServiceFromConsul();
        }
        
        public async Task<List<ServiceEntry>> GetCachedObjectsAsync()
        {
            await CheckCacheWasInitialized();
            return _cachedServices ?? await GetServiceFromConsul();
        }

        public void ReloadCache()
        {
            if (_reloadCacheTask is {IsCompleted:true} or {IsCompletedSuccessfully:true})
                _reloadCacheTask = GetServiceFromConsul();
        }

        private async Task CheckCacheWasInitialized()
        {
            if (!_useCaching)
                return;

            if (_reloadCacheTask.IsCompleted || _reloadCacheTask.IsCompletedSuccessfully)
                return;

            _cachedServices = await _reloadCacheTask;
        }

        private async Task<List<ServiceEntry>> GetServiceFromConsul()
        {
            try
            {
                using var cc = new ConsulClient();
                var fromConsul = await cc.Health.Service(_serviceName, _environment, true);
                if (fromConsul.StatusCode == HttpStatusCode.OK && fromConsul.Response.Length > 0)
                {
                    return fromConsul.Response.ToList();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            return new List<ServiceEntry>();
        }
    }
}
