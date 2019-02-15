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
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;
        private readonly AzureServiceBusOptions _asbOptions;

        public AzureServiceBusPublishMessageSender(
            ILogger<AzureServiceBusPublishMessageSender> logger,
            CapOptions options,
            AzureServiceBusOptions asbOptions,
            IStateChanger stateChanger,
            IStorageConnection connection,
            IConnectionPool connectionPool)
            : base(logger, options, connection, stateChanger)
        {
            _logger = logger;
            _asbOptions = asbOptions;
            _connectionPool = connectionPool;
            ServersAddress = _connectionPool.ConnectionString;
        }

        public override async Task<OperateResult> PublishAsync(string keyName, string content)
        {
            var connection = _connectionPool.Rent();

            try
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var topicClient = new TopicClient(connection, _asbOptions.TopicPath, RetryPolicy.NoRetry);

                var message = new Message
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Body = contentBytes,
                    Label = keyName,
                };

                await topicClient.SendAsync(message);

                _logger.LogDebug($"kafka topic message [{keyName}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
            finally
            {
                var returned = _connectionPool.Return(connection);
                if (!returned)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}