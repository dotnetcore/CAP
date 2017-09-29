using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    public class DefaultConsumerInvoker : IConsumerInvoker
    {
        private readonly ConsumerContext _consumerContext;
        private readonly ObjectMethodExecutor _executor;
        private readonly ILogger _logger;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IServiceProvider _serviceProvider;

        public DefaultConsumerInvoker(ILogger logger,
            IServiceProvider serviceProvider,
            IModelBinderFactory modelBinderFactory,
            ConsumerContext consumerContext)
        {
            _modelBinderFactory = modelBinderFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _consumerContext = consumerContext;

            _executor = ObjectMethodExecutor.Create(_consumerContext.ConsumerDescriptor.MethodInfo,
                _consumerContext.ConsumerDescriptor.ImplTypeInfo);
        }

        public async Task InvokeAsync()
        {
            _logger.LogDebug("Executing consumer Topic: {0}", _consumerContext.ConsumerDescriptor.MethodInfo.Name);

            var serializer = _serviceProvider.GetService<IContentSerializer>();
            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var serviceType = _consumerContext.ConsumerDescriptor.ImplTypeInfo.AsType();
                var obj = ActivatorUtilities.GetServiceOrCreateInstance(provider, serviceType);

                var jsonContent = _consumerContext.DeliverMessage.Content;
                var message = serializer.DeSerialize<CapMessageDto>(jsonContent);

                object result;
                if (_executor.MethodParameters.Length > 0)
                    result = await ExecuteWithParameterAsync(obj, message.Content.ToString());
                else
                    result = await ExecuteAsync(obj);

                if (!string.IsNullOrEmpty(message.CallbackName))
                    await SentCallbackMessage(message.Id, message.CallbackName, result);
            }
        }

        private async Task<object> ExecuteAsync(object @class)
        {
            if (_executor.IsMethodAsync)
                return await _executor.ExecuteAsync(@class);
            return _executor.Execute(@class);
        }

        private async Task<object> ExecuteWithParameterAsync(object @class, string parameterString)
        {
            var firstParameter = _executor.MethodParameters[0];
            try
            {
                var binder = _modelBinderFactory.CreateBinder(firstParameter);
                var bindResult = await binder.BindModelAsync(parameterString);
                if (bindResult.IsSuccess)
                {
                    if (_executor.IsMethodAsync)
                        return await _executor.ExecuteAsync(@class, bindResult.Model);
                    return _executor.Execute(@class, bindResult.Model);
                }
                throw new MethodBindException(
                    $"Parameters:{firstParameter.Name} bind failed! ParameterString is: {parameterString} ");
            }
            catch (FormatException ex)
            {
                _logger.ModelBinderFormattingException(_executor.MethodInfo?.Name, firstParameter.Name, parameterString,
                    ex);
                return null;
            }
        }

        private async Task SentCallbackMessage(string messageId, string topicName, object bodyObj)
        {
            var callbackMessage = new CapMessageDto
            {
                Id = messageId,
                Content = bodyObj
            };

            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var publisher = provider.GetRequiredService<ICallbackPublisher>();

                var publishedMessage = new CapPublishedMessage
                {
                    Name = topicName,
                    Content = Helper.ToJson(callbackMessage),
                    StatusName = StatusName.Scheduled
                };
                await publisher.PublishAsync(publishedMessage);
            }
        }
    }
}