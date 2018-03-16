// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Kafka
{
    internal class KafkaPublishMessageSender : BasePublishMessageSender
    {
        private readonly ConnectionPool _connectionPool;
        private readonly ILogger _logger;

        public KafkaPublishMessageSender(
            CapOptions options, IStateChanger stateChanger, IStorageConnection connection,
            ConnectionPool connectionPool, ILogger logger)
            : base(logger, options, connection, stateChanger)
        {
            _logger = logger;
            _connectionPool = connectionPool;
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
                    return OperateResult.Failed(new OperateError
                    {
                        Code = message.Error.Code.ToString(),
                        Description = message.Error.Reason
                    });
                }

                _logger.LogDebug($"kafka topic message [{keyName}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"An error occurred during sending the topic message to kafka. Topic:[{keyName}], Exception: {ex.Message}");

                return OperateResult.Failed(ex);
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