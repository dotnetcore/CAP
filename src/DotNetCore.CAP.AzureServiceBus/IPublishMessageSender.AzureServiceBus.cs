// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor.States;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal class AzureServiceBusPublishMessageSender : BasePublishMessageSender
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly ILogger _logger;
        private readonly IOptions<AzureServiceBusOptions> _asbOptions;

        private ITopicClient _topicClient;

        public AzureServiceBusPublishMessageSender(
            ILogger<AzureServiceBusPublishMessageSender> logger,
            IOptions<CapOptions> options,
            IOptions<AzureServiceBusOptions> asbOptions,
            IStateChanger stateChanger,
            IStorageConnection connection)
            : base(logger, options, connection, stateChanger)
        {
            _logger = logger;
            _asbOptions = asbOptions;
        }

        protected override string ServersAddress => _asbOptions.Value.ConnectionString;

        public override async Task<OperateResult> PublishAsync(string keyName, string content)
        {
            try
            {
                Connect();

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

        private void Connect()
        {
            if (_topicClient != null)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                if (_topicClient == null)
                {
                    _topicClient = new TopicClient(ServersAddress, _asbOptions.Value.TopicPath, RetryPolicy.NoRetry);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}