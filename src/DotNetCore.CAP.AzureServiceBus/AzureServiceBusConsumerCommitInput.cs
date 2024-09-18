// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

public class AzureServiceBusConsumerCommitInput
{
    public AzureServiceBusConsumerCommitInput(ProcessMessageEventArgs processMessageEventArgs)
    {
        ProcessMessageArgs = processMessageEventArgs;
    }

    public AzureServiceBusConsumerCommitInput(ProcessSessionMessageEventArgs processSessionMessageArgs)
    {
        ProcessSessionMessageArgs = processSessionMessageArgs;
    }

    private ProcessMessageEventArgs? ProcessMessageArgs { get; }
    private ProcessSessionMessageEventArgs? ProcessSessionMessageArgs { get; }

    private ServiceBusReceivedMessage Message => ProcessMessageArgs?.Message ?? ProcessSessionMessageArgs!.Message;

    public Task CompleteMessageAsync()
    {
        return ProcessMessageArgs != null
            ? ProcessMessageArgs.CompleteMessageAsync(Message)
            : ProcessSessionMessageArgs!.CompleteMessageAsync(Message);
    }

    public Task AbandonMessageAsync()
    {
        return ProcessMessageArgs != null
            ? ProcessMessageArgs.AbandonMessageAsync(Message)
            : ProcessSessionMessageArgs!.AbandonMessageAsync(Message);
    }
}