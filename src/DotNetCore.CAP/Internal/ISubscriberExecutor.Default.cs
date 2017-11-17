using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    internal class DefaultSubscriberExecutor : ISubscriberExecutor
    {
        private readonly ICallbackMessageSender _callbackMessageSender;
        private readonly ILogger<DefaultSubscriberExecutor> _logger;
        private readonly MethodMatcherCache _selector;

        private IConsumerInvoker Invoker { get; }

        public DefaultSubscriberExecutor(MethodMatcherCache selector,
            IConsumerInvokerFactory consumerInvokerFactory,
            ICallbackMessageSender callbackMessageSender,
            ILogger<DefaultSubscriberExecutor> logger)
        {
            _selector = selector;
            _callbackMessageSender = callbackMessageSender;
            _logger = logger;

            Invoker = consumerInvokerFactory.CreateInvoker();
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

                var ret = await Invoker.InvokeAsync(consumerContext);

                if (!string.IsNullOrEmpty(ret.CallbackName))
                    await _callbackMessageSender.SendAsync(ret.MessageId, ret.CallbackName, ret.Result);

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