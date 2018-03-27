// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    internal class HttpClientBuilder : IHttpClientBuilder
    {
        private readonly Dictionary<int, Func<DelegatingHandler>> _handlers =
            new Dictionary<int, Func<DelegatingHandler>>();

        public IHttpClient Create()
        {
            var httpclientHandler = new HttpClientHandler();

            var client = new HttpClient(CreateHttpMessageHandler(httpclientHandler));

            return new HttpClientWrapper(client);
        }

        private HttpMessageHandler CreateHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {
            _handlers
                .OrderByDescending(handler => handler.Key)
                .Select(handler => handler.Value)
                .Reverse()
                .ToList()
                .ForEach(handler =>
                {
                    var delegatingHandler = handler();
                    delegatingHandler.InnerHandler = httpMessageHandler;
                    httpMessageHandler = delegatingHandler;
                });
            return httpMessageHandler;
        }
    }

    /// <summary>
    /// This class was made to make unit testing easier when HttpClient is used.
    /// </summary>
    internal class HttpClientWrapper : IHttpClient
    {
        public HttpClientWrapper(HttpClient client)
        {
            Client = client;
        }

        public HttpClient Client { get; }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return Client.SendAsync(request);
        }
    }
}