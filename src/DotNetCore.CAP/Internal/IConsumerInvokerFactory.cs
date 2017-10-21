namespace DotNetCore.CAP.Internal
{
    public interface IConsumerInvokerFactory
    {
        IConsumerInvoker CreateInvoker();
    }
}