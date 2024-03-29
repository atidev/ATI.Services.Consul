﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Metrics;
using Consul;
using NLog;


namespace ATI.Services.Consul
{
    public class ConsulServiceAddress : IDisposable
    {
        private readonly string _environment;
        private readonly string _serviceName;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Func<Task<List<ServiceEntry>>> _getServices;
        private readonly ConsulServiceAddressCache _serviceAddressCache;
        private readonly ConsulAdapter _consulAdapter;

        public ConsulServiceAddress(
            MetricsFactory metricsFactory,
            string serviceName,
            string environment,
            TimeSpan? timeToReload = null,
            bool useCaching = true,
            bool passingOnly = true)
        {
            timeToReload ??= TimeSpan.FromSeconds(5);
            _environment = environment;
            _serviceName = serviceName;

            if (useCaching)
            {
                _serviceAddressCache = new ConsulServiceAddressCache(metricsFactory, _serviceName, _environment, timeToReload.Value, passingOnly);
                _getServices = () => Task.FromResult(_serviceAddressCache.GetCachedObjectsAsync());
            }
            else
            {
                _consulAdapter = new ConsulAdapter(metricsFactory);
                _getServices = async () =>
                    await _consulAdapter.GetPassingServiceInstancesAsync(serviceName, environment, passingOnly) is var result && result.Success
                        ? result.Value
                        : new List<ServiceEntry>();
            }
        }

        public async Task<List<ServiceEntry>> GetAllAsync() => await _getServices();

        public async Task<string> ToHttpAsync()
        {
            var serviceInfo = (await GetAllAsync()).RandomItem();
            var address = string.IsNullOrWhiteSpace(serviceInfo?.Service?.Address)
                ? serviceInfo?.Node.Address
                : serviceInfo.Service.Address;

            if (string.IsNullOrWhiteSpace(address) || serviceInfo.Service == null)
            {
                _logger.Error($"Не удалось взять настройки из консула для {_serviceName}:{_environment}");
                return null;
            }

            return $"http://{address}:{serviceInfo.Service.Port}";
        }

        public async Task<(string, int)> GetAddressAndPortAsync()
        {
            var serviceInfo = (await GetAllAsync()).RandomItem();
            var address = string.IsNullOrWhiteSpace(serviceInfo?.Service?.Address)
                ? serviceInfo?.Node.Address
                : serviceInfo.Service.Address;

            if (string.IsNullOrWhiteSpace(address) || serviceInfo.Service == null)
            {
                _logger.Error($"Не удалось взять настройки из консула для {_serviceName}:{_environment}");
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
            _serviceAddressCache?.Dispose();
            _consulAdapter?.Dispose();
        }
    }
}