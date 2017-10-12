using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    public class DefaultSubscriberExecutor : ISubscriberExecutor
    {
        private readonly IConsumerInvokerFactory _consumerInvokerFactory;
        private readonly ILogger<DefaultSubscriberExecutor> _logger;
        private readonly MethodMatcherCache _selector;

        public DefaultSubscriberExecutor(MethodMatcherCache selector,
            IConsumerInvokerFactory consumerInvokerFactory,
            ILogger<DefaultSubscriberExecutor> logger)
        {
            _selector = selector;
            _consumerInvokerFactory = consumerInvokerFactory;
            _logger = logger;
        }


        public async Task<OperateResult> ExecuteAsync(CapReceivedMessage receivedMessage)
        {
            try
            {
                var executeDescriptorGroup = _selector.GetTopicExector(receivedMessage.Name);

                if (!executeDescriptorGroup.ContainsKey(receivedMessage.Group))
                {
                    var error = $"Topic:{receivedMessage.Name}, can not be found subscriber method.";
                    throw new SubscriberNotFoundException(error);
                }

                // If there are multiple consumers in the same group, we will take the first
                var executeDescriptor = executeDescriptorGroup[receivedMessage.Group][0];
                var consumerContext = new ConsumerContext(executeDescriptor, receivedMessage.ToMessageContext());

                await _consumerInvokerFactory.CreateInvoker(consumerContext).InvokeAsync();

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                _logger.ConsumerMethodExecutingFailed($"Group:{receivedMessage.Group}, Topic:{receivedMessage.Name}",
                    ex);

                return OperateResult.Failed(ex);
            }
        }
    }
}