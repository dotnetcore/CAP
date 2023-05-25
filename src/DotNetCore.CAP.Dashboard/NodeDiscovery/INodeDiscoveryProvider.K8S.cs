// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    public class K8SNodeDiscoveryProvider : INodeDiscoveryProvider
    {
        private readonly ILogger<ConsulNodeDiscoveryProvider> _logger;
        private readonly KubernetesClientConfiguration _options;

        public K8SNodeDiscoveryProvider(ILoggerFactory logger, DiscoveryOptions options)
        {
            _logger = logger.CreateLogger<ConsulNodeDiscoveryProvider>();
            _options = options.K8SOptions;
        }

        public async Task<Node> GetNode(string svcName, string ns, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = new Kubernetes(_options);
                var service = await client.CoreV1.ReadNamespacedServiceAsync(svcName, ns, cancellationToken: cancellationToken);

                return new Node()
                {
                    Id = service.Uid(),
                    Name = service.Name(),
                    Address = "http://" + service.Metadata.Name + "." + ns,
                    Port = service.Spec.Ports?[0].Port ?? 0,
                    Tags = string.Join(',',
                        service.Labels()?.Select(x => x.Key + ":" + x.Value) ?? Array.Empty<string>())
                };
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
                if (ns == null) return new List<Node>();
                var client = new Kubernetes(_options);
                var services = await client.CoreV1.ListNamespacedServiceAsync(ns, cancellationToken: cancellationToken);

                var nodes = new List<Node>();
                foreach (var service in services.Items)
                {
                    nodes.Add(new Node()
                    {
                        Id = service.Uid(),
                        Name = service.Name(),
                        Address = "http://" + service.Metadata.Name + "." + ns,
                        Port = service.Spec.Ports?[0].Port ?? 0,
                        Tags = string.Join(',', service.Labels()?.Select(x => x.Key + ":" + x.Value) ?? Array.Empty<string>())
                    });
                }
                CapCache.Global.AddOrUpdate("cap.nodes.count", nodes.Count, TimeSpan.FromSeconds(60), true);

                return nodes;
            }
            catch (Exception ex)
            {
                CapCache.Global.AddOrUpdate("cap.nodes.count", 0, TimeSpan.FromSeconds(20));

                _logger.LogError(
                    $"Get consul nodes raised an exception. Exception:{ex.Message},{ex.InnerException?.Message}");
                return null;
            }
        }

        public async Task<IList<string>> GetNamespaces(CancellationToken cancellationToken)
        {
            var client = new Kubernetes(_options);
            var namespaces = await client.ListNamespaceAsync(cancellationToken: cancellationToken);
            return namespaces.Items.Select(x => x.Name()).ToList();
        }
    }
}
