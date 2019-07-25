// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
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
        private readonly IMessagePacker _messagePacker;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IServiceProvider _serviceProvider;

        public DefaultConsumerInvoker(ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            IMessagePacker messagePacker,
            IModelBinderFactory modelBinderFactory)
        {
            _modelBinderFactory = modelBinderFactory;
            _serviceProvider = serviceProvider;
            _messagePacker = messagePacker;
            _logger = loggerFactory.CreateLogger<DefaultConsumerInvoker>();
        }

        public async Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Executing consumer Topic: {0}", context.ConsumerDescriptor.MethodInfo.Name);

            var executor = ObjectMethodExecutor.Create(
                context.ConsumerDescriptor.MethodInfo,
                context.ConsumerDescriptor.ImplTypeInfo);

            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var srvType = context.ConsumerDescriptor.ServiceTypeInfo?.AsType();
                var implType = context.ConsumerDescriptor.ImplTypeInfo.AsType();

                object obj = null;

                if (srvType != null)
                {
                    obj = provider.GetServices(srvType).FirstOrDefault(o => o.GetType() == implType);
                }

                if (obj == null)
                {
                    obj = ActivatorUtilities.GetServiceOrCreateInstance(provider, implType);
                }

                var jsonContent = context.DeliverMessage.Content;
                var message = _messagePacker.UnPack(jsonContent);

                object resultObj;
                if (executor.MethodParameters.Length > 0)
                {
                    resultObj = await ExecuteWithParameterAsync(executor, obj, message.Content);
                }
                else
                {
                    resultObj = await ExecuteAsync(executor, obj);
                }

                return new ConsumerExecutedResult(resultObj, message.Id, message.CallbackName);
            }
        }

        private async Task<object> ExecuteAsync(ObjectMethodExecutor executor, object @class)
        {
            if (executor.IsMethodAsync)
            {
                return await executor.ExecuteAsync(@class);
            }

            return executor.Execute(@class);
        }

        private async Task<object> ExecuteWithParameterAsync(ObjectMethodExecutor executor,
            object @class, string parameterString)
        {
            var firstParameter = executor.MethodParameters[0];
            try
            {
                var binder = _modelBinderFactory.CreateBinder(firstParameter);
                var bindResult = await binder.BindModelAsync(parameterString);
                if (bindResult.IsSuccess)
                {
                    if (executor.IsMethodAsync)
                    {
                        return await executor.ExecuteAsync(@class, bindResult.Model);
                    }

                    return executor.Execute(@class, bindResult.Model);
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