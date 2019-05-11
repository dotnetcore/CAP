// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.NodeDiscovery
{
    public class ConsulNodeDiscoveryProvider : INodeDiscoveryProvider, IDisposable
    {
        private readonly ILogger<ConsulNodeDiscoveryProvider> _logger;
        private readonly DiscoveryOptions _options;
        private ConsulClient _consul;

        public ConsulNodeDiscoveryProvider(ILoggerFactory logger, DiscoveryOptions options)
        {
            _logger = logger.CreateLogger<ConsulNodeDiscoveryProvider>();
            _options = options;

            InitClient();
        }

        public void Dispose()
        {
            _consul.Dispose();
        }

        public async Task<IList<Node>> GetNodes()
        {
            try
            {
                var nodes = new List<Node>();
                var services = await _consul.Catalog.Services();
                foreach (var service in services.Response)
                {
                    var serviceInfo = await _consul.Catalog.Service(service.Key);
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

        public Task RegisterNode()
        {
            try
            {
                return _consul.Agent.ServiceRegister(new AgentServiceRegistration
                {
                    ID = _options.NodeId,
                    Name = _options.NodeName,
                    Address = _options.CurrentNodeHostName,
                    Port = _options.CurrentNodePort,
                    Tags = new[] {"CAP", "Client", "Dashboard"},
                    Check = new AgentServiceCheck
                    {
                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
                        Interval = TimeSpan.FromSeconds(10),
                        Status = HealthStatus.Passing,
                        HTTP =
                            $"http://{_options.CurrentNodeHostName}:{_options.CurrentNodePort}{_options.MatchPath}/health"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Get consul nodes raised an exception. Exception:{ex.Message},{ex.InnerException.Message}");
                return null;
            }
        }

        private void InitClient()
        {
            _consul = new ConsulClient(config =>
            {
                config.WaitTime = TimeSpan.FromSeconds(5);
                config.Address = new Uri($"http://{_options.DiscoveryServerHostName}:{_options.DiscoveryServerPort}");
            });
        }
    }
}