namespace DotNetCore.CAP.NodeDiscovery
{
    internal interface IDiscoveryProviderFactory
    {
        INodeDiscoveryProvider Create(DiscoveryOptions options);
    }
}