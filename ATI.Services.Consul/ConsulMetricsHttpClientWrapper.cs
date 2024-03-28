using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Metrics.HttpWrapper;
using ATI.Services.Common.Options;
using ATI.Services.Common.Serializers;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Consul
{
    /// <summary>
    /// Обертка, включающая в себя ConsulServiceAddress, MetricsHttpClientWrapper и MetricsTracingFactory
    /// </summary>
    [PublicAPI]
    public class ConsulMetricsHttpClientWrapper : IDisposable
    {
        private readonly BaseServiceOptions _serviceOptions;
        private readonly MetricsHttpClientWrapper _clientWrapper;
        private readonly MetricsHttpClientConfig _clientConfig;
        private readonly MetricsInstance _metrics;
        private readonly ConsulServiceAddress _serviceAddress;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public ConsulMetricsHttpClientWrapper(
            BaseServiceOptions serviceOptions,
            string adapterName,
            IHttpClientFactory httpClientFactory,
            MetricsFactory metricsFactory, 
            JsonSerializerSettings newtonsoftSettings = null,
            JsonSerializerOptions systemTextJsonOptions = null)
        {
            _serviceOptions = serviceOptions;
            _metrics = metricsFactory.CreateHttpClientMetricsFactory(adapterName,
                                                                                          serviceOptions.ConsulName, serviceOptions.LongRequestTime);

            _serviceAddress =
                new ConsulServiceAddress(metricsFactory, serviceOptions.ConsulName, serviceOptions.Environment);

            _clientConfig = new MetricsHttpClientConfig(serviceOptions.ConsulName, serviceOptions.TimeOut, 
                serviceOptions.SerializerType, serviceOptions.AddCultureToRequest, newtonsoftSettings, systemTextJsonOptions)
            {
                LogLevelOverride = serviceOptions.LogLevelOverride,
                HeadersToProxy = serviceOptions.HeadersToProxy,
                UseHttpClientFactory = serviceOptions.UseHttpClientFactory
            };

            if (serviceOptions.AdditionalHeaders != null)
            {
                foreach (var header in serviceOptions.AdditionalHeaders)
                    _clientConfig.Headers.TryAdd(header.Key, header.Value);
            }

            _clientWrapper = new MetricsHttpClientWrapper(_clientConfig, httpClientFactory);
        }

        public void SetSerializer(JsonSerializerSettings newtonsoftSettings)
        {
            _clientConfig.SetSerializer(_serviceOptions.SerializerType, newtonsoftSettings: newtonsoftSettings);
        }
        
        public void SetSerializer(JsonSerializerOptions systemTextJsonOptions)
        {
            _clientConfig.SetSerializer(_serviceOptions.SerializerType, systemTextJsonOptions: systemTextJsonOptions);
        }

        #region Get

        public Task<OperationResult<TResponse>> GetAsync<TResponse>(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.GetAsync<TResponse>(serviceAddress, metricName, url, headers), HttpMethod.Get,
                additionalErrorLogObjects);
        }

        public Task<OperationResult<string>> GetAsync(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.GetAsync(serviceAddress, metricName, url, headers), HttpMethod.Get,
                additionalErrorLogObjects);
        }

        #endregion

        #region Post

        public Task<OperationResult<TResponse>> PostAsync<TBody, TResponse>(string url, TBody body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync<TBody, TResponse>(serviceAddress, metricName, url, body, headers),
                HttpMethod.Post,
                additionalErrorLogObjects);
        }

        public Task<OperationResult<TResponse>> PostAsync<TResponse>(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PostAsync<TResponse>(serviceAddress, metricName, url, headers), HttpMethod.Post);
        }

        public Task<OperationResult<TResponse>> PostAsync<TResponse>(string url, string body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync<TResponse>(serviceAddress, metricName, url, body, headers),
                HttpMethod.Post);
        }

        public Task<OperationResult<string>> PostAsync(string url, string body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync(serviceAddress, metricName, url, body, headers), HttpMethod.Post);
        }

        public Task<OperationResult<string>> PostAsync<T>(string url, T body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync(serviceAddress, metricName, url, body, headers), HttpMethod.Post);
        }

        #endregion

        #region Put

        public Task<OperationResult<TResponse>> PutAsync<TBody, TResponse>(string url, TBody body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PutAsync<TBody, TResponse>(serviceAddress, metricName, url, body, headers),
                HttpMethod.Put);
        }

        public Task<OperationResult<TResponse>> PutAsync<TResponse>(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PutAsync<TResponse>(serviceAddress, metricName, url, headers), HttpMethod.Put);
        }

        public Task<OperationResult<string>> PutAsync(
            string url, string metricName, Dictionary<string, string> headers = null,
            string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PutAsync(serviceAddress, metricName, url, headers), HttpMethod.Put);
        }

        #endregion

        #region Delete

        public Task<OperationResult<TResponse>> DeleteAsync<TBody, TResponse>(
            string url,
            TBody body,
            string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.DeleteAsync<TBody, TResponse>(serviceAddress, metricName, url, body, headers),
                HttpMethod.Delete);
        }

        public Task<OperationResult<TResponse>> DeleteAsync<TResponse>(string url,
            string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.DeleteAsync<TResponse>(serviceAddress, metricName, url, headers), HttpMethod.Delete);
        }

        public Task<OperationResult<string>> DeleteAsync(string url,
            string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.DeleteAsync(serviceAddress, metricName, url, headers), HttpMethod.Delete);
        }

        #endregion

        #region Patch

        public Task<OperationResult<TResponse>> PatchAsync<TBody, TResponse>(string url, TBody body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PatchAsync<TBody, TResponse>(serviceAddress, metricName, url, body, headers),
                HttpMethod.Patch);
        }

        public Task<OperationResult<TResponse>> PatchAsync<TResponse>(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PatchAsync<TResponse>(serviceAddress, metricName, url, headers), HttpMethod.Patch);
        }

        public Task<OperationResult<string>> PatchAsync<TBody>(string url, TBody body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PatchAsync(serviceAddress, metricName, url, body, headers), HttpMethod.Patch);
        }

        public Task<OperationResult<string>> PatchAsync(
            string url, string metricName, Dictionary<string, string> headers = null,
            string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PatchAsync(serviceAddress, metricName, url, headers), HttpMethod.Patch);
        }

        #endregion

        #region Send

        public async Task<OperationResult<HttpResponseMessage<TResponse>>> SendAsync<TBody, TResponse>(
            HttpMethod methodName,
            string url,
            TBody body,
            string metricName,
            Dictionary<string, string> headers = null,
            string urlTemplate = null,
            string[] additionalLabels = null)
        {
            using var _ =
                _metrics.CreateLoggingMetricsTimer(metricName,
                                                                 $"{methodName}:{urlTemplate ?? url}",
                                                                 additionalLabels);
            {
                try
                {
                    var serviceAddress = await _serviceAddress.ToHttpAsync();
                    return await _clientWrapper.SendAsync<TBody, TResponse>(new Uri(new Uri(serviceAddress),url), metricName, body, headers, methodName);
                }
                catch (Exception e)
                {
                    _logger.LogWithObject(_serviceOptions.LogLevelOverride(LogLevel.Error), e);
                    return new(ActionStatus.InternalServerError);
                }
            }
        }

        #endregion

        private async Task<OperationResult<T>> SendAsync<T>(string url,
            string urlTemplate,
            string metricName,
            Dictionary<string, string> headers,
            string[] additionalLabels,
            Func<string, Task<OperationResult<T>>> methodExecuteFunc,
            HttpMethod methodName,
            params object[] errorLogObjects)
        {
            using (_metrics.CreateLoggingMetricsTimer(metricName,
                       $"{methodName}:{urlTemplate ?? url}", additionalLabels))
            {
                try
                {
                    var serviceAddress = await _serviceAddress.ToHttpAsync();
                    return await methodExecuteFunc(serviceAddress);
                }
                catch (Exception e)
                {
                    _logger.LogWithObject(_serviceOptions.LogLevelOverride(LogLevel.Error),
                                          e,
                                          logObjects: errorLogObjects);
                    return new OperationResult<T>(ActionStatus.InternalServerError);
                }
            }
        }

        private async Task<OperationResult<T>> SendAsync<T, TBody>(string url,
            string urlTemplate,
            string metricName,
            Dictionary<string, string> headers,
            string[] additionalLabels,
            TBody body,
            Func<string, Task<OperationResult<T>>> methodExecuteFunc,
            HttpMethod methodName,
            params object[] errorLogObjects)
        {
            using (_metrics.CreateLoggingMetricsTimer(metricName,
                       $"{methodName}:{urlTemplate ?? url}", additionalLabels))
            {
                try
                {
                    var serviceAddress = await _serviceAddress.ToHttpAsync();
                    return await methodExecuteFunc(serviceAddress);
                }
                catch (Exception e)
                {
                    _logger.LogWithObject(_serviceOptions.LogLevelOverride(LogLevel.Error),
                                          e,
                                          logObjects: new { body, additionalLabels });
                    return new OperationResult<T>(ActionStatus.InternalServerError);
                }
            }
        }

        public void Dispose()
        {
            _serviceAddress?.Dispose();
            _clientWrapper?.Dispose();
        }
    }

    /// <summary>
    /// Wrapper which includes ConsulServiceAddress, MetricsHttpClientWrapper and MetricsTracingFactory
    /// It must be used via DI, add it by .AddConsulMetricsHttpClientWrappers()
    /// </summary>
    [PublicAPI]
    public class ConsulMetricsHttpClientWrapper<T> : ConsulMetricsHttpClientWrapper
        where T : BaseServiceOptions
    {
        public ConsulMetricsHttpClientWrapper(
            IOptions<T> serviceOptions,
            IHttpClientFactory httpClientFactory,
            MetricsFactory metricsFactory) : base(serviceOptions.Value, serviceOptions.Value.ConsulName, httpClientFactory, metricsFactory)
        {
        }
    }
}