// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    public class SubscribeInvoker : ISubscribeInvoker
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISerializer _serializer;
        private readonly ConcurrentDictionary<string, ObjectMethodExecutor> _executors;

        public SubscribeInvoker(ILoggerFactory loggerFactory, IServiceProvider serviceProvider, ISerializer serializer)
        {
            _serviceProvider = serviceProvider;
            _serializer = serializer;
            _logger = loggerFactory.CreateLogger<SubscribeInvoker>();
            _executors = new ConcurrentDictionary<string, ObjectMethodExecutor>();
        }

        public async Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var methodInfo = context.ConsumerDescriptor.MethodInfo;
            var reflectedType = methodInfo.ReflectedType.Name;

            _logger.LogDebug("Executing subscriber method : {0}", methodInfo.Name);

            var key = $"{methodInfo.Module.Name}_{reflectedType}_{methodInfo.MetadataToken}";
            var executor = _executors.GetOrAdd(key, x => ObjectMethodExecutor.Create(methodInfo, context.ConsumerDescriptor.ImplTypeInfo));

            using var scope = _serviceProvider.CreateScope();

            var provider = scope.ServiceProvider;

            var obj = GetInstance(provider, context);

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
                    if (message.Value != null)
                    {
                        if (_serializer.IsJsonType(message.Value))  // use ISerializer when reading from storage, skip other objects if not Json
                        {
                            executeParameters[i] = _serializer.Deserialize(message.Value, parameterDescriptors[i].ParameterType);
                        }
                        else
                        {
                            var converter = TypeDescriptor.GetConverter(parameterDescriptors[i].ParameterType);
                            if (converter.CanConvertFrom(message.Value.GetType()))
                            {
                                executeParameters[i] = converter.ConvertFrom(message.Value);
                            }
                            else
                            {
                                if (parameterDescriptors[i].ParameterType.IsInstanceOfType(message.Value))
                                {
                                    executeParameters[i] = message.Value;
                                }
                                else
                                {
                                    executeParameters[i] = Convert.ChangeType(message.Value, parameterDescriptors[i].ParameterType);
                                }
                            }
                        }
                    }
                }
            }

            var resultObj = await ExecuteWithParameterAsync(executor, obj, executeParameters);
            return new ConsumerExecutedResult(resultObj, message.GetId(), message.GetCallbackName());
        }

        protected virtual object GetInstance(IServiceProvider provider, ConsumerContext context)
        {
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

            return obj;
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