﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Filter;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace DotNetCore.CAP.Internal;

public class SubscribeInvoker : ISubscribeInvoker
{
    private readonly ConcurrentDictionary<string, ObjectMethodExecutor> _executors;
    private readonly ISerializer _serializer;
    private readonly IServiceProvider _serviceProvider;

    public SubscribeInvoker(IServiceProvider serviceProvider, ISerializer serializer)
    {
        _serviceProvider = serviceProvider;
        _serializer = serializer;
        _executors = new ConcurrentDictionary<string, ObjectMethodExecutor>();
    }

    public async Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var methodInfo = context.ConsumerDescriptor.MethodInfo;
        var reflectedTypeHandle = methodInfo.ReflectedType!.TypeHandle.Value;
        var methodHandle = methodInfo.MethodHandle.Value;
        var key = $"{reflectedTypeHandle}_{methodHandle}";

        var executor = _executors.GetOrAdd(key,
            _ => ObjectMethodExecutor.Create(methodInfo, context.ConsumerDescriptor.ImplTypeInfo));

        await using var scope = _serviceProvider.CreateAsyncScope();

        var provider = scope.ServiceProvider;

        var obj = GetInstance(provider, context);

        var message = context.DeliverMessage;
        var parameterDescriptors = context.ConsumerDescriptor.Parameters;
        var executeParameters = new object?[parameterDescriptors.Count];
        for (var i = 0; i < parameterDescriptors.Count; i++)
        {
            var parameterDescriptor = parameterDescriptors[i];
            if (parameterDescriptor.IsFromCap)
            {
                executeParameters[i] = GetCapProvidedParameter(parameterDescriptor, message, cancellationToken);
            }
            else
            {
                if (message.Value != null)
                {
                    // use ISerializer when reading from storage, skip other objects if not Json
                    if (_serializer.IsJsonType(message.Value))
                    {
                        executeParameters[i] =
                            _serializer.Deserialize(message.Value, parameterDescriptor.ParameterType);
                    }
                    else
                    {
                        var converter = TypeDescriptor.GetConverter(parameterDescriptor.ParameterType);
                        if (converter.CanConvertFrom(message.Value.GetType()))
                        {
                            executeParameters[i] = converter.ConvertFrom(message.Value);
                        }
                        else
                        {
                            if (parameterDescriptor.ParameterType.IsInstanceOfType(message.Value))
                                executeParameters[i] = message.Value;
                            else
                                executeParameters[i] =
                                    Convert.ChangeType(message.Value, parameterDescriptor.ParameterType);
                        }
                    }
                }
            }
        }

        var filters = provider.GetServices<ISubscribeFilter>().ToList();
        Stack<ISubscribeFilter> executedFilters = new Stack<ISubscribeFilter>();

        object? resultObj = null;
        try
        {
            foreach (var filter in filters)
            {
                var ctx = new ExecutingContext(context, executeParameters);
                await filter.OnSubscribeExecutingAsync(ctx).ConfigureAwait(false);
                executeParameters = ctx.Arguments;
                executedFilters.Push(filter);
            }

            resultObj = await ExecuteWithParameterAsync(executor, obj, executeParameters).ConfigureAwait(false);

            while (executedFilters.Count > 0)
            {
                var filter = executedFilters.Peek();
                var ctx = new ExecutedContext(context, resultObj);
                await filter.OnSubscribeExecutedAsync(ctx).ConfigureAwait(false);
                resultObj = ctx.Result;
                executedFilters.Pop();
            }
        }
        catch (Exception ex)
        {
            if (executedFilters.Count == 0)
                throw;
            while (executedFilters.Count > 0)
            {
                var exContext = new ExceptionContext(context, ex);
                var filter = executedFilters.Pop();
                await filter.OnSubscribeExceptionAsync(exContext).ConfigureAwait(false);

                if (!exContext.ExceptionHandled) exContext.Exception.ReThrow();

                if (exContext.Result != null) resultObj = exContext.Result;
            }
        }

        var callbackName = message.GetCallbackName();
        if (string.IsNullOrEmpty(callbackName))
        {
            return new ConsumerExecutedResult(resultObj, message.GetId(), null, null);
        }
        else
        {
            var capHeader = executeParameters.FirstOrDefault(x => x is CapHeader) as CapHeader;
            return new ConsumerExecutedResult(resultObj, message.GetId(), callbackName, capHeader?.ResponseHeader);
        }
    }

    private static object GetCapProvidedParameter(ParameterDescriptor parameterDescriptor, Message message,
        CancellationToken cancellationToken)
    {
        if (typeof(CancellationToken).IsAssignableFrom(parameterDescriptor.ParameterType)) return cancellationToken;

        if (parameterDescriptor.ParameterType.IsAssignableFrom(typeof(CapHeader)))
            return new CapHeader(message.Headers);

        throw new ArgumentException(parameterDescriptor.Name);
    }

    protected virtual object GetInstance(IServiceProvider provider, ConsumerContext context)
    {
        var srvType = context.ConsumerDescriptor.ServiceTypeInfo?.AsType();
        var implType = context.ConsumerDescriptor.ImplTypeInfo.AsType();

        object? obj = null;
        if (srvType != null) obj = provider.GetServices(srvType).FirstOrDefault(o => o?.GetType() == implType);

        if (obj == null) obj = ActivatorUtilities.GetServiceOrCreateInstance(provider, implType);

        return obj;
    }

    private async Task<object?> ExecuteWithParameterAsync(ObjectMethodExecutor executor, object @class,
        object?[] parameter)
    {
        if (executor.IsMethodAsync) return await executor.ExecuteAsync(@class, parameter);

        return executor.Execute(@class, parameter);
    }
}