// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using k8s;

namespace DotNetCore.CAP.Dashboard.K8s
{
    public class K8SDiscoveryOptions
    {
        public KubernetesClientConfiguration K8SClientConfig { get; set; }

        public K8SDiscoveryOptions()
        {
            K8SClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
        }
    }
}