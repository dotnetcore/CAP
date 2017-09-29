using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;

namespace DotNetCore.CAP.NodeDiscovery
{
    public class ConsulNodeDiscoveryProvider : INodeDiscoveryProvider, IDisposable
    {
        private readonly DiscoveryOptions _options;
        private ConsulClient _consul;

        public ConsulNodeDiscoveryProvider(DiscoveryOptions options)
        {
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
                var services = await _consul.Agent.Services();

                var nodes = services.Response.Select(x => new Node
                {
                    Id = x.Key,
                    Name = x.Value.Service,
                    Address = x.Value.Address,
                    Port = x.Value.Port,
                    Tags = string.Join(", ", x.Value.Tags)
                });
                var nodeList = nodes.ToList();

                CapCache.Global.AddOrUpdate("cap.nodes.count", nodeList.Count, TimeSpan.FromSeconds(30), true);

                return nodeList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task RegisterNode()
        {
            return _consul.Agent.ServiceRegister(new AgentServiceRegistration
            {
                ID = _options.NodeId.ToString(),
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

        public void InitClient()
        {
            _consul = new ConsulClient(config =>
            {
                config.WaitTime = TimeSpan.FromSeconds(5);
                config.Address = new Uri($"http://{_options.DiscoveryServerHostName}:{_options.DiscoveryServerPort}");
            });
        }
    }
}