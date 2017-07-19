namespace DotNetCore.CAP.Processor
{
    public interface IDispatcher : IProcessor
    {
        bool Waiting { get; }
    }
}