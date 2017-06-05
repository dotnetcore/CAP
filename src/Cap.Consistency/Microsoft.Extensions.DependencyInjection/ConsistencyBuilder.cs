namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Used to verify Consistency service was called on a ServiceCollection
    /// </summary>
    public class ConsistencyMarkerService { }

    public class ConsistencyBuilder
    {
        public ConsistencyBuilder(IServiceCollection services) {
            Services = services;
        }

        public IServiceCollection Services { get; private set; }
    }
}