// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    internal class KafkaPublishMessageSender : BasePublishMessageSender
    {
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;

        public KafkaPublishMessageSender(
            ILogger<KafkaPublishMessageSender> logger, 
            IOptions<CapOptions> options,
            IStorageConnection connection,
            IConnectionPool connectionPool, 
            IStateChanger stateChanger)
            : base(logger, options, connection, stateChanger)
        {
            _logger = logger;
            _connectionPool = connectionPool;
        }

        protected override string ServersAddress => _connectionPool.ServersAddress;

        public override async Task<OperateResult> PublishAsync(string keyName, string content)
        {
            var producer = _connectionPool.RentProducer();

            try
            {
                var result = await producer.ProduceAsync(keyName, new Message<Null, string>()
                {
                    Value = content
                });

                if (result.Status == PersistenceStatus.Persisted || result.Status == PersistenceStatus.PossiblyPersisted)
                {
                    _logger.LogDebug($"kafka topic message [{keyName}] has been published.");

                    return OperateResult.Success;
                }

                throw new PublisherSentFailedException("kafka message persisted failed!");
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