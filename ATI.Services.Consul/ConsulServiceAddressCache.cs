using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Consul;
using NLog;

namespace ATI.Services.Consul
{
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
            _reloadCacheTask = GetServiceFromConsulAsync();
        }
        
        public async Task<List<ServiceEntry>> GetCachedObjectsAsync()
        {
            await CheckCacheWasInitializedAsync();
            return _cachedServices ?? await GetServiceFromConsulAsync();
        }

        public void ReloadCache()
        {
            if (_reloadCacheTask is {IsCompleted:true} or {IsCompletedSuccessfully:true})
                _reloadCacheTask = GetServiceFromConsulAsync();
        }

        private async Task CheckCacheWasInitializedAsync()
        {
            if (!_useCaching)
                return;

            if (_reloadCacheTask.IsCompleted || _reloadCacheTask.IsCompletedSuccessfully)
                return;

            _cachedServices = await _reloadCacheTask;
        }

        private async Task<List<ServiceEntry>> GetServiceFromConsulAsync()
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