// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    public class ConsulNodeDiscoveryProvider : INodeDiscoveryProvider
    {
        private readonly ILogger<ConsulNodeDiscoveryProvider> _logger;
        private readonly DiscoveryOptions _options;

        public ConsulNodeDiscoveryProvider(ILoggerFactory logger, DiscoveryOptions options)
        {
            _logger = logger.CreateLogger<ConsulNodeDiscoveryProvider>();
            _options = options;
        }

        public IList<Node> GetNodes(CancellationToken cancellationToken)
        {
            try
            {
                var nodes = new List<Node>();

                using var consul = new ConsulClient(config =>
                {
                    config.WaitTime = TimeSpan.FromSeconds(5);
                    config.Address = new Uri($"http://{_options.DiscoveryServerHostName}:{_options.DiscoveryServerPort}");
                });

                var services = consul.Catalog.Services(cancellationToken).GetAwaiter().GetResult();

                foreach (var service in services.Response)
                {
                    var serviceInfo = consul.Catalog.Service(service.Key, cancellationToken).GetAwaiter().GetResult();
                    var node = serviceInfo.Response.SkipWhile(x => !x.ServiceTags.Contains("CAP"))
                        .Select(info => new Node
                        {
                            Id = info.ServiceID,
                            Name = info.ServiceName,
                            Address = info.ServiceAddress,
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

                if (_options.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                    healthCheck.HTTP = $"http://{_options.CurrentNodeHostName}:{_options.CurrentNodePort}{_options.MatchPath}/api/health";
                else if (_options.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    healthCheck.TCP = $"{_options.CurrentNodeHostName}:{_options.CurrentNodePort}";

                var tags = new[] { "CAP", "Client", "Dashboard" };
                if (_options.CustomTags != null && _options.CustomTags.Length > 0)
                {
                    tags = tags.Union(_options.CustomTags).ToArray();
                }

                using var consul = new ConsulClient(config =>
                {
                    config.WaitTime = TimeSpan.FromSeconds(5);
                    config.Address = new Uri($"http://{_options.DiscoveryServerHostName}:{_options.DiscoveryServerPort}");
                });

                var result = await consul.Agent.ServiceRegister(new AgentServiceRegistration
                {
                    ID = _options.NodeId,
                    Name = _options.NodeName,
                    Address = _options.CurrentNodeHostName,
                    Port = _options.CurrentNodePort,
                    Tags = tags,
                    Check = healthCheck
                }, cancellationToken);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("Consul node register success!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get consul nodes raised an exception. Exception:{ex.Message},{ex.InnerException.Message}");
            }
        }
    }
}
