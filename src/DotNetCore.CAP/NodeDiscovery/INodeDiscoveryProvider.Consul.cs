using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Consul;
using System.Security.Cryptography;

namespace DotNetCore.CAP.NodeDiscovery
{
    class ConsulNodeDiscoveryProvider : INodeDiscoveryProvider, IDisposable
    {
        private ConsulClient _consul;
        private readonly DiscoveryOptions _options;

        public ConsulNodeDiscoveryProvider(DiscoveryOptions options)
        {
            _options = options;

            InitClient();
        }

        public void InitClient()
        {
            _consul = new ConsulClient(config =>
            {
                config.Address = new Uri($"http://{_options.DiscoveryServerHostName}:{_options.DiscoveryServerProt}");
            });
        }

        public async Task<IList<Node>> GetNodes()
        {
            var services = await _consul.Agent.Services();

            var nodes = services.Response.Select(x => new Node
            {
                Name = x.Value.Service,
                Address = x.Value.Address,
                Port = x.Value.Port,
                Tags = string.Join(", ", x.Value.Tags)
            });

            return nodes.ToList();
        }

        public Task RegisterNode()
        {
            return _consul.Agent.ServiceRegister(new AgentServiceRegistration
            {
                Name = _options.NodeName,
                Address = _options.CurrentNodeHostName,
                Port = _options.CurrentNodePort,
                Tags = new string[] { "CAP", "Client", "Dashboard" },
                Check = new AgentServiceCheck
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
                    Interval = TimeSpan.FromSeconds(10),
                    Status = HealthStatus.Passing,
                    HTTP = $"http://{_options.CurrentNodeHostName}:{_options.CurrentNodePort}{_options.MatchPath}/health"
                }
            });
        }

        public void Dispose()
        {
            _consul.Dispose();
        }
    }
}
