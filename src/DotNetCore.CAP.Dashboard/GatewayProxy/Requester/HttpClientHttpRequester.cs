// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        private readonly IHttpClientCache _cacheHandlers;
        private readonly ILogger _logger;

        public HttpClientHttpRequester(ILoggerFactory loggerFactory, IHttpClientCache cacheHandlers)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
        }

        public async Task<HttpResponseMessage> GetResponse(HttpRequestMessage request)
        {
            var builder = new HttpClientBuilder();

            var cacheKey = GetCacheKey(request, builder);

            var httpClient = GetHttpClient(cacheKey, builder);

            try
            {
                return await httpClient.SendAsync(request);
            }
            catch (Exception exception)
            {
                _logger.LogError("Error making http request, exception:" + exception.Message);
                throw;
            }
            finally
            {
                _cacheHandlers.Set(cacheKey, httpClient, TimeSpan.FromHours(24));
            }
        }

        private IHttpClient GetHttpClient(string cacheKey, IHttpClientBuilder builder)
        {
            var httpClient = _cacheHandlers.Get(cacheKey);

            if (httpClient == null)
            {
                httpClient = builder.Create();
            }

            return httpClient;
        }

        private string GetCacheKey(HttpRequestMessage request, IHttpClientBuilder builder)
        {
            var baseUrl = $"{request.RequestUri.Scheme}://{request.RequestUri.Authority}";

            return baseUrl;
        }
    }
}