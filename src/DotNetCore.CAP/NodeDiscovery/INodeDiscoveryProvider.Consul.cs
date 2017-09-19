using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Consul;

namespace DotNetCore.CAP.NodeDiscovery
{
    class ConsulNodeDiscoveryProvider : INodeDiscoveryProvider
    {
        private readonly string _hostName;
        private readonly int _port;

        private readonly ConsulClient _consul;

        public ConsulNodeDiscoveryProvider(string hostName, int port)
        {
            _hostName = hostName;
            _port = port;

            _consul = new ConsulClient(config =>
            {
                config.Address = new Uri($"http://{_hostName}:{_port}");
            });
        }

        public async Task<IList<Node>> GetNodes()
        {
            var members = await _consul.Agent.Members(false);

            var nodes = members.Response.Select(x => new Node
            {
                Address = x.Addr,
                Name = x.Name
            });

            return nodes.ToList();
        }

        public Task RegisterNode(string address, int port)
        {
            //CatalogRegistration registration = new CatalogRegistration();
            //registration.Node = "CAP";
            //registration.Address = "192.168.2.55";
            //registration.Service = new AgentService
            //{
            //    Port = 5000,
            //    Service = "CAP.Test.Service"
            //};
            //return _consul.Catalog.Register(registration);

            return _consul.Agent.ServiceRegister(new AgentServiceRegistration
            {
                Name = "CAP",
                Port = port,
                Address = address,
                Tags = new string[] { "CAP", "Client", "Dashboard" },
                Check = new AgentServiceCheck
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
                    Interval = TimeSpan.FromSeconds(10),
                    Status = HealthStatus.Passing,
                    HTTP = "/CAP"
                }
            });
        }
    }
}
