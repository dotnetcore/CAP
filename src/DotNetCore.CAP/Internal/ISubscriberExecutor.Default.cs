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
            if (!_selector.TryGetTopicExector(receivedMessage.Name, receivedMessage.Group,
                out var executor))
            {
                var error = "message can not be found subscriber. Message:" + receivedMessage;
                error += "\r\n  see: https://github.com/dotnetcore/CAP/issues/63";
                throw new SubscriberNotFoundException(error);
            }
            
            var consumerContext = new ConsumerContext(executor, receivedMessage.ToMessageContext());
            try
            {
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