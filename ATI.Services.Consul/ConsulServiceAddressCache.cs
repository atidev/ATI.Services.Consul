using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Consul;
using NLog;

namespace ATI.Services.Consul
{
    /// <summary>
    /// Обеспечивает получение доступных сервисов от консула и их кеширование (опционально)
    /// </summary>
    public class ConsulServiceAddressCache
    {
        private Task<List<ServiceEntry>> _reloadCacheTask;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly bool _useCaching;
        private readonly string _serviceName;
        private readonly string _environment;
        public ConsulServiceAddressCache(bool useCaching, string serviceName, string environment, string consulAgentAddress = null)
        {
            _useCaching = useCaching;
            _serviceName = serviceName;
            _environment = environment;
            
            if (!_useCaching)
                return;
            
            _reloadCacheTask = GetServiceFromConsulAsync(consulAgentAddress);
        }
        
        /// <summary>
        /// Возвращает коллекцию сервисов 
        /// </summary>
        /// <returns></returns>
        public Task<List<ServiceEntry>> GetCachedObjectsAsync(string consulAgentAddress = null)
        {
            return _useCaching ? _reloadCacheTask : GetServiceFromConsulAsync(consulAgentAddress);
        }

        /// <summary>
        /// Запускает таску на обновление кеша, если кеширование включено
        /// </summary>
        public void ReloadCache(string consulAgentAddress = null)
        {
            if (!_useCaching)
                return;
            
            if (!_reloadCacheTask.IsCompleted)
                return;
            
            _reloadCacheTask = GetServiceFromConsulAsync(consulAgentAddress);
        }

        /// <summary>
        /// Возвращает список живых сервисов
        /// </summary>
        /// <returns></returns>
        private async Task<List<ServiceEntry>> GetServiceFromConsulAsync(string consulAgentAddress = null)
        {
            try
            {
                using var client = new ConsulClient(configuration =>
                {
                    if (!string.IsNullOrEmpty(consulAgentAddress))
                        configuration.Address = new Uri(consulAgentAddress);
                });
                var fromConsul = await client.Health.Service(_serviceName, _environment, true);
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