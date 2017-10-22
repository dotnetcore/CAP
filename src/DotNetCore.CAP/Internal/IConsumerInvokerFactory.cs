namespace DotNetCore.CAP.Internal
{
    internal interface IConsumerInvokerFactory
    {
        IConsumerInvoker CreateInvoker();
    }
}