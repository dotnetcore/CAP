// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DotNetCore.CAP.Internal
{
    internal class SubscribeInvoker : ISubscribeInvoker
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public SubscribeInvoker(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<SubscribeInvoker>();
        }

        public async Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Executing subscriber method : {0}", context.ConsumerDescriptor.MethodInfo.Name);

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

                var message = context.DeliverMessage;
                var parameterDescriptors = context.ConsumerDescriptor.Parameters;
                var executeParameters = new object[parameterDescriptors.Count];
                for (var i = 0; i < parameterDescriptors.Count; i++)
                {
                    if (parameterDescriptors[i].IsFromCap)
                    {
                        executeParameters[i] = new CapHeader(message.Headers);
                    }
                    else
                    {
                        if (message.Value is JToken jToken)  //reading from storage
                        {
                            executeParameters[i] = jToken.ToObject(parameterDescriptors[i].ParameterType);
                        }
                        else
                        {
                            executeParameters[i] = message.Value;
                        }
                    }
                }

                var resultObj = await ExecuteWithParameterAsync(executor, obj, executeParameters);
                return new ConsumerExecutedResult(resultObj, message.GetId(), message.GetCallbackName());
            }
        }

        private async Task<object> ExecuteWithParameterAsync(ObjectMethodExecutor executor, object @class, object[] parameter)
        {
            if (executor.IsMethodAsync)
            {
                return await executor.ExecuteAsync(@class, parameter);
            }

            return executor.Execute(@class, parameter);
        }
    }
}