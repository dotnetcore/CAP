// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery;

public class ConsulNodeDiscoveryProvider : INodeDiscoveryProvider
{
    private readonly ConsulDiscoveryOptions _discoveryOptions;
    private readonly ILogger<ConsulNodeDiscoveryProvider> _logger;

    public ConsulNodeDiscoveryProvider(ILoggerFactory logger, ConsulDiscoveryOptions options)
    {
        _logger = logger.CreateLogger<ConsulNodeDiscoveryProvider>();
        _discoveryOptions = options;
    }

    public async Task<Node> GetNode(string nodeName, string ns, CancellationToken cancellationToken = default)
    {
        try
        {
            using var consul = new ConsulClient(config =>
            {
                config.WaitTime = TimeSpan.FromSeconds(5);
                config.Address =
                    new Uri(
                        $"http://{_discoveryOptions.DiscoveryServerHostName}:{_discoveryOptions.DiscoveryServerPort}");
            });
            var serviceCatalog = await consul.Catalog.Service(nodeName, "CAP", cancellationToken);
            if (serviceCatalog.StatusCode == HttpStatusCode.OK)
                return serviceCatalog.Response.Select(info => new Node
                {
                    Id = info.ServiceID,
                    Name = info.ServiceName,
                    Address = info.ServiceAddress,
                    Port = info.ServicePort,
                    Tags = string.Join(", ", info.ServiceTags)
                }).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Get consul nodes raised an exception. Exception:{ex.Message}");
        }

        return null;
    }


    public async Task<IList<Node>> GetNodes(string ns, CancellationToken cancellationToken)
    {
        try
        {
            var nodes = new List<Node>();

            using var consul = new ConsulClient(config =>
            {
                config.WaitTime = TimeSpan.FromSeconds(5);
                config.Address =
                    new Uri(
                        $"http://{_discoveryOptions.DiscoveryServerHostName}:{_discoveryOptions.DiscoveryServerPort}");
            });

            var services = await consul.Catalog.Services(cancellationToken);

            foreach (var service in services.Response)
            {
                var serviceInfo = consul.Catalog.Service(service.Key, "CAP", cancellationToken).GetAwaiter()
                    .GetResult();
                var node = serviceInfo.Response.Select(info => new Node
                {
                    Id = info.ServiceID,
                    Name = info.ServiceName,
                    Address = "http://" + info.ServiceAddress,
                    Port = info.ServicePort,
                    Tags = string.Join(", ", info.ServiceTags)
                }).ToList();

                nodes.AddRange(node);
            }

            CapCache.Global.AddOrUpdate("cap.nodes.count", nodes.Count, TimeSpan.FromSeconds(60), true);

            return nodes;
        }
        catch (Exception ex)
        {
            CapCache.Global.AddOrUpdate("cap.nodes.count", 0, TimeSpan.FromSeconds(20));

            _logger.LogError(
                $"Get consul nodes raised an exception. Exception:{ex.Message},{ex.InnerException.Message}");
            return null;
        }
    }

    public async Task RegisterNode(CancellationToken cancellationToken)
    {
        try
        {
            var healthCheck = new AgentServiceCheck
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
                Interval = TimeSpan.FromSeconds(10),
                Status = HealthStatus.Passing
            };

            if (_discoveryOptions.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                healthCheck.HTTP =
                    $"http://{_discoveryOptions.CurrentNodeHostName}:{_discoveryOptions.CurrentNodePort}{_discoveryOptions.MatchPath}/api/health";
            else if (_discoveryOptions.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                healthCheck.TCP = $"{_discoveryOptions.CurrentNodeHostName}:{_discoveryOptions.CurrentNodePort}";

            var tags = new[] { "CAP", "Client", "Dashboard" };
            if (_discoveryOptions.CustomTags != null && _discoveryOptions.CustomTags.Length > 0)
                tags = tags.Union(_discoveryOptions.CustomTags).ToArray();

            using var consul = new ConsulClient(config =>
            {
                config.WaitTime = TimeSpan.FromSeconds(5);
                config.Address =
                    new Uri(
                        $"http://{_discoveryOptions.DiscoveryServerHostName}:{_discoveryOptions.DiscoveryServerPort}");
            });

            var result = await consul.Agent.ServiceRegister(new AgentServiceRegistration
            {
                ID = _discoveryOptions.NodeId,
                Name = _discoveryOptions.NodeName,
                Address = _discoveryOptions.CurrentNodeHostName,
                Port = _discoveryOptions.CurrentNodePort,
                Tags = tags,
                Check = healthCheck
            }, cancellationToken);

            if (result.StatusCode == HttpStatusCode.OK) _logger.LogInformation("Consul node register success!");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Get consul nodes raised an exception. Exception:{ex.Message},{ex.InnerException.Message}");
        }
    }
}