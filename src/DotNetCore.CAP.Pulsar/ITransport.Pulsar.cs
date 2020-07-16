// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Pulsar
{
    internal class PulsarTransport : ITransport
    {
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;

        public PulsarTransport(ILogger<PulsarTransport> logger, IConnectionPool connectionPool)
        {
            _logger = logger;
            _connectionPool = connectionPool;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("Pulsar", _connectionPool.ServersAddress);

        public async Task<OperateResult> SendAsync(TransportMessage message)
        {
            var producer = _connectionPool.RentProducer();

            try
            {
                // var headers = new H. Headers();

                /*foreach (var header in message.Headers)
                {
                    headers.Add(header.Value != null
                        ? new Header(header.Key, Encoding.UTF8.GetBytes(header.Value))
                        : new Header(header.Key, null));
                }*/

                var result = await producer.SendAsync(message.Body);
                /*var result = await producer.SendAsync(message.GetName(), new Message
                {
                    Headers = headers,
                    Key = message.Headers.TryGetValue(KafkaHeaders.KafkaKey, out string kafkaMessageKey) && !string.IsNullOrEmpty(kafkaMessageKey) ? kafkaMessageKey : message.GetId(),
                    Value = message.Body
                });*/

                if (result.Type != null)
                {
                    _logger.LogDebug($"pulsar topic message [{message.GetName()}] has been published.");

                    return OperateResult.Success;
                }

                throw new PublisherSentFailedException("pulsar message persisted failed!");
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
                    await producer.DisposeAsync();
                }
            }
        }
    }
}