// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard.GatewayProxy.Requester;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class GatewayProxyAgent
    {
        public const string CookieNodeName = "cap.node";
        private readonly ILogger _logger;

        private readonly IHttpRequester _requester;
        private readonly IRequestMapper _requestMapper;

        private readonly DiscoveryOptions _discoveryOptions;
        private readonly INodeDiscoveryProvider _discoveryProvider;

        public GatewayProxyAgent(
            ILoggerFactory loggerFactory,
            IRequestMapper requestMapper,
            IHttpRequester requester,
            DiscoveryOptions discoveryOptions,
            INodeDiscoveryProvider discoveryProvider)
        {
            _logger = loggerFactory.CreateLogger<GatewayProxyAgent>();
            _requestMapper = requestMapper;
            _requester = requester;
            _discoveryProvider = discoveryProvider;
            _discoveryOptions = discoveryOptions;
        }

        protected HttpRequestMessage DownstreamRequest { get; set; }

        public async Task<bool> Invoke(HttpContext context)
        {
            if (_discoveryOptions == null)
            {
                return false;
            }

            var request = context.Request;
            //For performance reasons, we need to put this functionality in the else
            var isSwitchNode = request.Cookies.TryGetValue(CookieNodeName, out var requestNodeName);
            var isCurrentNode = _discoveryOptions.NodeName == requestNodeName;

            if (!isSwitchNode || isCurrentNode)
            {
                return false;
            }

            _logger.LogDebug("start calling remote endpoint...");

            var node = await _discoveryProvider.GetNode(requestNodeName);
            if (node != null)
            {
                try
                {
                    DownstreamRequest = await _requestMapper.Map(request);

                    SetDownStreamRequestUri(node, request.Path.Value, request.QueryString.Value);

                    var response = await _requester.GetResponse(DownstreamRequest);

                    await SetResponseOnHttpContext(context, response);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            else
            {
                context.Response.Cookies.Delete(CookieNodeName);
                return false;
            }

            return false;
        }

        private async Task SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response)
        {
            foreach (var httpResponseHeader in response.Content.Headers)
            {
                AddHeaderIfDoesntExist(context, httpResponseHeader);
            }

            var content = await response.Content.ReadAsByteArrayAsync();

            AddHeaderIfDoesntExist(context,
                new KeyValuePair<string, IEnumerable<string>>("Content-Length", new[] { content.Length.ToString() }));

            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state;

                httpContext.Response.StatusCode = (int)response.StatusCode;

                return Task.CompletedTask;
            }, context);

            await using Stream stream = new MemoryStream(content);
            if (response.StatusCode != HttpStatusCode.NotModified)
            {
                await stream.CopyToAsync(context.Response.Body);
            }
        } 

        private void SetDownStreamRequestUri(Node node, string requestPath, string queryString)
        {
            var uriBuilder = new UriBuilder("http://", node.Address, node.Port, requestPath, queryString);
            DownstreamRequest.RequestUri = uriBuilder.Uri;
        }

        private static void AddHeaderIfDoesntExist(HttpContext context,
            KeyValuePair<string, IEnumerable<string>> httpResponseHeader)
        {
            if (!context.Response.Headers.ContainsKey(httpResponseHeader.Key))
            {
                context.Response.Headers.Add(httpResponseHeader.Key,
                    new StringValues(httpResponseHeader.Value.ToArray()));
            }
        }
    }
}