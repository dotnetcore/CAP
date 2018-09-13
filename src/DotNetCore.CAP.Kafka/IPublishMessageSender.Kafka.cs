// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Kafka
{
    internal class KafkaPublishMessageSender : BasePublishMessageSender
    {
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;

        public KafkaPublishMessageSender(
            CapOptions options, IStateChanger stateChanger, IStorageConnection connection,
            IConnectionPool connectionPool, ILogger<KafkaPublishMessageSender> logger)
            : base(logger, options, connection, stateChanger)
        {
            _logger = logger;
            _connectionPool = connectionPool;
            ServersAddress = _connectionPool.ServersAddress;
        }

        public override async Task<OperateResult> PublishAsync(string keyName, string content)
        {
            var producer = _connectionPool.Rent();

            try
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var message = await producer.ProduceAsync(keyName, null, contentBytes);

                if (message.Error.HasError)
                {
                    throw new PublisherSentFailedException(message.Error.ToString());
                }

                _logger.LogDebug($"kafka topic message [{keyName}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wapperEx);
            }
            finally
            {
                var returned = _connectionPool.Return(producer);
                if (!returned)
                {
                    producer.Dispose();
                }
            }
        }
    }
}