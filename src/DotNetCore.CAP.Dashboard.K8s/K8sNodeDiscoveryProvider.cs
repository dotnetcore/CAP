// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Dashboard.NodeDiscovery;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.K8s;

// ReSharper disable once InconsistentNaming
public class K8sNodeDiscoveryProvider : INodeDiscoveryProvider
{
    const string TagPrefix = "dotnetcore.cap";
    private readonly ILogger<ConsulNodeDiscoveryProvider> _logger;
    private readonly K8sDiscoveryOptions _options;

    public K8sNodeDiscoveryProvider(ILoggerFactory logger, K8sDiscoveryOptions options)
    {
        _logger = logger.CreateLogger<ConsulNodeDiscoveryProvider>();
        _options = options;
    }

    public async Task<Node?> GetNode(string svcName, string ns, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new Kubernetes(_options.K8SClientConfig);
            var service =
                await client.CoreV1.ReadNamespacedServiceAsync(svcName, ns, cancellationToken: cancellationToken);

            return new Node
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

    public async Task<IList<Node>> GetNodes(string? ns, CancellationToken cancellationToken)
    {
        try
        {
            ns = _options.K8SClientConfig.Namespace;

            if (ns == null) return new List<Node>();

            var nodes = await ListServices(ns);

            CapCache.Global.AddOrUpdate("cap.nodes.count", nodes.Count, TimeSpan.FromSeconds(60), true);

            return nodes;
        }
        catch (Exception ex)
        {
            CapCache.Global.AddOrUpdate("cap.nodes.count", 0, TimeSpan.FromSeconds(20));

            _logger.LogError(
                $"Get k8s services raised an exception. Exception:{ex.Message},{ex.InnerException?.Message}");
            return new List<Node>();
        }
    }

    public async Task<List<string>> GetNamespaces(CancellationToken cancellationToken)
    {
        var client = new Kubernetes(_options.K8SClientConfig);
        try
        {
            var namespaces = await client.ListNamespaceAsync(cancellationToken: cancellationToken);
            return namespaces.Items.Select(x => x.Name()).ToList();
        }
        catch (Exception)
        {
            if (string.IsNullOrEmpty(_options.K8SClientConfig.Namespace))
            {
                return new List<string>();
            }

            return new List<string>() { _options.K8SClientConfig.Namespace };
        }
    }

    public async Task<IList<Node>> ListServices(string? ns = null)
    {
        var client = new Kubernetes(_options.K8SClientConfig);
        var services = await client.CoreV1.ListNamespacedServiceAsync(ns);

        var result = new List<Node>();
        foreach (var service in services.Items)
        {
            IDictionary<string, string> tags = service.Labels();

            var filterResult = FilterNodesByTags(tags);

            if (filterResult.hideNode)
            {
                continue;
            }

            int port = GetPortByNameOrIndex(service, filterResult.filteredPortName, filterResult.filteredPortIndex);

            result.Add(new Node
            {
                Id = service.Uid(),
                Name = service.Name(),
                Address = "http://" + service.Metadata.Name + "." + ns,
                Port = port,
                Tags = string.Join(',', service.Labels()?.Select(x => x.Key + ":" + x.Value) ?? Array.Empty<string>())
            });
        }

        return result;
    }


    /// <summary>
    /// Given the filters (filterPortName and filterPortIndex) this method will try to find the port 
    /// filterPortName is checked first and if no port is found by that name filterPortIndex is checked
    /// Returns 0 if service is null or no port specified in the service 
    /// Returns the portNumber of the matched port if something is found 
    /// </summary>
    /// <param name="service"></param>
    /// <param name="filterPortName"></param>
    /// <param name="filterPortIndex"></param>
    /// <returns></returns>
    private static int GetPortByNameOrIndex(V1Service? service, string filterPortName, int filterPortIndex)
    {
        if (service is null)
        {
            return 0;
        }

        if (service.Spec.Ports is null)
        {
            return 0;
        }

        var result = GetPortByName(service.Spec.Ports, filterPortName);
        if (result > 0)
        {
            return result;
        }

        result = GetPortByIndex(service.Spec.Ports, filterPortIndex);
        if (result > 0)
        {
            return result;
        }

        return service.Spec.Ports[0]?.Port ?? 0;
    }

    /// <summary>
    /// This method will try to find a port with the specified Index 
    /// Will Return 0 if index is not found  
    /// Returns: port number or 0 if not found 
    /// </summary>
    /// <param name="servicePorts"></param>
    /// <param name="filterIndex"></param>
    /// <returns></returns>
    private static int GetPortByIndex(IList<V1ServicePort> servicePorts, int filterIndex)
    {

        var portByIndex = servicePorts.ElementAtOrDefault(filterIndex);
        if (portByIndex is null)
        {
            return 0;
        }

        return portByIndex.Port;
    }

    /// <summary>
    /// This method will try to find a port with the specified name 
    /// Will Return 0 if none found 
    /// Returns: port number or 0 if not found 
    /// </summary>
    /// <param name="service"></param>
    /// <param name="portName"></param>
    /// <returns></returns>
    private static int GetPortByName(IList<V1ServicePort> servicePorts, string portName)
    {
        if (!string.IsNullOrEmpty(portName))
        {
            return 0;
        }

        var portByName = servicePorts.FirstOrDefault(p => p.Name == portName);
        if (portByName is null)
        {
            return 0;
        }

        return portByName.Port;
    }

    private record TagFilterResult(bool hideNode, int filteredPortIndex, string filteredPortName);

    private TagFilterResult FilterNodesByTags(IDictionary<string, string> tags)
    {
        var isNodeHidden = _options.ShowOnlyExplicitVisibleNodes;
        var filteredPortIndex = 0; //this the default port index 
        var filteredPortName = string.Empty; //this the default port index 

        if (tags == null)
        {

            return new TagFilterResult(isNodeHidden, filteredPortIndex, filteredPortName);
        }


        foreach (var tag in tags)
        {
            //look out for dotnetcore.cap tags 
            //based on value will do conditions 
            var isCapTag = tag.Key.StartsWith(TagPrefix, StringComparison.InvariantCultureIgnoreCase);

            if (!isCapTag)
            {
                continue;
            }

            string capTagScope = GetTagScope(tag);

            //check for hide Tag
            if (IsNodeHidden(tag, capTagScope))
            {
                return new TagFilterResult(true, filteredPortIndex, filteredPortName);
            }
            else
            {
                isNodeHidden = false;
            }

            //check for portIndex-X tag.
            //If multiple tags with portIndex are found only the last has power
            var hasNewPort = CheckFilterPortIndex(tag, capTagScope);
            if (hasNewPort.HasValue)
            {
                filteredPortIndex = hasNewPort.Value;
            }

            //check for portName-X tag.
            //If multiple tags with portName are found only the last has power
            if (capTagScope.Equals("portName", StringComparison.InvariantCultureIgnoreCase))
            {
                filteredPortName = tag.Value;
            }

        }

        return new TagFilterResult(isNodeHidden, filteredPortIndex, filteredPortName);
    }

    private int? CheckFilterPortIndex(KeyValuePair<string, string> tag, string capTagScope)
    {
        if (!capTagScope.Equals("portIndex", StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        var hasPort = int.TryParse(tag.Value, out int filterPort);
        if (!hasPort)
        {
            return null;
        }

        return filterPort;
    }

    private bool IsNodeHidden(KeyValuePair<string, string> tag, string capTagScope)
    {
        if (!capTagScope.Equals("visibility", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        //We will not show the node if the tag value is "dotnetcore.cap.visibility:hide"
        if (tag.Value.Equals("hide", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        //We will not show the node if the K8s Dashboard option is 
        //ShowOnlyExplicitVisibleNodes=True
        //and the tag value is NOT "dotnetcore.cap.visibility:show"
        if (!_options.ShowOnlyExplicitVisibleNodes)
        {
            return false;
        }

        return !tag.Value.Equals("show", StringComparison.InvariantCultureIgnoreCase);
    }

    private string GetTagScope(KeyValuePair<string, string> tag)
    {
        var capTagScope = tag.Key.Replace(TagPrefix, "", StringComparison.InvariantCultureIgnoreCase);
        if (capTagScope.StartsWith("."))
        {
            capTagScope = capTagScope.Substring(1);
        }

        return capTagScope;
    }
}