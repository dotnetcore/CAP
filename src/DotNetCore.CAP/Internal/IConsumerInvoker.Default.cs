using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    internal class DefaultConsumerInvoker : IConsumerInvoker
    {
        private readonly ILogger _logger;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessagePacker _messagePacker;

        public DefaultConsumerInvoker(ILogger logger,
            IServiceProvider serviceProvider,
            IMessagePacker messagePacker,
            IModelBinderFactory modelBinderFactory)
        {
            _modelBinderFactory = modelBinderFactory;
            _serviceProvider = serviceProvider;
            _messagePacker = messagePacker;
            _logger = logger;
        }

        public async Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context)
        {
            _logger.LogDebug("Executing consumer Topic: {0}", context.ConsumerDescriptor.MethodInfo.Name);

            var executor = ObjectMethodExecutor.Create(
                context.ConsumerDescriptor.MethodInfo,
                context.ConsumerDescriptor.ImplTypeInfo);

            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var serviceType = context.ConsumerDescriptor.ImplTypeInfo.AsType();
                var obj = ActivatorUtilities.GetServiceOrCreateInstance(provider, serviceType);

                var jsonContent = context.DeliverMessage.Content;
                var message = _messagePacker.UnPack(jsonContent);

                object resultObj;
                if (executor.MethodParameters.Length > 0)
                    resultObj = await ExecuteWithParameterAsync(executor, obj, message.Content, context.DeliverMessage.Name);
                else
                    resultObj = await ExecuteAsync(executor, obj);
                return new ConsumerExecutedResult(resultObj, message.Id, message.CallbackName);
            }
        }

        private async Task<object> ExecuteAsync(ObjectMethodExecutor executor, object @class)
        {
            if (executor.IsMethodAsync)
                return await executor.ExecuteAsync(@class);
            return executor.Execute(@class);
        }

        private async Task<object> ExecuteWithParameterAsync(ObjectMethodExecutor executor,
            object @class, string parameterString, string topic)
        {
            var firstParameter = executor.MethodParameters[0]; //topic
            var secondParameter = executor.MethodParameters[1]; // content
            try
            {
                var binder1 = _modelBinderFactory.CreateBinder(firstParameter);
                var bindResult1 = await binder1.BindModelAsync(topic);
                var binder2 = _modelBinderFactory.CreateBinder(secondParameter);
                var bindResult2 = await binder2.BindModelAsync(parameterString);
                if (bindResult1.IsSuccess && bindResult2.IsSuccess)
                {
                    if (executor.IsMethodAsync)
                        return await executor.ExecuteAsync(@class, new object[] { bindResult1.Model, bindResult2.Model });
                    return executor.Execute(@class, new object[] { bindResult1.Model, bindResult2.Model });
                }
                throw new MethodBindException(
                    $"Parameters:{firstParameter.Name} bind failed! ParameterString is: {parameterString} ");
            }
            catch (FormatException ex)
            {
                _logger.ModelBinderFormattingException(executor.MethodInfo?.Name, firstParameter.Name, parameterString,
                    ex);
                return null;
            }
        }
    }
}