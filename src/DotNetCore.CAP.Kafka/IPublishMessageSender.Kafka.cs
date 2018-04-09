// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Kafka
{
    internal class KafkaPublishMessageSender : BasePublishMessageSender
    {
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;
        private readonly string _serversAddress;

        public KafkaPublishMessageSender(
            CapOptions options, IStateChanger stateChanger, IStorageConnection connection,
            IConnectionPool connectionPool, ILogger<KafkaPublishMessageSender> logger)
            : base(logger, options, connection, stateChanger)
        {
            _logger = logger;
            _connectionPool = connectionPool;
            _serversAddress = _connectionPool.ServersAddress;
        }

        public override async Task<OperateResult> PublishAsync(string keyName, string content)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            Guid operationId = Guid.Empty;

            var producer = _connectionPool.Rent();

            try
            {
                operationId = s_diagnosticListener.WritePublishBefore(keyName, content, _serversAddress);

                var contentBytes = Encoding.UTF8.GetBytes(content);
                var message = await producer.ProduceAsync(keyName, null, contentBytes);

                if (message.Error.HasError)
                {
                    return OperateResult.Failed(new OperateError
                    {
                        Code = message.Error.Code.ToString(),
                        Description = message.Error.Reason
                    });
                }

                s_diagnosticListener.WritePublishAfter(operationId, message.Topic, content, _serversAddress, startTime, stopwatch.Elapsed);

                _logger.LogDebug($"kafka topic message [{keyName}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                s_diagnosticListener.WritePublishError(operationId, keyName, content, _serversAddress, ex, startTime, stopwatch.Elapsed);

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