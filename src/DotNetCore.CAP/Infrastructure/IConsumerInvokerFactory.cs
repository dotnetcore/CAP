using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Infrastructure
{
    public interface IConsumerInvokerFactory
    {
        IConsumerInvoker CreateInvoker(ConsumerContext actionContext);
    }
}