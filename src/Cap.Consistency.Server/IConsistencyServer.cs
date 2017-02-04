namespace Cap.Consistency.Server
{
    public interface IConsistencyServer
    {
        ConsistencyServerOptions Options { get; }

        void Run();
    }
}