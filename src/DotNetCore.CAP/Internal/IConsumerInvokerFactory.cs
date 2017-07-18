using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Internal
{
    public interface IConsumerInvokerFactory
    {
        IConsumerInvoker CreateInvoker(ConsumerContext actionContext);
    }
}