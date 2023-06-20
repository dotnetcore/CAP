// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using k8s;

namespace DotNetCore.CAP.Dashboard.K8s
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Represents all the option you can use to configure the k8s discovery.
    /// </summary>
    public class K8sDiscoveryOptions
    {
        public KubernetesClientConfiguration K8SClientConfig { get; set; }

        public K8sDiscoveryOptions()
        {
            K8SClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
        }
    }
}