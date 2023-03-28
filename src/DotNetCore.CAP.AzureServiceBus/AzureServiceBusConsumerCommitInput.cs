// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Messaging.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus
{
    public class AzureServiceBusConsumerCommitInput
    {
        public AzureServiceBusConsumerCommitInput(ProcessMessageEventArgs processMessageEventArgs)
        {
            ProcessMessageArgs = processMessageEventArgs;
        }

        public ProcessMessageEventArgs ProcessMessageArgs { get; set; }

    }
}
