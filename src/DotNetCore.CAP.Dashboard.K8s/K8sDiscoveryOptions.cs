// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using k8s;

namespace DotNetCore.CAP.Dashboard.K8s;

// ReSharper disable once InconsistentNaming
/// <summary>
/// Represents all the option you can use to configure the k8s discovery.
/// </summary>
public class K8sDiscoveryOptions
{
    public K8sDiscoveryOptions()
    {
        K8SClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
        ShowOnlyExplicitVisibleNodes = true;
    }

    public KubernetesClientConfiguration K8SClientConfig { get; set; }

    /// <summary>
    /// If this is set to TRUE will make all nodes hidden by default. Only kubernetes services 
    /// with label "dotnetcore.cap.visibility:show" will be listed in the nodes section.
    /// </summary>
    public bool ShowOnlyExplicitVisibleNodes { get; set; }
}