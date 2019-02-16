// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor.States;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal class AzureServiceBusPublishMessageSender : BasePublishMessageSender
    {
        private readonly ILogger _logger;
        private readonly ITopicClient _topicClient;

        public AzureServiceBusPublishMessageSender(
            ILogger<AzureServiceBusPublishMessageSender> logger,
            CapOptions options,
            AzureServiceBusOptions asbOptions,
            IStateChanger stateChanger,
            IStorageConnection connection)
            : base(logger, options, connection, stateChanger)
        {
            _logger = logger;
            ServersAddress = asbOptions.ConnectionString;

            _topicClient = new TopicClient(
                ServersAddress, 
                asbOptions.TopicPath,
                RetryPolicy.NoRetry);
        }

        public override async Task<OperateResult> PublishAsync(string keyName, string content)
        {
            try
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);

                var message = new Message
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Body = contentBytes,
                    Label = keyName,
                };

                await _topicClient.SendAsync(message);

                _logger.LogDebug($"Azure Service Bus message [{keyName}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
        }
    }
}