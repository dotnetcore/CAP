// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

public class ServiceBusProcessorFacade : IAsyncDisposable
{
    private readonly ServiceBusProcessor? _serviceBusProcessor;
    private readonly ServiceBusSessionProcessor? _serviceBusSessionProcessor;

    public bool IsSessionProcessor { get; }

    public bool IsProcessing => IsSessionProcessor
        ? _serviceBusSessionProcessor!.IsProcessing
        : _serviceBusProcessor!.IsProcessing;

    public bool AutoCompleteMessages => IsSessionProcessor
        ? _serviceBusSessionProcessor!.AutoCompleteMessages
        : _serviceBusProcessor!.AutoCompleteMessages;

    public ServiceBusProcessorFacade(ServiceBusProcessor? serviceBusProcessor = null,
        ServiceBusSessionProcessor? serviceBusSessionProcessor = null)
    {
        if (serviceBusProcessor is null && serviceBusSessionProcessor is null)
        {
            throw new ArgumentNullException(nameof(serviceBusProcessor),
                "Either serviceBusProcessor or serviceBusSessionProcessor must be provided");
        }

        _serviceBusProcessor = serviceBusProcessor;
        _serviceBusSessionProcessor = serviceBusSessionProcessor;

        IsSessionProcessor = _serviceBusSessionProcessor is not null;
    }

    public Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        return IsSessionProcessor
            ? _serviceBusSessionProcessor!.StartProcessingAsync(cancellationToken)
            : _serviceBusProcessor!.StartProcessingAsync(cancellationToken);
    }

    public event Func<ProcessMessageEventArgs, Task> ProcessMessageAsync
    {
        add => _serviceBusProcessor!.ProcessMessageAsync += value;

        remove => _serviceBusProcessor!.ProcessMessageAsync -= value;
    }

    public event Func<ProcessSessionMessageEventArgs, Task> ProcessSessionMessageAsync
    {
        add => _serviceBusSessionProcessor!.ProcessMessageAsync += value;

        remove => _serviceBusSessionProcessor!.ProcessMessageAsync -= value;
    }

    public event Func<ProcessErrorEventArgs, Task> ProcessErrorAsync
    {
        add
        {
            if (IsSessionProcessor)
            {
                _serviceBusSessionProcessor!.ProcessErrorAsync += value;
            }
            else
            {
                _serviceBusProcessor!.ProcessErrorAsync += value;
            }
        }

        remove
        {
            if (IsSessionProcessor)
            {
                _serviceBusSessionProcessor!.ProcessErrorAsync -= value;
            }
            else
            {
                _serviceBusProcessor!.ProcessErrorAsync -= value;
            }
        }
    }


    public async ValueTask DisposeAsync()
    {
        if (_serviceBusProcessor is not null) await _serviceBusProcessor.DisposeAsync();
        if (_serviceBusSessionProcessor is not null) await _serviceBusSessionProcessor.DisposeAsync();
    }
}