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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DotNetCore.CAP.Dashboard.GatewayProxy;

public class GatewayProxyAgent
{
    public const string CookieNodeName = "cap.node";
    public const string CookieNodeNsName = "cap.node.ns";

    private readonly ConsulDiscoveryOptions _consulDiscoveryOptions;
    private readonly INodeDiscoveryProvider _discoveryProvider;
    private readonly ILogger _logger;

    private readonly IHttpRequester _requester;
    private readonly IRequestMapper _requestMapper;

    public GatewayProxyAgent(
        ILoggerFactory loggerFactory,
        IRequestMapper requestMapper,
        IHttpRequester requester,
        IServiceProvider serviceProvider,
        INodeDiscoveryProvider discoveryProvider)
    {
        _logger = loggerFactory.CreateLogger<GatewayProxyAgent>();
        _requestMapper = requestMapper;
        _requester = requester;
        _discoveryProvider = discoveryProvider;
        _consulDiscoveryOptions = serviceProvider.GetService<ConsulDiscoveryOptions>();
    }

    protected HttpRequestMessage DownstreamRequest { get; set; }

    public async Task<bool> Invoke(HttpContext context)
    {
        var request = context.Request;
        var isSwitchNode = request.Cookies.TryGetValue(CookieNodeName, out var requestNodeName);
        if (!isSwitchNode) return false;

        _logger.LogDebug("start calling remote endpoint...");

        Node node;
        if (_consulDiscoveryOptions == null) // it's k8s
        {
            if (request.Cookies.TryGetValue(CookieNodeNsName, out var ns))
            {
                if (CapCache.Global.TryGet(requestNodeName + ns, out var nodeObj))
                {
                    node = (Node)nodeObj;
                }
                else
                {
                    node = await _discoveryProvider.GetNode(requestNodeName, ns);
                    CapCache.Global.AddOrUpdate(requestNodeName + ns, node);
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (_consulDiscoveryOptions.NodeName == requestNodeName) return false;

            if (CapCache.Global.TryGet(requestNodeName, out var nodeObj))
            {
                node = (Node)nodeObj;
            }
            else
            {
                node = await _discoveryProvider.GetNode(requestNodeName);
                CapCache.Global.AddOrUpdate(requestNodeName, node);
            }
        }

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
        if (response.StatusCode != HttpStatusCode.NotModified) await stream.CopyToAsync(context.Response.Body);
    }

    private void SetDownStreamRequestUri(Node node, string requestPath, string queryString)
    {
        UriBuilder uriBuilder;
        if (node.Address.StartsWith("http"))
            uriBuilder = new UriBuilder(node.Address + requestPath + queryString);
        else
            uriBuilder = new UriBuilder("http://", node.Address, node.Port, requestPath, queryString);
        
        if (node.Port > 0)
            uriBuilder.Port = node.Port;
        
        DownstreamRequest.RequestUri = uriBuilder.Uri;
    }

    private static void AddHeaderIfDoesntExist(HttpContext context,
        KeyValuePair<string, IEnumerable<string>> httpResponseHeader)
    {
        if (!context.Response.Headers.ContainsKey(httpResponseHeader.Key))
            context.Response.Headers.Add(httpResponseHeader.Key,
                new StringValues(httpResponseHeader.Value.ToArray()));
    }
}
