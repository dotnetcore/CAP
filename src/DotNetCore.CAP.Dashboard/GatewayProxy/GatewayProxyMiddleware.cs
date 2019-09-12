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
using DotNetCore.CAP.NodeDiscovery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class GatewayProxyMiddleware
    {
        public const string NodeCookieName = "cap.node";
        private readonly ILogger _logger;

        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IRequestMapper _requestMapper;

        private INodeDiscoveryProvider _discoveryProvider;

        public GatewayProxyMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory,
            IRequestMapper requestMapper,
            IHttpRequester requester)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<GatewayProxyMiddleware>();
            _requestMapper = requestMapper;
            _requester = requester;
        }

        protected HttpRequestMessage DownstreamRequest { get; set; }

        public async Task Invoke(HttpContext context,
            DiscoveryOptions discoveryOptions,
            INodeDiscoveryProvider discoveryProvider)
        {
            _discoveryProvider = discoveryProvider;

            var request = context.Request;
            var pathMatch = discoveryOptions.MatchPath;
            var isCapRequest = request.Path.StartsWithSegments(new PathString(pathMatch));
            if (!isCapRequest)
            {
                await _next.Invoke(context);
            }
            else
            {
                //For performance reasons, we need to put this functionality in the else
                var isSwitchNode = request.Cookies.TryGetValue(NodeCookieName, out var requestNodeId);
                var isCurrentNode = discoveryOptions.NodeId == requestNodeId;
                var isNodesPage = request.Path.StartsWithSegments(new PathString(pathMatch + "/nodes"));

                if (!isSwitchNode || isCurrentNode || isNodesPage)
                {
                    await _next.Invoke(context);
                }
                else
                {
                    _logger.LogDebug("started calling gateway proxy middleware");

                    if (TryGetRemoteNode(requestNodeId, out var node))
                    {
                        try
                        {
                            DownstreamRequest = await _requestMapper.Map(request);

                            SetDownStreamRequestUri(node, request.Path.Value);

                            var response = await _requester.GetResponse(DownstreamRequest);

                            await SetResponseOnHttpContext(context, response);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                        }
                    }
                    else
                    {
                        context.Response.Cookies.Delete(NodeCookieName);
                        await _next.Invoke(context);
                    }
                }
            }
        }

        public async Task SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response)
        {
            foreach (var httpResponseHeader in response.Content.Headers)
            {
                AddHeaderIfDoesntExist(context, httpResponseHeader);
            }

            var content = await response.Content.ReadAsByteArrayAsync();

            AddHeaderIfDoesntExist(context,
                new KeyValuePair<string, IEnumerable<string>>("Content-Length", new[] {content.Length.ToString()}));

            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext) state;

                httpContext.Response.StatusCode = (int) response.StatusCode;

                return Task.CompletedTask;
            }, context);

            using (Stream stream = new MemoryStream(content))
            {
                if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    await stream.CopyToAsync(context.Response.Body);
                }
            }
        }

        private bool TryGetRemoteNode(string requestNodeId, out Node node)
        {
            var nodes = _discoveryProvider.GetNodes().GetAwaiter().GetResult();
            node = nodes.FirstOrDefault(x => x.Id == requestNodeId);
            return node != null;
        }

        private void SetDownStreamRequestUri(Node node, string requestPath)
        {
            var uriBuilder = new UriBuilder("http://", node.Address, node.Port, requestPath);
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