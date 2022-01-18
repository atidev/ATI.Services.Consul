using System;
using System.Collections.Generic;
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
            timeToReload ??= TimeSpan.FromSeconds(5);
            _environment = environment;
            _serviceName = serviceName;

            CachedServices = new ConsulServiceAddressCache(useCaching, _serviceName, _environment);
            
            _updateCacheTimer = new Timer(_ => CachedServices.ReloadCache(), null, timeToReload.Value,
                timeToReload.Value);
        }

        public async Task<List<ServiceEntry>> GetAllAsync()
        {
            return await CachedServices.GetCachedObjectsAsync();
        }

        public async Task<string> ToHttpAsync()
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

        public async Task<(string, int)> GetAddressAndPortAsync()
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

        #region Obsolete
        
        [Obsolete("Method GetAll is deprecated, pls use GetAllAsync instead")]
        public List<ServiceEntry> GetAll()
        {
            return GetAllAsync().GetAwaiter().GetResult();
        }
        
        [Obsolete("Method ToHttp is deprecated, pls use ToHttpAsync instead")]
        public string ToHttp()
        {
            return ToHttpAsync().GetAwaiter().GetResult();
        }
        
        [Obsolete("Method GetAddressAndPort is deprecated, pls use GetAddressAndPortAsync instead")]
        public (string, int) GetAddressAndPort()
        {
            return GetAddressAndPortAsync().GetAwaiter().GetResult();
        }

        #endregion
        
        public void Dispose()
        {
            _updateCacheTimer.Dispose();
        }
    }
}
